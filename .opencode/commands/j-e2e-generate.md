---
description: >-
  Generate E2E test checklist from use cases (junior workflow). Creates
  template.md with test scenarios from task docs and productContext. Use before
  j-e2e to prepare the test plan, or when user says generate tests, crea test,
  vygeneruj testy.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs

## Purpose
Generate or update `{@test-e2e}/template.md` (folder from j-settings.md, default `tests/e2e/`) based on:
- Use case documentation from `{@tasks}/` folder
- Product features from `memory-bank/productContext.md`
- Settings from `j-settings.md`

## Steps

### 1. Read Sources
Read these files to understand what to test:
- Search in `{@tasks}/` for use-cases*.md or similar documentation
- `memory-bank/productContext.md` (features list)
- `j-settings.md` (ports, URLs, auth method)

### 2. Extract Test Sections
From use cases and productContext, create sections based on:
- Features listed in productContext.md
- Use cases found in {@tasks}/
- Project-specific functionality

**Standard structure:**
- **0. Prerequisites** - services running, DB ready
- **1. Authentication** - login flows (per @auth-method in j-settings.md)
- **2. Main features** - based on productContext.md
- **N. Additional features** - one section per major feature

NOTE: Section names come from YOUR project's features,
not from this template. Read productContext.md to identify them.

### 3. Generate Template
Create `{@test-e2e}/template.md` with:

```markdown
# E2E Test Suite - {@project-name}

## Test Run Info
- **Date:** YYYY-MM-DD
- **Tester:** Claude Code (Autonomous)
- **Frontend URL:** http://localhost:{@frontend-port}
- **Backend URL:** http://localhost:{@backend-port}

## Legend
- [x] = PASS
- [!] = FAIL (with notes)
- [-] = SKIP (with reason)

---

## 0. PREREQUISITES

### 0.1 Backend
- [ ] Backend running on http://localhost:{@backend-port}
- [ ] Health check OK

### 0.2 Frontend
- [ ] Frontend running on http://localhost:{@frontend-port}
- [ ] No console errors on load

### 0.3 Database (if applicable)
- [ ] Database running (see j-settings.md @db-type)
- [ ] Migrations applied

---

## 1. AUTHENTICATION

### 1.1 Login Screen
- [ ] Page loads without errors
- [ ] Login UI elements visible (per @auth-method)

### 1.2 Login Flow
- [ ] Authentication works (method: {@auth-method})
- [ ] Redirects to authenticated area after login
- [ ] Session persists

---

## 2. [FEATURE FROM productContext.md]
<!-- Repeat for each feature -->

### 2.1 [Sub-feature]
- [ ] [Test case from use cases]
- [ ] [Test case from use cases]

---

[Continue with each feature from productContext.md...]
```

### 4. Save Template
```bash
mkdir -p {@test-e2e}/results
```

Write to `{@test-e2e}/template.md` (path from j-settings.md, default: `tests/e2e/`)

### 5. Report
```
E2E Test Template Generated!

Source: productContext.md + {@tasks}/ use cases
Output: {@test-e2e}/template.md

Sections:
- 0. Prerequisites (N tests)
- 1. Authentication (N tests)
- 2. [Feature name] (N tests)
...

Total: NN test cases

Run `j-e2e` to execute the tests.
```

## Rules
- Each feature in productContext.md = at least one test section
- Each use case = at least one test case
- Group related tests in subsections
- Include both happy path and error cases
- Add "console error check" after each page load
- Ports, paths and settings come from j-settings.md, not hardcoded
- Use `{@test-e2e}` for test folder path
- Keep tests atomic - one check per line
