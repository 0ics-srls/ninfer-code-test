---
description: >-
  Playbook — migrate a C#/.NET project from net9.0 to net10.0 (C# 14 breaking
  changes, CPM, AsyncEnumerable, SYSLIB0058-62)
---
# Playbook: .NET 9 → .NET 10 Migration

Condensed from the official dotnet/skills `migrate-dotnet9-to-dotnet10` (excludes AOT per project policy).

## When to use
- Upgrading `<TargetFramework>` from `net9.0` to `net10.0`
- Resolving build errors / new warnings after `.NET 10 SDK` install
- Adapting to C# 14 + ASP.NET Core 10 + EF Core 10 behavioral changes

## When NOT to use
- Projects already on `net10.0` and clean build
- Upgrading from `net8.0` or earlier — run a `8→9` step first
- .NET Framework migrations
- Greenfield projects (start directly from `mind-sets/csharp-senior_10.md` + `mind-sets/project-foundations-csharp_10.md` via j-setup `@csharp-mindset: net10`)

---

## Workflow

Commit at each logical boundary (Step 2, 3, 4, 5). Each step reviewable and bisectable.

### Step 1 — Assess

1. Read `global.json` — confirm/bump to `10.0.100+`
2. Read first `.csproj` / `Directory.Build.props` — note current `<TargetFramework>`
3. Read `Directory.Packages.props` — note Microsoft.* package versions
4. Confirm SDK installed: `dotnet --version` must report `10.0.x`
5. Baseline clean build: `dotnet build --no-incremental` — record pre-existing warnings
6. Baseline tests: `dotnet test` — record pass/fail counts

### Step 2 — Bump TFM + packages

```xml
<!-- Directory.Build.props (or .csproj) -->
<TargetFramework>net10.0</TargetFramework>
```

For multi-target, add `net10.0` to `<TargetFrameworks>`.

Update ALL `Microsoft.*` package versions to `10.0.x` in `Directory.Packages.props`:
- `Microsoft.Extensions.*`
- `Microsoft.AspNetCore.*`
- `Microsoft.EntityFrameworkCore.*`

Run `dotnet restore`. Watch for:
- **NU1510**: direct refs pruned (package now in shared framework) — remove the explicit `<PackageReference>`
- **`PackageReference` without `Version`** is now an error (unless CPM active)
- **NuGet transitive audit**: review new vulnerability warnings

**Commit:** "chore: bump to net10 TFM + package versions"

### Step 3 — Fix C# 14 compiler breaking changes

Run `dotnet build`. Common errors and fixes:

#### `field` contextual keyword (CS9272 error, CS9258 warning)
```csharp
// BREAKS
public string Name { get { int field = 0; return ""; } }     // CS9272 error

// FIX (rename or escape)
public string Name { get { int fieldValue = 0; return ""; } }
```

#### `extension` contextual keyword
```csharp
class extension { }           // BREAKS — rename or @extension
```

#### Span overload resolution
```csharp
// Enumerable.Reverse on array now resolves to in-place MemoryExtensions.Reverse
var reversed = arr.Reverse();              // BREAKS
var reversed = Enumerable.Reverse(arr);    // FIX — explicit static
var reversed = arr.AsEnumerable().Reverse(); // FIX — AsEnumerable

// Assert.Equal([2], arr) ambiguous
Assert.Equal([2], arr.AsSpan());           // FIX — disambiguate
```

#### `System.Linq.Async` conflicts
```xml
<!-- Remove the community package (built-in System.Linq.AsyncEnumerable available in net10) -->
<PackageReference Remove="System.Linq.Async" />
```
Or upgrade to `System.Linq.Async` v7.0.0+ which is compatible.

### Step 4 — Resolve obsoletions SYSLIB0058–SYSLIB0062

| Code | Replace | With |
|---|---|---|
| `SYSLIB0058` | `SslStream.KeyExchangeAlgorithm` / `CipherAlgorithm` / `HashAlgorithm` | `NegotiatedCipherSuite` (preserve any weak-cipher rejection logic) |
| `SYSLIB0059` | `SystemEvents.EventsThreadShutdown` | `AppDomain.ProcessExit` |
| `SYSLIB0060` | `Rfc2898DeriveBytes` ctor | `Rfc2898DeriveBytes.Pbkdf2(pwd, salt, iter, hash, outLen)` |
| `SYSLIB0061` | `Queryable.MaxBy`/`MinBy(IComparer<TSource>)` | Overload with `IComparer<TKey>` |
| `SYSLIB0062` | `XsltSettings.EnableScript` | Remove usage |

