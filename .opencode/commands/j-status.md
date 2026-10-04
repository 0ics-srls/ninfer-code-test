---
description: >-
  Show current status (junior workflow). Shows branch, state, progress, and
  pending work. Use when the user asks where are we, what's the status, cosa
  manca, co sme robili, or wants a summary of current work.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs

## Steps

### 1. Gather Info
```bash
git branch --show-current
git status
```

### 2. Read from MB
- `activeContext.md` - current state, feature, branch
- `progress.md` - what's done

### 3. Read from j-settings.md
- Project name and type
- Key settings

### 3b. Verify MB matches reality
Quick sanity check:
- Does current branch match `@branch` in MB?
- Does task folder exist if MB says feature in progress?

If mismatch → note it in output with ⚠️

### 4. Show Output
```
=== CURRENT STATUS ===

Project: {@project-name} ({@project-type})
Branch: [branch name]
Uncommitted changes: [yes/no]

CURRENT FEATURE:
[feature name or "none"]

STATE:
[LISTEN/ANALYZE/PROPOSE/PLAN/DEVELOP/TEST or IDLE]

[If mismatch found:]
⚠️ MB says branch X but you're on Y - run j-continue to sync

DONE:
- [list of completed items]

TODO:
- [list of remaining items]

LAST MB UPDATE:
[when was MB last updated]

=== AVAILABLE COMMANDS ===
j-continue  → continue working
j-save      → save and leave
j-close     → close feature and deploy
j-setup     → change project settings
j-help      → show all commands
```

## Rules
- This command is read-only, does not modify anything
- If MB is empty/outdated, suggest updating it
- If j-settings.md is missing, suggest running j-setup
