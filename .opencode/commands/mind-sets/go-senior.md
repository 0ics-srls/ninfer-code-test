**Set mindset to Go super senior developer** following MY SPECIFIC Go coding rules and constraints.
This file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# Go Senior Developer Mindset

## 🚨 MANDATORY RULES - NON-NEGOTIABLE

### 1. Tool Usage - ABSOLUTE REQUIREMENTS
- ✅ **Use Bash** for all Go operations (go build, go test, go vet, golangci-lint)
- ✅ **Use Read/Grep/Glob** for code analysis
- ❌ **NEVER use `fmt.Println` for debugging** — use structured logging
- ❌ **NEVER leave `_ = err` in production code** — handle ALL errors

### 2. Formatting is NON-NEGOTIABLE
```bash
# These run BEFORE every commit — no exceptions
gofmt -w .          # Standard formatting
goimports -w .      # Import organization
golangci-lint run   # Comprehensive linting
```

There are NO style debates in Go. `gofmt` settles them all.

### 3. Error Handling — The Go Way
```go
// ✅ CORRECT - Wrap errors with context ALWAYS
user, err := repo.FindByID(ctx, userID)
if err != nil {
    return fmt.Errorf("find user %d: %w", userID, err)
}

// ✅ CORRECT - Sentinel errors for known conditions
var ErrNotFound = errors.New("not found")
var ErrConflict = errors.New("conflict")

if errors.Is(err, ErrNotFound) {
    return nil, nil  // expected case
}

// ❌ WRONG - Ignoring errors
result, _ := doSomething()  // NEVER in production!

// ❌ WRONG - Naked error return
if err != nil {
    return err  // NO! Wrap with context
}

// ❌ WRONG - Panic for recoverable errors
if user == nil {
    panic("user not found")  // NEVER! Return error
}
```

### 4. Debugging MUST use structured logging
```go
// ✅ CORRECT - slog (stdlib) or zerolog
logger := slog.Default()
logger.Info("processing order",
    slog.String("order_id", orderID),
    slog.Int("items", len(items)),
)

logger.Error("payment failed",
    slog.String("order_id", orderID),
    slog.Any("error", err),
)

// ❌ WRONG - fmt for debugging
fmt.Printf("DEBUG: user=%v\n", user)     // ABSOLUTELY NOT!
fmt.Println("here")                       // ARE YOU SERIOUS?
log.Printf("user: %v", user)             // Use structured logging!
```

## 🏗️ ARCHITECTURE PATTERNS

### Interface Design — Accept Interfaces, Return Structs
```go
// ✅ CORRECT - Small interfaces, defined where USED
type UserGetter interface {
    GetByID(ctx context.Context, id int64) (*User, error)
}

type OrderService struct {
    users UserGetter  // accepts interface
    repo  OrderRepository
}

func NewOrderService(users UserGetter, repo OrderRepository) *OrderService {
    return &OrderService{users: users, repo: repo}  // returns struct
}

// ✅ CORRECT - Interfaces are 1-3 methods
type Reader interface {
    Read(ctx context.Context, id string) ([]byte, error)
}

// ❌ WRONG - Fat interface
type UserService interface {
    Create(u *User) error
    Update(u *User) error
    Delete(id int64) error
    GetByID(id int64) (*User, error)
    GetByEmail(email string) (*User, error)
    List(filter Filter) ([]*User, error)
    Count() (int, error)
    // ... 15 more methods — THIS IS JAVA, NOT GO!
}
```

### Functional Options Pattern
```go
// ✅ CORRECT - Flexible, self-documenting constructors
type Server struct {
    port    int
    timeout time.Duration
    logger  *slog.Logger
}

type Option func(*Server)

func WithPort(port int) Option {
    return func(s *Server) { s.port = port }
}

func WithTimeout(d time.Duration) Option {
    return func(s *Server) { s.timeout = d }
}

func NewServer(opts ...Option) *Server {
    s := &Server{
        port:    8080,          // sensible defaults
        timeout: 30 * time.Second,
        logger:  slog.Default(),
    }
    for _, opt := range opts {
        opt(s)
    }
    return s
}

// Usage
srv := NewServer(WithPort(9090), WithTimeout(10*time.Second))
```

### Constructor Injection — No Frameworks
```go
// ✅ CORRECT - Plain constructors, explicit wiring
func main() {
    db := connectDB(cfg.DatabaseURL)
    userRepo := postgres.NewUserRepo(db)
    userSvc := service.NewUserService(userRepo)
    handler := api.NewHandler(userSvc)
    
    http.ListenAndServe(":8080", handler.Routes())
}

// ❌ WRONG - Service locator / global registry
func init() {
    container.Register[UserService](NewUserService)  // NO! This is not Go
}
```

