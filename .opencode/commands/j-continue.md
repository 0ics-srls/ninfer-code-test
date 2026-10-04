---
description: >-
  Continue existing feature (junior workflow). Resumes work from where you left
  off — reads MB state, validates reality, picks up the thread. Use when the
  user says continue, riprendi, pokracuj, or starts a new session on existing
  work.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs

## Steps

### 1. Check Current Branch
```bash
git branch --show-current
```
- If not on feature/* or bugfix/* branch → ask which feature to continue

### 2. Read State from MB
From `activeContext.md` get:
- `@state` - current state (LISTEN, ANALYZE, PROPOSE, PLAN, DEVELOP, TEST)
- `@feature` OR `@bug` - task name (whichever exists)
- `@branch` - branch name

### 2b. Validate State (use intelligence)
If `@state` is empty or invalid:
- **Check reality first:** Is there a feature branch? Task folder with notes? Recent commits?
- If reality shows work in progress → propose updating MB to match reality
- If truly nothing exists → then:
```
Invalid state in Memory Bank.
Use j-status to see what's there, or j-new-feature to start fresh.
```
STOP.

If task folder doesn't exist in `{@tasks}/`:
- **Check branch name** - maybe folder has different name?
- If truly missing → then:
```
Task folder not found. Something went wrong.
Ask a senior to check.
```
STOP.

### 3. Read Task Folder
- If `@feature` exists → folder is `{@tasks}/{@feature}/`
- If `@bug` exists → folder is `{@tasks}/{@bug}/`
- Read notes.md (or bug-notes.md) and other files
- Read plan.md if exists

### 4. Show Status
Say:
```
Here's where we were:

Feature: [name]
Branch: [current branch]
State: [state]

DONE:
[summary from MB - what's completed]

NEXT:
[what we need to do now]

Want to continue from here or do you have updates?
```

### 5. Resume
- If user has updates → listen
- If confirmed → resume from correct state

## Rules
- Always read j-settings.md + MB before anything
- Never assume - if unclear, ask
- Remind user to do `j-save` before leaving
