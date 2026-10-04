#!/bin/bash
# setup/install-ubuntu.sh — installs everything the benchmark needs on Ubuntu 24.04 (also WSL2), then checks it.
# Safe to re-run: every step skips what is already there. Run it as your normal user (it uses sudo for apt).
#
#   .NET 10 SDK (~/.dotnet)   the app, and the runtime LSAI runs on
#   Node.js 24                the Angular frontend, and the npx-based MCP servers (cvm, chrome-devtools, primeng)
#   Google Chrome             chrome-devtools MCP drives it for the DOM checks of the plan
#   Playwright chromium       e2e tests
#   sqlite3                   the plan inspects the dev database
#   OpenCode                  the coding agent
#   LSAI (~/.lsai)            semantic code navigation of this repository (C# + TypeScript) for the agent
#   xmp4                      nothing to install: a free remote MCP endpoint, configured in opencode.jsonc
set -euo pipefail
SUDO=""; [ "$(id -u)" -ne 0 ] && SUDO=sudo
step() { printf '\n== %s\n' "$*"; }
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.opencode/bin:$PATH"

step "base packages"
$SUDO apt-get update -qq
$SUDO apt-get install -y -qq git curl wget ca-certificates gnupg unzip sqlite3 python3 libicu-dev >/dev/null

step ".NET 10 SDK"
if ! dotnet --list-sdks 2>/dev/null | grep -q '^10\.'; then
  curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0 --install-dir "$HOME/.dotnet" >/dev/null
fi
grep -q '.dotnet' "$HOME/.bashrc" 2>/dev/null || \
  echo 'export DOTNET_ROOT="$HOME/.dotnet"; export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$PATH"' >> "$HOME/.bashrc"
export DOTNET_ROOT="$HOME/.dotnet"
dotnet --version

step "Node.js 24"
if ! node --version 2>/dev/null | grep -q '^v2[4-9]'; then
  curl -fsSL https://deb.nodesource.com/setup_24.x | ${SUDO:+$SUDO -E} bash - >/dev/null
  $SUDO apt-get install -y -qq nodejs >/dev/null
fi
node --version

step "Google Chrome (for the chrome-devtools MCP)"
if ! command -v google-chrome >/dev/null; then
  wget -q -O /tmp/chrome.deb https://dl.google.com/linux/direct/google-chrome-stable_current_amd64.deb
  $SUDO apt-get install -y -qq /tmp/chrome.deb >/dev/null && rm -f /tmp/chrome.deb
fi
google-chrome --version

step "OpenCode"
if ! command -v opencode >/dev/null; then
  curl -fsSL https://opencode.ai/install | bash >/dev/null
fi
opencode --version

step "LSAI (needs dotnet on PATH)"
if [ ! -x "$HOME/.lsai/run" ]; then
  curl -fsSL https://github.com/0ics-srls/Zerox.Lsai.Public/releases/latest/download/install.sh | bash
fi
"$HOME/.lsai/run" --version
# LSAI's TypeScript server: typescript-language-server 6 cannot use TypeScript 7 ("Could not find a valid TypeScript
# installation" -> the TypeScript workspace stays in Error). Keep TypeScript 5 next to it (the Angular 21 app uses 5.9).
TSLS="$HOME/.lsai/servers/typescript-language-server"
if [ -d "$TSLS" ] && ! node -e "process.exit(require('$TSLS/node_modules/typescript/package.json').version.startsWith('5.') ? 0 : 1)" 2>/dev/null; then
  npm install --prefix "$TSLS" --silent typescript@5
fi

[ -n "${TOOLCHAIN_ONLY:-}" ] && { echo; echo "toolchain installed (TOOLCHAIN_ONLY)"; exit 0; }

step "project dependencies"
cd "$(dirname "$0")/.."
npm ci --prefix web --silent
( cd web && npx playwright install --with-deps chromium >/dev/null )
dotnet build -nologo -v q

step "baseline (expected: Core 33, Server 69, frontend 23 in 5 files)"
dotnet exec tests/MyApp.Core.Tests/bin/Debug/net10.0/MyApp.Core.Tests.dll | grep -E "total:|failed:" | tr -s ' '
dotnet exec tests/MyApp.Server.Tests/bin/Debug/net10.0/MyApp.Server.Tests.dll | grep -E "total:|failed:" | tr -s ' '
npm test --prefix web -- --watch=false 2>&1 | grep -E "Test Files|Tests " | tail -2

step "MCP servers (expected: cvm, lsai, xmp4, chrome-devtools, primeng connected)"
opencode mcp list 2>&1 | sed 's/\x1b\[[0-9;]*m//g' | grep -E "✓|✗" || true

echo
echo "done. Open a new shell (PATH), then: export OPENCODE_EXPERIMENTAL_OUTPUT_TOKEN_MAX=65536 && opencode"
