---
description: >-
  Setup/validate Junior workflow settings for this project. Auto-detects
  language, stack, commands, ports, and creates j-settings.md. MUST run first
  before any other j-* command. Use when starting junior workflow on a new
  project or to update settings.
---
## Purpose
Create or update `j-settings.md` in project root with all settings needed by Junior workflow commands.
**Auto-detects as much as possible, only asks for what it can't figure out.**

## Steps

### 1. Check for Existing Settings
```bash
ls -la j-settings.md 2>/dev/null
```

**If j-settings.md EXISTS → go to Step 20 (Validate Mode)**
**If j-settings.md DOES NOT EXIST → continue to Step 2 (Setup Mode)**

---

## SETUP MODE - Phase 1: SCAN

### 2. Scan Project Structure
```bash
# Get full picture
ls -la
ls -la */ 2>/dev/null | head -50
find . -maxdepth 3 -type f \( -name "*.csproj" -o -name "*.sln" -o -name "package.json" -o -name "pom.xml" -o -name "build.gradle" -o -name "requirements.txt" -o -name "Cargo.toml" -o -name "go.mod" -o -name "CMakeLists.txt" -o -name "Package.swift" -o -name "pubspec.yaml" -o -name "cpanfile" -o -name "composer.json" \) 2>/dev/null
```

### 3. Detect Language/Stack
Based on files found:

| File Found | Language | Stack |
|------------|----------|-------|
| `*.csproj` or `*.sln` | C# | .NET |
| `package.json` | TypeScript/JavaScript | Node.js |
| `pom.xml` | Java | Maven |
| `build.gradle` | Java/Kotlin | Gradle |
| `build.gradle.kts` or `*.kt` | Kotlin | Kotlin |
| `build.sbt` or `*.scala` | Scala | SBT |
| `requirements.txt` or `pyproject.toml` | Python | Python |
| `Cargo.toml` | Rust | Cargo |
| `go.mod` | Go | Go |
| `CMakeLists.txt` (no `*.csproj`) | C++ | CMake |
| `mix.exs` | Elixir | Mix |
| `Gemfile` | Ruby | Bundler |
| `composer.json` | PHP | Composer |
| `Package.swift` or `*.xcodeproj` | Swift | SPM/Xcode |
| `pubspec.yaml` | Dart | Flutter/Dart |
| `cpanfile` or `Makefile.PL` | Perl | CPAN |

**If package.json found**, read it:
```bash
cat package.json
```
Look for:
- `scripts` → test, build, start, dev commands
- `dependencies` → Angular (@angular/core), React (react), Vue (vue), etc.
- `devDependencies` → framework hints

**If *.csproj found**, read it:
```bash
cat **/*.csproj | head -100
```
Look for:
- `<TargetFramework>` → .NET version
- `<PackageReference>` → ASP.NET, EF Core, etc.

**C# mindset version detection** — decide `@csharp-mindset` (legacy vs net10):
```bash
# TFM check
grep -rE "<TargetFramework>net10\.0</TargetFramework>" --include="*.csproj" --include="*.props" 2>/dev/null
# Solution format
ls *.slnx 2>/dev/null
# Modern packages
grep -E "xunit\.v3|AwesomeAssertions|Scalar\.AspNetCore" Directory.Packages.props 2>/dev/null
```

Rule:
- ANY of: `net10.0` TFM, `.slnx` present, `xunit.v3`, `AwesomeAssertions`, `Scalar.AspNetCore` → `@csharp-mindset: net10`
- Legacy signals (`net8.0`/`net9.0` TFM + `xunit` v2 + `FluentAssertions` + `Swashbuckle`) → `@csharp-mindset: legacy`
- No C# project → `@csharp-mindset: n/a`

