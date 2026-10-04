---
description: Project Foundations - PHP/Laravel implementation
---
# Project Foundations — PHP/Laravel

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```neon
# phpstan.neon — maximum strictness
parameters:
    level: max
    paths:
        - app
        - src
    treatPhpDocTypesAsCertain: false
```

```bash
# CI pipeline — zero tolerance
php-cs-fixer fix --dry-run --diff --using-cache=no
phpstan analyse --level=max --no-progress
```

PHPStan level max catches type mismatches, dead code, and impossible conditions. PHP-CS-Fixer enforces PSR-12. Every untyped parameter, unused import, and missing return type is caught.

---

## 2. Central Dependency Management

```json
// composer.json — single source for ALL dependencies
{
    "require": {
        "php": "^8.3",
        "laravel/framework": "^11.0",
        "spatie/laravel-data": "^4.0"
    },
    "require-dev": {
        "phpstan/phpstan": "^1.0",
        "phpunit/phpunit": "^11.0",
        "friendsofphp/php-cs-fixer": "^3.0"
    }
}
```

```bash
# Keep dependencies audited
composer audit          # Known CVEs
composer outdated       # Check for updates
composer why package    # Why is this installed?
```

`composer.lock` is ALWAYS committed. Upgrading a dependency = single-line change in `composer.json`. Pin major versions (`^`), let Composer resolve minor/patch.

---

## 3. Versioning Strategy

```php
// config/app.php
'version' => env('APP_VERSION', '1.0.0'),

// Or dedicated file
// version.txt at project root
1.2.3
```

```php
// Health endpoint
Route::get('/health', fn () => response()->json([
    'status' => 'ok',
    'version' => config('app.version'),
    'environment' => app()->environment(),
]));
```

The application reports its version at startup in logs and via a `/health` endpoint. CI sets `APP_VERSION` from git tags or version file.

---

## 4. Structured Error Handling

```php
// app/Exceptions/AppException.php
abstract class AppException extends RuntimeException
{
    abstract public function getStatusCode(): int;
    abstract public function getErrorCode(): string;
}

final class NotFoundException extends AppException
{
    public function __construct(
        private readonly string $entity,
        private readonly string|int $id,
    ) {
        parent::__construct("{$entity} '{$id}' not found");
    }

    public function getStatusCode(): int { return 404; }
    public function getErrorCode(): string { return 'NOT_FOUND'; }
}

// Handler — consistent API responses
// bootstrap/app.php (Laravel 11)
->withExceptions(function (Exceptions $exceptions) {
    $exceptions->render(function (AppException $e) {
        return response()->json([
            'error' => $e->getErrorCode(),
            'message' => $e->getMessage(),
        ], $e->getStatusCode());
    });
})
```

Every exception has a typed class. Error codes for programmatic handling. Consistent JSON error responses. Never expose stack traces in API responses.

---

## 5. Test Configuration (Fast/Slow Split)

```xml
<!-- phpunit.xml -->
<testsuites>
    <testsuite name="Unit">
        <directory>tests/Unit</directory>
    </testsuite>
    <testsuite name="Feature">
        <directory>tests/Feature</directory>
    </testsuite>
</testsuites>
```

```bash
# Fast tests (unit only) — seconds
phpunit --testsuite=Unit

# Feature tests (with DB) — slower
phpunit --testsuite=Feature

# All tests
phpunit

# Specific test
phpunit --filter=test_should_create_user
```

```php
// Use RefreshDatabase trait for feature tests
final class UserControllerTest extends TestCase
{
    use RefreshDatabase;
    // ...
}
```

---

## 6. Immutable DTOs

