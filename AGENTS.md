# AGENTS.md — opencode project guide

This file provides guidance to opencode when working with code in this repository.

# CODING & INTERACTION NOTES

If you want to ask questions for more spec or other info always use AskUserQuestion tool.

## Memory Bank - Critical System

The Memory Bank is opencode's ONLY connection to the project between sessions. Without it, the agent starts completely fresh with zero knowledge of the project.

### How Memory Bank Works

1. **User triggers**: Type `mb`, `update memory bank`, or `check memory bank`
2. **The agent's process**:
   - Reads `memory-bank/README.md` and follows its instructions

### Important Rules

- Follow instructions in `memory-bank/README.md` - it defines what to read and when
- Memory Bank is the single source of truth - overrides any other documentation

## Project Documentation

Each project has its own README.md with:
- Purpose and architecture
- Build, test, and run commands  
- API usage examples
- Configuration details

Always check the project's README for localized information before working on it.

## Summary of Key Commands

- `mb` or `update memory bank` - Trigger Memory Bank update
- `mcp` - Check MCP server status

## When Spawning Agents (Task tool)

If MCP tools are available (e.g., `mcp__vs-mcp__*`), ALWAYS include in the agent prompt:
> "Use mcp__vs-mcp__* tools instead of Grep/Glob/LS for symbol search and project exploration.
> Use FindSymbols, GetSolutionTree, GetDocumentOutline, FindSymbolUsages instead of file system tools."

This ensures sub-agents also benefit from token-efficient MCP tools.

## Code Navigation

Code intelligence is provided by MCP servers. Prefer these over grep/glob for symbol search and navigation.

- **Local project navigation:** `lsai` — compiler-grade symbol search across C#, TypeScript, etc. Use LSAI tools (FindSymbols, outline, call graph, usages) for navigating THIS project's own code.
- **Third-party library exploration:** `xmp4` — SCIP-backed intelligence for external/OSS libraries. Use when you need to understand a dependency's API (e.g. how `@primeng/*`, Angular, or EF Core APIs are actually used).

When spawning agents, instruct them to use these MCP tools instead of file-system searches.

## Memory Bank — stato reale (AUTO-LOADED)
@memory-bank/README.md
@memory-bank/productContext.md
@memory-bank/systemPatterns.md
@memory-bank/techContext.md
@memory-bank/activeContext.md
@memory-bank/progress.md
MB was READEN!
