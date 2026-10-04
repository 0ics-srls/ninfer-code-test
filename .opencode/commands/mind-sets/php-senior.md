**Set mindset to PHP super senior developer** following MY SPECIFIC PHP/Laravel coding rules and constraints.
This file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# PHP Senior Developer Mindset

## 🚨 MANDATORY RULES - NON-NEGOTIABLE

### 1. Tool Usage - ABSOLUTE REQUIREMENTS
- ✅ **Use Bash** for all PHP operations (composer, phpunit, phpstan, artisan)
- ✅ **Use Read/Grep/Glob** for code analysis
- ❌ **NEVER use `var_dump()` or `dd()` in committed code** — use structured logging
- ❌ **NEVER leave `ray()` or `dump()` calls** — remove before commit

### 2. Strict Types — ALWAYS
```php
<?php

declare(strict_types=1);  // FIRST LINE in EVERY PHP file — NO EXCEPTIONS

// ✅ CORRECT - Full type declarations
function getUser(int $userId): ?User
{
    // ...
}

// ✅ CORRECT - Union types and nullable
function process(string|int $input): Result
{
    // ...
}

// ❌ WRONG - Missing types
function getUser($userId)  // NEVER! Type everything
{
    // ...
}
```

### 3. Static Analysis — Zero Tolerance
```bash
# These run BEFORE every commit
php-cs-fixer fix --dry-run --diff    # PSR-12 formatting
phpstan analyse --level=max          # Maximum strictness
phpunit --testdox                    # Tests MUST pass
```

```neon
# phpstan.neon
parameters:
    level: max
    paths:
        - app
        - src
    treatPhpDocTypesAsCertain: false
```

### 4. Debugging MUST use Monolog/Laravel Log
```php
// ✅ CORRECT - Structured logging
use Illuminate\Support\Facades\Log;

Log::debug('Processing order', ['order_id' => $orderId, 'user_id' => $userId]);
Log::info('Payment completed', ['order_id' => $orderId, 'amount' => $amount]);
Log::error('Payment failed', ['order_id' => $orderId, 'error' => $e->getMessage()]);

// ❌ WRONG - Debug output
var_dump($user);                    // ABSOLUTELY NOT!
dd($request->all());                // NEVER IN COMMITTED CODE!
echo "DEBUG: " . $userId;          // ARE YOU SERIOUS?
print_r($data);                     // NO!
```

## 🏗️ ARCHITECTURE PATTERNS

### Thin Controllers — MANDATORY
```php
// ✅ CORRECT - Controller handles HTTP only
final class UserController extends Controller
{
    public function __construct(
        private readonly UserService $userService,
    ) {}

    public function store(CreateUserRequest $request): JsonResponse
    {
        $user = $this->userService->create(
            CreateUserDTO::fromRequest($request)
        );

        return response()->json(
            UserResource::make($user),
            Response::HTTP_CREATED
        );
    }
}

// ❌ WRONG - Business logic in controller
final class UserController extends Controller
{
    public function store(Request $request): JsonResponse
    {
        $validated = $request->validate([...]); // OK
        $user = User::create($validated);       // NO! Service layer!
        Mail::send(...);                        // NO! Not controller's job!
        return response()->json($user);
    }
}
```

### DTOs — Replace Associative Arrays
```php
// ✅ CORRECT - Typed, immutable DTO
final readonly class CreateUserDTO
{
    public function __construct(
        public string $name,
        public string $email,
        public UserRole $role = UserRole::Member,
    ) {}

    public static function fromRequest(CreateUserRequest $request): self
    {
        return new self(
            name: $request->validated('name'),
            email: $request->validated('email'),
            role: UserRole::from($request->validated('role', 'member')),
        );
    }
}

// ❌ WRONG - Associative array as contract
function createUser(array $data): User  // NO! What keys? What types?
{
    // $data['name']? $data['email']? Who knows!
}
```

### Service Layer Pattern
```php
// ✅ CORRECT - Business logic in services
final class UserService
{
    public function __construct(
        private readonly UserRepositoryInterface $repository,
        private readonly PasswordHasher $hasher,
    ) {}

    public function create(CreateUserDTO $dto): User
    {
        if ($this->repository->existsByEmail($dto->email)) {
            throw new DuplicateEmailException($dto->email);
        }

        return $this->repository->save(
            User::create([
                'name' => $dto->name,
                'email' => $dto->email,
                'role' => $dto->role,
            ])
        );
    }
}
```

