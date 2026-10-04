---
description: >-
  Install and configure MCP servers for the project. Universal — works with both
  Claude Code (.mcp.json / mcpServers) and opencode (opencode.jsonc / mcp).
  Interactive — asks which MCPs to add, auto-installs everything, merges into
  the right config. Use when user says mcp setup, add mcp, configura mcp, or
  wants to enable AI tooling.
---
## Prerequisites
- Read `j-settings.md` from project root (if exists — used for language detection)

## Step 0: Detect host & resolve config targets

Detect which agent runtime this project targets:
- **opencode** if any present: `opencode.json` / `opencode.jsonc` in root, a `.opencode/` directory, or `AGENTS.md` (and no `.claude/`).
- **Claude Code** if any present: `.claude/` directory, `CLAUDE.md`, or `.mcp.json`.
- If BOTH (or neither is clear) → ask with AskUserQuestion which runtime to configure.

Resolve these targets from the detected host and use them everywhere below:

| Target | Claude Code | opencode |
|---|---|---|
| **PROFILE_CFG** (user-wide) | `~/.claude/mcp.json` | `~/.config/opencode/opencode.json` |
| **PROJECT_CFG** | `.mcp.json` | `opencode.jsonc` |
| **ROOT_KEY** | `mcpServers` | `mcp` |
| **GUIDE_FILE** | `CLAUDE.md` | `AGENTS.md` |
| **Empty skeleton** | `{"mcpServers":{}}` | `{"mcp":{}}` |

## Config format mapping (canonical → host)

Every config block below is written in **canonical (Claude) form**. Emit it under ROOT_KEY:
- **Claude Code** → use the snippet **as-is** under `mcpServers`.
- **opencode** → under `mcp`, transform each entry:
  - **stdio** (`command` + `args` [+ `env`]) → `{ "type":"local", "command":[<command>, <...args>], "enabled":true, "environment":{<env>} }`
    (merge `command`+`args` into ONE array; rename `env`→`environment`; omit `environment` if there is no env).
  - **http / remote** (`type:"http"`|`"stdio"` with a `url`) → `{ "type":"remote", "url":<url>, "enabled":true }`
    (add `headers` only if the canonical block carries auth).

Example — canonical `cvm` → **opencode**:
```json
"cvm": {
  "type": "local",
  "command": ["npx", "-y", "cvm-server@latest"],
  "enabled": true,
  "environment": { "CVM_STORAGE_TYPE": "file", "CVM_DATA_DIR": ".cvm", "CVM_LOG_LEVEL": "info", "CVM_SANDBOX_PATHS": "." }
}
```

## Step 1: Detect what's ALREADY installed (BOTH locations!)

Check the two config files for the host — user-wide AND project-level:
```bash
echo "=== User-wide ===" && cat PROFILE_CFG 2>/dev/null || echo "{}"
echo "=== Project ===" && cat PROJECT_CFG 2>/dev/null || echo "{}"
```

Parse ROOT_KEY keys from BOTH files. An MCP installed at user level works in ALL projects — don't offer to install it again.

Show: "Already installed (user): [list]" and "Already installed (project): [list]"

## Step 2: Show FULL catalog, ask which to install

Show ALL available MCPs in ONE table, marking installed ones and WHERE they're installed.

```
=== MCP SERVER CATALOG ===

CODE INTELLIGENCE
  [u] lsai            — Compiler-grade code navigation (9 languages, 14 tools)
  [ ] xmp4            — Code intelligence via SCIP for third-party libraries
  [ ] vs-mcp          — Visual Studio semantic analysis (C#)

FRAMEWORK-SPECIFIC
  [ ] primeng         — PrimeNG component docs, props, theming (Angular)
  [ ] angular-cli     — Angular CLI integration
  [ ] nx-mcp          — Nx workspace architecture, generators (Nx monorepos)
  [ ] laravel-boost   — Laravel tools — DB, artisan, tinker, docs (PHP)

DEVELOPMENT TOOLS
  [ ] chrome-devtools — Browser automation — click, type, screenshot
  [ ] cvm             — Claude Virtual Machine — autonomous task execution
  [ ] ui-ticket-mcp   — UI ticket review with browser overlay
  [ ] brave-search    — Web search via Brave API

Legend: [u]=user-wide  [p]=project  [ ]=not installed
```

