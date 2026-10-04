#!/bin/bash
# setup/check-agent-tools.sh [model]
#
# Asks a model to actually CALL every MCP tool the plan relies on, so you know the toolchain works end to end before a
# 2-hour run. Default model: one of OpenCode's free models (no API key needed); pass your own, e.g. local/qwen3.8-27b-ninfer.
# Expected: five lines, all OK. A small free model may stumble on reading the xmp4/primeng answers: what matters is that
# every tool call succeeds (no "failed" lines above the answer).
export PATH="$HOME/.dotnet:$HOME/.dotnet/tools:$HOME/.opencode/bin:$PATH" DOTNET_ROOT="$HOME/.dotnet"
cd "$(dirname "$0")/.."
MODEL="${1:-opencode/big-pickle}"
echo "== MCP servers"
opencode mcp list 2>&1 | sed 's/\x1b\[[0-9;]*m//g' | grep -E "✓|✗"
echo "== tool calls ($MODEL)"
opencode run -m "$MODEL" "This is a tool check. Use ONLY the MCP tools named below, do not read or edit files, and answer with exactly five lines, one per check, each starting with OK or FAIL:
1. lsai: call lsai_server (again if a workspace is still Loading, up to 3 times) and report each workspace with language and status. OK only if both C# and TypeScript are Ready.
2. cvm: call parsePlan with filePath tasks/07-required-due-date/plan.md and report how many blocks it found (expected 6).
3. xmp4: call xmp4_projects with query angular, then xmp4_search in the first Angular project with query signal, and report the first result line.
4. primeng: call get_component for the DatePicker component and report one line of what it returned.
5. chrome-devtools: new_page with url https://example.com, take_snapshot, and report the page heading." 2>&1 \
  | sed 's/\x1b\[[0-9;]*m//g' | grep -vE "^\s*$" | tail -30