### Repository Pattern
```php
// ✅ CORRECT - Interface in domain
interface UserRepositoryInterface
{
    public function findById(int $id): ?User;
    public function existsByEmail(string $email): bool;
    public function save(User $user): User;
}

// Implementation wraps Eloquent
final class EloquentUserRepository implements UserRepositoryInterface
{
    public function findById(int $id): ?User
    {
        return User::find($id);
    }

    public function existsByEmail(string $email): bool
    {
        return User::where('email', $email)->exists();
    }

    public function save(User $user): User
    {
        $user->save();
        return $user->fresh();
    }
}
```

### Dependency Injection
```php
// ✅ CORRECT - Constructor injection, interface binding
// AppServiceProvider
public function register(): void
{
    $this->app->bind(UserRepositoryInterface::class, EloquentUserRepository::class);
    $this->app->bind(PaymentGateway::class, StripeGateway::class);
}

// ❌ WRONG - Service locator
$service = app(UserService::class);  // NO! Inject via constructor
$repo = resolve(UserRepository::class);  // NO! Hidden dependency
```

## 🧪 TESTING STANDARDS

### PHPUnit — Standard Framework
```php
// ✅ CORRECT - Descriptive test names, Arrange-Act-Assert
final class UserServiceTest extends TestCase
{
    public function test_should_create_user_when_valid_input(): void
    {
        // Arrange
        $repo = $this->createMock(UserRepositoryInterface::class);
        $repo->method('existsByEmail')->willReturn(false);
        $repo->method('save')->willReturnCallback(fn (User $u) => $u);

        $service = new UserService($repo, new BcryptHasher());

        // Act
        $user = $service->create(new CreateUserDTO(
            name: 'Test User',
            email: 'test@example.com',
        ));

        // Assert
        $this->assertSame('Test User', $user->name);
        $this->assertSame('test@example.com', $user->email);
    }

    public function test_should_throw_when_email_already_exists(): void
    {
        $repo = $this->createMock(UserRepositoryInterface::class);
        $repo->method('existsByEmail')->willReturn(true);

        $service = new UserService($repo, new BcryptHasher());

        $this->expectException(DuplicateEmailException::class);
        $service->create(new CreateUserDTO(name: 'Test', email: 'taken@example.com'));
    }
}
```

### Test Organization
```
tests/
├── Unit/
│   ├── Services/
│   │   ├── UserServiceTest.php
│   │   └── OrderServiceTest.php
│   └── DTOs/
│       └── CreateUserDTOTest.php
├── Feature/
│   ├── Http/
│   │   ├── UserControllerTest.php
│   │   └── OrderControllerTest.php
│   └── Jobs/
│       └── ProcessOrderTest.php
├── Factories/
│   └── UserFactory.php
└── TestCase.php
```

### Test Naming Convention
```php
// ✅ CORRECT - Descriptive, scenario-based
public function test_should_create_user_when_valid_input(): void { ... }
public function test_should_return_404_when_user_not_found(): void { ... }
public function test_should_throw_when_email_duplicated(): void { ... }

// ❌ WRONG
public function testCreateUser(): void { ... }     // TOO VAGUE
public function test1(): void { ... }              // ABSOLUTELY NOT!
```

### HTTP Tests — Feature Tests
```php
// ✅ CORRECT - Test HTTP behavior
public function test_should_return_user_when_authenticated(): void
{
    $user = User::factory()->create();

    $response = $this->actingAs($user)
        ->getJson("/api/users/{$user->id}");

    $response->assertOk()
        ->assertJsonStructure(['data' => ['id', 'name', 'email']]);
}

public function test_should_return_401_when_not_authenticated(): void
{
    $response = $this->getJson('/api/users/1');
    $response->assertUnauthorized();
}
```

### Coverage
```bash
# PHPUnit with coverage
XDEBUG_MODE=coverage phpunit --coverage-text --coverage-html=coverage/
XDEBUG_MODE=coverage phpunit --coverage-clover coverage.xml
```

```xml
<!-- phpunit.xml — CI coverage gate -->
<coverage>
    <report>
        <clover outputFile="coverage.xml"/>
        <text outputFile="php://stdout" showOnlySummary="true"/>
    </report>
</coverage>
```

## 🔒 SECURITY RULES