Use AskUserQuestion with multiSelect. Show only MCPs NOT already installed (in either location).
Ask in rounds by category (max 4 options per question).
Skip rounds where all MCPs in that group are already installed.

## Step 3: Ask config questions for selected MCPs

After ALL selections, ask ONLY for MCPs that need user input:
- **ui-ticket-mcp** → ASK: port (default 3200)
- **cvm** → ASK: data dir (default .cvm)
- **brave-search** → ASK: BRAVE_API_KEY
- **xmp4** → ASK: install user-wide or project-only?

## Step 4: AUTO-INSTALL everything (DO NOT tell user to install manually!)

For each selected MCP, run the installation AND write the config (translated per Step 0 mapping). The user should do NOTHING.

### lsai — AUTO-INSTALL
**Detect OS and install automatically:**

**Windows (native or detected via `USERPROFILE` env var):**
```powershell
iwr https://github.com/0ics-srls/Zerox.Lsai.Public/releases/latest/download/install.ps1 -OutFile $env:TEMP\lsai-install.ps1; & $env:TEMP\lsai-install.ps1
```
Skip if `%USERPROFILE%\.lsai\run.cmd` already exists.

**Linux / macOS / WSL:**
```bash
curl -fsSL https://github.com/0ics-srls/Zerox.Lsai.Public/releases/latest/download/install.sh | bash
```
Skip if `~/.lsai/run` already exists.

Config (write to **PROJECT_CFG** — per-project, LSAI needs project context):

**Windows:**
```json
"lsai": {
  "command": "C:\\Users\\{USERNAME}\\.lsai\\run.cmd",
  "args": ["--stdio"]
}
```
Use absolute path with `run.cmd` — MCP configs don't expand `~`.

**Linux / macOS / WSL:**
```json
"lsai": {
  "command": "/home/{USERNAME}/.lsai/run",
  "args": ["--stdio"]
}
```
Use the absolute path (resolve `$HOME` and substitute) — MCP configs don't expand `~` on Linux either.

### xmp4 — CONFIG ONLY (no install needed)
If user chose user-wide → write to **PROFILE_CFG**
If user chose project → write to **PROJECT_CFG**
```json
"xmp4": {
  "type": "http",
  "url": "https://mcp.example4.ai/mcp"
}
```
(`mcp.example4.ai` is the REAL production endpoint — not a placeholder, do not "fix" it.)

### vs-mcp — CONFIG ONLY (no install needed, VS provides it)
Write to **PROJECT_CFG** (port is project-specific):
```json
"vs-mcp": {
  "type": "http",
  "url": "http://localhost:3001/sdk/"
}
```

### primeng — CONFIG ONLY (npx auto-downloads on first use)
Write to **PROJECT_CFG**:
```json
"primeng": {
  "command": "npx",
  "args": ["-y", "@primeng/mcp"]
}
```

### angular-cli — CONFIG ONLY (npx auto-downloads)
Write to **PROJECT_CFG**:
```json
"angular-cli": {
  "command": "npx",
  "args": ["-y", "@angular/cli", "mcp"]
}
```

### nx-mcp — CONFIG ONLY (npx auto-downloads)
Write to **PROJECT_CFG**:
```json
"nx-mcp": {
  "type": "stdio",
  "command": "npx",
  "args": ["nx-mcp@latest"]
}
```

### laravel-boost — AUTO-INSTALL
```bash
composer require laravel/boost --dev
php artisan boost:install
```
Skip if `composer.json` already has `laravel/boost`.

Write to **PROJECT_CFG**:
```json
"laravel-boost": {
  "command": "php",
  "args": ["artisan", "boost:mcp"]
}
```

### chrome-devtools — CONFIG ONLY (npx auto-downloads)
Write to **PROJECT_CFG**:
```json
"chrome-devtools": {
  "command": "npx",
  "args": ["-y", "chrome-devtools-mcp@latest", "--no-sandbox", "--disable-setuid-sandbox"]
}
```

### cvm — CONFIG ONLY (npx auto-downloads)
Write to **PROJECT_CFG**:
```json
"cvm": {
  "command": "npx",
  "args": ["-y", "cvm-server@latest"],
  "env": {
    "CVM_STORAGE_TYPE": "file",
    "CVM_DATA_DIR": ".cvm",
    "CVM_LOG_LEVEL": "info",
    "CVM_SANDBOX_PATHS": "."
  }
}
```

### ui-ticket-mcp — AUTO-INSTALL (backend + frontend)

