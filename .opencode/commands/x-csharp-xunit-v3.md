---
description: >-
  Playbook — migrate .NET test projects from xUnit.net v2 to xUnit.net v3 (+
  FluentAssertions → AwesomeAssertions swap)
---
# Playbook: xUnit v2 → xUnit v3 Migration

Condensed from the official dotnet/skills `migrate-xunit-to-xunit-v3`, with an added **FluentAssertions → AwesomeAssertions** swap step (project policy: no paid deps).

## When to use
- Test projects reference `xunit` (v2) and need to move to `xunit.v3`
- Team wants MTP (Microsoft.Testing.Platform) runner support

## When NOT to use
- Migrating between frameworks (MSTest/NUnit → xUnit) — different effort
- Projects already on `xunit.v3`
- TFM below `net8.0` (xUnit v3 minimum — upgrade TFM first)
- Non-SDK-style projects (convert to SDK-style first)

---

## Workflow

Commit after each major step — separate project changes from code changes.

### Step 1 — Identify xUnit v2 projects

Search for v2 package references in `.csproj`, `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props`:
- `xunit`, `xunit.abstractions`, `xunit.assert`, `xunit.core`
- `xunit.extensibility.core`, `xunit.extensibility.execution`
- `xunit.runner.visualstudio`

### Step 2 — Verify compatibility (STOP if not met)

- **TFM**: xUnit v3 requires `net8.0+` or `netfx 4.7.2+`. Test library projects also support `netstandard2.0`. If any test project targets below, STOP and tell user to upgrade TFM first.
- **SDK-style**: xUnit v3 only supports SDK-style projects. If non-SDK found, STOP and tell user to convert first.

### Step 3 — Baseline

```bash
dotnet test > baseline.txt 2>&1
```

Record pass/fail count. No `--no-restore` / `--no-build` flags.

### Step 4 — Update package references

Mapping:
| v2 | v3 |
|---|---|
| `xunit` | `xunit.v3` |
| `xunit.abstractions` | **REMOVE** (namespace eliminated) |
| `xunit.assert` | `xunit.v3.assert` |
| `xunit.core` | `xunit.v3.core` |
| `xunit.extensibility.core` + `xunit.extensibility.execution` | `xunit.v3.extensibility.core` (merged — single ref) |
| `xunit.runner.visualstudio` | `xunit.runner.visualstudio` (latest) |

Update versions to latest stable in `Directory.Packages.props`.

**Commit:** "chore: bump xunit to v3 package names"

### Step 5 — `OutputType=Exe` (MTP requirement)

xUnit v3 test projects must be executable. Add to `Directory.Build.props` with condition:

```xml
<PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Tests'))">
  <OutputType>Exe</OutputType>
</PropertyGroup>
```

Or per-project if test projects don't share a naming pattern.

### Step 6 — Remove `Xunit.Abstractions` usings

Grep for `using Xunit.Abstractions;` and delete the lines. Namespace is gone in v3.

Replace any `ITestOutputHelper` constructor parameter with `TestContext.Current.TestOutputHelper`:
```csharp
// v2
public MyTests(ITestOutputHelper output) { _output = output; }

// v3
private ITestOutputHelper Output => TestContext.Current.TestOutputHelper!;
```

### Step 7 — `async void` → `async Task`

xUnit v3 fails to compile `async void` test methods. Find every `[Fact]` / `[Theory]` with `async void` and change to `async Task`.

### Step 8 — Attributes now take `typeof()`

Convert string-based attribute params to `typeof()`:
```csharp
// v2
[assembly: CollectionBehavior("MyNamespace.Factory", "MyAssembly")]

// v3
[assembly: CollectionBehavior(typeof(MyNamespace.Factory))]
```

Affected: `CollectionBehaviorAttribute`, `TestCaseOrdererAttribute`, `TestCollectionOrdererAttribute`, `TestFrameworkAttribute`.

