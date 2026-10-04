# CLAUDE.md — my-app Project Guide

## Stack
- Backend: .NET 10 (ASP.NET Core, EF Core, SQLite, Serilog, Scalar OpenAPI)
- Frontend: Angular 21 + PrimeNG 21 + @primeuix/themes 2.x
- Tests: xUnit v3 + AwesomeAssertions + NSubstitute
- Connection: OpenAPI spec → ng-openapi-gen → typed Angular HttpClient client

## App Surface
- `/todos` — TodosView: lazy `p-table`, search/status/date filters, stats card
- `/week` — WeekView: 8-col Mon–Sun + Undated card grid, drag&drop between days
- `GET /api/Todos/week` — no params; server computes current Mon–Sun, returns `{ days[7], undated[] }`

## Version Locks (do not change without re-checking peers)
- Angular: 21.x (NOT 22 — PrimeNG 21 requires @angular/core ^21.0.7)
- PrimeNG: 21.1.9
- @primeuix/themes: 2.0.3 (NOT deprecated @primeng/themes)
- EF Core SQLite: 10.0.9
- xunit.v3: 3.2.2
- ng-openapi-gen: 1.0.5
- eslint stack: angular-eslint + typescript-eslint (dev-only, 2026-09)
- e2e: Playwright (web/e2e, npm run e2e --prefix web)

## Angular v21 Caveats
- OnPush is NOT default → set ChangeDetectionStrategy.OnPush explicitly
- No @Service decorator → use @Injectable({ providedIn: 'root' })
- Use Reactive Forms (Signal Forms not stable in v21)
- standalone is default — do NOT set standalone: true
- Use inject() function, native control flow @if/@for/@switch

## Build Commands
- Backend: `dotnet build` (zero warnings enforced)
- Backend test: `dotnet run --project tests/MyApp.Server.Tests`
- Frontend: `cd web && npm run build`
- Gen API: `cd web && npm run gen-api` (backend must be running on :5000)

## Code Navigation
- Local: lsai (compiler-grade symbol search)
- External libs: xmp4 (SCIP-backed for third-party libraries)
- PrimeNG docs: primeng MCP

## Conventions
- Enums: string JSON serialization (JsonStringEnumConverter)
- JSON: camelCase property names (ASP.NET Core web defaults, no override)
- Entities: sealed record implementing IEntity
- Repositories: generic IRepository<T>, internal sealed implementation
- Test data: TodoFactory.Create() — never new directly in tests
- Tests: [Trait("Category","Integration")] on DB-touching tests