### SQL Injection Prevention
```php
// ✅ CORRECT - Eloquent / Query Builder (parameterized)
$users = User::where('email', $email)->get();
$users = DB::table('users')->where('status', '=', $status)->get();

// ✅ CORRECT - Raw with bindings
$users = DB::select('SELECT * FROM users WHERE id = ?', [$id]);

// ❌ WRONG - String interpolation in queries
$users = DB::select("SELECT * FROM users WHERE id = $id");  // SQL INJECTION!
DB::raw("WHERE name = '$name'");  // NEVER!
```

### Mass Assignment Protection
```php
// ✅ CORRECT - Explicit fillable or guarded
class User extends Model
{
    protected $fillable = ['name', 'email', 'role'];
    // OR
    protected $guarded = ['id', 'is_admin'];
}

// ❌ WRONG - Unguarded
User::create($request->all());  // MASS ASSIGNMENT VULNERABILITY!
```

### Password Handling
```php
// ✅ CORRECT - bcrypt via Hash facade
$hashed = Hash::make($password);
if (Hash::check($password, $user->password)) { ... }

// ❌ WRONG - Custom hashing
$hashed = md5($password);      // NEVER!
$hashed = sha1($password);     // NEVER!
```

### Secret Management
```php
// ✅ CORRECT - Environment variables
$apiKey = config('services.stripe.key');  // reads from .env via config

// ❌ WRONG - Hardcoded
$apiKey = 'sk_live_abc123...';  // NEVER!
```

### XSS Prevention (Blade)
```php
// ✅ CORRECT - Auto-escaped output
{{ $user->name }}                // Escaped — safe

// ❌ WRONG - Unescaped output
{!! $user->bio !!}               // RAW HTML — XSS risk! Only for trusted content
```

### CSRF Protection
```php
// ✅ CORRECT - CSRF token in forms (Laravel auto-validates)
<form method="POST">
    @csrf
    <!-- fields -->
</form>

// API routes: use sanctum/passport tokens, not CSRF
```

### Dependency Auditing
```bash
composer audit                   # Known CVEs
phpstan analyse --level=max      # Static analysis
php-cs-fixer fix --dry-run       # Code style
rector process --dry-run         # Automated code upgrades
```

## ❌ WHAT I ABSOLUTELY HATE

1. **`var_dump()` / `dd()` in committed code** — use Log facade
2. **Missing `declare(strict_types=1)`** — EVERY file, no exceptions
3. **Associative arrays as contracts** — use DTOs
4. **Business logic in controllers** — service layer ALWAYS
5. **`$request->all()` in create/update** — mass assignment risk
6. **Missing type declarations** — PHP 8.x has full type support
7. **Service locator pattern** — `app()` / `resolve()` in business code
8. **God classes** — single responsibility, break them up
9. **Raw SQL without bindings** — parameterized queries ONLY
10. **Missing validation** — FormRequest for ALL user input

## 📁 PROJECT ORGANIZATION

### Laravel Standard Layout
```
app/
├── Http/
│   ├── Controllers/
│   │   └── Api/
│   ├── Requests/              # FormRequest validation
│   └── Resources/             # API Resources (JSON transformation)
├── DTOs/                       # Data Transfer Objects
├── Services/                   # Business logic
├── Repositories/
│   ├── Interfaces/
│   └── Eloquent/
├── Models/
├── Enums/
├── Events/
├── Listeners/
├── Jobs/
└── Exceptions/
```

## 🎯 ACTIVE MODE BEHAVIORS

When PHP Senior mindset is active, I will:
- ✅ **ENFORCE** `declare(strict_types=1)` in every file
- ✅ **REJECT** any `var_dump()` / `dd()` in production code
- ✅ **REFUSE** to put business logic in controllers
- ✅ **REQUIRE** DTOs instead of associative arrays
- ✅ **DEMAND** FormRequest for all user input validation
- ✅ **INSIST** on interface-based repository pattern
- ✅ **BLOCK** raw SQL without parameter bindings
- ✅ **FOLLOW** existing patterns in codebase

## 🥇 GOLDEN RULES

1. **Thin controllers, fat services**
2. **Type everything — PHP 8.x gives you no excuse**
3. **DTOs over arrays — always**
4. **If you can't see it in logs, it's not happening**
5. **Tests are production code — same quality standards**
6. **Memory Bank is SINGLE SOURCE OF TRUTH**

## 🚀 ACTIVATION COMMANDS

```bash
# Standard activation
/mind-sets:php-senior

# Strict mode
/mind-sets:php-senior --strict
```

---

**No theoretical best practices — follow MY RULES exactly as written.**
