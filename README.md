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
- [OpenCode](https://opencode.ai) 1.18+ and the MCP servers of the next section (`setup/install-ubuntu.sh` installs everything)
- An OpenAI-compatible endpoint serving the model. `opencode.jsonc` points to `http://localhost:8080/v1` (llama-swap)
  with the model id `qwen3.8-27b-ninfer`; change `provider.local` to match yours.

## Agent toolchain — how it works and why it matters

The plan is not executed by the model alone. A model working "freely" on a task like this drifts: it reads too much,
forgets what it decided, skips tests, guesses APIs. In our own tests the same model went from 0/5 working freely to
14/17 when driven step by step through strict TDD. The pieces below are what makes the difference, and **the plan
assumes all of them are active**: without them it still runs, but worse.

| piece | what it is | why the plan needs it |
|---|---|---|
| **OpenCode** | the coding agent: tools (read, edit, bash), sub-agents, context compaction | runs the model in a loop for hours |
| **j-\* workflow** (`.opencode/`, `AGENTS.md`) | commands, mind-sets and sub-agents that define *how* to work: TDDAB planning, step execution, debugging protocols, memory bank discipline | gives every session the same method; `/j-cvm-exec-plan` is the entry point for this benchmark |
| **CVM** (`cvm-server`, MCP) | a plan executor: parses the TDDAB plan into blocks and phases and hands the agent **one phase at a time** (RED → GREEN → VERIFY → FIX → COMMIT), saving progress in `.cvm/` | the agent never reads the whole plan (no drift), cannot skip the red test or the green gate, and resumes exactly where it stopped after a crash or a compaction |
| **Memory bank** (`memory-bank/`, MBEL v5) | the agent's compact notes on the project, updated after every block | survives context compaction and new sessions; the agent reads it first |
| **LSAI** (MCP, local) | compiler-grade navigation of **this repository** (C# via Roslyn, TypeScript via tsserver): symbols, usages, callers, outlines, impact | the plan cites exact `file:line` facts; the agent checks them and finds every usage without reading whole files — far fewer tokens, no missed call sites |
| **xmp4** (MCP, remote) | compiler-resolved knowledge of open-source libraries (signatures, real source, call graphs) | PrimeNG 21, Angular 21 and EF Core 10 are newer than most models' training data: the agent looks up the real API instead of guessing |
| **chrome-devtools** (MCP, local) | drives Chrome: navigate, click, read the DOM, console | blocks 03–05 end with DOM checks on the running app |
| **PrimeNG** (MCP, local) | PrimeNG component docs and props | correct component inputs and theming |

### Setting it up

**Option A — one script (Ubuntu 24.04 or WSL2):**

```bash
bash setup/install-ubuntu.sh
```

It installs .NET 10 SDK, Node.js 24, Google Chrome, Playwright's chromium, sqlite3, OpenCode and LSAI, restores the
project dependencies, checks the baseline (Core 33, Server 69, frontend 23) and prints the MCP status. The npx-based
servers (CVM, chrome-devtools, PrimeNG) download themselves on first use; xmp4 needs no install.

**Option B — by hand**, same order: `.NET 10 SDK` (`curl -fsSL https://dot.net/v1/dotnet-install.sh | bash -s -- --channel 10.0`),
`Node.js 24` (NodeSource), `Google Chrome`, `sqlite3`, OpenCode (`curl -fsSL https://opencode.ai/install | bash`), LSAI
(`curl -fsSL https://github.com/0ics-srls/Zerox.Lsai.Public/releases/latest/download/install.sh | bash`, needs `dotnet` on
`PATH`), then `npm ci --prefix web` and `cd web && npx playwright install --with-deps chromium`.

**Check** — from the repository root (LSAI uses the directory OpenCode starts in as its workspace):

```bash
opencode mcp list
#  ✓ cvm connected
#  ✓ lsai connected
#  ✓ xmp4 connected
#  ✓ chrome-devtools connected
#  ✓ primeng connected
```

Then check that a model can really **call** every tool (uses one of OpenCode's free models, no API key needed):

```bash
bash setup/check-agent-tools.sh                 # or: bash setup/check-agent-tools.sh local/qwen3.8-27b-ninfer
# OK 1. lsai: myapp-c-1 C# Ready, myapp-typescript-2 TypeScript Ready
# OK 2. cvm: parsePlan found 6 blocks
# OK 3. xmp4: ...signals...   OK 4. primeng: ...   OK 5. chrome-devtools: Example Domain
```

We ran exactly this on a clean Ubuntu 24.04 container as a non-root user: install script, baseline green, five MCP
servers connected, every tool called successfully.

**Two traps we hit, already handled by the script and the config:**
- *LSAI TypeScript workspace in `Error` ("Could not find a valid TypeScript installation")*: the LSAI installer currently
  pulls TypeScript 7 next to `typescript-language-server` 6, which cannot use it. The script pins TypeScript 5 there:
  `npm install --prefix ~/.lsai/servers/typescript-language-server typescript@5`.
- *chrome-devtools "Target closed"*: Chrome options must be passed as `--chrome-arg=...` (plain `--no-sandbox` is ignored
  by chrome-devtools-mcp), and on a machine without a display Chrome needs `--headless`. `opencode.jsonc` has both;
  remove `--headless` if you want to watch the browser.

All MCP servers are configured in `opencode.jsonc` (project level), so nothing has to be added to your user config.
`/j-mcp-setup` can also install and configure them interactively.

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
| `.opencode/` | the **j-\*** agent workflow used for all our runs: 25 `j-*` commands (`/j-cvm-exec-plan` runs a plan, `/j-cvm-check-plan` validates one, `/j-review-plan`, `/j-develop`, `/j-debug`, …), their mind-sets (TDDAB planner, step planner, per-language senior guides and project foundations, debug protocols), 4 sub-agents (build, test, memory-bank reader and writer) |
| `AGENTS.md` | the workflow's agent guide: memory bank first, then the task |
| `CLAUDE.md` | project facts: stack, app surface, version locks, conventions |
| `memory-bank/` | the agent's memory of the project (MBEL v5, grammar in `memory-bank/README.md`), state = before plan 07 |
| `j-settings.md` | project settings read by the `j-*` commands (folders, build/test commands, ports) |
| `opencode.jsonc` | model provider, reasoning variants, MCP servers (CVM, LSAI, xmp4, chrome-devtools, PrimeNG) |
| `setup/install-ubuntu.sh` | installs the whole toolchain on Ubuntu 24.04 / WSL2 and checks it |
| `setup/check-agent-tools.sh` | asks a model to call every MCP tool the plan needs |

## Notes on the workflow

- `j-settings.md` declares `@code-nav-local: lsai` and `@code-nav-external: xmp4`: the commands use them when connected
  and fall back to grep/glob otherwise (vs-mcp is a Visual Studio-only alternative, not used here). Our runs had both.
- Known stall: at the end of each block the CVM executor asks for a JSON summary, and some models print it in the chat
  instead of passing it to `cvm_submitTask`; the run then waits. Answer "Submit it with cvm_submitTask, then continue
  with cvm_getTask" and it resumes. (Our runs counted these nudges; run 4 needed none.)

## The app

Todo app: table view with server-side search, filters, sort and paging; week view with drag & drop between days;
shared create/edit dialog; stats.

```bash
dotnet run --project src/MyApp.Server       # API on http://localhost:5000
cd web && npm start                          # UI on http://localhost:4200, proxied to the API
```


## License

MIT — see [LICENSE](LICENSE).
