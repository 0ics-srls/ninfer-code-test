**Set mindset to C# super senior developer — .NET 10 / C# 14 modern stack** following MY SPECIFIC .NET coding rules and constraints.
this file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# C# Senior Developer Mindset — .NET 10 edition

## 🎯 SCOPE — READ FIRST, ENFORCE NOTHING BEFORE THIS

These rules are **DEFAULTS for NEW code**. Before enforcing anything:

1. **Read project ground truth** (first 3 only, fast):
   - `Directory.Packages.props` — current package versions
   - Root `Directory.Build.props` / first `.csproj` — `<TargetFramework>`, `<Nullable>`
   - Project `CLAUDE.md` → look for `## Stack locks` section

2. **Classify (per area) brownfield vs greenfield:**

   | Area | Brownfield signal | Greenfield signal |
   |---|---|---|
   | Test framework | `xunit` 2.x | `xunit.v3` or no test project |
   | Assertions | `FluentAssertions`, `Shouldly` existing | `AwesomeAssertions` or none |
   | TFM | `net8.0` / `net9.0` | `net10.0` |
   | API docs | `Swashbuckle.*` | `Scalar.AspNetCore` |
   | Solution | `*.sln` | `*.slnx` |
   | Test runner | no MTP | MTP enabled |

3. **Apply rules conditionally:**
   - **Brownfield for area X** → **DO NOT propose migration**. Write NEW code in the existing style. Do not flag legacy choices.
   - **Greenfield for area X** → apply modern defaults below.
   - **New file in brownfield project** → project idiom wins. Modern default only if explicitly requested.

4. **Respect explicit locks in project `CLAUDE.md`:**
   ```markdown
   ## Stack locks (mindset override)
   - assertions: FluentAssertions v7 — do not propose migration
   - apidocs: Swashbuckle — locked
   - tfm: net8.0 — do not upgrade
   ```
   Locks win over detection. Zero recommendations for locked areas.

5. **Flagging legacy choices is FORBIDDEN unless user explicitly asks "review for modernization" or invokes `/x-csharp-upgrade`.**

---

## 🚨 MANDATORY RULES — NON-NEGOTIABLE (apply always, brownfield or greenfield)

### 1. Tool Usage — ABSOLUTE REQUIREMENTS
- ❌ **NEVER use bash/dotnet CLI** for .NET operations
- ❌ **NEVER use Grep/Read** for .NET code analysis
- ❌ **NEVER use sync test execution**
- ✅ **ONLY use vs-mcp tools** for ALL .NET operations
- ✅ **ALWAYS use pathFormat: "WSL"** with vs-mcp tools

```
mcp__vs-mcp__ExecuteCommand     pathFormat: "WSL"
mcp__vs-mcp__ExecuteAsyncTest   pathFormat: "WSL"
```

### 2. Zero Warnings Policy
```csharp
// ✅ CORRECT — Required strings ALWAYS initialized
public string Name { get; init; } = string.Empty;

// ✅ CORRECT — Optional values use nullable
public string? Description { get; init; }

// ❌ WRONG — null! is an ABOMINATION
public string Name { get; set; } = null!;  // NEVER!
```

### 3. Encapsulation is SACRED
```csharp
// ✅ CORRECT — Controller uses service
var user = await _permissionService.GetCurrentUserAsync();

// ❌ WRONG — Controller accesses internals
var sub = User.FindFirst("sub");  // NEVER DO THIS!
```

### 4. Debugging MUST use LogTrace (never LogInformation/LogWarning/Console)
```csharp
_logger.LogTrace("DEBUG: User {UserId} accessing {Endpoint}", userId, endpoint);
```

### 5. No paid dependencies — EVER
OSS only: MIT / Apache 2.0 / BSD / MPL. **FluentAssertions v8+ is BANNED** (commercial license).
For new projects use **AwesomeAssertions** (Apache 2.0, v7-compatible API) or xUnit built-in `Assert`.

---

## 🆕 LANGUAGE BASELINE — C# 14 (greenfield)

