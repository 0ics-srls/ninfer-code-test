# Product Context
§MBEL:5.0
@project::my-app

[VISION]
FullStackTodoApp{backend:"ASP.NET Core 10",frontend:"Angular 21+PrimeNG"}
Purpose::Learning+Demo{FullStackStack,OpenAPIFlow,ModernConventions}
StackGoal::Showcase{.NET10,Angular21,Signals,OnPush,OpenAPI-GeneratedClient}

[PROBLEMS]
Solved::TypedApiContract{OpenAPI→ng-openapi-gen→HttpClient,¬handwrittenClient}
Solved::ConsistentCrudStack{REST+SQLite+SPA}
Solved::DarkModeUI{PrimeNG Aura,persisted preference}
Solved::CentralErrorHandling{AppException→ProblemDetails}
Solved::ServerSideQuery{DB-side sort+filter+pagination,DevExtreme loadOptions protocol,reusable BE binder+FE adapter}{04 done 2026-09-03}

[GOALS]
UserGoal::ManageTodos{create,edit,delete,status:Pending|InProgress|Completed}
UserGoal::DarkModeToggle{persisted localStorage,fallback prefers-color-scheme}
UserGoal::ApiSync{regenerate client from live backend}
✓UserGoal::ServerSideQuery{sort+filter+pagination lato DB,reusable layers}{04 done}
AgentGoal::CleanLayeredArchitecture{Core zero-dep,DI extensions,thin controllers}

[SUCCESS]
✓Backend::CrudTodos+ProblemDetails+Health+Version+LoadOptions+Stats
✓Frontend::TodosView{lazy p-table+paginator+sort,3 stat cards,CRUD dialog,confirm delete,filters toolbar{search/status/date-range/clear}}
✓Tests::80 BE{20 Core+60 Server}+9 FE+44 E2E pass
✓Build::dotnet build 0 warn+ng build green
?Next::merge 04→master|?tasks/05+{roadmap::tasks/}
