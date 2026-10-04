# AGENTS.md — rules for the coding agent

This repository is a small full-stack todo app (ASP.NET Core 10 + Angular 21 + PrimeNG) used as a **benchmark for local
coding models**: the agent executes a TDDAB plan (`tasks/07-required-due-date/plan.md`) block by block through the CVM
plan executor. Read `CLAUDE.md` for the stack, the app surface and the conventions.

## At the start of every session
1. Read `memory-bank/activeContext.md` and `memory-bank/progress.md`.
2. Read `CLAUDE.md`.
3. If you are asked to execute a plan, use `/j-cvm-exec-plan <plan path>` and follow it exactly.

## TDDAB (Test-Driven Development in Atomic Blocks)
- Every block goes RED → GREEN → VERIFY → COMMIT. Write the failing test first and run it to see it fail for the right
  reason; then the smallest code that makes it pass; then the full green gate.
- **Green gate** (every block, before its commit):
  - `dotnet build` — 0 warnings (do not pipe it into `tail`: the pipe hides the exit code)
  - `dotnet exec tests/MyApp.Core.Tests/bin/Debug/net10.0/MyApp.Core.Tests.dll`
  - `dotnet exec tests/MyApp.Server.Tests/bin/Debug/net10.0/MyApp.Server.Tests.dll`
    (xUnit v3 on Microsoft.Testing.Platform: `dotnet test` prints nothing here)
  - `npm test --prefix web -- --watch=false`
  - `npm run lint --prefix web`
  - `npm run build --prefix web`
- Never weaken, skip or delete an existing assertion to make a test pass. If a test is wrong, say why and fix the test
  explicitly in its own step.
- Conventional commits (`feat:`, `fix:`, `refactor:`, `test:`, `chore:`), one commit per block.
- After each block update `memory-bank/activeContext.md` and `memory-bank/progress.md` and commit them as
  `chore: update memory bank after <plan> block <nn>`.

## CVM protocol (plan executor)
- **One CVM tool call per turn.** Strict sequence: `getTask` → do the work → `submitTask` → wait for the response → `getTask`.
- **Always answer the executor through `submitTask`.** When a phase asks you to "respond with" a word or a JSON object,
  pass it as the `submitTask` argument — never just print it in the chat, or the run stalls waiting for you.
- All CVM calls stay in the main conversation; subagents may do the work, never the protocol.

## Servers and processes
- During plan 07 the app runs on **:5001 (backend) and :4201 (frontend)**; the plan explains how to start them.
- Start long-running processes with `setsid nohup … < /dev/null &`; stop them **by PID** only. Never `pkill -f`
  (it matches your own shell) and never kill processes you did not start.

## Dates
- Never `toISOString()` / `new Date('yyyy-MM-dd')` in the frontend: use `web/src/app/core/dates.ts` (local dates).
