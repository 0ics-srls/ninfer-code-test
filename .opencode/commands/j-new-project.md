---
description: >-
  Scaffold a new project with all foundations in place (junior workflow).
  Gathers requirements, analyzes stack, proposes structure, creates TDDAB plan —
  then STOPS. User triggers j-develop to execute. Use when starting a brand new
  project from scratch, nuovo progetto, novy projekt.
---
## Prerequisites
- If not read this session: Read `.opencode/commands/mind-sets/junior.md`
- Read (if not already done) MB skip all subdirs

## Purpose

Guided project scaffolding that establishes ALL project foundations from Day 1. Uses `project-foundations-{language}.md` as the blueprint.

The cost of adding these foundations on Day 1 is zero. The cost of adding them later is massive.

**This command creates the PLAN. It does NOT write project code. j-develop executes the plan.**

## Steps

### 1. LISTEN — Gather Project Info
**Update MB:** `@state::LISTEN`

Ask user (use AskUserQuestion):
```
Let's set up a new project. I need some info:

1. Project name?
2. Language/framework?
   - C#/.NET, TypeScript, Java/Spring Boot, Kotlin/KMP
   - Python, Go, Rust, C++, Swift, PHP, Dart/Flutter, Perl
3. Project type?
   - Web API (backend only)
   - Full stack (frontend + backend)
   - Library/package
   - Console/CLI application
4. Will it have a database? If yes, which one?
5. Brief description (one sentence)?
```

Do NOT proceed until ALL questions are answered.
Do NOT read code, do NOT propose solutions — only listen.

### 2. ANALYZE — Study the Stack
**Update MB:** `@state::ANALYZE`

Read the appropriate foundations file:
- C#: `mind-sets/project-foundations-csharp_10.md` (default). Use `project-foundations-csharp.md` only if user explicitly requests net8.0/net9.0
- TypeScript: `mind-sets/project-foundations-typescript.md`
- Java: `mind-sets/project-foundations-java.md`
- Kotlin/KMP: `mind-sets/project-foundations-kotlin.md`
- Python: `mind-sets/project-foundations-python.md`
- Go: `mind-sets/project-foundations-go.md`
- Rust: `mind-sets/project-foundations-rust.md`
- C++: `mind-sets/project-foundations-cpp.md`
- Swift: `mind-sets/project-foundations-swift.md`
- PHP: `mind-sets/project-foundations-php.md`
- Dart/Flutter: `mind-sets/project-foundations-dart.md`
- Perl: `mind-sets/project-foundations-perl.md`
- **Other:** `mind-sets/project-foundations.md` (base only, use own knowledge)

All paths relative to `.opencode/commands/`.

Read TDDAB methodology — **HARD GATE, do not skip even if you think you know TDDAB**:
- Read `mind-sets/tddab-planner.md` IN FULL
- Read `mind-sets/{lang}-tddab-overlay.md` (if exists)
- TDDAB v2 is **not** generic TDD (bottom-up decomposition, RED = the contract, reference code intentionally non-compilable, no fixed test-count limits). You do NOT already know these from training.
- **Prove you read it**: before creating any plan (step 4), state the 3-5 key TDDAB rules you will apply, in your own words. If you cannot, you have not read it — read it again.

### 3. PROPOSE — Show Project Structure
**Update MB:** `@state::PROPOSE`

Show the user what will be created:

```
## Project: {name}

### Directory Structure
[show directory tree based on project type + language]

### 16 Foundations that will be applied:
 1. Zero-tolerance warnings — [language-specific config]
 2. Central dependency management — [approach]
 3. Versioning strategy — [how]
 4. Structured error handling — [types]
 5. Test configuration — [fast/slow split]
 6. Immutable DTOs — [records/dataclass/etc.]
 7. DI registration — [framework-specific]
 8. Centralized config with validation — [approach]
 9. Structured file logging — [framework]
10. Zero-dependency core module — [core project]
11. Interface-first design — [pattern]
12. Internal by default — [visibility]
13. Test module per production module — [structure]
14. Convention over configuration — [conventions]
15. Deterministic build output — [config]
16. Black Box Composition ready — [modular structure]

### Architecture Conventions:
- Enums everywhere (string serialization, never numbers)
- IEntity base → generic repos/controllers
- Factory pattern for test data
- JSON "as is" naming (no transformation)

Does this look right? Any changes?
```

Wait for user confirmation. Do NOT proceed without it.

### 4. PLAN — Create TDDAB Scaffolding Plan
**Update MB:** `@state::PLAN`

Create a TDDAB plan with `<mission>`, `<block>`, `<intro>`, `<red>`, `<success>` tags.

