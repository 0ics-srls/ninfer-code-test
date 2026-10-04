---
description: >-
  Close feature and deploy (junior workflow). Verifies completion, runs tests,
  merges branch, optionally deploys. Use when work is done, user says close,
  merge, chiudi, hotovo, or feature is ready for production.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs

## Steps

### 1. Verify Completion (use intelligence!)
Read MB, task folder (notes.md, plan.md, bug-notes.md), and recent commits.

**Check if work is actually complete:**
- Are all planned items done?
- Were tests run? (check notes or recent output)
- Does the code look finished?

**If something seems incomplete or unclear:**
```
Before closing, I want to make sure everything is ready.

I noticed that: [what doesn't add up - e.g.: "the plan has 3 items but only 2 are marked done"]

Do you want to close anyway or complete the remaining items first?
```

**If everything looks good → proceed.**

NOTE: Don't rely only on `@state` in MB - it could be wrong. Use your intelligence to assess the actual situation.

### 2. Review Changes Summary
Show user what will be closed and deployed:
```bash
git log {@main-branch}..HEAD --oneline
git diff {@main-branch}..HEAD --stat
```

```
Changes summary for this branch:

Commits: [number] commit(s)
Files changed: [list from diff --stat]

All of this will be merged into {@main-branch} and deployed.
```

This gives the junior a clear picture before proceeding.

### 3. Security & Quality Review
Before testing, scan the diff for common issues:
```bash
git diff {@main-branch}..HEAD
```

**Security checklist** (Claude checks internally, reports issues in plain language):
- [ ] No passwords or secret keys written directly in code
- [ ] No sensitive data (names, emails) printed to screen/logs
- [ ] Database queries are safe (no user text pasted into queries)
- [ ] User input is checked before using it
- [ ] Error messages don't show internal details to users

**Code quality checklist:**
- [ ] Functions focused (<50 lines)
- [ ] No deep nesting (>4 levels → use early returns)
- [ ] Errors handled explicitly
- [ ] Tests exist for new functionality

If CRITICAL issues found → FIX before proceeding.

### 3b. Definition of Done Verification (ENFORCED)
Before proceeding to tests, verify the FULL feature DoD:
- [ ] **ALL plan items** marked `[x]` in plan.md
- [ ] **Tests exist** for every new piece of functionality
- [ ] **No subtasks skipped** — every planned item was implemented or explicitly descoped with user approval
- [ ] **No overwritten done work** — completed subtasks were not modified (new subtasks created instead)

**If ANY gate fails → STOP. Tell user what's missing. Do NOT proceed.**

### 4. Final Tests
- If backend modified: run `{@test-backend}` (from j-settings.md)
- If frontend modified: test with ChromeDevTools locally
- **Update notes.md:** mark "Tested" checkbox
- If tests fail → FIX before proceeding

### 5. Final Commit
**Review what will be committed** before staging:
- No sensitive files (.env, credentials, API keys)?
- No large binary files accidentally included?
- Changes make sense for current work?

If something unexpected → ask user before proceeding.

**Determine commit prefix from branch name:**
- If branch starts with `bugfix/` → use `fix`
- If branch starts with `feature/` → use `feat`

```bash
git add .
git commit -m "feat(feature-name): complete description"
# or: git commit -m "fix(bug-name): complete description"
```

### 6. Push Feature Branch
**IMPORTANT:** Always push the feature branch BEFORE merging so user can review it on remote.
```bash
git push -u origin feature/NN-feature-name
```

### 7. Merge or Create PR

**Read `@merge-strategy` from j-settings.md:**

#### If `@merge-strategy: direct` — Merge locally and push main
```bash
git checkout {@main-branch}
git pull origin {@main-branch}
git merge feature/NN-feature-name
```

**If merge shows CONFLICT:**
```
There are merge conflicts. This requires a senior developer.

WHAT TO DO:
1. DO NOT touch anything
2. Call a senior or write in the #help channel
3. Tell them: "I have merge conflicts in [branch]"

The senior will resolve it and tell you when to continue.
```
STOP - do not proceed until senior resolves.

**Push main:**
```bash
git push origin {@main-branch}
```

→ Continue to step 8 (Deploy).

#### If `@merge-strategy: pr` — Create Pull Request
```bash
gh pr create --base {@main-branch} --head feature/NN-feature-name \
  --title "feat(feature-name): description" \
  --body "$(cat <<'EOF'
## Changes
- [list from notes.md]

## Testing
- [test results]
EOF
)"
```

