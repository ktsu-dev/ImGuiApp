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
CIMGUI_REF="${CIMGUI_REF:-docking_inter}"
IMGUI_TAG="${IMGUI_TAG:-v1.92.2b}"
OUT_DIR="${1:-ImGui.App/Platform/iOS/native}"
OUT_LIB="$OUT_DIR/libcimgui-sim.a"

if [ -f "$OUT_LIB" ]; then
	echo "cimgui static lib already present: $OUT_LIB; skipping build (cache hit)."
	exit 0
fi

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

echo "Cloning cimgui ($CIMGUI_REF) ..."
git clone --branch "$CIMGUI_REF" --recursive "$CIMGUI_REPO" "$WORK/cimgui"
cd "$WORK/cimgui"

# Pin the bundled Dear ImGui to the exact version Hexa 2.2.9 was generated against. Best-effort:
# if the tag can't be fetched, fall back to cimgui's submodule pointer and rely on the runtime
# version assertion to flag a mismatch.
if ( cd imgui && git fetch --depth 1 origin tag "$IMGUI_TAG" 2>/dev/null && git checkout -q "$IMGUI_TAG" 2>/dev/null ); then
	echo "Pinned Dear ImGui to $IMGUI_TAG."
else
	echo "WARN: could not pin Dear ImGui to $IMGUI_TAG; using cimgui's submodule pointer."
fi
echo "Building cimgui against: $(grep -E '#define IMGUI_VERSION ' imgui/imgui.h)"

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