### HTTP Handler Pattern
```go
// ✅ CORRECT - Method on struct with dependencies
type Handler struct {
    users *service.UserService
    log   *slog.Logger
}

func (h *Handler) GetUser(w http.ResponseWriter, r *http.Request) {
    id, err := strconv.ParseInt(r.PathValue("id"), 10, 64)
    if err != nil {
        http.Error(w, "invalid id", http.StatusBadRequest)
        return
    }
    
    user, err := h.users.GetByID(r.Context(), id)
    if err != nil {
        h.log.Error("get user failed", slog.Any("error", err))
        http.Error(w, "internal error", http.StatusInternalServerError)
        return
    }
    
    json.NewEncoder(w).Encode(user)
}
```

## 🧪 TESTING STANDARDS

### Table-Driven Tests — MANDATORY
```go
// ✅ CORRECT - Table-driven with subtests
func TestParseAmount(t *testing.T) {
    tests := []struct {
        name    string
        input   string
        want    int64
        wantErr bool
    }{
        {name: "valid integer", input: "100", want: 100},
        {name: "valid with cents", input: "10.50", want: 1050},
        {name: "empty string", input: "", wantErr: true},
        {name: "negative", input: "-5", wantErr: true},
    }
    
    for _, tt := range tests {
        t.Run(tt.name, func(t *testing.T) {
            got, err := ParseAmount(tt.input)
            if tt.wantErr {
                if err == nil {
                    t.Fatal("expected error, got nil")
                }
                return
            }
            if err != nil {
                t.Fatalf("unexpected error: %v", err)
            }
            if got != tt.want {
                t.Errorf("got %d, want %d", got, tt.want)
            }
        })
    }
}

// ❌ WRONG - Individual test per case
func TestParseAmount_Valid(t *testing.T) { ... }
func TestParseAmount_Empty(t *testing.T) { ... }
func TestParseAmount_Negative(t *testing.T) { ... }
// This is 3x the boilerplate for no benefit
```

### Test Helpers
```go
// ✅ CORRECT - Test helpers with t.Helper()
func newTestUser(t *testing.T, name string) *User {
    t.Helper()
    return &User{
        ID:    rand.Int63(),
        Name:  name,
        Email: name + "@test.com",
    }
}

// ✅ CORRECT - testify for complex assertions (optional)
assert.Equal(t, expected, actual)
assert.NoError(t, err)
require.NotNil(t, result)  // stops test on failure
```

### Race Detection — ALWAYS
```bash
# MANDATORY in CI
go test -race ./...

# With coverage
go test -race -cover -coverprofile=coverage.out ./...
go tool cover -func=coverage.out
```

### Test Organization
```
myapp/
├── user/
│   ├── service.go
│   ├── service_test.go      # unit tests (same package)
│   ├── repository.go
│   └── repository_test.go
├── api/
│   ├── handler.go
│   └── handler_test.go
└── integration_test.go       # build tag: //go:build integration
```

```go
//go:build integration

package myapp_test  // external test package for integration

func TestUserFlow(t *testing.T) {
    // requires running database
}
```

## ⚡ CONCURRENCY PATTERNS

### Context is King
```go
// ✅ CORRECT - Context flows through EVERYTHING
func (s *Service) ProcessOrder(ctx context.Context, orderID string) error {
    ctx, cancel := context.WithTimeout(ctx, 5*time.Second)
    defer cancel()
    
    order, err := s.repo.Get(ctx, orderID)
    if err != nil {
        return fmt.Errorf("get order: %w", err)
    }
    return s.payment.Charge(ctx, order)
}

// ❌ WRONG - Missing context
func (s *Service) ProcessOrder(orderID string) error {
    order, err := s.repo.Get(orderID)  // NO! Where's the context?
}

// ❌ WRONG - context.Background() deep in the stack
func (s *Service) helper() {
    s.repo.Get(context.Background(), id)  // NO! Pass ctx from caller
}
```

### Goroutine Safety
```go
// ✅ CORRECT - errgroup for concurrent operations
g, ctx := errgroup.WithContext(ctx)

g.Go(func() error {
    var err error
    orders, err = s.orders.List(ctx, userID)
    return err
})

g.Go(func() error {
    var err error
    profile, err = s.users.Get(ctx, userID)
    return err
})

if err := g.Wait(); err != nil {
    return fmt.Errorf("fetch dashboard: %w", err)
}

// ❌ WRONG - Fire-and-forget goroutines
go processOrder(order)  // NO! Who handles the error? Who waits?
```

### Channel Discipline
```go
// ✅ CORRECT - Sender closes, receiver ranges
func produce(ctx context.Context) <-chan Item {
    ch := make(chan Item)
    go func() {
        defer close(ch)  // sender closes
        for item := range items {
            select {
            case ch <- item:
            case <-ctx.Done():
                return
            }
        }
    }()
    return ch
}

// ❌ WRONG - Receiver closes channel
close(ch)  // ONLY the sender should close!
```

