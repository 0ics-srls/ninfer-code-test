---
description: >-
  Start new feature (junior workflow). Creates feature branch, gathers
  requirements, plans implementation. Use when the user wants to add something
  new, implement a feature, aggiungere funzionalita, or pridat novu feature.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- If not read this session: Read `.opencode/commands/mind-sets/common-development-workflow.md` (research-first, 5-phase pipeline) — during PROPOSE also consult `mind-sets/common-patterns.md` for cross-language design patterns
- Read (if not already done) MB skip all subdirs
- **Code navigation** — read `.opencode/commands/mind-sets/code-navigation.md` and follow it for code exploration and symbol lookups (tool from `@code-nav-local`/`@code-nav-external`; grep is the last-resort fallback when the tool is absent/not-ready, flagged).

## Steps

### 1. Clean State
- `git status` - check for uncommitted changes
- If changes exist:
  - **Review what will be committed** - no sensitive files (.env, credentials)?
  - If something unexpected → ask user before committing
  - `git add . && git commit -m "WIP: save before new feature"`
- `git push`

### 2. Sync with Base Branch
**Read `@merge-strategy` from j-settings.md:**
- If `gitflow` or `gitflow-simple` → base branch is `{@dev-branch}` (develop)
- Otherwise → base branch is `{@main-branch}`

```bash
git checkout {base-branch}
git pull origin {base-branch}
```
**IMPORTANT:** Always start from latest base branch to avoid conflicts later.

### 3. Find Next Number
- Check existing **folders** (not files) in `{@tasks}/` matching pattern `NN-*`
- Use: `ls -d {@tasks}/[0-9][0-9]-*/ 2>/dev/null | sort | tail -1`
- Extract number and increment (e.g., if 03-xxx exists, next is 04)
- If no numbered folders exist, start with 01

### 4. Ask Feature Name
Ask user: "What do you want to call this feature? (short, like: login-social, dark-mode)"

### 4b. Confirm Before Creating
```
I'll create:
- Branch: feature/NN-{name}
- Folder: {@tasks}/NN-{name}/

Is this correct? (yes/no)
```
If no → ask for correct name again.
If yes → proceed.

### 5. Create Branch, Folder, Files, and Push
```bash
git checkout -b feature/NN-feature-name
mkdir -p {@tasks}/NN-feature-name
git push -u origin feature/NN-feature-name
```
**IMPORTANT:** Push branch immediately so user can see it on remote.

### 6. Create Notes File in Task Folder
Create `{@tasks}/NN-feature-name/notes.md`:
```markdown
# Feature: NN-feature-name

## Requirements (from user)
<!-- Claude writes here what user says during LISTEN -->

## Analysis
<!-- Claude writes here findings from ANALYZE -->

## Proposed Solution
<!-- Claude writes here the proposal from PROPOSE -->

## Status
- [ ] Requirements gathered
- [ ] Code analyzed
- [ ] Solution proposed
- [ ] Plan created
- [ ] Development done
- [ ] Tested
- [ ] Deployed
```

### 7. Update MB
In `activeContext.md` set:
```
@state::LISTEN
@feature::NN-feature-name
@branch::feature/NN-feature-name
```

### 8. Enter LISTEN State
Say:
```
Ready!
Branch: feature/NN-feature-name
Folder: {@tasks}/NN-feature-name/

Now TELL ME what you need. I will listen and do nothing until you say "done".
You can tell me:
- What this feature should do
- How it should work
- What you see in your mind

Go!
```

## State Transitions (UPDATE MB AT EACH!)

### When user says "done" → ANALYZE
1. **Update MB:** `@state::ANALYZE`
2. **Update notes.md:** mark "Requirements gathered" done
3. **Context Gathering** (pre-load everything before analyzing):
   - Read MB activeContext + progress (current state, recent decisions)
   - `git log --oneline -10` (what patterns were established recently?)
   - Read architecture docs if they exist (CLAUDE.md, architecture.md)
   - Check library/dependency versions (are they current? breaking changes?)
   - Read previous task notes if this continues prior work
4. Read code related to the feature, use ChromeDevTools if frontend
5. **Update notes.md:** write Analysis section with findings

### After analysis complete → RESEARCH
Before proposing a solution, search for existing approaches:
1. **Codebase FIRST** — is there a similar feature already? Can it be extended?
2. **Framework/library docs** — does the framework provide built-in support?
3. **Package registries** — npm/pip/cargo/pub/composer for battle-tested libraries
4. **GitHub** — proven implementations, skeleton projects, reference architectures
5. **Web search LAST** — only if above sources are insufficient
**Rule: Prefer proven approach over net-new code when it meets 80%+ of requirement.**
Document findings in notes.md under "Research" section.

### After research → PROPOSE
1. **Update MB:** `@state::PROPOSE`
2. **Update notes.md:** mark "Code analyzed" done
3. Present solution to user (include research findings — what exists, what to reuse)
4. **Update notes.md:** write Proposed Solution section

### When user approves → COMPLEXITY SCORING
Before creating the plan, score the complexity of each major task:
1. Rate each task **1-10** based on: dependencies, unknowns, cross-cutting concerns, testing difficulty
2. Tasks scored **1-3**: keep as single items (simple, well-understood)
3. Tasks scored **4-6**: break into 2-3 subtasks
4. Tasks scored **7-10**: break into 4-8 subtasks with custom expansion
5. Document scores in notes.md under "Complexity Assessment"

This prevents under-planning complex tasks and over-planning simple ones.

### After scoring → PLAN
1. **Update MB:** `@state::PLAN`
2. **Update notes.md:** mark "Solution proposed" done
3. **Read `@backend-method` from j-settings.md** and branch on its value.

**If `tddab` — the methodology read is a HARD GATE. Do these steps IN ORDER. Do NOT write the plan before step 3c.**

  - **3a. Read the methodology IN FULL** — open and read `@tddab-file` AND the language overlay `@tddab-lang-overlay` (both paths are in j-settings.md). This is not optional and you do NOT already know these rules from training: TDDAB v2 is **not** generic TDD — it has specific rules (bottom-up decomposition, RED = the contract/interface, reference code is intentionally non-compilable, no fixed test-count limits). The tag names below are **not** the spec; the rules live in those files.
  - **3b. Prove you read it (verification gate).** Before writing any plan, write to `notes.md` under a "TDDAB Rules Applied" heading the **3-5 key rules** you will follow, in your own words: decomposition direction, what RED must contain, what makes a block self-sufficient. **If you cannot write these from what you just read, you did not read it — go back to 3a.** Do not paraphrase from memory.
  - **3c. Only now create** `{@tasks}/NN-feature-name/plan.md` following the structure defined in `@tddab-file` (tags such as `<mission>`, `<block>`, `<intro>`, `<red>`, `<success>` — their exact rules come from the file, not from this list).
  - **3d. Self-check with `/j-review-plan`** before showing the plan — it reads the same mindset file as source of truth and cross-checks plan↔code, catching an invented or non-conformant plan.

**If `tdd`** → Create plan with test-first structure.
**If `manual`** → Create plan with generic checklist structure (see j-develop for template).

4. Ask user to review plan

### When user approves plan → ready for j-develop
Tell user: "Plan approved! Use `j-develop` to start implementation."

## Rules
- Do NOT read code during LISTEN
- Do NOT propose solutions during LISTEN
- **Write what user says to notes.md** as they speak
- **UPDATE MB at EVERY state change!**