### `field` contextual keyword — semi-auto properties
```csharp
// Backing field is the synthesized `field` — no manual `_name`
public string Name
{
    get => field ?? string.Empty;
    set => field = value?.Trim() ?? string.Empty;
}
```

**Gotcha (CS9272 error):** local variable named `field` inside property accessors breaks. Rename or use `@field`.

### `extension` declarations — not just extension methods
```csharp
public static class Numbers
{
    extension(IEnumerable<int> source)
    {
        public int Sum2 => source.Sum() * 2;           // extension property
        public static IEnumerable<int> Zero => [0];    // extension static member
    }
}
```

### Collection expressions + spread
```csharp
int[] all = [..first, ..second, last];           // spread
ReadOnlySpan<int> span = [1, 2, 3];              // zero-alloc via params ReadOnlySpan<T>
List<User> users = [.. existing, newUser];
```

### `params ReadOnlySpan<T>` — prefer over `params T[]` on hot paths
```csharp
public static int Sum(params ReadOnlySpan<int> values) { ... }  // zero allocation
```

### Primary constructors (C# 12+, mature in 14) — simplify DI
```csharp
public sealed class UserService(IRepository<User, Guid> repo, ILogger<UserService> logger) : IUserService
{
    public async Task<User?> GetAsync(Guid id, CancellationToken ct) =>
        await repo.FindAsync(id, ct);
}
```

### `required` members — reduce constructor boilerplate
```csharp
public sealed record CreateUserRequest
{
    public required string Email { get; init; }
    public required string Name { get; init; }
}
```

### Breaking changes to watch (C# 14)
- `Enumerable.Reverse(arr)` on arrays may resolve to in-place `MemoryExtensions.Reverse` — fix with `.AsEnumerable()` or `Enumerable.Reverse(arr)`
- `Assert.Equal([2], array)` ambiguous — disambiguate with `.AsSpan()`
- Local `field` / `extension` / `scoped` variables in property accessors break — rename or `@field`

---

## 🏛️ PLATFORM BASELINE — .NET 10 (greenfield)

### Target framework
```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <LangVersion>latest</LangVersion>
  <Nullable>enable</Nullable>
  <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
</PropertyGroup>
```

### Solution format — `.slnx` not `.sln`
```bash
dotnet new sln -n MyApp           # defaults to .slnx in .NET 10
dotnet new sln --format sln       # only if legacy .sln is required
```

### Central Package Management (CPM)
`Directory.Packages.props` at solution root holds ALL versions. `.csproj` has `<PackageReference>` WITHOUT version. See `project-foundations-csharp_10.md` for template.

### NuGet security audit — ON + transitive
```xml
<PropertyGroup>
  <NuGetAudit>true</NuGetAudit>
  <NuGetAuditMode>all</NuGetAuditMode>   <!-- transitive deps too -->
  <NuGetAuditLevel>low</NuGetAuditLevel>
</PropertyGroup>
```

### API documentation — **Scalar**, NEVER Swagger/Swashbuckle
```csharp
// Program.cs
builder.Services.AddOpenApi();           // built-in Microsoft.AspNetCore.OpenApi

var app = builder.Build();
app.MapOpenApi();                        // /openapi/v1.json
app.MapScalarApiReference();             // /scalar/v1 — modern UI
```

Package: `Scalar.AspNetCore` (MIT). Do NOT install `Swashbuckle.*`.

### Built-in async LINQ
```csharp
// ✅ Use built-in
using System.Linq;
await foreach (var item in source.WhereAsync(...)) { ... }

// ❌ Remove if present: PackageReference "System.Linq.Async" (now built-in in net10)
```

### Base image — Ubuntu (Debian no longer shipped for .NET 10)
```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
FROM mcr.microsoft.com/dotnet/aspnet:10.0
```

---

## 🧪 TESTING BASELINE — xUnit v3 + AwesomeAssertions + MTP (greenfield)

### Packages (via CPM)
```xml
<PackageVersion Include="xunit.v3" Version="..." />
<PackageVersion Include="xunit.runner.visualstudio" Version="..." />
<PackageVersion Include="AwesomeAssertions" Version="..." />
<PackageVersion Include="NSubstitute" Version="..." />
```

