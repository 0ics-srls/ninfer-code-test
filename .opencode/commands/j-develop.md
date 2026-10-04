---
description: >-
  Start development after analysis (junior workflow). Writes code following the
  plan from j-new-feature. Supports TDDAB, TDD, or manual methodology. Use when
  the plan is ready and user says develop, inizia a codare, zacni programovat,
  or let's code.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs
- **Load language mindset** based on `@language` in j-settings.md:
  - `csharp` → based on `@csharp-mindset`: `net10` → `mind-sets/csharp-senior_10.md`, `legacy` → `mind-sets/csharp-senior.md`, missing → default to `net10`
  - `typescript` → `mind-sets/typescript-senior.md`
  - `java` → `mind-sets/java-senior.md`
  - `kotlin` → `mind-sets/kmp-senior.md`
  - `python` → `mind-sets/python-senior.md`
  - `go` → `mind-sets/go-senior.md`
  - `rust` → `mind-sets/rust-senior.md`
  - `cpp` → `mind-sets/cpp-senior.md`
  - `php` → `mind-sets/php-senior.md`
  - `swift` → `mind-sets/swift-senior.md`
  - `dart` → `mind-sets/dart-senior.md`
  - `perl` → `mind-sets/perl-senior.md`
  - other → skip (junior.md is sufficient)
  - **If the mindset file does not exist** (e.g. lite version without senior mindsets) → continue with junior.md + `mind-sets/project-foundations-{language}.md` (if present). Do NOT stop.
- **Load cross-language rules**: Read `mind-sets/common-coding-style.md` if not read this session (immutability, KISS/DRY/YAGNI, file limits — applies to every language)
- If `sprint-status.yaml` exists in the task folder → follow `mind-sets/common-sprint-tracker.md` for task state transitions
- **Code navigation** — read `.opencode/commands/mind-sets/code-navigation.md` and follow it for all symbol/usage/existence lookups (tool from `@code-nav-local`/`@code-nav-external`; grep is the last-resort fallback when the tool is absent/not-ready, flagged). When spawning agents, pass the same instruction.

## Pre-check
This command is used AFTER:
- User explained what's needed (LISTEN)
- Claude analyzed the code (ANALYZE)
- Claude proposed solution and user approved (PROPOSE)
- Plan created and approved (PLAN)

**State Validation (use intelligence):**
Read MB → check `@state`
- If `@state` is PLAN or DEVELOP → OK, proceed
- If `@state` is LISTEN, ANALYZE, or PROPOSE:
  - **But first, check reality:** Does plan.md exist in task folder? Are there commits suggesting progress?
  - If reality shows progress beyond MB state → ask user if MB needs updating
  - If reality matches MB → then:
```
The plan is not ready yet. Complete the previous phases first.
Current state: {@state}
Use j-continue to resume from the right point.
```
STOP.

## Steps

### 1. Load Plan
Read MB → get `@feature` → folder is `{@tasks}/{feature}/`
Look for `{@tasks}/{feature}/plan.md`

**If plan.md does NOT exist — check the bug workflow first (j-bug handoff):**
- Read MB → if `@bug` is set, the folder is `{@tasks}/{bug}/` and the plan lives inside `bug-notes.md` (created by j-bug step 9). Use that as the plan and continue.
- Also accept `{@tasks}/{feature}/bug-notes.md` if present.

**If neither plan.md nor a bug-notes.md plan exists:**
```
No plan found. The plan should be created during j-new-feature (or j-bug for bugfixes).
Use j-new-feature to create the plan first, or j-continue to resume.
```
STOP.

**If plan exists → check for multi-file format:** If the plan contains a `<files>` tag, it's an index.md — read `<mission>` from it, then read each listed sub-file for `<block>` tags. If no `<files>` tag, it's a single-file plan.

**Read the full plan (all files if multi-file).** Understand ALL steps before starting.

### 2. Determine Scope & Start
**Update MB:** `@state::DEVELOP`
**Update notes.md:** mark "Plan created" done

Determine from the plan:
- [ ] Backend only
- [ ] Frontend only
- [ ] Both

Tell the user:
```
Plan loaded! I'll implement it step by step.

Steps to do: [number of steps from plan]

I'll tell you after each step what I did. If something goes wrong,
I'll stop and ask you — you don't need to worry about anything.

Starting now...
```

### 3. Execute Plan Step by Step

**Read `@backend-method` and `@frontend-method` from j-settings.md.**

For EACH step in the plan, in order:

**If Backend (TDDAB):**
1. Read TDDAB reference file (see `@tddab-file` in j-settings.md)
2. Read language overlay (see `@tddab-lang-overlay` in j-settings.md, if present)
3. For each atomic block: RED → GREEN → VERIFY → COMMIT
4. Run tests: `{@test-backend}`

**If Backend (TDD):**
1. Write test first, then implementation
2. Run tests: `{@test-backend}`

**If Backend (Manual):**
1. Implement changes
2. Test manually

**If Frontend (ChromeDevTools):**
1. Make incremental changes
2. After each change → test with ChromeDevTools
3. Ask user for visual confirmation

**If Frontend (Automated):**
1. Write/update tests
2. Run: `{@test-frontend}`

**After EACH step:**
- Run tests to verify (don't assume — actually run them)
- If tests fail → fix before moving on
- Commit checkpoint: `git add . && git commit -m "feat({feature}): step N — {description}"`
- Update MB with progress
- Never overwrite completed steps — if a done step needs changes, add a new step

**If unexpected problem → stop and discuss with user.**

### 4. Report Completion
When ALL steps are done:
- **Update MB:** `@state::TEST`
- **Update plan.md** — mark all items `[x]`
- **Update notes.md** — mark "Development done"
- Run full test suite one final time
- Verify no warnings in build output

Tell the user:
```
Done!

What was done:
- [list of changes, in plain language]

Tests: [X passing, 0 failing]

Ready? Use j-close to merge and deploy.
```

## Internal Quality Checks (Claude follows silently — do NOT show to user)

Before marking ANY plan step as done, verify internally:
- Tests exist for this step's functionality
- Tests pass (run them)
- Implementation matches the plan (not more, not less)
- Full test suite still passes (no regressions)

Before reporting completion (step 4), verify internally:
- ALL plan steps pass quality checks above
- Full test suite passes with zero failures
- No warnings in build output
- Nothing missing from the plan, nothing extra added

If any check fails → fix it silently. Only tell user if you need their input.

## Rules
- Follow methodology from j-settings.md
- Update MB often during development
- If something unclear → stop and ask
- Never modify files not discussed in plan