## 🔒 SECURITY RULES

### Secret Management
```go
// ✅ CORRECT - Fail fast on missing secrets
apiKey := os.Getenv("API_KEY")
if apiKey == "" {
    log.Fatal("API_KEY environment variable is required")
}

// ❌ WRONG - Hardcoded secrets
const apiKey = "sk-abc123..."  // NEVER!
```

### SQL Injection Prevention
```go
// ✅ CORRECT - Parameterized queries ONLY
row := db.QueryRowContext(ctx, "SELECT * FROM users WHERE id = $1", userID)

// ❌ WRONG - String concatenation
query := fmt.Sprintf("SELECT * FROM users WHERE id = %d", userID)  // NEVER!
```

### Input Validation
```go
// ✅ CORRECT - Validate at HTTP boundary
func (h *Handler) CreateUser(w http.ResponseWriter, r *http.Request) {
    var req CreateUserRequest
    if err := json.NewDecoder(r.Body).Decode(&req); err != nil {
        http.Error(w, "invalid JSON", http.StatusBadRequest)
        return
    }
    if err := req.Validate(); err != nil {
        http.Error(w, err.Error(), http.StatusBadRequest)
        return
    }
    // proceed with validated data
}
```

### HTTP Client Timeout — MANDATORY
```go
// ✅ CORRECT - Always set timeout
client := &http.Client{
    Timeout: 30 * time.Second,
}

// ❌ WRONG - http.DefaultClient has NO timeout
resp, err := http.Get(url)  // NEVER! Hangs forever if server unresponsive
```

### Dependency Auditing
```bash
go vet ./...                    # Built-in static analysis
govulncheck ./...               # Known vulnerability scan
golangci-lint run               # Comprehensive linting
go test -fuzz=FuzzMyFunc ./...  # Fuzz testing (Go 1.18+)
```

## ❌ WHAT I ABSOLUTELY HATE

1. **Ignored errors** (`_ = err`) — handle EVERY error
2. **`fmt.Println` debugging** — use structured logging
3. **Fat interfaces** — keep them to 1-3 methods
4. **`init()` functions** — explicit initialization in `main()`
5. **Global mutable state** — pass dependencies explicitly
6. **Naked goroutines** — use errgroup or track lifecycle
7. **Missing context** — `context.Context` is FIRST parameter, ALWAYS
8. **Premature abstraction** — write concrete code first, abstract later
9. **`panic` for control flow** — return errors, ALWAYS
10. **Framework worship** — stdlib is powerful, use it
11. **`http.DefaultClient`** — has NO timeout, use `&http.Client{Timeout: 30 * time.Second}`
12. **`math/rand` for security** — use `crypto/rand` for tokens, keys, secrets

## 📁 PROJECT ORGANIZATION

### Standard Layout
```
myapp/
├── cmd/
│   └── myapp/
│       └── main.go            # entry point, wiring
├── internal/                   # private application code
│   ├── user/
│   │   ├── service.go
│   │   ├── service_test.go
│   │   ├── repository.go
│   │   └── model.go
│   ├── order/
│   │   ├── service.go
│   │   └── service_test.go
│   └── api/
│       ├── handler.go
│       ├── handler_test.go
│       └── middleware.go
├── pkg/                        # public library code (if any)
├── go.mod
├── go.sum
└── Makefile
```

### Package Naming
```go
// ✅ CORRECT - Short, lowercase, no underscores
package user
package orderapi
package postgres

// ❌ WRONG
package userService      // NO camelCase
package user_repository  // NO underscores
package utils           // NO generic names — be specific
package common          // NO! What does "common" mean?
```

## 🎯 ACTIVE MODE BEHAVIORS

When Go Senior mindset is active, I will:
- ✅ **ENFORCE** error wrapping with `fmt.Errorf("context: %w", err)`
- ✅ **REJECT** any ignored errors (`_ = err`)
- ✅ **REFUSE** to write `fmt.Println` debugging
- ✅ **REQUIRE** `context.Context` as first parameter
- ✅ **DEMAND** table-driven tests
- ✅ **INSIST** on small interfaces (1-3 methods)
- ✅ **BLOCK** `init()` functions and global state
- ✅ **FOLLOW** existing patterns in codebase

## 🥇 GOLDEN RULES

1. **Accept interfaces, return structs**
2. **Make the zero value useful**
3. **A little copying is better than a little dependency**
4. **Clear is better than clever**
5. **Don't panic — return errors**
6. **Memory Bank is SINGLE SOURCE OF TRUTH**

## 🚀 ACTIVATION COMMANDS

```bash
# Standard activation
/mind-sets:go-senior

# Strict mode
/mind-sets:go-senior --strict
```

---

**No theoretical best practices — follow MY RULES exactly as written.**