### Step 5 — ASP.NET Core / EF Core source changes (if applicable)

#### ASP.NET Core 10
- `WebHostBuilder` / `IWebHost` / `WebHost` obsolete → `Host.CreateDefaultBuilder` or `WebApplication.CreateBuilder`
- `WithOpenApi` extension deprecated
- `IActionContextAccessor` / `ActionContextAccessor` obsolete
- Razor runtime compilation obsolete
- **OpenAPI v2 breaking**: `OpenApiString`/`OpenApiAny` removed (use `JsonNode`); `OpenApiSecurityScheme.Reference` → `OpenApiSecuritySchemeReference`; `OpenApiSchema.Nullable` removed

#### EF Core 10
- `ExecuteUpdateAsync` now accepts regular lambda (rewrite expression-tree construction code)
- `.Contains()` on collections now uses multiple scalar params by default (performance impact for large collections) — mitigation: `UseParameterizedCollectionMode(ParameterTranslationMode.Parameter)`
- Azure SQL (compat level ≥170): `json` data type replaces `nvarchar(max)` — review migrations
- Sqlite `DateTimeOffset`: `GetDateTimeOffset` now assumes UTC (was local). Mitigation: `AppContext.SetSwitch("Microsoft.Data.Sqlite.Pre10TimeZoneHandling", true)` as temp

**Commit:** "fix: resolve net10 breaking changes (C# 14, SYSLIB0058-62, ASP.NET/EF obsoletions)"

### Step 6 — Behavioral changes (check runtime behavior)

1. **SIGTERM**: runtime no longer registers default SIGTERM handler. Console apps without Generic Host must register explicitly:
   ```csharp
   PosixSignalRegistration.Create(PosixSignal.SIGTERM, _ => Environment.Exit(0));
   ```
   ASP.NET Core / Generic Host apps unaffected.
2. **`BackgroundService.ExecuteAsync`** runs entirely on background thread — sync startup code before first `await` no longer blocks. Move to `StartAsync` / ctor if ordering matters.
3. **Configuration null preserved**: JSON `null` no longer becomes empty string.
4. **`System.Text.Json` polymorphism**: properties conflicting with `$type`/`$id`/`$ref` metadata now throw `InvalidOperationException`. Add `[JsonIgnore]`.
5. **HTTP streaming** enabled by default in browser clients.
6. **`Uri` length limits removed** — add explicit validation if `Uri` was a length gate for untrusted input.
7. **`XmlSerializer` no longer ignores `[Obsolete]` properties** — add `[XmlIgnore]` to obsolete properties holding sensitive data.

### Step 7 — Infrastructure

#### Dockerfile (Debian → Ubuntu)
```dockerfile
# Before
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:9.0
# After
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:10.0
```

#### global.json
```json
{ "sdk": { "version": "10.0.100" } }
```

#### Environment variables renamed
- `CLR_OPENSSL_VERSION_OVERRIDE` → `DOTNET_OPENSSL_VERSION_OVERRIDE`
- `CLR_ICU_VERSION_OVERRIDE` → `DOTNET_ICU_VERSION_OVERRIDE`
- `NUGET_ENABLE_ENHANCED_HTTP_RETRY` removed

#### OpenSSL
OpenSSL 1.1.1+ now required on Unix. OpenSSL cryptographic primitives no longer supported on macOS.

#### Solution format
If scripts invoke `dotnet new sln`, note it now defaults to **SLNX**. Pass `--format sln` if legacy is required — but prefer migration to `.slnx`.

**Commit:** "chore: update infra for net10 (Ubuntu base, global.json, env vars)"

### Step 8 — Security review

Before closing the migration, verify:
- TLS cipher validation logic preserved after `SslStream` API change
- `[XmlIgnore]` on obsolete props holding sensitive data
- `Uri` length validation still in place for untrusted input
- Exception handlers emit security-relevant telemetry before `return true`
- `dotnet list package --vulnerable --include-transitive` → no unresolved CVE

### Step 9 — Verify

- Clean build + all tests pass
- Container image builds + runs
- Smoke test critical paths: SIGTERM handling, background services startup, config binding with nulls, Sqlite date/time, JSON serialization, EF `.Contains()` on collections

**Final commit:** "chore: complete net9→net10 migration"

---

## Notes

- AOT migration is **NOT** covered by this playbook (per project policy). Apply separately on explicit request only.
- `.slnx` conversion is separate — not triggered by this playbook unless user asks.
- xUnit v2→v3 is a separate playbook — see `/x-csharp-xunit-v3`.
- CPM adoption, Scalar migration, OTel adoption: separate ad-hoc tasks (see foundations).
