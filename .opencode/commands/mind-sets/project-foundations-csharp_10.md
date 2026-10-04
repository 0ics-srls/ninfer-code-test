---
description: >-
  Project Foundations — C#/.NET 10 implementation (modern stack: .slnx, CPM,
  Scalar, OTel, xUnit v3, AwesomeAssertions)
---
# Project Foundations — C#/.NET 10

$include: ./project-foundations.md

## 🎯 SCOPE

Foundations for **NEW projects** (greenfield). For existing projects, see `project-foundations-csharp.md` and respect existing stack. `## Stack locks` in project `CLAUDE.md` override everything below.

---

## 1. Target Framework & Language

```xml
<!-- Directory.Build.props -->
<Project>
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
    <LangVersion>latest</LangVersion>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <TreatWarningsAsErrors>true</TreatWarningsAsErrors>
    <AnalysisLevel>latest-recommended</AnalysisLevel>
    <EnforceCodeStyleInBuild>true</EnforceCodeStyleInBuild>
  </PropertyGroup>

  <!-- Reproducible build metadata -->
  <PropertyGroup>
    <Deterministic>true</Deterministic>
    <ContinuousIntegrationBuild Condition="'$(CI)' == 'true'">true</ContinuousIntegrationBuild>
    <PublishRepositoryUrl>true</PublishRepositoryUrl>
    <EmbedUntrackedSources>true</EmbedUntrackedSources>
  </PropertyGroup>

  <!-- NuGet security audit — ON, transitive included -->
  <PropertyGroup>
    <NuGetAudit>true</NuGetAudit>
    <NuGetAuditMode>all</NuGetAuditMode>
    <NuGetAuditLevel>low</NuGetAuditLevel>
  </PropertyGroup>
</Project>
```

C# 14 features: `field` keyword, `extension` blocks, collection spread, primary constructors, `required`, `params ReadOnlySpan<T>`.

---

## 2. Solution Format — `.slnx`

```bash
dotnet new sln -n MyApp          # defaults to .slnx in .NET 10
```

Use `.slnx` (XML) — shorter, readable, merge-friendly. Never `.sln` in new projects.

---

## 3. Central Package Management (CPM)

```xml
<!-- Directory.Packages.props (solution root) -->
<Project>
  <PropertyGroup>
    <ManagePackageVersionsCentrally>true</ManagePackageVersionsCentrally>
    <CentralPackageTransitivePinningEnabled>true</CentralPackageTransitivePinningEnabled>
  </PropertyGroup>

  <ItemGroup>
    <!-- Runtime -->
    <PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.0" />
    <PackageVersion Include="Microsoft.Extensions.Hosting" Version="10.0.0" />

    <!-- API docs (NO Swagger/Swashbuckle) -->
    <PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.0" />
    <PackageVersion Include="Scalar.AspNetCore" Version="2.x" />

    <!-- Observability -->
    <PackageVersion Include="OpenTelemetry.Extensions.Hosting" Version="1.x" />
    <PackageVersion Include="OpenTelemetry.Exporter.OpenTelemetryProtocol" Version="1.x" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.AspNetCore" Version="1.x" />
    <PackageVersion Include="OpenTelemetry.Instrumentation.Http" Version="1.x" />

    <!-- Testing (xUnit v3 + AwesomeAssertions + MTP) -->
    <PackageVersion Include="xunit.v3" Version="..." />
    <PackageVersion Include="xunit.runner.visualstudio" Version="..." />
    <PackageVersion Include="AwesomeAssertions" Version="..." />
    <PackageVersion Include="NSubstitute" Version="5.x" />
  </ItemGroup>
</Project>
```

`.csproj` references packages WITHOUT version:
```xml
<PackageReference Include="Microsoft.Extensions.Logging.Abstractions" />
```

---

## 4. `Directory.Build.props` vs `Directory.Build.targets` — critical

**`.props` imported BEFORE SDK — for defaults.** `.targets` imported AFTER SDK — for late-bound overrides.