```php
// readonly class — immutable by design (PHP 8.2+)
final readonly class UserDTO
{
    public function __construct(
        public int $id,
        public string $name,
        public string $email,
        public DateTimeImmutable $createdAt,
    ) {}

    public function withEmail(string $email): self
    {
        return new self(
            id: $this->id,
            name: $this->name,
            email: $email,
            createdAt: $this->createdAt,
        );
    }
}

// Or use spatie/laravel-data for richer features
final class UserData extends Data
{
    public function __construct(
        public readonly int $id,
        public readonly string $name,
        public readonly string $email,
    ) {}
}
```

Use `readonly class` (PHP 8.2+) for all DTOs. All properties are `public readonly` — no setters. Create modified copies via named constructors. Collections use typed arrays or Collections, never plain arrays.

---

## 7. Dependency Injection with Proper Registration

```php
// AppServiceProvider — clean registration
final class AppServiceProvider extends ServiceProvider
{
    public function register(): void
    {
        $this->app->bind(UserRepositoryInterface::class, EloquentUserRepository::class);
        $this->app->bind(PaymentGateway::class, StripeGateway::class);
        $this->app->singleton(CacheService::class);
    }
}
```

```php
// Constructor injection — the ONLY way
final class OrderService
{
    public function __construct(
        private readonly UserRepositoryInterface $userRepo,
        private readonly OrderRepositoryInterface $orderRepo,
        private readonly LoggerInterface $logger,
    ) {}
}
```

ServiceProvider is the composition root. Constructor injection only — never `app()` or `resolve()` in business code. `bind()` for transient, `singleton()` for shared instances.

---

## 8. Centralized Configuration with Validation

```php
// config/services.php — ALL values explicit
return [
    'stripe' => [
        'key' => env('STRIPE_KEY'),
        'secret' => env('STRIPE_SECRET'),
        'webhook_secret' => env('STRIPE_WEBHOOK_SECRET'),
    ],
];

// Validation at boot — fail fast (use config(), NOT env())
final class AppServiceProvider extends ServiceProvider
{
    public function boot(): void
    {
        if (app()->isProduction()) {
            $required = [
                'services.stripe.key',
                'services.stripe.secret',
                'database.connections.pgsql.host',
            ];
            foreach ($required as $key) {
                if (empty(config($key))) {
                    throw new RuntimeException("Config {$key} is required in production");
                }
            }
        }
    }
}
```

Config files in `config/` — never `env()` outside config files. Validation at boot for required values. Fail fast in production if secrets are missing. Access via `config('services.stripe.key')` only.

---

## 9. Structured File Logging

```php
// config/logging.php — channels
'channels' => [
    'daily' => [
        'driver' => 'daily',
        'path' => storage_path('logs/app.log'),
        'level' => 'debug',
        'days' => 14,
    ],
    'json' => [
        'driver' => 'daily',
        'path' => storage_path('logs/app-json.log'),
        'formatter' => JsonFormatter::class,
    ],
],

// Usage — structured context
Log::info('Order processed', [
    'order_id' => $order->id,
    'user_id' => $order->user_id,
    'total' => $order->total,
    'elapsed_ms' => $elapsed,
]);
```

Use Laravel Log facade with context arrays. JSON formatter for production (machine-readable). Daily rotation with configurable retention. Never use `echo`, `print`, `var_dump` for logging.

---

## 10. Zero-Dependency Core Module

```
app/
├── Domain/           → zero framework dependencies
│   ├── Interfaces/   → repository contracts
│   ├── DTOs/         → data transfer objects
│   ├── Enums/        → business enums
│   └── Exceptions/   → domain exceptions
├── Services/         → depends on Domain only
├── Infrastructure/   → depends on Domain + Laravel
└── Http/             → depends on Domain + Services
```

Domain contains: interfaces, DTOs, enums, exceptions. No Eloquent, no Laravel facades. Services contain business logic with interface dependencies. Infrastructure implements interfaces with Laravel/Eloquent.

---

## 11. Interface-First Design

```php
// app/Domain/Interfaces/UserRepositoryInterface.php
interface UserRepositoryInterface
{
    public function findById(int $id): ?User;
    public function existsByEmail(string $email): bool;
    public function save(User $user): User;
    public function delete(int $id): void;
}
```