If `gh` CLI is not available, show the URL:
```
Pull Request ready to create:

https://github.com/{owner}/{repo}/compare/{@main-branch}...feature/NN-feature-name

Title: feat(feature-name): description
```

```
PR created! Link: [PR URL]

A reviewer will merge it. After merge:
- Deploy will happen via CI/CD (if configured)
- Or run j-deploy manually after merge

Branch: feature/NN-feature-name (kept for review)
```

→ Skip step 8 (Deploy). Go directly to step 9.

#### If `@merge-strategy: gitflow` — PR to develop
Features always target `@dev-branch` (develop), never main directly.
```bash
gh pr create --base {@dev-branch} --head feature/NN-feature-name \
  --title "feat(feature-name): description" \
  --body "$(cat <<'EOF'
## Changes
- [list from notes.md]

## Testing
- [test results]
EOF
)"
```

If `gh` CLI is not available, show the URL:
```
Pull Request ready to create:

https://github.com/{owner}/{repo}/compare/{@dev-branch}...feature/NN-feature-name
```

```
PR created to {@dev-branch}! Link: [PR URL]

A reviewer will merge it into develop.
Release to main will happen through a release branch (managed by team lead).

Branch: feature/NN-feature-name (kept for review)
```

→ Skip step 8 (Deploy). Go directly to step 9.

#### If `@merge-strategy: gitflow-simple` — Direct merge to develop
Features merge directly to `@dev-branch` (develop).
```bash
git checkout {@dev-branch}
git pull origin {@dev-branch}
git merge feature/NN-feature-name
```

**If merge shows CONFLICT:**
```
There are merge conflicts. This requires a senior developer.

WHAT TO DO:
1. DO NOT touch anything
2. Call a senior or write in the #help channel
3. Tell them: "I have merge conflicts in [branch]"

The senior will resolve it and tell you when to continue.
```
STOP - do not proceed until senior resolves.

```bash
git push origin {@dev-branch}
```

```
Feature merged into {@dev-branch}!

Deploy to production happens when develop is merged to main (managed by team lead).

Branch: feature/NN-feature-name (kept for review)
```

→ Skip step 8 (Deploy). Go directly to step 9.

### 8. Deploy (if not already done, only for @merge-strategy: direct)

#### 8a. Check if Already Deployed
Check if user already deployed manually with j-deploy:
- Check notes.md for "Deployed" checkbox already marked
- Or ask user: "Have you already deployed with j-deploy?"

**If already deployed → skip to step 9**

If not deployed yet → continue:

**Update MB:** `@state::DEPLOY`

#### 8b. Run the deploy (delegate to j-deploy)

Read `.opencode/commands/j-deploy.md` and follow its **steps 3-7** EXACTLY (read deploy docs, backup, execute deploy, verify/failure dialog with restore option, confirm success).

Notes for the j-close context:
- SKIP j-deploy steps 1-2 (branch check and tests) — j-close already verified them in steps 1-4
- On SUCCESS → **update notes.md:** mark "Deployed" checkbox → continue to step 9
- On FAILURE → follow j-deploy's failure dialog; keep `@state::DEPLOY` until resolved, do NOT continue to step 9
- If `@deploy-script` is "none" → nothing to deploy, continue to step 9

### 9. Update MB → IDLE (NO branch deletion)
**IMPORTANT:** Do NOT delete branches! User needs to see all branches on remote for control.
- Update MB:
  - In `progress.md` → add feature as completed
  - In `activeContext.md` → clear feature info, set state to IDLE

### 10. Confirm

**If `@merge-strategy: direct`:**
```
Feature [name] completed and deployed!

Verify at: {@domain} (from j-settings.md)

Next time you want to work, use j-new-feature
```

**If `@merge-strategy: pr`:**
```
Feature [name] — PR created!

PR: [link]
Branch: feature/NN-feature-name (kept for review)

After the PR is merged and deployed, use j-new-feature for the next task.
```

**If `@merge-strategy: gitflow` or `gitflow-simple`:**
```
Feature [name] — merged/PR'd to {@dev-branch}!

Deploy to production happens when develop is released to main (managed by team lead).

Next time you want to work, use j-new-feature
```

## Rules
- NEVER merge if tests fail
- NEVER deploy without testing
- NEVER delete branches (user needs them for review/control)
- ALWAYS push feature branch before merging
- Always update MB at the end
