---
description: >-
  Autonomous TDD development using CVM (junior workflow). Thin alias of
  j-cvm-exec-plan — parses the TDDAB plan and executes it via the CVM
  planexecutor, one task at a time, with full resume support. Use when the plan
  is ready and user wants hands-off autonomous development.
---
# Autopilot (alias of j-cvm-exec-plan)

This command is a thin wrapper. The autonomous execution path is **j-cvm-exec-plan** — there is exactly ONE way to run a plan via CVM.

## What to do

1. Read `.opencode/commands/j-cvm-exec-plan.md` and follow it EXACTLY, including:
   - Prerequisites (CVM MCP server check)
   - `mcp__cvm__parsePlan` on the plan file — **do NOT read the plan yourself**
   - The STRICTLY SYNCHRONOUS protocol (one CVM call per turn, all CVM calls from the main conversation)
   - Resume via `.cvm/uplan-progress.json`
2. There is no separate "autopilot program" — do NOT generate a CVM `.ts` program. The planexecutor (`@planexecutor`) is the program.

## When NOT to Use
- Exploratory work (use j-poc instead)
- Bug fixes (use j-bug instead)
- Tasks requiring user input/decisions at each step (use j-develop instead)
- When CVM MCP server is not available (use j-develop instead)
