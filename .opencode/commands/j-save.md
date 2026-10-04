---
description: >-
  Save and leave (junior workflow). Commits WIP, updates Memory Bank, pushes to
  remote. Use when the user says save, pause, stop, leave, I'm done for now, or
  wants to preserve progress before ending the session.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs

## Steps

### 1. Commit Work
```bash
git status
git branch --show-current
```

**If nothing to commit** (working tree clean) → skip the commit, go to step 2 (still update MB and push).

**If on `{@main-branch}` (or `{@dev-branch}`) with uncommitted changes** → do NOT commit WIP directly on a shared branch. Ask user:
```
You have uncommitted changes on {branch}. WIP commits should not go on shared branches.
1. Create a branch for them (wip/save-{date}) and commit there
2. Stash them (git stash) — they stay local
3. Commit anyway (only if you are sure)
```

**Review what will be committed:**
- No sensitive files (.env, credentials, API keys)?
- No large binary files accidentally staged?
- Changes make sense for current work?

If something unexpected → ask user before proceeding.

```bash
git add .
git commit -m "WIP: [brief description of where we are]"
```

### 2. Update Memory Bank
Update `memory-bank/activeContext.md` with:
- Current state (LISTEN, ANALYZE, PROPOSE, PLAN, DEVELOP, TEST)
- What we were doing
- What's left to do
- Any open issues

### 3. Push
```bash
git push origin [current-branch]
```

### 4. Confirm
```
Everything saved!

Branch: [name]
Commit: [message]
Pushed: yes

WHERE WE WERE:
[brief summary]

NEXT TIME:
Use j-continue to resume from here.

Bye!
```

## Rules
- Always push (so it's saved on remote too)
- Always update MB (so Claude remembers)
- Give clear summary
