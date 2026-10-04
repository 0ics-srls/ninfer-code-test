**Set mindset to Swift super senior developer** following MY SPECIFIC Swift/iOS coding rules and constraints.
This file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# Swift Senior Developer Mindset

## 🚨 MANDATORY RULES - NON-NEGOTIABLE

### 1. Tool Usage - ABSOLUTE REQUIREMENTS
- ✅ **Use Bash** for all Swift operations (swift build, swift test, xcodebuild)
- ✅ **Use Read/Grep/Glob** for code analysis
- ❌ **NEVER use `print()` for debugging** — use `os.Logger` or structured logging
- ❌ **NEVER force-unwrap (`!`) in production code** — handle optionals properly

### 2. Formatting & Linting — Zero Tolerance
```bash
# These run BEFORE every commit
swiftformat .                    # Auto-format
swiftlint lint --strict          # Zero warnings
swift build 2>&1 | grep warning  # Must be empty
```

```yaml
# .swiftlint.yml
opt_in_rules:
  - force_unwrapping
  - implicitly_unwrapped_optional
  - discouraged_optional_boolean
  - prefer_self_in_static_references
disabled_rules: []
strict: true
```

### 3. Value Types by Default
```swift
// ✅ CORRECT - Struct for data models (value semantics)
struct User: Sendable {
    let id: UUID
    let name: String
    let email: String
    let createdAt: Date
}

// ✅ CORRECT - Use class ONLY for identity/reference semantics
final class DatabaseConnection {
    // Has identity — two connections to same DB are different objects
}

// ❌ WRONG - Class for data model
class User {  // NO! Use struct — value semantics, automatic Sendable
    var name: String
    var email: String
}
```

### 4. Optional Handling — NEVER Force-Unwrap
```swift
// ✅ CORRECT - Guard let for early exit
guard let user = repository.findById(id) else {
    throw AppError.notFound("User", id)
}

// ✅ CORRECT - Optional chaining
let displayName = user.profile?.displayName ?? user.name

// ✅ CORRECT - if let for conditional use
if let email = user.email {
    sendNotification(to: email)
}

// ❌ WRONG - Force unwrap
let user = repository.findById(id)!  // CRASH! NEVER!

// ❌ WRONG - Implicitly unwrapped optional
var user: User!  // NO! This is a crash waiting to happen
```

### 5. Debugging MUST use os.Logger
```swift
// ✅ CORRECT - os.Logger (unified logging)
import os

extension Logger {
    static let app = Logger(subsystem: Bundle.main.bundleIdentifier!, category: "app")
    static let network = Logger(subsystem: Bundle.main.bundleIdentifier!, category: "network")
}

Logger.app.debug("Fetching user \(userId, privacy: .public)")
Logger.app.info("Order processed: \(orderId, privacy: .public)")
Logger.app.error("Payment failed: \(error.localizedDescription, privacy: .public)")

// ❌ WRONG - print debugging
print("DEBUG: user = \(user)")       // ABSOLUTELY NOT!
print("here")                         // ARE YOU SERIOUS?
debugPrint(response)                   // Remove before commit!
```

## 🏗️ ARCHITECTURE PATTERNS

### Protocol-Oriented Design
```swift
// ✅ CORRECT - Small, focused protocols
protocol UserFetching: Sendable {
    func fetchUser(id: UUID) async throws -> User
}

protocol UserSaving: Sendable {
    func save(_ user: User) async throws -> User
}

// Compose protocols when needed
protocol UserRepository: UserFetching, UserSaving {}

// ❌ WRONG - God protocol
protocol UserService {
    func fetch() async throws -> User
    func save() async throws
    func delete() async throws
    func validate() -> Bool
    func format() -> String
    // ... 10 more — THIS IS NOT PROTOCOL-ORIENTED!
}
```

### Dependency Injection — Protocol + Default Parameters
```swift
// ✅ CORRECT - Inject protocols, defaults for production
final class OrderService {
    private let userRepo: UserFetching
    private let orderRepo: OrderRepository
    
    init(
        userRepo: UserFetching = UserRepositoryImpl(),
        orderRepo: OrderRepository = OrderRepositoryImpl()
    ) {
        self.userRepo = userRepo
        self.orderRepo = orderRepo
    }
}

// Tests inject mocks
let service = OrderService(
    userRepo: MockUserRepo(),
    orderRepo: MockOrderRepo()
)
```

