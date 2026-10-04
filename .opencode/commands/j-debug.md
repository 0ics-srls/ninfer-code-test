---
description: >-
  Debug any problem with Protocol D (junior workflow). Full verbose mindset —
  use when context window is not a concern. For large context sessions use
  j-debug-mb instead.
---
## Prerequisites
- Read `j-settings.md` from project root (REQUIRED - run `j-setup` if missing)
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- If not read this session: Read `.opencode/commands/mind-sets/debug-protocol.md`
- If not read this session: Read `.opencode/commands/mind-sets/debug-protocol-e2e.md`
- Read (if not already done) MB skip all subdirs

## Purpose

Universal debugging command. No branches, no folders, no workflow overhead.
Just Protocol D applied to find and understand the problem.

Use this when:
- Something doesn't work and you don't know why
- You need to investigate before deciding if it's a bug or missing feature
- Quick debugging session without full j-bug workflow

## Steps

### 1. Identify Problem Type

Ask user:
```
What's broken?
1. Frontend only (UI doesn't work, visual bug)
2. Frontend → Backend (API call fails, wrong data)
3. Backend only (API error, logic bug, DB issue)
4. Don't know (let's find out)

Which one?
```

### 2. Gather Initial Info

Based on answer:

**Frontend only:**
- What page/component?
- What should happen vs what happens?
- Any console errors?

**Frontend → Backend:**
- What action triggers it?
- What does network tab show?
- Backend running?

**Backend only:**
- What endpoint/function?
- What's the error?
- Where are the logs?

**Don't know:**
- Describe what you see
- When did it start?
- Any recent changes?

### 3. Pre-Debug Checklist

Verify before debugging:

```
Services:
- [ ] Backend running? (check: {@start-backend})
- [ ] Frontend running? (check: {@start-frontend})
- [ ] Database accessible?

Tools ready:
- [ ] Browser DevTools open (Console + Network)
- [ ] Know where backend logs are
- [ ] Can reproduce the problem
```

If something not running → start it first, then continue.

### 4. Protocol D - READ

**Find ALL error sources:**

| Layer | Where to Look | Command/Tool |
|-------|---------------|--------------|
| Frontend | Browser Console | DevTools → Console |
| Network | Network Tab | DevTools → Network (red = failed) |
| Backend | App Logs | Check {@docs} for log location |
| Database | Query errors | Backend logs usually show these |

**Output required:**
```
**FRONTEND ERROR**: [console message or "none"]
**NETWORK ERROR**: [failed request or "none"]
**BACKEND ERROR**: [log entry or "none"]
**DB ERROR**: [if visible or "none"]
```

### 5. Protocol D - ISOLATE

**Find which layer OWNS the problem:**

Trace the request flow:
```
UI action → Request sent? → Backend received? → Logic OK? → DB OK? → Response? → UI updated?
```

Find where it breaks:
- Last step that worked = ✓
- First step that failed = ✗ ← **This is your target layer**

**Output required:**
```
**FAILING LAYER**: [frontend / backend / database / network]
**LAST SUCCESS**: [what worked]
**FIRST FAILURE**: [what failed]
**LOCATION**: [file:line if known]
```

### 6. Protocol D - DOCS

If the error involves framework/library:
- Check official docs
- Verify correct version
- Look for known patterns

Skip if it's clearly business logic bug.

### 7. Protocol D - HYPOTHESIZE

**BEFORE touching any code:**

```
**HYPOTHESIS**: [what I think causes the error]
**REASONING**: [why I think this - cause-effect chain]
**PREDICTION**: [what will change after fix]
```

Rules:
- Must be falsifiable (you can test it)
- Must explain ALL symptoms
- No "let's try" - only "I believe X because Y"

Ask user:
```
My hypothesis: [explanation]

Does this match what you're seeing? Should I investigate further or proceed?
```

### 8. Protocol D - VERIFY

**ONE change at a time.**

After change:
```
**CHANGE**: [single change made]
**RESULT**: [actual outcome]
**PREDICTION MATCH**: [yes/no]
**CONCLUSION**: [what we learned]
```

If prediction wrong → go back to READ with new information.
If prediction correct → problem understood.

### 9. Decision Point

After Protocol D completes:

```
Problem found: [summary]
Location: [file:line]
Cause: [explanation]

Options:
1. Fix it now (quick fix, no branch needed)
2. Create bug ticket → use j-bug for full workflow
3. It's actually a feature → use j-new-feature
4. Need more investigation

What do you want to do?
```

### 10. Update MB

Set in `activeContext.md`:
```
@lastDebug::{description}
@debugResult::{found|needsMore|wasBug|wasFeature}
```

## Rules

- **Protocol D always** - READ, ISOLATE, DOCS, HYPOTHESIZE, VERIFY
- **No guessing** - evidence from logs required
- **One change at a time** - never multiple changes without verifying
- **Quote exact errors** - no paraphrasing
- **Layer identification first** - know WHERE before fixing WHAT
- **No workflow overhead** - this is pure debugging, not bug-fix workflow
- **Ask if unsure** - use AskUserQuestion to clarify

## Quick Reference

```
RIDHV = Read → Isolate → Docs → Hypothesize → Verify
         ↑                                      ↓
         └────────── if wrong ──────────────────┘

Frontend: Browser Console + Network Tab
Backend: Application Logs
Database: Query errors in backend logs

Always ask: "Which layer OWNS this problem?"
```
