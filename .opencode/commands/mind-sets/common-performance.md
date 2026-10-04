---
description: >-
  Cross-language performance rules — model selection, context window management,
  agent delegation
---
# Common Performance Rules — AI-Assisted Development

These rules optimize how Claude Code is used across all languages.

## Model Selection Strategy

Choose the right model for the task:

| Task | Model | Why |
|------|-------|-----|
| Quick edits, formatting, simple fixes | **Haiku** | 90% of Sonnet capability, 3x cost savings |
| Main development work, features, refactoring | **Sonnet** | Best coding performance/cost balance |
| Complex architecture, deep research, hard bugs | **Opus** | Deepest reasoning, handles ambiguity |
| Planning, reviewing, security analysis | **Opus** | Needs broad context understanding |

**Default**: Sonnet for development agents, Opus for planning/review agents.

## Context Window Management

- **Avoid the last 20% of context** for: large-scale refactoring, features spanning multiple files, debugging complex interactions
- **Lower sensitivity for**: single-file edits, utility creation, documentation, simple bug fixes
- **When context is large**: delegate to subagents (they get fresh context)
- **Compress outputs**: build/test agents should return summaries, not raw logs
- Use `/compact` or context-efficient agents when approaching limits

## Build & Error Troubleshooting

When a build fails:
1. **Delegate to a build-resolver agent** — they have focused context
2. Let the agent: analyze errors → categorize → fix incrementally → verify
3. Don't debug build errors in the main conversation — it wastes context
4. Same for test failures — delegate to test agents

## Agent Delegation Rules

Use specialized agents for:
- **Build errors** — language-specific build agents know error patterns
- **Code review** — reviewer agents have security checklists
- **Testing** — test agents compress output and report actionable results
- **Research** — explore agents search broadly without polluting main context

Don't use agents for:
- Simple single-file edits
- Reading a specific file you know the path of
- Quick grep for a known symbol

## Architect/Editor Split

For complex features, separate PLANNING from IMPLEMENTATION:

**Architect phase** (Opus — deep reasoning):
- Reads codebase, understands requirements
- Produces a PLAN in natural language: "Modify file X, add method Y, update test Z"
- Does NOT write code — only describes what to change and why
- Human reviews plan before implementation starts

**Editor phase** (Sonnet — fast execution):
- Receives architect's plan as input
- Implements EXACTLY what was planned — no deviation
- Focuses on surgical code changes, not reasoning
- Faster and cheaper than using Opus for implementation

**When to use**: Features touching 3+ files, architectural changes, unfamiliar code areas.
**When to skip**: Single-file edits, simple bug fixes, documentation updates.

## Token Efficiency

- Prefer `Edit` over `Write` for modifications (sends only the diff)
- Use `Glob` for file discovery, not `find` via Bash
- Use `Grep` for content search, not `grep` via Bash
- Compress agent output: binary status (PASS/FAIL) + actionable list only
- Don't read files you don't need — be surgical
