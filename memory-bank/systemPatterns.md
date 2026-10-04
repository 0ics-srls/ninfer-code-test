# System Patterns
§MBEL:5.0

[ARCHITECTURE]
flowchart TD
    SPA[web/ Angular21 SPA] -->|HTTP /api| API[MyApp.Server]
    API -->|DI| INFRA[MyApp.Infrastructure]
    INFRA -->|IRepository<T>| DB[(SQLite via EF Core)]
    API --> CORE[MyApp.Core{contracts}]
    INFRA --> CORE
    API -->|/openapi/v1.json| GEN[ng-openapi-gen → src/app/generated]
    GEN --> SPA

[LAYERS]
MyApp.Core::ZeroDepContracts{IEntity,Todo,TodoStatus,DTOs,AppError}
MyApp.Infrastructure::EFCore+SQLite+Repository<T>{AppDbContext,TodoConfiguration}
MyApp.Server::WebHost{controllers,middleware,DI,OpenAPI,Scalar,Serilog}
web::SPA{standalone,OnPush,signals,PrimeNG Aura}
DepRule::Core→¬Infra¬Server{inner layers ¬depend on outer}

[BACKEND PATTERNS]
Records::Entities+DTOs+Result{immutable,init-only}
Repo::IRepository<T> generic{GetAllAsync,GetByIdAsync,AddAsync,UpdateAsync,DeleteAsync,SaveChangesAsync,Query()→IQueryable<T> AsNoTracking}
Controller::Thin{repo→ToDto mapping→ActionResult}
LoadOptions::Devextreme.AspNet.Data 5.1.0{MIT}+manual binding{src/MyApp.Server/Binding/DataSourceLoadOptions.cs:DataSourceLoadOptions:DataSourceLoadOptionsBase+FromValues via DataSourceLoadOptionsParser.Parse+[ModelBinder] IModelBinder}
LoadProtocol::GET query string{skip,take,requireTotalCount,sort=JSON array,filter=JSON expr}→DataSourceLoader.LoadAsync→SQL-side{EF translates}
LoadGotchas::StringToLower=true{EF SQLite instr() case-sensitive}+totalCount=-1 sentinel{¬requireTotalCount}+ActionResult<LoadResult>{¬IActionResult,OpenAPI}
Mapping::TodoExtensions{ToDto,ToEntity,Id=0 for create,CreatedAt=UtcNow}+EF projection uses explicit new TodoDto{¬extension calls}
Errors::AppException→GlobalExceptionMiddleware→ProblemDetails{NotFound→404,InvalidParams→400,Unauthorized→401,_→500}
DI::ServiceCollectionExtensions{AddAppInfrastructure,AddAppJson}{AddAppCore purged,audit-cleanup 02}
Options::AppOptions+ValidateDataAnnotations+ValidateOnStart{section "App"}
Json::JsonStringEnumConverter{enum as string in payload}
Db::EnsureCreated+Seed2Todos{dev-only,¬migrations in use}
Cors::ConfigDriven{App:CorsOrigins,AllowAnyHeader+Method}
Logging::Serilog{ReadFrom.Configuration,file sink}
Endpoints::/api/todos CRUD+loadOptions→LoadResult+stats{total,completed,pending}+/health+/version+/scalar/v1

[FRONTEND PATTERNS]
Components::Standalone{app,todos-view}
State::Signals{todos,totalRecords,stats×3,filters,searchTerm,dialogVisible,editingId}+plain props ngModel-bound{filterStatus,filterRange}
ChangeDetection::OnPush{all components}
Forms::Reactive+NonNullable{required,maxLength 200}+ngModel toolbar controls{search/status/date}
Theme::ThemeService{isDark signal,localStorage key my-app-theme,.app-dark on documentElement}
ThemeNG::providePrimeNG{preset:Aura,darkModeSelector:.app-dark}
Api::GeneratedFunctions{apiTodosGet$Json{params PascalCase,Sort/Filter=raw JSON strings},apiTodosStatsGet$Json,apiTodosPost$Json,apiTodosIdPut$Json,apiTodosIdDelete}
LazyTable::p-table lazy+lazyLoadOnInit=true{¬ngOnInit load}+onLazyLoad→buildLoadParams→invoke+totalRecords drives paginator{rowsPerPageOptions[10,25,50],sortMode=single}
LoadParamsAdapter::web/src/app/core/load-options.builder.ts{buildLoadParams:skip/take/requireTotalCount+sort JSON+filter JSON{title contains,status eq,createdAt >=/<= endOfDay,'and' join},pure+unit-tested}
FiltersToolbar::search{[value]+(input)→Subject+debounceTime(400)→applyFilters}+p-select status+clear{ngModel}+p-date-picker range{ngModel,readonlyInput,onSelect reads ngModel prop}+clear button;applyFilters{filters.set+lastLazyEvent.first=0+reload,¬@ViewChild}
StatsCards::global counts via /api/todos/stats{¬filtered,reloaded on every reload}
ErrorHandling::invoke catch→console.error{reset todos to [],keep dialog open on save fail}
Routing::''→todos{single route}
DevProxy::/api+/openapi+/health+/version→localhost:5000{proxy.conf.json}

[DATA FLOW]
User→TodosView→Api.invoke(fn)→HttpClient→/api/todos→TodosController→Repository→AppDbContext→SQLite
SQLite→Todo→ToDto→Json{camelCase,enum string}→generated model→todos.set→OnPush render
Query::p-table lazyLoad/filter/sort event→buildLoadParams→GET /api/todos?loadOptions{JSON sort/filter}→DataSourceLoadOptionsBinder→DataSourceLoader.LoadAsync→IQueryable{Query()}→SQL-side filter/sort/paginate→LoadResult{data,totalCount}→table+totalRecords

[TESTS]
Core.Tests::Unit 20{Todo,DTOs,TodoFactory}
Server.Tests::Integration 60{WebApplicationFactory,controllers+middleware+DI+options+logging,RepositoryQuery,DataSourceLoadOptions,TodosLoadEndpoint{WipeAllAsync per test},TodosStatsEndpoint}
FE.Tests::Vitest 9{load-options.builder 7+app 2}
E2E::tests/e2e{44 pass,DOM+Vision verified}+04 DOM-verified via chrome-devtools{6 scenarios}
Assert::AwesomeAssertions
Mock::NSubstitute
