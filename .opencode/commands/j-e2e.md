---
description: >-
  Run E2E tests on frontend with ChromeDevTools (junior workflow). Executes test
  cases from template against running app. Use when the user says run e2e, testa
  il frontend, spusti e2e testy, or wants to verify UI behavior end-to-end.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs

## Steps

### 0. Check ChromeDevTools MCP
If `j-settings.md @chromedevtools` is "no" OR MCP is not responding:
```
ChromeDevTools MCP is not configured.
It is required for automated E2E tests.

Run /j-mcp-setup to install ChromeDevTools MCP, then retry.
```
STOP.

### 1. Read Ports from j-settings.md
Get from `j-settings.md`:
- `@frontend-port` (e.g., 5173)
- `@backend-port` (e.g., 3000)
- `@domain` for production tests

### 2. Check Services Running
```bash
# Check if frontend is running
curl -s http://localhost:{@frontend-port} > /dev/null && echo "FE OK" || echo "FE NOT RUNNING"

# Check if backend is running (use @health-endpoint from j-settings.md)
curl -s http://localhost:{@backend-port}{@health-endpoint} > /dev/null && echo "BE OK" || echo "BE NOT RUNNING"
```

If services not running, ask user to start them.

### 3. Check for Test Template
Look for test checklist in `{@test-e2e}/template.md` (path from j-settings.md).

If template doesn't exist or is outdated:
```
No test template found for this project.
Run `j-e2e-generate` to create one from use cases.
```

### 4. Create Test Run File
```bash
mkdir -p {@test-e2e}/results
cp {@test-e2e}/template.md {@test-e2e}/results/$(date +%Y-%m-%d).md
```

### 5. Login Process (if authentication required)
Check `j-settings.md @auth-method` for project-specific method.

**General login flow:**
1. Navigate to login page (URL from app)
2. Take snapshot to see login UI elements
3. Authenticate using project-specific method:
   - `google-oauth` → Click OAuth button
   - `credentials` → Fill username/password form
   - `none` → Skip login
4. Verify redirect to authenticated area

**If login fails:**
- Take screenshot of error
- Ask user: "Login failed. Can you complete it manually? Say 'done' when ready."

### 6. Execute Tests
Use ChromeDevTools MCP to:
- `mcp__chrome-devtools__navigate_page` to go to URLs
- `mcp__chrome-devtools__take_snapshot` to verify UI elements
- `mcp__chrome-devtools__click` to test interactions
- `mcp__chrome-devtools__fill` to test forms
- `mcp__chrome-devtools__list_console_messages` to check for errors

For each test case:
1. Execute the action
2. Take snapshot to verify result
3. Mark result: `[x]` pass, `[!]` fail, `[-]` skip

### 7. Document Results
Update the test file with:
- Results for each test case
- Console errors found
- Screenshots of failures (save to `tmp/`)
- Summary table

Full report saved to: `{@test-e2e}/results/YYYY-MM-DD.md`

### 8. Report to User
```
E2E Test Run Complete!

Date: YYYY-MM-DD
Environment: localhost:{@frontend-port}

RESULTS:
- Passed: NN
- Failed: NN
- Skipped: NN

ISSUES FOUND:
- [describe any bugs]

Full report: {@test-e2e}/results/YYYY-MM-DD.md
```

## Test Execution Order
Follow the order in template:
1. Prerequisites (services running)
2. Public pages (login screen, about)
3. Auth flow (per j-settings.md @auth-method)
4. Dashboard
5. Each feature section
6. Edge cases

## Rules
- Auth method is project-specific - check j-settings.md @auth-method
- Take snapshot BEFORE and AFTER each major action
- Check console for errors after each page load
- If test fails, document WHY before moving on
- Save screenshots of failed tests to `tmp/e2e-{testname}.png`