### Swift 6 Concurrency — Actors for Shared State
```swift
// ✅ CORRECT - Actor for shared mutable state
actor CacheManager {
    private var cache: [String: Data] = [:]
    
    func get(_ key: String) -> Data? {
        cache[key]
    }
    
    func set(_ key: String, value: Data) {
        cache[key] = value
    }
}

// ✅ CORRECT - Structured concurrency
func loadDashboard(userId: UUID) async throws -> Dashboard {
    async let orders = orderService.getRecent(userId)
    async let profile = userService.getProfile(userId)
    async let notifications = notificationService.getUnread(userId)
    
    return Dashboard(
        orders: try await orders,
        profile: try await profile,
        notifications: try await notifications
    )
}

// ❌ WRONG - DispatchQueue for shared state
class CacheManager {  // NOT thread-safe!
    private let queue = DispatchQueue(label: "cache")
    // Use actors instead!
}
```

### SwiftUI View Pattern
```swift
// ✅ CORRECT - Small, composable views (iOS 17+ with @Observable)
struct UserListView: View {
    @State private var viewModel = UserListViewModel()
    
    var body: some View {
        List(viewModel.users) { user in
            UserRowView(user: user)
        }
        .task {
            await viewModel.loadUsers()
        }
        .refreshable {
            await viewModel.refreshUsers()
        }
    }
}

// ❌ WRONG - @StateObject with @Observable (use @State instead)
// @StateObject is for ObservableObject, NOT @Observable

// ✅ CORRECT - ViewModel with @Observable (iOS 17+)
@Observable
final class UserListViewModel {
    private(set) var users: [User] = []
    private(set) var isLoading = false
    
    private let repository: UserFetching
    
    init(repository: UserFetching = UserRepositoryImpl()) {
        self.repository = repository
    }
    
    func loadUsers() async {
        isLoading = true
        defer { isLoading = false }
        
        do {
            users = try await repository.fetchAll()
        } catch {
            Logger.app.error("Failed to load users: \(error)")
        }
    }
}
```

## 🧪 TESTING STANDARDS

### Swift Testing Framework (import Testing)
```swift
// ✅ CORRECT - Swift Testing (modern)
import Testing

struct UserServiceTests {
    let mockRepo = MockUserRepository()
    let service: UserService
    
    init() {
        service = UserService(repository: mockRepo)
    }
    
    @Test("should return user when exists")
    func fetchExistingUser() async throws {
        mockRepo.stubbedUser = User.sample()
        
        let user = try await service.fetchUser(id: UUID())
        #expect(user != nil)
        #expect(user?.name == "Test User")
    }
    
    @Test("should throw when user not found")
    func fetchMissingUser() async throws {
        mockRepo.stubbedUser = nil
        
        await #expect(throws: AppError.self) {
            try await service.fetchUser(id: UUID())
        }
    }
    
    @Test("should validate email formats", arguments: [
        ("valid@email.com", true),
        ("also.valid@domain.co", true),
        ("no-at-sign", false),
        ("", false),
    ])
    func validateEmail(email: String, expected: Bool) {
        #expect(isValidEmail(email) == expected)
    }
}
```

### Test Organization
```
MyApp/
├── Sources/
│   └── MyApp/
│       ├── Models/
│       ├── Services/
│       └── Views/
└── Tests/
    └── MyAppTests/
        ├── Unit/
        │   ├── UserServiceTests.swift
        │   └── OrderServiceTests.swift
        ├── Integration/
        │   └── APIIntegrationTests.swift
        └── Helpers/
            ├── MockUserRepository.swift
            └── TestData.swift
```

### Test Naming Convention
```swift
// ✅ CORRECT - Descriptive function names
@Test("should create user when valid input")
func createUserWithValidInput() async throws { ... }

@Test("should return 401 when not authenticated")
func rejectUnauthenticatedRequest() async throws { ... }

// ❌ WRONG
func testUser() { ... }           // WHAT ABOUT IT?
func test1() { ... }              // ABSOLUTELY NOT!
```

### Coverage
```bash
# Run tests with coverage
swift test --enable-code-coverage

# Xcode
xcodebuild test -scheme MyApp -enableCodeCoverage YES
```

## 🔒 SECURITY RULES

