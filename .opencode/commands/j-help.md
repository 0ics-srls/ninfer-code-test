---
description: >-
  Help (junior workflow). Shows all available junior commands with descriptions.
  Use when the user asks what commands exist, how junior workflow works, or says
  help/aiuto/pomoc.
---
## Prerequisites
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- MB: not required for help

## Show
```
=== JUNIOR WORKFLOW HELP ===

FIRST TIME SETUP:
  j-setup        → Configure project settings (REQUIRED before other commands)
  j-mcp-setup    → Install/configure MCP servers (CVM, ChromeDevTools, code navigation)
  j-new-project  → Create a brand new project from scratch (with all foundations)

START WORK:
  j-new-feature  → Start a new feature
  j-bug          → Fix a bug
  j-poc          → Experiment with something new (proof of concept)
  j-continue     → Resume where you left off

DURING WORK:
  j-develop      → Start development (after plan is ready)
  j-review-plan  → Review plan for methodology/project conformity
  j-debug        → Debug any problem (Protocol D)
  j-debug-mb     → Same as j-debug but uses less memory (for long sessions)
  j-status       → See current status
  j-save         → Save everything and leave

TESTING & BROWSING:
  j-browse       → Browse/interact with any web page (you guide, I click)
  j-test-browser → Auto-test a web page (navigate all pages, check errors, report)
  j-e2e          → Run full E2E test suite
  j-e2e-generate → Generate test checklist from use cases

FINISH:
  j-close        → Close feature and deploy
  j-deploy       → Deploy only (works standalone on the right branch)

HELP:
  j-help-advanced → Visual diagrams of how commands connect (opens in browser)

ADVANCED (ask someone experienced first):
  j-cvm-check-plan → Validate a TDDAB plan with CVM (parsePlan)
  j-cvm-exec-plan  → Execute a TDDAB plan autonomously via CVM planexecutor
  j-autopilot      → Alias of j-cvm-exec-plan (hands-off autonomous development)

=== FIRST TIME? ===

Run j-setup first! It will:
1. Ask about your project (language, structure, commands)
2. Create j-settings.md with all configurations
3. Make all other j-* commands work properly

=== HOW IT WORKS ===

1. Run j-setup (only once per project)
2. Use j-new-feature or j-bug
3. Tell me what you need (I listen)
4. When you say "done", I analyze the code and research solutions
5. I propose a solution, we discuss
6. We create a step-by-step plan, you review it
7. You say "j-develop" and I build it step by step
8. When done, use j-close to merge and deploy

For diagrams of these flows, use j-help-advanced.

=== IF YOU'RE STUCK ===

- Don't know what to do? → j-status to see where we are
- Need to leave? → j-save to save everything
- Something not working? → Tell me the problem
- Have doubts? → Ask! Better to ask than make mistakes
- Settings wrong? → j-setup to change them

=== IMPORTANT RULES ===

- I do NOTHING until you ask me
- Before modifying code, I ask confirmation
- If you need to leave, always use j-save
- Don't be afraid of mistakes, we can always go back

=== IF SOMETHING GOES VERY WRONG ===

1. Use j-save
2. Contact someone experienced
```