If ambiguous (mixed signals), ASK the user:
> "Detected mixed signals for C# stack. Which mindset version should j-* commands load?
> - `legacy` — current csharp-senior.md (zero risk on existing projects)
> - `net10` — modern csharp-senior_10.md (.NET 10, C# 14, xUnit v3, AwesomeAssertions, Scalar, .slnx)"

### 4. Detect Folder Structure
```bash
# Common patterns
ls -d src/ 2>/dev/null
ls -d backend/ src/backend/ Backend/ src/Backend/ server/ api/ 2>/dev/null
ls -d frontend/ src/frontend/ Frontend/ src/Frontend/ client/ web/ app/ 2>/dev/null
ls -d tests/ test/ Tests/ Test/ spec/ 2>/dev/null
ls -d tasks/ docs/ documentation/ 2>/dev/null
ls -d tools/ scripts/ deploy/ 2>/dev/null
```

### 5. Detect Ports
**From package.json:**
```bash
grep -E "(port|PORT|Port)[\"']?[: ]+[0-9]+" package.json 2>/dev/null
grep -E "localhost:[0-9]+" package.json 2>/dev/null
```

**From .env or .env.example:**
```bash
cat .env .env.example .env.local 2>/dev/null | grep -i port
```

**From appsettings.json (C#):**
```bash
cat **/appsettings*.json 2>/dev/null | grep -i -E "(port|url|host)" | head -10
```

**From launchSettings.json (C#):**
```bash
cat **/launchSettings.json 2>/dev/null | grep -E "applicationUrl" | head -5
```

### 6. Detect Git Branches
```bash
git branch -a 2>/dev/null | head -10
git remote show origin 2>/dev/null | grep "HEAD branch"
```

### 6b. Detect Merge Strategy
Check if branch protection or GitFlow is likely:
```bash
# Check if develop branch exists (GitFlow indicator)
git branch -a 2>/dev/null | grep -E "develop|dev$"
# Check if GitHub CLI is available and repo has branch protection
gh api repos/{owner}/{repo}/branches/{main-branch}/protection 2>/dev/null
```
- If `develop` branch exists → likely GitFlow
- If branch is protected → likely PR workflow
- If neither → default to `direct`

### 7. Detect Deploy Setup
```bash
# Look for deploy scripts
ls -la tools/deploy* scripts/deploy* deploy* Dockerfile docker-compose* 2>/dev/null
ls -la .github/workflows/ .gitlab-ci.yml Jenkinsfile 2>/dev/null
```

### 7a. Detect Backup/Restore Scripts
```bash
# Look for backup/restore scripts
ls -la tools/backup* scripts/backup* backup* 2>/dev/null
ls -la tools/restore* scripts/restore* restore* 2>/dev/null
```

### 7b. Detect ChromeDevTools MCP
Try to call ChromeDevTools MCP:
```
mcp__chrome-devtools__list_pages
```
If responds → `@chromedevtools: yes`
If fails/not found → `@chromedevtools: no`

### 8. Detect Database
```bash
# From docker-compose
grep -i -E "(postgres|mysql|mongodb|sqlite|redis)" docker-compose*.yml 2>/dev/null
# From connection strings
grep -i -E "(connectionstring|database|db_)" .env* **/appsettings*.json 2>/dev/null | head -5
```

---

## SETUP MODE - Phase 2: PROPOSE

### 9. Present Findings
Show user what was detected:

```
🔍 Project Analysis Complete!

DETECTED:
✓ Language: [C# .NET 8 / TypeScript / Java / etc.]
✓ Project type: [fullstack / backend-only / frontend-only]
✓ Backend: [path or "not found"]
✓ Frontend: [path or "not found"] ([Angular/React/Vue/etc.])
✓ Tasks folder: [path or "not found"]
✓ Tests: [path or "not found"]

COMMANDS (from package.json/csproj):
✓ Test: [detected command or "?"]
✓ Build: [detected command or "?"]
✓ Start backend: [detected command or "?"]
✓ Start frontend: [detected command or "?"]

PORTS:
✓ Frontend: [port or "?"]
✓ Backend: [port or "?"]
✓ Database: [port or "?"]

GIT:
✓ Main branch: [main/master/detected]

DEPLOY:
[✓/✗] Deploy script: [path or "not found"]
[✓/✗] Backup script: [path or "not found"]
[✓/✗] Restore script: [path or "not found"]
[✓/✗] Docker: [yes/no]

DATABASE:
[✓/✗] Type: [postgres/mysql/sqlite/none detected]

---
Is this correct? What needs to be changed?
```

### 10. Ask ONLY for Missing/Uncertain Items

**Only ask if NOT detected:**

If test command not found:
```
❓ Test command not detected. What command runs tests?
   (e.g., "dotnet test", "npm test", "pytest", or "none")
```

If ports not found:
```
❓ Ports not detected. What ports does the app use locally?
   - Frontend port: [default 5173]
   - Backend port: [default 3000]
```

If deploy not found:
```
❓ Deploy setup not detected.
   - Deploy script path: (e.g., "tools/deploy.sh" or "none")
   - Production domain: (e.g., "https://myapp.com" or "none")
```

If backup/restore not found:
```
❓ Backup/Restore scripts not detected.
   - Backup script: (e.g., "tools/backup.sh" or "none")
   - Restore script: (e.g., "tools/restore.sh" or "none")

   These are optional. If provided, j-deploy will:
   1. Run backup BEFORE deploy
   2. Offer restore if deploy fails
```

If auth method not detectable:
```
❓ Authentication method?
   1. Google OAuth
   2. Username/password
   3. Other OAuth
   4. None
```

If database not detected:
```
❓ Database type?
   1. PostgreSQL
   2. MySQL
   3. SQLite
   4. None
```

If merge strategy not detected:
```
How do you merge finished work?
   1. Direct — I merge to main and push directly
   2. PR — I create a Pull Request to main
   3. GitFlow — feature→develop (PR), release→main (full GitFlow)
   4. GitFlow Simple — feature→develop (direct push), develop→main manually
```

If methodology not obvious:
```
❓ Development methodology for backend?
   1. TDDAB — Test Driven Development Atomic Block (recommended)
   2. TDD — Test Driven Development (classic)
   3. Manual testing
```

If local limitations unknown:
```
❓ Any services that DON'T work locally?
   (e.g., "Telegram bot", "external payment API", or "none")
```

---

## SETUP MODE - Phase 3: CREATE

### 11. Confirm Before Creating
```
Ready to create j-settings.md with:

[show final values]

Create file? (y/n)
```

### 12. Create j-settings.md
Write to project root:

```markdown
# Junior Workflow Settings
# Generated by j-setup on YYYY-MM-DD
# Run j-setup again to modify

## Project
@project-name: {detected or asked}
@project-type: {fullstack|backend-only|frontend-only}
@language: {csharp|typescript|java|python|...}

## Branches
@main-branch: {main|master|detected}
@dev-branch: {if detected, otherwise empty}
@merge-strategy: {direct|pr|gitflow|gitflow-simple}

## Folders
@code: {detected}
@backend: {detected}
@frontend: {detected}
@tasks: tasks/
@docs: {detected or "sources-docs-pocs/"}
@tools: {detected or "tools/"}

## Commands
@test-backend: {detected}
@test-frontend: {detected or "manual"}
@build: {detected}
@start-backend: {detected}
@start-frontend: {detected}
@migrations-add: {detected or "n/a"}
@migrations-apply: {detected or "n/a"}

## Ports
@frontend-port: {detected}
@backend-port: {detected}
@db-port: {detected or 5432}

## Deploy
@deploy-script: {detected or "none"}
@deploy-docs: {detected or "none"}
@domain: {asked}
@backup-script: {detected or "none"}
@restore-script: {detected or "none"}

## Auth
@auth-method: {asked: google-oauth|credentials|none}
@auth-notes: {optional}

## Database
@db-type: {detected: postgresql|mysql|sqlite|none}
@db-local: {docker|native|none}

## Methodology
@backend-method: {asked: tddab|tdd|manual}
@frontend-method: {chromedevtools|automated|manual}
@tddab-file: .opencode/commands/mind-sets/tddab-planner.md
@tddab-lang-overlay: {based on @language: csharp→csharp-tddab-overlay.md, typescript→typescript-tddab-overlay.md, java→java-tddab-overlay.md, python→python-tddab-overlay.md, go→go-tddab-overlay.md, rust→rust-tddab-overlay.md, cpp→cpp-tddab-overlay.md, kotlin→kotlin-tddab-overlay.md, php→php-tddab-overlay.md, swift→swift-tddab-overlay.md, dart→dart-tddab-overlay.md, perl→perl-tddab-overlay.md} (all paths relative to .opencode/commands/mind-sets/)

## C# mindset version (only if @language == csharp)
# net10  → loads csharp-senior_10.md (DEFAULT — .NET 10, C# 14, xUnit v3, AwesomeAssertions, Scalar, .slnx)
# legacy → loads csharp-senior.md (only for existing net8.0/net9.0 projects that can't migrate)
# n/a    → project is not C#
@csharp-mindset: {detected: net10|legacy|n/a, default: net10}

## Testing
@test-e2e: tests/e2e/
@health-endpoint: /health
@chromedevtools: {detected: yes|no}
@credentials-file: {asked or "none"}  # file with login credentials for browser tests (e.g. secrets/accessi-platform.md)

## Code Navigation
@code-nav-local: {detected: lsai|vs-mcp|grep}
@code-nav-external: {detected: xmp4|none}

## Local Limitations
@limitations: {asked or "none"}
```

### 13. Validate Created File
```bash
cat j-settings.md
```

### 14. Report Success
```
✅ Junior Workflow configured!

Settings saved to: j-settings.md

Auto-detected:
- [list what was auto-detected]

You provided:
- [list what user specified]

ChromeDevTools MCP: {yes|no}
(Required for j-test-browser and j-e2e. If "no", run /j-mcp-setup to install it.)

You can now use:
- j-new-feature  → start a new feature
- j-bug          → fix a bug
- j-status       → check current state
- j-help         → see all commands

Run j-setup anytime to change settings.
```

---

## VALIDATE MODE (j-settings.md exists)

### 20. Read Current Settings and REPAIR if needed
```bash
cat j-settings.md
```

**Auto-repair stale references** (check silently, fix without asking):
- If `@tddab-file` points to a file that doesn't exist → set to `.opencode/commands/mind-sets/tddab-planner.md`
- If `@tddab-lang-overlay` is missing or wrong for `@language` → set correct overlay (all 12 languages have overlays: csharp, typescript, java, python, go, rust, cpp, kotlin, php, swift, dart, perl → `{lang}-tddab-overlay.md`)
- If any field references `y-csharp-senior`, `wsl-csharp-senior`, or other deleted files → replace with correct current path
- If `@csharp-mindset` is missing and `@language: csharp` → detect and add it
- If `@code-nav-local` or `@code-nav-external` is missing → detect from .mcp.json (lsai installed → lsai, vs-mcp → vs-mcp, neither → grep; xmp4 → xmp4, else → none)

### 21. Show Current Configuration
```
📋 Current Junior Workflow Settings:

PROJECT
  Name: {name}
  Type: {type}
  Language: {language}

STRUCTURE
  Code: {path}
  Backend: {path}
  Frontend: {path}

COMMANDS
  Test backend: {command}
  Test frontend: {command}
  Build: {command}

PORTS
  Frontend: {port}
  Backend: {port}

DEPLOY
  Script: {path}
  Domain: {url}
  Backup: {path or "none"}
  Restore: {path or "none"}

... [show all]

Options:
1. Keep these settings ✓
2. Change specific setting
3. Re-scan project (detect again)
4. Start from scratch
```

### 22. Handle User Choice
- **Option 1**: Exit, settings are valid
- **Option 2**: Ask which setting to change, update that section only
- **Option 3**: Go to Step 2 (re-scan), propose changes
- **Option 4**: Delete j-settings.md, go to Step 2

---

## Rules
- **SCAN FIRST** - detect as much as possible automatically
- **ASK ONLY UNKNOWNS** - don't ask what you can figure out
- **PROPOSE THEN CONFIRM** - show findings, let user correct
- **PRESERVE USER EDITS** - in validate mode, don't overwrite manual changes
- This command is SAFE - only creates/modifies j-settings.md