**CRITICAL: The plan must be LANGUAGE-SPECIFIC.** Use foundations file + TDDAB overlay for:
- Exact build/test commands (e.g. `dotnet test`, `pytest`, `go test ./...`)
- Exact project structure (e.g. `.slnx`, `package.json`, `go.mod`)
- Exact test framework and assertion patterns
- Exact config/DI/logging approach

**Decomposition strategy (bottom-up). ADAPT every block for the specific language:**

```
Block 01: Project skeleton + build config
  C#:    dotnet new slnx, dotnet new webapi, Directory.Build.props, CPM
  TS:    npm init, tsconfig.json, vitest.config.ts, package.json scripts
  Java:  gradle init, build.gradle.kts, settings.gradle.kts
  Go:    go mod init, cmd/main.go, internal/ structure
  Rust:  cargo init --lib, Cargo.toml workspace, clippy config
  Python: pyproject.toml, src/ layout, pytest config
  PHP:   composer init, Laravel structure
  RED: build with zero warnings using language build command

Block 02: Core module (interfaces, DTOs, error types)
  C#: records, Result<T>, IEntity    TS: interfaces, type guards
  Java: records, sealed interfaces   Go: structs, interfaces, errors
  Rust: structs, traits, thiserror   Python: dataclasses, Protocol
  RED: unit tests for DTOs + error types

Block 03: Configuration + validation
  C#: IOptions<T>, appsettings.json  TS: zod/joi, typed config
  Java: @ConfigurationProperties     Go: viper/envconfig
  Python: pydantic Settings           PHP: config/*.php
  RED: missing config throws, valid config loads

Block 04: Structured logging
  C#: Serilog    TS: pino/winston    Java: SLF4J+Logback
  Go: slog       Python: structlog   Rust: tracing
  RED: log output has timestamp, level, message

Block 05: DI registration + wiring
  C#: MS.Extensions.DI   TS: tsyringe   Java: Spring @Config
  Go: manual injection   Kotlin: Koin   PHP: service providers
  RED: all services resolve from container

Block 06: Error handling
  C#: middleware+ProblemDetails   TS: exception filter
  Java: @ControllerAdvice        Go: recovery middleware
  Python: exception handlers     PHP: Laravel handler
  RED: unhandled exception → structured error, correct HTTP status

Block 07: Test infrastructure
  C#: xUnit v3+AwesomeAssertions   TS: vitest   Java: JUnit5+AssertJ
  Go: testify   Rust: #[cfg(test)]   Python: pytest
  RED: EntityFactory creates valid entities, all tests pass

Block 08: Health/version endpoint (if API project)
  RED: GET /health → 200, GET /version → version string

Block 09: Dev files + CLAUDE.md
  .gitignore, README.md, .editorconfig, project CLAUDE.md from template
```

Adjust for project type:
- **Library** → skip blocks 05-06-08
- **Console/CLI** → skip 08, adapt 06
- **Full stack** → add frontend blocks
- **KMP** → Gradle KTS, version catalog, source sets in block 01

### 5. Create empty project directory + run j-setup
After plan is created:
1. Create the empty project directory (just `mkdir`, NO code) and **make it the working directory** — all following steps run INSIDE the new project, not in the parent folder
2. Run `j-setup` to configure j-settings.md for the new project. The directory is empty, so auto-detection finds nothing: answer j-setup's questions from the decisions made in steps 2-4 (language, structure, commands, ports)
3. Only AFTER j-settings.md exists: save the plan to `{@tasks}/01-scaffold/plan.md` (create the folder if needed)

### 6. STOP — Tell user what to do next
```
✅ Scaffolding plan created: {@tasks}/01-scaffold/plan.md

Next steps:
  1. Review the plan (optional: /j-review-plan)
  2. Start development: /j-develop

The plan has {N} TDDAB blocks. Each will be built test-first.
```

**DO NOT start coding. Wait for user to trigger j-develop.**

## Rules

- **Follow LISTEN → ANALYZE → PROPOSE → PLAN flow** — no shortcuts
- **STOP after PLAN** — development is triggered by the user via j-develop
- **All 16 foundations** — apply every single one, no exceptions
- **Architecture conventions** — enums, IEntity, generic repo/controller, factories, JSON "as is"
- **Language-specific plan** — use foundations file + TDDAB overlay, not generic
- **Ask if unsure** — use AskUserQuestion for any ambiguity

## What NOT to do

- Don't write project code — only the plan + empty directory + j-setup
- Don't skip PROPOSE — user must see and approve structure before planning
- Don't create generic blocks — every block must have language-specific details
- Don't start j-develop automatically — user triggers it