Evaluation order: `Directory.Build.props → SDK .props → .csproj → SDK .targets → Directory.Build.targets`

**⚠️ Gotcha:** `$(TargetFramework)` in `.props` is EMPTY for single-target projects (SDK hasn't resolved it yet). Conditions on `$(TargetFramework)` must live in `.targets`, not `.props`.

```xml
<!-- Directory.Build.targets — late-bound conditions work here -->
<Project>
  <PropertyGroup Condition="'$(TargetFramework)' == 'net10.0'">
    <DefineConstants>$(DefineConstants);NET10</DefineConstants>
  </PropertyGroup>

  <!-- Test project conditioning — OutputType must be Exe for MTP -->
  <PropertyGroup Condition="$(MSBuildProjectName.EndsWith('.Tests'))">
    <OutputType>Exe</OutputType>
    <IsPackable>false</IsPackable>
    <UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>
  </PropertyGroup>
</Project>
```

---

## 5. API Documentation — Scalar (NOT Swagger)

```csharp
// Program.cs
var builder = WebApplication.CreateBuilder(args);
builder.Services.AddOpenApi();           // built-in .NET 10
builder.Services.AddControllers();

var app = builder.Build();
app.MapOpenApi();                        // /openapi/v1.json
app.MapScalarApiReference();             // /scalar/v1 — modern UI
app.MapControllers();
app.Run();
```

**Never install** `Swashbuckle.AspNetCore` / Swagger UI in new projects.

---

## 6. OpenTelemetry Baseline (observability)

```csharp
// Program.cs
builder.Services.AddOpenTelemetry()
    .ConfigureResource(r => r.AddService(serviceName: "MyApp", serviceVersion: "1.0.0"))
    .WithTracing(t => t
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddOtlpExporter())              // OTLP — backend-neutral
    .WithMetrics(m => m
        .AddAspNetCoreInstrumentation()
        .AddHttpClientInstrumentation()
        .AddRuntimeInstrumentation()
        .AddOtlpExporter());
```

Export OTLP to Aspire dashboard, Jaeger (accepts OTLP natively), or any collector.

---

## 7. Zero-Warnings & Versioning (inherited from legacy)

```csharp
public string Name { get; init; } = string.Empty;   // never null!
public string? Description { get; init; }           // nullable when legit
```

Versioning via single-source `version.build` (unchanged):
```xml
<_BuildNumber>$([System.IO.File]::ReadAllText('$(MSBuildThisFileDirectory)version.build').Trim())</_BuildNumber>
<Version>1.0.$(_BuildNumber)</Version>
```

---

## 8. Structured Error Handling

```csharp
public sealed record AppError(AppErrorCode Code, string Message,
    IReadOnlyList<string>? Candidates = null, string? Suggestion = null);

public enum AppErrorCode
{
    NotFound, InvalidParams, Unauthorized,
    Timeout, NotSupported, InternalError
}
```

---

## 9. Testing — xUnit v3 + AwesomeAssertions + MTP

**Package refs in `Directory.Packages.props`** (see §3). **MTP config** in `Directory.Build.targets` (see §4).

### `xunit.runner.json` (per test project or shared)
```json
{
  "diagnosticMessages": false,
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true
}
```

### Test filter — MTP syntax (xUnit v3)
```bash
dotnet run --project MyApp.Tests -- --filter-class "MyApp.Tests.UserServiceTests"
dotnet run --project MyApp.Tests -- --filter-method "Should_Create_*"
```

### Category split — replace `.runsettings` with traits
```csharp
[Trait("Category", "Integration")]
public sealed class EfCoreIntegrationTests { ... }
```

```bash
# Fast
dotnet run --project MyApp.Tests -- --filter-not-trait "Category=Integration"
# Integration
dotnet run --project MyApp.Tests -- --filter-trait "Category=Integration"
```

---

## 10. Immutable DTOs — `record` with `required`

```csharp
public sealed record UserDto
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public DateTime CreatedAt { get; init; }
}

public sealed record ApiResponse<T>(T Data, int TotalCount);

// 'with' for derived copies
var updated = user with { Email = "new@email.com" };
```

Collections use `IReadOnlyList<T>` / `IReadOnlyDictionary<TK,TV>`.

---

## 11. DI — per-library extension methods

```csharp
public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAppCore(this IServiceCollection services)
    {
        services.AddSingleton<IMyService, MyService>();
        services.AddScoped<IRepository, Repository>();
        return services;
    }
}

// Program.cs — composition root readable in 10 seconds
builder.Services.AddAppCore();
builder.Services.AddAppInfrastructure(builder.Configuration);
```

---

## 12. Options with validation (fail-fast)

```csharp
services.AddOptions<AppOptions>()
    .Bind(configuration.GetSection("App"))
    .ValidateDataAnnotations()
    .ValidateOnStart();
```

Never read config files directly. `IOptions<T>` / `IOptionsMonitor<T>` via DI only.

---

## 13. Structured Logging

```csharp
// Program.cs — Serilog as implementation
builder.Host.UseSerilog((context, config) =>
    config.ReadFrom.Configuration(context.Configuration));
```

Libraries use `ILogger<T>` — **never** Serilog types directly. Only the host references Serilog. Tests use `NullLogger<T>.Instance`.

---

## 14. Zero-Dependency Core + Interface-First + Internal Sealed

```
MyApp.Core → zero NuGet deps, only Microsoft.Extensions.*.Abstractions
```

```csharp
public interface IUserService
{
    Task<UserDto?> GetByIdAsync(int id, CancellationToken ct = default);
}

internal sealed class UserService(IRepository<User> repo) : IUserService { ... }
```

Grant test access:
```xml
<ItemGroup>
  <InternalsVisibleTo Include="$(MSBuildProjectName).Tests" />
</ItemGroup>
```

---

## 15. Test Module per Production Module

```
MyApp.Core       → MyApp.Core.Tests
MyApp.Server     → MyApp.Server.Tests
MyApp.Plugin.Foo → MyApp.Plugin.Foo.Tests
```

---

## 16. Deterministic Build Output

```xml
<PropertyGroup Condition="$([MSBuild]::IsOSPlatform('Linux'))">
  <BaseOutputPath>$(HOME)/app-build/bin/$(MSBuildProjectName)/</BaseOutputPath>
  <BaseIntermediateOutputPath>$(HOME)/app-build/obj/$(MSBuildProjectName)/</BaseIntermediateOutputPath>
</PropertyGroup>
```

---

## 17. Docker — Ubuntu base (.NET 10 default)

```dockerfile
FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY . .
RUN dotnet publish ./src/MyApp -c Release -o /app

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /app .
ENTRYPOINT ["dotnet", "MyApp.dll"]
```

Debian images are no longer shipped for .NET 10.

---

## 18. Black Box Composition

```csharp
public interface IOrderProcessor
{
    Task<OrderResult> ProcessAsync(OrderRequest request, CancellationToken ct = default);
}

internal sealed class OrderProcessor(/* ctor deps */) : IOrderProcessor { ... }
```

`public interface` + `public record` DTOs in Core. `internal sealed` implementation.

---

## Verification Commands (manual terminal only — not for agent use)

```bash
# Confirm modern stack
grep -r "TargetFramework.*net10" Directory.Build.props
grep -r "ManagePackageVersionsCentrally" Directory.Packages.props
test -f *.slnx && echo "OK: .slnx" || echo "FAIL: use .slnx"
grep -rL "Swashbuckle" --include="*.csproj" && echo "OK: no Swagger"
grep -r "Scalar.AspNetCore" Directory.Packages.props
grep -r "xunit.v3" Directory.Packages.props
grep -r "AwesomeAssertions" Directory.Packages.props
grep -rL "FluentAssertions" --include="*.csproj" && echo "OK: no FA"

# Build clean + audit
dotnet build --no-incremental -warnaserror
dotnet list package --vulnerable --include-transitive
```

When working through an agent, prefer `build-agent` / `test-agent` over these raw commands.