### Step 9 — Custom `FactAttribute`/`TheoryAttribute` subclasses

Must now accept source info:
```csharp
internal sealed class MyFactAttribute : FactAttribute
{
    public MyFactAttribute(
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int sourceLineNumber = -1
    ) : base(sourceFilePath, sourceLineNumber) { }
}
```

### Step 10 — Custom `BeforeAfterTestAttribute` signatures

`Before`/`After` now take `IXunitTest test` as second parameter:
```csharp
public override void Before(MethodInfo methodUnderTest, IXunitTest test) { ... }
public override void After(MethodInfo methodUnderTest, IXunitTest test) { ... }
```

### Step 11 — xUnit1051 — pass `TestContext.Current.CancellationToken`

New analyzer: every async API call that accepts a `CancellationToken` must receive `TestContext.Current.CancellationToken`:
```csharp
// BEFORE
await _svc.GetAsync(id);

// AFTER
await _svc.GetAsync(id, TestContext.Current.CancellationToken);
```

Also apply to `Task.Delay`, `HttpClient.GetAsync`, DB queries, etc.

### Step 12 — Test platform: VSTest vs MTP

Recommend MTP combo with xUnit v3 — it's native support and faster.

**Option A — Switch to MTP (recommended):**
```xml
<!-- Directory.Build.props (unconditional PropertyGroup) -->
<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
```

**Option B — Keep VSTest (short-term):**
```xml
<IsTestingPlatformApplication>false</IsTestingPlatformApplication>
```

### Step 13 — Replace `Xunit.SkippableFact`

Package removal required. Map:
- `[SkippableFact]` → `[Fact]`
- `[SkippableTheory]` → `[Theory]`
- `Skip.If(cond, msg)` → `Assert.SkipWhen(cond, msg)`
- `Skip.IfNot(cond, msg)` → `Assert.SkipUnless(cond, msg)`

Remove `Xunit.SkippableFact` from `Directory.Packages.props`.

### Step 14 — Update `Xunit.Combinatorial` + `Xunit.StaFact`

- `Xunit.Combinatorial` 1.x → 2.x
- `Xunit.StaFact` 1.x → 3.x

### Step 15 — **PROJECT POLICY: FluentAssertions → AwesomeAssertions**

This is our additional step (not in the official skill): swap banned paid dep.

#### Package swap (in `Directory.Packages.props`)
```xml
<!-- REMOVE -->
<PackageVersion Include="FluentAssertions" Version="..." />

<!-- ADD -->
<PackageVersion Include="AwesomeAssertions" Version="..." />
```

#### Usings update
```csharp
// BEFORE
using FluentAssertions;

// AFTER
using AwesomeAssertions;
```

API is **compatible with FA v7** — no code changes needed. If any `FluentAssertions.*` deeper namespace imports exist, replace with `AwesomeAssertions.*` equivalents.

**Commit:** "chore: swap FluentAssertions for AwesomeAssertions (OSS)"

### Step 16 — Build + fix residuals

```bash
dotnet build --no-incremental 2>&1 | tee build.log
```

Fix straightforward errors iteratively. Refer to:
- <https://xunit.net/docs/getting-started/v3/migration>
- <https://xunit.net/docs/getting-started/v3/migration-extensibility>

It's acceptable to leave some residual errors — report them to user for manual review. Don't guess.

### Step 17 — Run + compare

```bash
dotnet test > after.txt 2>&1
diff baseline.txt after.txt
```

All previously-passing tests must still pass. New analyzer warnings are OK if addressed.

**Final commit:** "chore: complete xunit v2→v3 migration"

---

## Notes
- If the project is also migrating to .NET 10, do that **first** via `/x-csharp-upgrade`, then this playbook.
- MTP adoption is strongly recommended but not mandatory — if team is not ready, keep VSTest (Step 12 Option B) and revisit later.
- Do NOT propose AOT during this migration.