**Step A: Install backend (MCP server)**
```bash
# Ensure uv/uvx is available
if ! command -v uvx &>/dev/null; then
    pip install uv
fi
```

Write to **PROJECT_CFG**:
```json
"ui-ticket-mcp": {
  "command": "uvx",
  "args": ["ui-ticket-mcp"],
  "env": {
    "PROJECT_ROOT": ".",
    "REVIEW_PORT": "{PORT}"
  }
}
```

**Step B: Install frontend panel**
```bash
npm install ui-ticket-panel
```

**Step C: Add component to the app automatically**
Find the root component of the project:
- Angular → look for `src/index.html`
- React → look for `public/index.html` or `src/index.html`
- Vue → look for `index.html` or `public/index.html`
- Plain HTML → look for `index.html`
- If j-settings exists, use `@frontend` path as hint

Once found, add BEFORE `</body>`:
```html
<script type="module" src="https://unpkg.com/ui-ticket-panel/dist/bundle.js"></script>
<review-panel api-url="http://localhost:{PORT}/api"></review-panel>
```

If using a bundler (detected from package.json having angular/react/vue):
- Also add to the main entry file (main.ts, main.tsx, main.js):
```typescript
import { defineReviewPanel } from 'ui-ticket-panel';
defineReviewPanel();
```

The `api-url` port MUST match the `REVIEW_PORT` in PROJECT_CFG.

### brave-search — CONFIG ONLY (npx auto-downloads)
Write to **PROFILE_CFG** (user-wide — generic tool):
```json
"brave-search": {
  "type": "stdio",
  "command": "npx",
  "args": ["-y", "@modelcontextprotocol/server-brave-search"],
  "env": {
    "BRAVE_API_KEY": "{KEY}"
  }
}
```

## Step 5: Write configs (merge, never overwrite)

For each target file (**PROJECT_CFG** and/or **PROFILE_CFG**):
1. Read existing content (or start with the host's **Empty skeleton**)
2. Translate each selected MCP to the host format (Step 0 mapping) and add it under **ROOT_KEY** — do NOT remove existing ones
3. Write merged result
4. Validate JSON: `python3 -m json.tool <file>`
   (opencode `opencode.jsonc` accepts plain JSON; if the existing file has comments, preserve them or write `opencode.json` instead.)

## Step 6: Update j-settings.md — Code Navigation

If `j-settings.md` exists, add/update `## Code Navigation`:
```markdown
@code-nav-local: {lsai if installed, else vs-mcp if installed, else grep}
@code-nav-external: {xmp4 if installed, else none}
```

## Step 7: Update GUIDE_FILE — Code Navigation section

Read project's **GUIDE_FILE** (`CLAUDE.md` or `AGENTS.md`). Find or create `## Code Navigation` section.
Write content based on which code intelligence MCPs were installed (lsai, vs-mcp, xmp4).
Only modify `## Code Navigation` — do NOT touch other sections.

## Step 8: Summary

```
✅ MCP servers installed and configured:
  + lsai (installed + configured in PROJECT_CFG)
  + chrome-devtools (configured in PROJECT_CFG)
  + ui-ticket-mcp (backend configured + frontend panel added to src/index.html)
  • xmp4 (already installed in PROFILE_CFG)

Restart the agent (Claude Code / opencode) to activate new MCP servers.
```

Only show "⚠️ Action required" for things that genuinely need user action (like replacing a placeholder API key).

## Rules
- NEVER tell user to install something manually — DO IT
- NEVER remove existing MCP entries — only add/update
- Resolve the host (Step 0) FIRST — every path/format/key below derives from it
- Check BOTH PROFILE_CFG AND PROJECT_CFG before offering MCPs
- User-wide MCPs (xmp4, brave-search) → PROFILE_CFG
- Project MCPs (lsai, vs-mcp, cvm, chrome-devtools, ui-ticket, primeng, angular-cli, nx-mcp, laravel-boost) → PROJECT_CFG
- xmp4 defaults to user-wide (explores external libraries, not project-specific)
- Translate every block to the host format (canonical → host) before writing
- Detect OS for platform-specific installers (LSAI: bash vs PowerShell)
- For ui-ticket-mcp: install BOTH backend AND frontend, modify HTML automatically
- Validate JSON before writing
- Ask ALL selection questions FIRST, config questions AFTER, install LAST