Every significant service has an interface in Domain. Implementations live in Infrastructure. Testing uses mocks or fakes against interfaces. Binding in ServiceProvider.

---

## 12. Internal/Private by Default

```php
// Final by default — prevent accidental inheritance
final class UserService { ... }
final readonly class UserDTO { ... }
final class EloquentUserRepository { ... }

// Private methods for internals
final class OrderService
{
    public function process(OrderDTO $dto): Order { ... }
    
    private function validateInventory(OrderDTO $dto): void { ... }
    private function calculateTotal(array $items): Money { ... }
}
```

`final` on all classes by default. `private` for all internal methods. Only `public` what's needed for the contract. Extend via composition, not inheritance.

---

## 13. Test Module per Production Module

```
tests/
├── Unit/
│   ├── Services/
│   │   ├── UserServiceTest.php      → tests UserService
│   │   └── OrderServiceTest.php     → tests OrderService
│   └── DTOs/
│       └── CreateUserDTOTest.php    → tests DTO validation
├── Feature/
│   ├── Http/
│   │   └── UserControllerTest.php   → tests HTTP layer
│   └── Jobs/
│       └── ProcessOrderTest.php     → tests job processing
```

Each test class tests exactly one production class. A failing test tells you which component is broken. Unit tests for services/DTOs, Feature tests for HTTP/jobs.

---

## 14. Convention Over Configuration

```php
// Laravel conventions — follow them
// Models in app/Models/ → auto-discovered table name
class User extends Model {}  // table: users

// Controllers follow RESTful naming
class UserController extends Controller
{
    public function index() { ... }    // GET /users
    public function store() { ... }    // POST /users
    public function show() { ... }     // GET /users/{id}
    public function update() { ... }   // PUT /users/{id}
    public function destroy() { ... }  // DELETE /users/{id}
}

// Form Requests named after action
class CreateUserRequest extends FormRequest { ... }
class UpdateUserRequest extends FormRequest { ... }
```

Follow Laravel naming conventions. Route-model binding by convention. Config file names match purpose. Migration timestamps ensure ordering.

---

## 15. Deterministic Build Output

```bash
# Composer install (not update) for reproducible builds
composer install --no-dev --optimize-autoloader --no-interaction

# Cache config for production
php artisan config:cache
php artisan route:cache
php artisan view:cache
```

```dockerfile
FROM php:8.3-fpm
COPY composer.json composer.lock ./
RUN composer install --no-dev --optimize-autoloader
COPY . .
RUN php artisan config:cache && php artisan route:cache
```

`composer.lock` committed for deterministic installs. `--no-dev` in production. Artisan cache commands for performance. Docker multi-stage for consistent builds.

---

## 16. Black Box Composition

```php
// Each service is a black box with clear public API
// app/Services/OrderService.php — public surface
final class OrderService
{
    public function create(CreateOrderDTO $dto): Order { ... }
    public function cancel(int $orderId, string $reason): void { ... }
    public function getStatus(int $orderId): OrderStatus { ... }
    
    // Private implementation details
    private function validateInventory(CreateOrderDTO $dto): void { ... }
    private function notifyWarehouse(Order $order): void { ... }
}
```

Each service exposes a clean public API. Internal methods are private. Services communicate via DTOs, not Eloquent models. Test each service in isolation via its public methods.

---

## Verification Commands

```bash
# Check strict types declared
grep -rL "declare(strict_types=1)" app/ --include="*.php"  # must be empty

# Check formatting
php-cs-fixer fix --dry-run --diff

# Static analysis at max level
phpstan analyse --level=max

# Run all tests with coverage
XDEBUG_MODE=coverage phpunit --coverage-text --min-coverage=80

# Security audit
composer audit

# Check for unused dependencies
composer-unused

# Laravel-specific
php artisan route:list              # verify routes
php artisan config:show app         # verify config
```
