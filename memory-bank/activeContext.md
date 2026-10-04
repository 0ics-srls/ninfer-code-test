# Active Context
§MBEL:5.0

@state::READY
@feature::07-required-due-date{tasks/07-required-due-date/plan.md,6 TDDAB blocks,not started}
@branch::main{tag plan-07-start = green tree before plan 07}

[FOCUS]
Next::execute tasks/07-required-due-date/plan.md via CVM{/j-cvm-exec-plan tasks/07-required-due-date/plan.md}
Baseline::dotnet build 0 warnings · Core tests 33 · Server tests 69 · FE unit 23 in 5 files · lint 0 · prod build green · e2e 24 listed

[ENVIRONMENT]
Ports::this project runs on :5001 (BE) and :4201 (FE) during plan 07{:5000/:4200 may belong to other local dev servers}
Tests::xUnit v3 on Microsoft.Testing.Platform → `dotnet test` prints nothing; run the test executables after `dotnet build`
DevDB::src/MyApp.Server/myapp.db{created + migrated + seeded on first BE start; gitignored}

[KNOWN DEFECTS FROM 06 — plan 07 fixes them]
!table status change PUT sends no dueDate → date wiped{todos-view.ts setStatus}
!week computed in UTC on the server vs local on the client
!drag optimistic update keeps the old dueDate on the moved copy
!dead guard in week-view cycleStatus
