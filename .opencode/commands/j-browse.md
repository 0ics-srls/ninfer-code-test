---
description: >-
  INTERACTIVE browsing - user guides, Claude clicks. Browse and interact with
  any web page via ChromeDevTools MCP, token-efficient (text snapshots, Haiku
  for visual checks). Use when user says browse, naviga, open page, apri questa
  pagina, click on, fill the form. For an AUTONOMOUS pass/fail page test use
  j-test-browser instead.
---
# Browser Navigation — Token-Efficient

Interactive browsing of any web page. The user guides what to do, you execute via ChromeDevTools.

## Arguments

`$ARGUMENTS` may contain a URL. If provided, navigate there. If empty, ask the user.

## Core Principle: TEXT FIRST

- **Always** use `take_snapshot` to understand a page — gives UIDs, element types, text
- **Never** take a screenshot just to see what's on the page
- **Visual checks** only when text is insufficient: screenshot as webp quality 30 → Haiku agent

## Navigation

```
mcp__chrome-devtools__navigate_page url=<target>
mcp__chrome-devtools__take_snapshot
```

## Interaction

All interaction uses UIDs from the latest snapshot:
- `mcp__chrome-devtools__click` uid=<uid>
- `mcp__chrome-devtools__fill` uid=<uid> value=<text>
- `mcp__chrome-devtools__press_key` key=Enter
- `mcp__chrome-devtools__type_text` text=<text>
- `mcp__chrome-devtools__evaluate_script` for complex JS checks

## Login (when needed)

If the page shows a login form (Keycloak or other), check the file pointed to by `@credentials-file` in j-settings.md (fallback: `secrets/accessi-platform.md` if the key is missing) for credentials matching the app. Fill username, password, click sign in, verify redirect.

Not all sites require login — only handle it if a login page appears.

## Visual Check Pattern

Save screenshot to file, then Haiku interprets it (not Opus):
```
mcp__chrome-devtools__take_screenshot filePath=problem-chrome-dt/visual-check.webp format=webp quality=30
```
```
Agent model=haiku prompt="Read the screenshot at <path> and describe what you see in 5 lines max."
```

## Rules

- **NEVER** read screenshot files with Read tool in main conversation (burns Opus tokens)
- **CHECK** console messages when something seems wrong: `list_console_messages`
- **REPORT** what you find concisely to the user
- **ASK** the user what to do next — this is interactive browsing, not automated testing
