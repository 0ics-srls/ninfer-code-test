---
description: Project Foundations - Go implementation
---
# Project Foundations — Go

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```bash
# go vet — built-in, catches real bugs
go vet ./...

# golangci-lint — comprehensive (includes staticcheck, errcheck, gosec)
golangci-lint run ./...

# In CI — treat ALL as errors
golangci-lint run --out-format=github-actions ./...
```

`.golangci.yml`:
```yaml
linters:
  enable:
    - errcheck
    - govet
    - staticcheck
    - unused
    - gosec
    - errorlint
    - gocritic
    - exhaustive
  disable:
    - wsl
    - gofumpt  # gofmt is enough
linters-settings:
  errcheck:
    check-blank: true  # catches _ = err
  govet:
    enable-all: true
```

Every ignored error, unused variable, and suspicious construct is caught. No warnings in CI, ever.

---

## 2. Central Dependency Management

```
go.mod              → single source for ALL dependencies
go.sum              → cryptographic verification of ALL dependencies
```

```bash
# Keep dependencies tidy
go mod tidy          # remove unused, add missing
go mod verify        # verify checksums
go mod graph         # inspect dependency tree
```

Go modules are centralized by design. One `go.mod` per module. Use `go mod tidy` before every commit. Pin with exact versions — Go uses MVS (Minimum Version Selection).

---

## 3. Versioning Strategy

```go
// version.go — set at build time via ldflags
var (
    Version   = "dev"
    BuildTime = "unknown"
    Commit    = "unknown"
)
```

```bash
# Build with version info
go build -ldflags "-X main.Version=1.2.3 -X main.Commit=$(git rev-parse --short HEAD) -X main.BuildTime=$(date -u +%Y-%m-%dT%H:%M:%SZ)" -o myapp ./cmd/myapp
```

The application reports its version at startup and via a `/health` endpoint. Every binary carries the version. CI sets ldflags automatically.

---

## 4. Structured Error Handling

```go
// errors.go — typed errors in domain package
var (
    ErrNotFound    = errors.New("not found")
    ErrConflict    = errors.New("conflict")
    ErrUnauthorized = errors.New("unauthorized")
    ErrValidation  = errors.New("validation")
)

// Wrapping with context
func (s *UserService) GetByID(ctx context.Context, id int64) (*User, error) {
    user, err := s.repo.Find(ctx, id)
    if err != nil {
        return nil, fmt.Errorf("get user %d: %w", id, err)
    }
    if user == nil {
        return nil, fmt.Errorf("user %d: %w", id, ErrNotFound)
    }
    return user, nil
}

// Checking at boundaries
if errors.Is(err, ErrNotFound) {
    http.Error(w, "not found", http.StatusNotFound)
    return
}
```

Every error has context — `fmt.Errorf("what failed: %w", err)`. Sentinel errors for known conditions. `errors.Is` / `errors.As` for programmatic handling. Never `panic` for recoverable errors.

---

## 5. Test Configuration (Fast/Slow Split)

```go
// integration_test.go — build tag separates slow tests
//go:build integration

package myapp_test

func TestDatabaseIntegration(t *testing.T) {
    // requires running database
}
```

```bash
# Fast tests (default) — seconds
go test ./...

# Integration tests — on demand
go test -tags=integration ./...

# All tests with race detection
go test -race -tags=integration ./...
```

```makefile
# Makefile
test:
	go test -race ./...

test-integration:
	go test -race -tags=integration ./...

test-cover:
	go test -race -coverprofile=coverage.out ./...
	go tool cover -func=coverage.out
```

---

## 6. Immutable DTOs

```go
// Structs are value types — naturally immutable when passed by value
type UserDTO struct {
    ID        int64     `json:"id"`
    Name      string    `json:"name"`
    Email     string    `json:"email"`
    CreatedAt time.Time `json:"created_at"`
}

// Copy with modifications — create new struct
func (u UserDTO) WithEmail(email string) UserDTO {
    u.Email = email
    return u  // returns copy, original unchanged
}

// Collections — return copies, not internal slices
func (s *Service) ListUsers() []UserDTO {
    result := make([]UserDTO, len(s.users))
    copy(result, s.users)
    return result
}
```

Go structs are value types. Pass by value for immutability. Return copies of internal slices. Use exported fields for DTOs (JSON serialization), unexported for internal state.

---

## 7. Dependency Injection with Proper Registration

```go
// main.go — explicit wiring, no frameworks
func main() {
    cfg := config.MustLoad()
    
    // Infrastructure
    db := postgres.MustConnect(cfg.DatabaseURL)
    defer db.Close()
    
    // Repositories
    userRepo := postgres.NewUserRepo(db)
    orderRepo := postgres.NewOrderRepo(db)
    
    // Services
    userSvc := service.NewUserService(userRepo)
    orderSvc := service.NewOrderService(orderRepo, userSvc)
    
    // HTTP
    handler := api.NewHandler(userSvc, orderSvc)
    
    log.Fatal(http.ListenAndServe(":"+cfg.Port, handler.Routes()))
}
```

`main()` is the composition root. No DI frameworks — Go constructors are explicit enough. Each `New*()` function declares its dependencies. The dependency graph is readable in 10 seconds.

---

## 8. Centralized Configuration with Validation

