---
description: >-
  Review plan for TDDAB/Step and project conformity (junior workflow). Checks
  methodology compliance and code quality before development. Use when the user
  says review plan, controlla il piano, skontroluj plan, or before starting
  j-develop.
---
## Project context
- Read MB, can use mb-reader. (if already read this session, it can be skipped)

## Prerequisites (ALL steps are important)
- Read (if not already done) MB
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- **Code navigation** — read `.opencode/commands/mind-sets/code-navigation.md` and FOLLOW it for every symbol/code lookup in step 5: print the CODE-NAV PREFLIGHT block before the cross-check, apply the per-query discipline, and never emit a "doesn't exist" verdict without a quoted ready-tool result (or a documented grep-only fallback flagged low-confidence). Tools come from `@code-nav-local` / `@code-nav-external` in j-settings.md.

## Steps

### 1. Read Methodology Reference
**Check `j-settings.md @backend-method`:**
- If `tddab` → Read `@tddab-file` + `@tddab-lang-overlay` from j-settings.md
- If `tdd` → Standard TDD rules apply
- If `manual` → No strict methodology

**Read the mindset file completely.** The rules in that file are the ONLY source of truth for the review. Do NOT apply rules from memory or training — only what the mindset file says.

### 2. Find the Plan
Look for plan in:
- Current task folder: `{@tasks}/NN-*/plan.md`, `index.md`, or `tddab-plan.md`
- Or ask user: "Where is the plan file?"

### 3. Detect Plan Type
- If plan has `<red>` with `- test:` lines → TDDAB plan, use tddab-planner.md rules
- If plan has `<actions>` with `- action:` lines → Step plan, read `.opencode/commands/mind-sets/step-planner.md` for rules
- If plan has `<files>` tag → multi-file plan, read index.md + all sub-files

### 4. Review — Apply Rules From Mindset File

**Do NOT use a hardcoded checklist.** Review the plan against the rules you read in step 1. The mindset file defines what is correct and what is not.

Focus on these categories:

#### A. Structural Correctness
- Are all required tags present as defined in the mindset file?
- Are block IDs unique and correctly formatted?
- Is `<mission>` comprehensive enough for clean-context execution?

#### B. Dependency & Ordering (CRITICAL)
- For each block: does it use types, functions, or files defined in a LATER block?
- If yes → **dependency error** — the block cannot execute without the later block
- Verify the execution order matches actual dependencies
- Check "no dependencies on future blocks" rule

#### C. Self-Sufficiency
- Can each block be understood with ZERO context beyond the mission?
- Are file paths complete?
- Are there references to "previous discussion" or "as we decided"?

#### D. Completeness (as defined by mindset file)
- Check what the mindset file says about code completeness — apply THOSE rules, not stricter ones
- Check for TODOs, unresolved decisions, "Option A or B"
- Check for "..." that skips DECISIONS (not boilerplate — boilerplate "..." is fine per mindset)

#### E. Project Conformity
- Follows project architecture patterns
- Uses project conventions from j-settings.md
- No security issues (SQL injection, XSS, etc.)

### 5. Code Cross-Check — Plan vs Real Codebase (CRITICAL)

Steps 1-4 review the plan in isolation. This step verifies the plan against the **actual code** using the navigation tool configured in `@code-nav-local`. A plan that is internally perfect can still reference symbols that don't exist or duplicate code that already exists.

> **Apply `.opencode/commands/mind-sets/code-navigation.md` here.** Print the CODE-NAV PREFLIGHT block first; re-apply the per-query discipline to each lookup below; a `Not found` / dependency-error verdict is valid ONLY with a quoted ready-tool result (or a documented grep-only fallback flagged low-confidence) — never from a not-ready/unscoped query.

**Extract from the plan every reference to EXISTING code**, then verify each one:

#### A. Existence — symbols the plan assumes already exist
For each type, class, interface, function, or method the plan says it will *use, call, extend, inject, or import* (i.e. NOT the ones the block itself creates):
- Look it up with the `@code-nav-local` tool (symbol search; grep/glob only if that resolves to `grep`)
- **Not found** → **dependency error**: the plan references a symbol that doesn't exist (typo, hallucination, or renamed/removed). Report the block ID + symbol.
- **Found** → confirm the signature/shape matches how the plan uses it (param count, return type, namespace).

#### B. File paths — targets the plan will modify
For each file path the plan says it will edit or extend (not create):
- Verify the file exists at that path in the project
- **Missing path** → report it; the plan's layout doesn't match the real project structure.

#### C. Reuse — is the plan reinventing existing code?
For the main new types/functions the plan *creates*, search the codebase for an existing equivalent:
- If a similar symbol/pattern already exists → flag as **reuse opportunity** (per the 80%-reuse rule), suggest extending it instead of net-new code.

#### D. Third-party APIs (if `@code-nav-external` is set)
For external library calls the plan makes, verify the API signature with the `@code-nav-external` tool:
- **Signature mismatch / symbol absent in that version** → report it.
- If `@code-nav-external` is `none` → skip this check.

If `@code-nav-local` resolves to `grep` (or is absent), do a best-effort grep/glob check and note in the report that cross-check was grep-only (lower confidence).

### 6. CVM Structural Validation (if available)

If `mcp__cvm__parsePlan` tool is available, call it on the plan file as a final objective check. The parser validates tag structure, block IDs, and format — things Claude's review might miss.

- If parsePlan returns errors → add them to the report as Structural Issues
- If parsePlan succeeds → note "CVM parsePlan: valid" in the report
- If CVM is not available → skip this step (review is still valid without it)

### 7. Report

If issues found:
```
PLAN REVIEW — ISSUES FOUND

[Dependency / Ordering Errors]
- [specific issue with block IDs and explanation]

[Code Cross-Check Errors]
- [block ID + symbol/path that does not exist in the codebase, or reuse opportunity]

[Structural Issues]
- [specific issue]

[Completeness Issues]
- [specific issue]

SUGGESTED FIXES:
1. [specific fix]
2. [specific fix]

Fix these before proceeding with j-develop or j-cvm-exec-plan.
```

If plan is OK:
```
PLAN REVIEW — APPROVED ✅

✓ Structure: all required tags present
✓ Dependencies: execution order matches dependencies
✓ Self-sufficiency: blocks work on clean context
✓ Completeness: per methodology rules
✓ Code cross-check: referenced symbols/paths exist, no unintended duplication
✓ Project conformity: OK

Ready to proceed with j-develop or j-cvm-exec-plan.
```

### 8. Update Plan (if needed)
If user agrees to fixes:
- Edit the plan file with corrections
- Mark reviewed sections

## Rules
- **The mindset file is the ONLY source of truth** — do not invent stricter or looser rules
- Be strict on dependency ordering — this causes real compilation failures
- Be strict on self-sufficiency — blocks execute on clean context
- **Cross-check against real code with the `@code-nav-local` tool, not from memory** — a referenced symbol/path either exists in the codebase or it doesn't; verify, don't assume
- Always use whatever `@code-nav-local` / `@code-nav-external` specify — never hardcode a tool name; grep/glob is the fallback only when the setting resolves to `grep` or `none`
- Be lenient on code style — the mindset file defines what "complete" means
- Explain WHY something is wrong
- Suggest specific fixes, not vague advice
- If plan is good, say so quickly and move on