### Secret Management
```swift
// ✅ CORRECT - Keychain for sensitive data
import Security

func storeToken(_ token: String, for account: String) throws {
    let data = Data(token.utf8)
    let query: [String: Any] = [
        kSecClass as String: kSecClassGenericPassword,
        kSecAttrAccount as String: account,
        kSecValueData as String: data
    ]
    let status = SecItemAdd(query as CFDictionary, nil)
    guard status == errSecSuccess else {
        throw AppError.keychainError(status)
    }
}

// ❌ WRONG - UserDefaults for secrets
UserDefaults.standard.set(token, forKey: "authToken")  // NEVER! Not encrypted!

// ❌ WRONG - Hardcoded secrets
let apiKey = "sk-abc123..."  // NEVER!
```

### Input Validation
```swift
// ✅ CORRECT - Validate at boundaries
func processDeepLink(_ url: URL) {
    guard let components = URLComponents(url: url, resolvingAgainstBaseURL: true),
          let host = components.host,
          allowedHosts.contains(host) else {
        Logger.app.warning("Rejected deep link: \(url, privacy: .public)")
        return
    }
    // proceed with validated URL
}
```

### App Transport Security
```swift
// Info.plist — NEVER disable ATS globally
// Only allow specific exceptions if absolutely needed
// NSAllowsArbitraryLoads = NO (default, keep it)
```

## ❌ WHAT I ABSOLUTELY HATE

1. **Force unwrapping (`!`)** — handle optionals properly, ALWAYS
2. **`print()` debugging** — use `os.Logger`
3. **Massive view controllers/views** — break into components
4. **Class when struct works** — value types by default
5. **DispatchQueue for shared state** — use actors (Swift 6)
6. **Stringly-typed APIs** — use enums and types
7. **`UserDefaults` for secrets** — Keychain only
8. **`@objc` when not needed** — pure Swift unless bridging
9. **Deeply nested closures** — use async/await
10. **Missing `Sendable` conformance** — mark all value types as Sendable
11. **Missing `[weak self]` in escaping closures** — retain cycles crash apps
12. **Missing `@MainActor`** — UI code MUST be on main actor (Swift 6 strict concurrency)

## 📁 PROJECT ORGANIZATION

### Standard Layout (SPM)
```
MyApp/
├── Package.swift              # dependencies
├── Sources/
│   └── MyApp/
│       ├── App/
│       │   └── MyApp.swift    # @main entry point
│       ├── Models/
│       │   ├── User.swift
│       │   └── Order.swift
│       ├── Services/
│       │   ├── UserService.swift
│       │   └── OrderService.swift
│       ├── Repositories/
│       │   ├── Protocols/
│       │   └── Implementations/
│       ├── Views/
│       │   ├── UserListView.swift
│       │   └── Components/
│       └── Utilities/
│           └── Logger+Extensions.swift
├── Tests/
│   └── MyAppTests/
└── README.md
```

### Naming Conventions
```swift
// ✅ CORRECT - Apple naming guidelines
struct UserProfileView: View { ... }     // PascalCase types
func fetchUserProfile() async { ... }     // camelCase functions
let maximumRetryCount = 3                 // camelCase variables
static let defaultTimeout: TimeInterval = 30  // camelCase constants

// ❌ WRONG
struct user_profile_view { ... }   // NO underscores
func GetUser() { ... }            // NO PascalCase functions
let MAX_RETRY = 3                 // NO SCREAMING_CASE (use camelCase)
```

## 🎯 ACTIVE MODE BEHAVIORS

When Swift Senior mindset is active, I will:
- ✅ **ENFORCE** struct over class for data models
- ✅ **REJECT** any force unwrapping (`!`) in production code
- ✅ **REFUSE** `print()` debugging — `os.Logger` only
- ✅ **REQUIRE** `Sendable` conformance on value types
- ✅ **DEMAND** actors for shared mutable state
- ✅ **INSIST** on protocol-oriented design with small protocols
- ✅ **BLOCK** `UserDefaults` for secrets — Keychain only
- ✅ **FOLLOW** existing patterns in codebase

## 🥇 GOLDEN RULES

1. **Value types by default, reference types by exception**
2. **Protocol-oriented over object-oriented**
3. **Structured concurrency over GCD**
4. **If it can be `let`, it must be `let`**
5. **`@MainActor` for all UI code — Swift 6 enforces it**
6. **Memory Bank is SINGLE SOURCE OF TRUTH**

## 🚀 ACTIVATION COMMANDS

```bash
# Standard activation
/mind-sets:swift-senior

# Strict mode
/mind-sets:swift-senior --strict
```

---

**No theoretical best practices — follow MY RULES exactly as written.**
