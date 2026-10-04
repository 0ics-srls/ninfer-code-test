# ninfer-code-test

A real agentic-coding benchmark for **local LLMs**: a small full-stack todo app and a 6-block TDD plan that a coding agent
(OpenCode) executes end to end through the CVM plan executor. It is the test we used to measure Qwen3.8-27B on a
Tesla V100 + RTX 4090 running the [NInfer `v100-4090`](https://github.com/0ics-srls/ninfer) engine.

Synthetic benchmarks tell you tokens per second. This tells you whether the model, the engine and the context handling
hold up for **2–3 hours of real work at 100k–260k of context**: reading code, writing failing tests first, migrations,
regenerating an API client, Angular UI changes, Playwright e2e tests, commits — without a human pushing it.

## What the agent has to do

`tasks/07-required-due-date/plan.md` — make the due date mandatory across the whole stack and rebuild the week view:

| block | what changes |
|---|---|
| 01 | `DueDate` becomes required: entity, DTOs, EF migration with backfill before NOT NULL, API returns 400 without it, test and e2e seeds |
| 02 | `GET /api/Todos/week?date=` — any week, filtered in SQL |
| 03 | regenerate the OpenAPI client; create dialog defaults to today; sortable Due Date column; fixes 4 defects left by feature 06 |
| 04 | week navigation: Prev / Today / Next, range label, jump-to-week picker, active nav pill |
| 05 | "+" on each week column opens the dialog with that date |
| 06 | Playwright e2e for the new contract (25 tests, run twice) and docs |

Every block goes RED → GREEN → VERIFY → COMMIT with a full green gate (build with 0 warnings, backend unit and
integration tests, frontend unit tests, lint, production build).

## Requirements

- Linux or WSL2, git, `sqlite3`
- .NET 10 SDK, Node.js 24+
- Playwright browsers: `cd web && npx playwright install chromium`
- [OpenCode](https://opencode.ai) 1.18+
- An OpenAI-compatible endpoint serving the model. `opencode.json` points to `http://localhost:8080/v1` (llama-swap)
  with the model id `qwen3.8-27b-ninfer`; change `provider.local` to match yours.

## Run the benchmark

```bash
git clone https://github.com/0ics-srls/ninfer-code-test.git
cd ninfer-code-test

# 1. baseline must be green
dotnet build
dotnet exec tests/MyApp.Core.Tests/bin/Debug/net10.0/MyApp.Core.Tests.dll     # 33 passed
dotnet exec tests/MyApp.Server.Tests/bin/Debug/net10.0/MyApp.Server.Tests.dll # 69 passed
npm ci --prefix web
npm test --prefix web -- --watch=false                                         # 23 passed in 5 files

# 2. branch for the run
git switch -c 07-required-due-date

# 3. let OpenCode use 64k output tokens (the default is 32k)
export OPENCODE_EXPERIMENTAL_OUTPUT_TOKEN_MAX=65536

# 4. start OpenCode in the repo and run the plan
opencode
#   then type:  /j-cvm-exec-plan tasks/07-required-due-date/plan.md
```

Do not type anything while it runs. When it finishes, check the result yourself on a clean tree:

```bash
dotnet build && \
dotnet exec tests/MyApp.Core.Tests/bin/Debug/net10.0/MyApp.Core.Tests.dll && \
dotnet exec tests/MyApp.Server.Tests/bin/Debug/net10.0/MyApp.Server.Tests.dll && \
npm test --prefix web -- --watch=false && npm run lint --prefix web && npm run build --prefix web
```

Expected after the plan: Core **34**, Server **77**, frontend **37** in 6 files, lint 0, production build green, and
**25 e2e** green twice (run them on free ports as described in block 06 of the plan).

To run it again from scratch: `git switch main && git branch -D 07-required-due-date && rm -rf .cvm src/MyApp.Server/myapp.db*`.

## Our results

Qwen3.8-27B (MLP Q8_0, attention FP8, int8 KV cache, MTP) on a Tesla V100 32 GB + RTX 4090 in tensor parallel,
NInfer `v100-4090`. Times are active work, idle gaps over 10 minutes removed.

| run | model | blocks | active work | generation <100k / 100–160k / beyond | e2e | code review (3 reviewers, /25) |
|---|---|---|---|---|---|---|
| 1 | official | 6/6 | 2 h 05 | 94 / 87 / 85 t/s | 25/25 ×2 | 21 / 20 / 22 |
| 2 | official | 6/6 | 1 h 23 | 96 / 93 / 89 t/s | 25/25 ×2 | 21 / 20 / 20 |
| 3 | Uncensored | 6/6 | 1 h 29 | 104 / 97 / 90 t/s | 25/25 ×2 | 21 / 21 / 20 |
| 4 | Uncensored | 6/6 | 2 h 28 | 106 / 100 / 91 t/s | 25/25 ×2 | 20 / 20 / 20 |

Run 4 took longer with the fastest engine: the model produced 458k reasoning tokens instead of 149–165k. Replaying the
same requests on two engine builds gives the same reasoning (57.0k vs 57.5k tokens), so reasoning length depends on the
path the conversation takes, not on the engine. Compare engines on replayed requests, not on single runs.

The same plan on Qwen3.8-27B FP8 under vLLM (1Cat fork, V100 + 4090): 6/6, review 19/25, 17–19 t/s on real work.

## What is in here

| path | what |
|---|---|
| `src/`, `tests/` | ASP.NET Core 10 Web API, EF Core SQLite, xUnit v3 |
| `web/` | Angular 21 + PrimeNG 21, vitest, Playwright e2e |
| `tasks/07-required-due-date/plan.md` | the benchmark plan |
| `.opencode/commands/` | `j-cvm-exec-plan` (runs a plan), `j-cvm-check-plan` (validates one), `j-review-plan` |
| `opencode.json` | model provider, reasoning variants, MCP servers (CVM, chrome-devtools, PrimeNG docs) |
| `AGENTS.md`, `CLAUDE.md` | rules and project facts for the agent |
| `memory-bank/` | the agent's notes about the project, state = before plan 07 |

## The app

Todo app: table view with server-side search, filters, sort and paging; week view with drag & drop between days;
shared create/edit dialog; stats.

```bash
dotnet run --project src/MyApp.Server       # API on http://localhost:5000
cd web && npm start                          # UI on http://localhost:4200, proxied to the API
```


## License

MIT — see [LICENSE](LICENSE).
