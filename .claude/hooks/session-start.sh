#!/bin/bash
# SessionStart hook for Claude Code cloud sessions: installs the .NET SDK this
# repository's global.json asks for and restores its packages, so a session
# can build and test straight away.
set -euo pipefail

if [ "${CLAUDE_CODE_REMOTE:-}" != "true" ]; then
	exit 0
fi

cd "$CLAUDE_PROJECT_DIR"

minimum=10.0.100
if [ -f global.json ]; then
	minimum="$(jq -r '.sdk.version // "10.0.100"' global.json)"
fi
# The test projects multi-target, so the older runtimes are needed to run them.
bash .claude/hooks/install-dotnet.sh "${minimum%.*}" "$minimum" 8.0 9.0

export DOTNET_ROOT="$HOME/.dotnet"
export PATH="$DOTNET_ROOT:$DOTNET_ROOT/tools:$PATH"
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_NOLOGO=1
if [ -n "${CLAUDE_ENV_FILE:-}" ]; then
	{
		echo "export DOTNET_ROOT=\"$DOTNET_ROOT\""
		echo "export PATH=\"$DOTNET_ROOT:$DOTNET_ROOT/tools:\$PATH\""
		echo 'export DOTNET_CLI_TELEMETRY_OPTOUT=1'
		echo 'export DOTNET_NOLOGO=1'
	} >>"$CLAUDE_ENV_FILE"
fi

if [ -f .config/dotnet-tools.json ]; then
	dotnet tool restore
fi
dotnet restore
