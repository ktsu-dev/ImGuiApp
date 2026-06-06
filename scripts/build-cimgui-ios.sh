#!/usr/bin/env bash
# Copyright (c) ktsu.dev
# All rights reserved.
# Licensed under the MIT license.
#
# Builds cimgui (the C API over Dear ImGui that Hexa.NET.ImGui binds to) as a static library for the
# iOS simulator (arm64). Hexa.NET.ImGui ships native cimgui for desktop + Android but NOT iOS, so to
# run Dear ImGui on iOS we statically link cimgui into the app and resolve its symbols through
# HexaGen's function table (see ImGuiApp.iOS EnsureNativeLibraryResolver). The managed library
# references the produced .a via a NativeReference (ImGui.App.csproj).
#
# Version pin: Hexa.NET.ImGui 2.2.9 is generated against Dear ImGui 1.92.2b (docking). We build
# cimgui's docking branch with its bundled Dear ImGui pinned to that exact tag so the native struct
# layouts and exported symbols match the managed bindings. The smoke test asserts the version at
# runtime (IMGUIAPP_IOS_IMGUI_VERSION) to catch any drift.
#
# Scope: simulator-arm64 only (all the CI smoke test needs). On-device arm64 slices and an
# xcframework are a follow-up for shipping to real hardware.
set -euo pipefail

CIMGUI_REPO="${CIMGUI_REPO:-https://github.com/cimgui/cimgui.git}"
CIMGUI_BRANCH="${CIMGUI_BRANCH:-docking_inter}"
# Hexa.NET.ImGui 2.2.9 binds Dear ImGui 1.92.2b, but cimgui/cimgui skipped 1.92.2 (it went 1.92.1 ->
# 1.92.3). We pin the 1.92.3 docking commit — a clean, internally-consistent release adjacent to
# 1.92.2b; the structs this port touches (ImGuiIO / ImDrawData / ImDrawVert / ImFontAtlas /
# ImTextureData) are stable across these patches, and the smoke run verifies it at runtime. Override
# CIMGUI_COMMIT=d61baef... + EXPECTED_IMGUI=1.92.1 to fall back to 1.92.1 if the ABI ever disagrees.
CIMGUI_COMMIT="${CIMGUI_COMMIT:-207fca2d361179c349f3c9d1893b8274f4bbfebf}"
EXPECTED_IMGUI="${EXPECTED_IMGUI:-1.92.3}"
OUT_DIR="${1:-ImGui.App/Platform/iOS/native}"
OUT_LIB="$OUT_DIR/libcimgui-sim.a"

if [ -f "$OUT_LIB" ]; then
	echo "cimgui static lib already present: $OUT_LIB; skipping build (cache hit)."
	exit 0
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

echo "Cloning cimgui ($CIMGUI_BRANCH @ $CIMGUI_COMMIT) ..."
# Clone the branch (full history so the pinned commit is reachable), check it out, THEN init the
# imgui submodule — so imgui lands on the commit's pointer, not the branch HEAD's. cimgui's generated
# cimgui.h must match its OWN bundled Dear ImGui (it references docking/viewport types that only exist
# in that submodule), so the commit + its submodule are built as a consistent pair.
git clone --branch "$CIMGUI_BRANCH" "$CIMGUI_REPO" "$WORK/cimgui"
cd "$WORK/cimgui"
git checkout -q "$CIMGUI_COMMIT"
git submodule update --init --recursive

# Assert the bundled Dear ImGui version is the one we expect, so an upstream cimgui change can never
# silently swap the ABI out from under the managed Hexa.NET.ImGui bindings.
BUILT_VER="$(sed -nE 's/^#define IMGUI_VERSION[[:space:]]+"([^"]+)".*/\1/p' imgui/imgui.h)"
echo "cimgui @ $CIMGUI_COMMIT bundles Dear ImGui $BUILT_VER"
case "$BUILT_VER" in
	"$EXPECTED_IMGUI"*) : ;;
	*)
		echo "ERROR: cimgui @ $CIMGUI_COMMIT bundles Dear ImGui $BUILT_VER, expected $EXPECTED_IMGUI." >&2
		echo "       Pin CIMGUI_COMMIT to a cimgui commit whose imgui submodule is $EXPECTED_IMGUI (docking)." >&2
		exit 1
		;;
esac

SDK=iphonesimulator
SYSROOT="$(xcrun --sdk "$SDK" --show-sdk-path)"
ARCH=arm64
MIN="-mios-simulator-version-min=15.0"

# cimgui.cpp wraps Dear ImGui; the imgui_*.cpp translation units provide the implementation. No
# backends, no freetype (default stb_truetype builder) — matches cimgui's default ABI.
SRCS=(cimgui.cpp imgui/imgui.cpp imgui/imgui_demo.cpp imgui/imgui_draw.cpp imgui/imgui_tables.cpp imgui/imgui_widgets.cpp)

OBJS=()
for s in "${SRCS[@]}"; do
	o="$WORK/$(echo "$s" | tr '/.' '__').o"
	echo "  CXX $s"
	xcrun --sdk "$SDK" clang++ -O2 -std=c++17 -arch "$ARCH" -isysroot "$SYSROOT" "$MIN" \
		-I. -Iimgui -c "$s" -o "$o"
	OBJS+=("$o")
done

mkdir -p "$OUT_DIR"
xcrun --sdk "$SDK" libtool -static -o "$OUT_LIB" "${OBJS[@]}"
echo "Built $OUT_LIB"

# Sanity: confirm the key C exports are present (Mach-O prefixes symbols with an underscore).
echo "--- key exported symbols ---"
xcrun --sdk "$SDK" nm -gU "$OUT_LIB" | grep -E ' _(igGetVersion|igNewFrame|igRender|igGetDrawData)$' || {
	echo "ERROR: expected cimgui C exports missing from $OUT_LIB" >&2
	exit 1
}