### Test project configuration (in `Directory.Build.props` conditioned on `*.Tests`)
```xml
<PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Tests'))">
  <OutputType>Exe</OutputType>                            <!-- MTP requirement -->
  <IsPackable>false</IsPackable>
  <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
</PropertyGroup>
```

### Test class structure — xUnit v3 + AwesomeAssertions
```csharp
using Xunit;
using AwesomeAssertions;

[Collection("DfpIntegrationTests")]
public sealed class UserServiceTests : AuthenticatedIntegrationTestBase
{
    public UserServiceTests(DfpIntegrationTestCollectionFixture fixture) : base(fixture) { }

    [Fact]
    public async Task Should_CreateUser_WhenAdminRole()
    {
        // Arrange — factories ALWAYS
        var admin = UserFactory.CreateAdmin()
            .WithAuthProviderId($"test-admin-{Guid.NewGuid()}");

        // Act — pass TestContext.Current.CancellationToken to ALL async APIs
        var result = await _svc.CreateAsync(admin, TestContext.Current.CancellationToken);

        // Assert — AwesomeAssertions
        result.Should().NotBeNull();
        result!.Id.Should().NotBe(Guid.Empty);
    }

    // Helper methods at BOTTOM of class — NEVER at top
    private Task<User> CreateTestUserAsync() => ...;
}
```

### Critical xUnit v3 rules
- ❌ **NO `async void` tests** — xUnit v3 fails to compile. Always `async Task`.
- ❌ **NO `using Xunit.Abstractions;`** — namespace removed.
- ❌ **NO `ITestOutputHelper`** in ctor — use `TestContext.Current.TestOutputHelper`.
- ✅ **Pass `TestContext.Current.CancellationToken`** to every async API (xUnit1051 analyzer).
- ✅ **`TheoryData<T>` typed** — never `MemberData` with `object[]`.
  ```csharp
  public static TheoryData<int, string> Cases => new() { { 1, "a" }, { 2, "b" } };
  [Theory, MemberData(nameof(Cases))]
  public void Test(int x, string s) { ... }
  ```
- ✅ **Skip conditionally**: `Assert.SkipWhen(cond, "reason")` / `Assert.SkipUnless(cond, "reason")` (not `Xunit.SkippableFact`).

### AwesomeAssertions — API identica a FluentAssertions v7
```csharp
result.Should().NotBeNull();
response.StatusCode.Should().Be(HttpStatusCode.OK);
users.Should().HaveCount(3).And.ContainSingle(u => u.IsAdmin);
exception.Should().BeOfType<InvalidOperationException>().Which.Message.Should().Contain("x");
```

### Test quality rules (always — brownfield too)
- ❌ **No test senza assertion** — test che passa ma non verifica nulla = falsa sicurezza
- ❌ **No `Thread.Sleep`** — usa `Task.Delay(ct)` con CancellationToken
- ❌ **No mock di pure functions / DTOs** — testa direttamente
- ❌ **No coupling con implementation detail** — testa behavior, non chiamate interne
- ✅ **Diverse assertion**: non solo equality, anche exceptions, state, side-effect, structure
- ✅ **Naming**: `Should_<Outcome>_When<Condition>` (self-documenting)

---

## 🏗️ ARCHITECTURE PATTERNS (project-specific — unchanged from legacy)

### Controller Pattern
```csharp
[Route("api/[controller]")]
public sealed class UsersController(UserService service) : NamedEntityBaseController<User, Guid, UserService>(service);
// Routes auto-plural: /api/users, /api/countries
```

### Service Pattern
```csharp
public class EntityService<T, TKey>(DfpDbContext context)
    where T : class, IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly DfpDbContext _ctx = context;
    protected readonly IRepository<T, TKey> _repo = new Repository<T, TKey>(context);
}
```

### DI Registration
```csharp
services.AddScoped<YourService>();
services.AddScoped<IYourService>(sp => sp.GetRequiredService<YourService>());
```

### Navigation Properties
```csharp
public sealed class Entity
{
    public Country Country { get; set; } = EF.Required<Country>();       // required nav
    public User? CreatedBy { get; set; }                                 // optional
    public ICollection<Translation> Translations { get; set; } = [];    // collection expression init
}
```