```go
// config/config.go
type Config struct {
    Port        string        `env:"PORT" envDefault:"8080"`
    DatabaseURL string        `env:"DATABASE_URL,required"`
    APIKey      string        `env:"API_KEY,required"`
    Timeout     time.Duration `env:"TIMEOUT" envDefault:"30s"`
}

func MustLoad() *Config {
    cfg := &Config{}
    if err := env.Parse(cfg); err != nil {
        log.Fatalf("failed to load config: %v", err)
    }
    return cfg
}
```

All configuration via environment variables. `required` tag fails fast at startup. Sensible defaults for non-critical values. Never read config files deep in the stack — load once in `main()`, pass via constructors.

---

## 9. Structured File Logging

```go
// logging.go — slog (stdlib, Go 1.21+)
func SetupLogger(env string) *slog.Logger {
    var handler slog.Handler
    switch env {
    case "production":
        handler = slog.NewJSONHandler(os.Stdout, &slog.HandlerOptions{
            Level: slog.LevelInfo,
        })
    default:
        handler = slog.NewTextHandler(os.Stderr, &slog.HandlerOptions{
            Level: slog.LevelDebug,
        })
    }
    return slog.New(handler)
}

// Usage — structured key-value pairs
logger.Info("order processed",
    slog.String("order_id", orderID),
    slog.Int("items", count),
    slog.Duration("elapsed", elapsed),
)
```

Use `slog` (stdlib) for new projects. JSON format in production, text in development. Structured key-value pairs, not formatted strings. Pass logger via constructor injection.

---

## 10. Zero-Dependency Core Module

```
internal/domain/     → zero external dependencies, only stdlib
```

Domain package contains: interfaces, DTOs (structs), errors (sentinels), constants. No implementations. Every other package imports domain. Domain imports nothing external.

```go
// internal/domain/user.go
package domain

type User struct {
    ID    int64
    Name  string
    Email string
}

type UserRepository interface {
    FindByID(ctx context.Context, id int64) (*User, error)
    Save(ctx context.Context, user *User) error
}
```

---

## 11. Interface-First Design

```go
// Interfaces defined WHERE THEY ARE USED (not where implemented)
// internal/service/user.go
type userStore interface {
    FindByID(ctx context.Context, id int64) (*domain.User, error)
    Save(ctx context.Context, user *domain.User) error
}

type UserService struct {
    store userStore
}
```

Small interfaces (1-3 methods). Defined in the consumer package. Implicit satisfaction — no `implements` keyword needed. Test with fakes that satisfy the interface.

---

## 12. Internal/Private by Default

```
myapp/
├── internal/       # CANNOT be imported by other modules
│   ├── user/       # private to this module
│   ├── order/
│   └── api/
├── pkg/            # CAN be imported (public API — use sparingly)
└── cmd/            # entry points
```

Use `internal/` for application code. Only `pkg/` for genuinely reusable libraries. Unexported (lowercase) by default — export only what's needed.

---

## 13. Test Module per Production Module

```
internal/
├── user/
│   ├── service.go
│   └── service_test.go     # same package — tests internals
├── order/
│   ├── service.go
│   └── service_test.go
└── api/
    ├── handler.go
    └── handler_test.go     # same package — tests internals
```

Test files live next to production code. Same package for unit tests (access unexported). `_test` package suffix for integration tests (external perspective).

---

## 14. Convention Over Configuration

```go
// Router auto-registration by convention
func (h *Handler) Routes() http.Handler {
    mux := http.NewServeMux()
    mux.HandleFunc("GET /users/{id}", h.GetUser)
    mux.HandleFunc("POST /users", h.CreateUser)
    mux.HandleFunc("GET /health", h.Health)
    return mux
}
```

Go 1.22+ patterns in `http.NewServeMux()`. RESTful routes by convention. Package structure implies purpose (`internal/user/` = user domain).

---

## 15. Deterministic Build Output

```makefile
# Makefile
BINARY := myapp
VERSION := $(shell git describe --tags --always --dirty)

build:
	CGO_ENABLED=0 go build \
		-ldflags "-s -w -X main.Version=$(VERSION)" \
		-o bin/$(BINARY) ./cmd/$(BINARY)

# Reproducible builds
build-reproducible:
	CGO_ENABLED=0 GOFLAGS=-trimpath go build \
		-ldflags "-s -w -X main.Version=$(VERSION)" \
		-o bin/$(BINARY) ./cmd/$(BINARY)
```

`CGO_ENABLED=0` for static binaries. `-trimpath` for reproducible builds. Output always in `bin/`. Version embedded via ldflags.

---

## 16. Black Box Composition

```go
// Each package is a black box with a clear interface
// internal/user/service.go — public interface
type Service struct { ... }  // exported

func NewService(repo Repository) *Service { ... }  // constructor
func (s *Service) GetByID(ctx context.Context, id int64) (*User, error) { ... }

// internal/user/helpers.go — private implementation
func (s *Service) validateEmail(email string) error { ... }  // unexported
```

Each package exposes exported types and functions — its contract. Internal helpers are unexported. Package boundaries enforce isolation. Test each package independently via its exported API.

---

## Verification Commands

```bash
# Check formatting
gofmt -l .                     # list unformatted files
test -z "$(gofmt -l .)"       # CI check — fails if any unformatted

# Check all lints pass
golangci-lint run ./...

# Check no ignored errors
grep -rn "_ = " --include="*.go" | grep -v "_test.go"

# Run all tests with race detection
go test -race ./...

# Check coverage
go test -coverprofile=coverage.out ./...
go tool cover -func=coverage.out | grep total

# Check for vulnerabilities
govulncheck ./...

# Verify dependencies
go mod verify
```
