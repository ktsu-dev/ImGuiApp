#!/bin/bash
# Installs the .NET SDK into ~/.dotnet for Claude Code cloud sessions.
#
# The official route is dotnet-install.sh, which downloads from
# builds.dotnet.microsoft.com. The default cloud network policy does not allow
# that host, but it does allow mcr.microsoft.com, so when the script cannot be
# fetched the SDK is unpacked from the layers of the official
# mcr.microsoft.com/dotnet/sdk container image instead. Both give the same
# Microsoft-built SDK; Ubuntu's own dotnet-sdk-10.0 package is the 1xx feature
# band, whose compiler is too old for ktsu.Sdk.Analyzers (CS9057).
#
# Usage: install-dotnet.sh <channel> <minimum sdk version> [runtime channel...]
#   e.g. install-dotnet.sh 10.0 10.0.400 8.0 9.0
# The extra runtime channels are for test projects that multi-target older
# frameworks: building them needs only the SDK, running them needs the runtime.
set -euo pipefail

channel="${1:-10.0}"
minimum="${2:-${channel}.100}"
shift 2 || shift $#
runtimes=("$@")
root="${DOTNET_INSTALL_DIR:-$HOME/.dotnet}"

version_at_least() {
	[ "$(printf '%s\n%s\n' "$2" "$1" | sort -V | head -n1)" = "$2" ]
}

has_runtime() {
	[ -x "$root/dotnet" ] && "$root/dotnet" --list-runtimes 2>/dev/null | grep -q "^Microsoft.NETCore.App $1\."
}

has_sdk() {
	[ -x "$root/dotnet" ] || return 1
	local sdk
	for sdk in $("$root/dotnet" --list-sdks 2>/dev/null | awk '{print $1}'); do
		case "$sdk" in
			"$channel".*) version_at_least "$sdk" "$minimum" && return 0 ;;
		esac
	done
	return 1
}

# install_with_script <channel> [dotnet-install.sh options...]
install_with_script() {
	local script
	script="$(mktemp)"
	curl -fsSL --max-time 30 -o "$script" https://builds.dotnet.microsoft.com/dotnet/scripts/v1/dotnet-install.sh 2>/dev/null || {
		rm -f "$script"
		return 1
	}
	bash "$script" --channel "$1" --install-dir "$root" --no-path "${@:2}"
	rm -f "$script"
}

# install_from_container_image <image> <tag>, e.g. sdk 10.0 or runtime 8.0
install_from_container_image() {
	local registry="https://mcr.microsoft.com/v2/dotnet/$1"
	local arch
	case "$(uname -m)" in
		x86_64) arch=amd64 ;;
		aarch64 | arm64) arch=arm64 ;;
		*) echo "install-dotnet: unsupported architecture $(uname -m)" >&2; return 1 ;;
	esac

	local index manifest digest layer work
	index="$(curl -fsSL -H 'Accept: application/vnd.oci.image.index.v1+json,application/vnd.docker.distribution.manifest.list.v2+json' \
		"$registry/manifests/$2-noble")"
	digest="$(jq -r --arg arch "$arch" \
		'.manifests[] | select(.platform.os == "linux" and .platform.architecture == $arch and (.platform.variant // "") == "") | .digest' \
		<<<"$index" | head -n1)"
	manifest="$(curl -fsSL -H 'Accept: application/vnd.oci.image.manifest.v1+json,application/vnd.docker.distribution.manifest.v2+json' \
		"$registry/manifests/$digest")"

	# A runtime image contributes its shared framework and nothing else, so its
	# older dotnet host never replaces the one the SDK brought.
	local wanted='usr/share/dotnet/*'
	[ "$1" = sdk ] || wanted='usr/share/dotnet/shared/*'

	work="$(mktemp -d)"
	mkdir -p "$root"
	# Only the files under usr/share/dotnet are taken: the runtime, ASP.NET Core
	# and SDK layers. The rest of the image is its operating system. Layers are
	# extracted over what is there, so an SDK and older runtimes share one root.
	for layer in $(jq -r '.layers[].digest' <<<"$manifest"); do
		curl -fsSL -o "$work/layer.tgz" "$registry/blobs/$layer"
		tar -xzf "$work/layer.tgz" -C "$root" --strip-components=3 --wildcards "$wanted" 2>/dev/null || true
	done
	rm -rf "$work"
}

if ! has_sdk; then
	echo "install-dotnet: installing the .NET $channel SDK (at least $minimum) into $root" >&2
	install_with_script "$channel" || {
		echo "install-dotnet: builds.dotnet.microsoft.com is not reachable; using the mcr.microsoft.com/dotnet/sdk image" >&2
		install_from_container_image sdk "$channel"
	}
	if ! has_sdk; then
		echo "install-dotnet: no .NET $channel SDK at or above $minimum after installing" >&2
		exit 1
	fi
fi

for runtime in "${runtimes[@]}"; do
	has_runtime "$runtime" && continue
	echo "install-dotnet: installing the .NET $runtime runtime into $root" >&2
	install_with_script "$runtime" --runtime dotnet || install_from_container_image runtime "$runtime"
	if ! has_runtime "$runtime"; then
		echo "install-dotnet: no .NET $runtime runtime after installing" >&2
		exit 1
	fi
done