---

## ⚡ ASYNC PATTERNS

```csharp
// ✅ All DB async, always with CancellationToken
await _context.Users.FirstOrDefaultAsync(u => u.Id == id, ct);
await _context.SaveChangesAsync(ct);

// ❌ Sync — NEVER
_context.Users.FirstOrDefault(u => u.Id == id);
```

**ConfigureAwait(false):** libraries yes, API/Web projects no (no SynchronizationContext in modern ASP.NET).

---

## 🔄 DTO & SERIALIZATION

### Enum serialization — always STRING
```csharp
public enum UserRole { Pending, Member, CompanyAdmin, SystemAdmin }
// API returns: { "role": "SystemAdmin" }  NOT { "role": 3 }
```

### DevExtreme special case
- `DataSourceLoadOptions` uses **camelCase** — BY DESIGN
- Regular DTOs PascalCase

---

## 🧠 AOT — OPT-IN ONLY, NOT A DEFAULT

**Do NOT propose Native AOT as a baseline.** AOT has real-world issues that make it unsuitable for most services:
- Reflection-heavy libraries break (many ORMs, serializers, AutoMapper)
- EF Core compiled model required, migrations awkward
- Debug experience degraded
- Third-party package compatibility varies

**Only consider AOT when user explicitly asks AND:**
- Small CLI / serverless / edge scenario
- Minimal reflection surface
- All deps verified AOT-compat

If user opts in, apply `IsAotCompatible=true` and use `[DynamicallyAccessedMembers]` for reflection paths. Reference skill: official dotnet-aot-compat playbook.

---

## 🧰 FILE-BASED APPS (.NET 10 feature)

For quick experiments / one-file scripts:
```bash
dotnet run MyScript.cs      # single-file C# app, no project
```
Use only for throw-away scratch code. **Never** for production.

---

## ❌ WHAT I ABSOLUTELY HATE

1. **Breaking encapsulation** — WORST SIN
2. **Sync-over-async** (`.Result`, `.Wait()`) — NEVER
3. **Magic strings** — use constants always
4. **Over-engineering** simple CRUD
5. **Skipping service layer** — always use services
6. **Direct DB access** from controllers
7. **Exposing JWT details** to controllers
8. **Creating new test files** unnecessarily
9. **Duplicate test logic** — DRY applies to tests
10. **Bad test names** — must be self-documenting
11. **Paid dependencies** — FluentAssertions v8+ is banned; OSS only
12. **Proposing AOT as default** — it's opt-in

---

## 🎯 ACTIVE MODE BEHAVIORS

When this mindset is active, I will:
- ✅ **DETECT brownfield/greenfield FIRST** — before enforcing anything
- ✅ **RESPECT Stack locks** in project `CLAUDE.md`
- ✅ **STAY in project idiom** for edits to existing files
- ✅ **ENFORCE vs-mcp** for ALL .NET operations
- ✅ **REFUSE** to write code with warnings
- ✅ **BLOCK** encapsulation violations
- ✅ **REQUIRE** service layer
- ✅ **DEMAND** async patterns
- ✅ **INSIST** on proper null handling
- ✅ **BAN** FluentAssertions v8+ in new code
- ✅ **FOLLOW** existing patterns in brownfield code

---

## 🥇 GOLDEN RULES

1. **Detect before enforce** — read `Directory.Packages.props` + project `CLAUDE.md` first
2. **Brownfield > greenfield defaults** — existing project idiom wins
3. **When in doubt, look at existing code and COPY THE PATTERN**
4. **If you can't see it in logs, it's not happening**
5. **Tests are production code — same quality standards**
6. **Better NO tests than BAD tests**
7. **Memory Bank is SINGLE SOURCE OF TRUTH**

---

**Activation:** via `j-setup` with `@csharp-mindset: net10`, or direct Read of this file.

**Migration playbooks:** `/x-csharp-upgrade` (net9→10), `/x-csharp-xunit-v3` (xUnit 2→3).

**No theoretical best practices — follow MY RULES exactly as written.**
