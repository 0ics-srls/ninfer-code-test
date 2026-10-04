# Tech Context
§MBEL:5.0

[STACK]
Backend::.NET 10{SDK ≥10.0.109,ASP.NET Core 10.0.9}
Frontend::Angular 21.2+TypeScript 5.9.2+PrimeNG 21.1.9{Aura preset}
DB::SQLite{EF Core 10.0.9,EnsureCreated}
Runtime::Node.js 24+,npm 11
TestBE::xUnit v3.2.2{Microsoft.Testing.Platform}+AwesomeAssertions 9.4.0+NSubstitute 5.3.0
TestFE::Vitest 4+jsdom 28{npx vitest run --prefix web}
Tailwind::4.3.3{postcss plugin,added 02-todo-crud close}

[STRUCTURE]
MyApp.slnx{CPM via Directory.Packages.props,transitive pinning}
src/::MyApp.Core+MyApp.Infrastructure+MyApp.Server{3 projects}
tests/::MyApp.Core.Tests+MyApp.Server.Tests+e2e{3 projects}
web/::Angular SPA{angular.json,ng-openapi-gen.json,proxy.conf.json}
tasks/::01-scaffold✓+02-todo-crud✓+03-audit-cleanup✓+04-search-filter-sort✓{CVM plans,04 branch ¬merged}
.cvm/::execution manager state

[COMMANDS]
buildBE::dotnet build
runBE::dotnet run --project src/MyApp.Server{port 5000}
testBE::dotnet run --project tests/MyApp.Core.Tests|tests/MyApp.Server.Tests
migrate::dotnet ef migrations add X --project src/MyApp.Infrastructure --startup-project src/MyApp.Server
migrateApply::dotnet ef database update{same flags}
installFE::cd web && npm install
genApi::npm run gen-api{!requires backend running on 5000}
runFE::npm start{port 4200}
buildFE::npm run build
testFE::npm test

[PORTS]
Backend#5000{/health,/version,/scalar/v1,/openapi/v1.json,/api/todos}
Frontend#4200{proxy →5000}

[TOOLS]
CVM::program/execution manager{.cvm/}
MemoryBank::MBEL v5.0

[KEY DEPS]
BE::EF Core 10.0.9+Serilog.AspNetCore 10.0.0+Scalar.AspNetCore 2.16.5+Microsoft.AspNetCore.OpenApi 10.0.9+Devextreme.AspNet.Data 5.1.0{MIT,loadOptions protocol+DataSourceLoader,04 done}
FE::primeng 21.1.9{p-date-picker=p-calendar renamed,ngModel-only value binding}+@primeuix/themes 2.0.3+ng-openapi-gen 1.0.5{params PascalCase,Sort/Filter raw JSON strings}+rxjs 7.8{debounceTime filters}+tailwindcss 4.3.3
