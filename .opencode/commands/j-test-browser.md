---
description: >-
  AUTONOMOUS pass/fail web page test via ChromeDevTools MCP — navigates all
  pages, handles Keycloak login, checks for errors, reports results. Text
  snapshots (zero image cost), Haiku only for visual verification. Use when the
  user says test browser, prova nel browser, controlla se funziona, verify the
  UI works. For interactive user-guided browsing use j-browse instead.
---
# Browser Testing — Token-Efficient

Test a web application by navigating its pages, checking for errors, and reporting results.
All interaction uses text snapshots (a11y tree) — screenshots only when visual verification is needed.

## Arguments

`$ARGUMENTS` may contain a URL. If provided, navigate there. If empty, ask the user what URL to test.

## Step 1: Navigate

```
mcp__chrome-devtools__navigate_page url=<target-url>
```

Then take a snapshot to understand the page:

```
mcp__chrome-devtools__take_snapshot
```

## Step 2: Handle Login (if needed)

If the snapshot shows a Keycloak/OIDC login page (look for "Sign in to your account", textbox "Username", textbox "Password", button "Sign In"):

1. Read credentials from the file pointed to by `@credentials-file` in j-settings.md (fallback: `secrets/accessi-platform.md` if the key is missing) — find the section matching the app
2. `fill` the username field uid
3. `fill` the password field uid
4. `click` the Sign In button uid
5. Wait for redirect, then `take_snapshot` to confirm you're in the app

If no login page appears, proceed directly.

## Step 3: Explore Pages

Detect navigation links from the snapshot (look for `link` elements in sidebar/nav).

For each nav link:
1. `click` the link uid
2. `wait_for` some expected text (page title, content keyword) with 5s timeout
3. `take_snapshot` — scan for error indicators (empty states are OK, error messages are not)
4. `list_console_messages` — flag any `[error]` messages (ignore `[info]` and `[warn]`)
5. Note the page name, element count, and any issues

After visiting all nav pages, return to the first page.

## Step 4: Report

Summarize what you found:

```
Browser Test: <url>
Version: <if visible in UI>

Pages visited:
  - <page name> — <status> (<element count>, <notes>)
  - <page name> — <status>

Console errors: <count> (list if any)
Network errors: <count> (list if any)

Overall: PASS / ISSUES FOUND
```

## Visual Verification (only when needed)

If a text snapshot shows something suspicious (unexpected empty page, error text, broken layout hints),
take a visual screenshot for confirmation:

```
mcp__chrome-devtools__take_screenshot filePath=problem-chrome-dt/visual-check.webp format=webp quality=30
```

Then spawn a Haiku agent to interpret it — this costs almost nothing compared to Opus reading an image:

```
Agent model=haiku prompt="Read the screenshot at <absolute-path> and describe:
1) What page/state is shown
2) Any errors or issues visible
3) Key visual elements
Answer in 5 lines max."
```

## Interaction Techniques

When you need to interact with the page beyond just viewing:

- **Click**: `mcp__chrome-devtools__click` uid=<uid from snapshot>
- **Fill form**: `mcp__chrome-devtools__fill` uid=<uid> value=<text>
- **Press key**: `mcp__chrome-devtools__press_key` key=Enter
- **Type text**: `mcp__chrome-devtools__type_text` text=<text>
- **Run JS**: `mcp__chrome-devtools__evaluate_script` for complex checks
- **Check network**: `mcp__chrome-devtools__list_network_requests` resourceTypes=["fetch","xhr"] for API failures

## Rules

- **TEXT FIRST**: Always use `take_snapshot` to understand pages. Never take a screenshot just to see what's on the page.
- **NO OPUS IMAGES**: Never read a screenshot file with the Read tool in the main conversation — this burns Opus tokens on image processing. Always delegate to a Haiku agent.
- **CONSOLE CHECK**: Check console messages after every page navigation. Errors are important, warnings and info are noise.
- **DON'T OVER-TEST**: Visit each main nav page once. Don't click every button or fill every form unless the user asks.
- **REPORT CONCISELY**: The user wants to know if it works, not a play-by-play of every click.
