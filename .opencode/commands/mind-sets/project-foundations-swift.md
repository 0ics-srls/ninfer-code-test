---
description: Project Foundations - Swift/iOS implementation
---
# Project Foundations — Swift

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```yaml
# .swiftlint.yml — strict mode
strict: true
opt_in_rules:
  - force_unwrapping
  - implicitly_unwrapped_optional
  - discouraged_optional_boolean
  - missing_docs
```

```bash
# CI pipeline — zero tolerance
swiftlint lint --strict
swift build 2>&1 | grep -c warning  # must be 0
```

SwiftLint strict mode with force_unwrapping opt-in catches real bugs. Every force unwrap, unused import, and missing access control is caught. No warnings in CI, ever.

---

## 2. Central Dependency Management

```swift
// Package.swift — single source for ALL dependencies
let package = Package(
    name: "MyApp",
    platforms: [.iOS(.v17), .macOS(.v14)],
    dependencies: [
        .package(url: "https://github.com/pointfreeco/swift-composable-architecture", from: "1.0.0"),
        .package(url: "https://github.com/apple/swift-algorithms", from: "1.0.0"),
    ],
    targets: [
        .target(name: "MyApp", dependencies: [
            .product(name: "ComposableArchitecture", package: "swift-composable-architecture"),
        ]),
    ]
)
```

SPM `Package.swift` is the single source. `Package.resolved` committed for deterministic builds. Upgrading a dependency = single-line change. Prefer Apple frameworks over third-party when equivalent.

---

## 3. Versioning Strategy

```swift
// AppVersion.swift
enum AppVersion {
    static let current = Bundle.main.infoDictionary?["CFBundleShortVersionString"] as? String ?? "dev"
    static let build = Bundle.main.infoDictionary?["CFBundleVersion"] as? String ?? "0"
}
```

```bash
# Xcode — set via build settings or agvtool
agvtool new-marketing-version 1.2.3
agvtool new-version -all 42
```

The application reports its version at startup in logs. Marketing version (1.2.3) for users, build number for internal tracking. CI increments build number automatically.

---

## 4. Structured Error Handling

```swift
// AppError.swift — typed errors
enum AppError: LocalizedError {
    case notFound(entity: String, id: String)
    case validation(String)
    case unauthorized
    case networkError(underlying: Error)
    case unexpected(Error)
    
    var errorDescription: String? {
        switch self {
        case .notFound(let entity, let id): "The \(entity) '\(id)' was not found"
        case .validation(let message): message
        case .unauthorized: "You are not authorized to perform this action"
        case .networkError(let error): "Network error: \(error.localizedDescription)"
        case .unexpected(let error): "Unexpected error: \(error.localizedDescription)"
        }
    }
}

// Usage — typed throws (Swift 6+)
func getUser(id: UUID) async throws(AppError) -> User {
    guard let user = try await repository.findById(id) else {
        throw .notFound(entity: "User", id: id.uuidString)
    }
    return user
}
```

Every error has a typed case. `LocalizedError` for user-facing messages. Typed throws in Swift 6+ for compile-time error checking. Never catch `Error` without logging it.

---

## 5. Test Configuration (Fast/Slow Split)

```swift
// Unit tests — always run
struct UserServiceTests {
    @Test func shouldValidateEmail() { ... }
}

// Integration tests — tagged
@Suite("Integration", .tags(.integration))
struct APIIntegrationTests {
    @Test func shouldFetchRealUser() async throws { ... }
}

// Tag definition
extension Tag {
    @Tag static var integration: Self
    @Tag static var slow: Self
}
```

```bash
# Fast tests (default) — seconds
swift test --filter "!integration"

# Integration tests — on demand
swift test --filter "integration"

# Xcode — use test plans for fast/slow split
xcodebuild test -scheme MyApp -testPlan FastTests
```

---

## 6. Immutable DTOs

```swift
// Structs with let — immutable by default
struct UserDTO: Codable, Sendable {
    let id: UUID
    let name: String
    let email: String
    let createdAt: Date
}

// Copies with modification
extension UserDTO {
    func withEmail(_ email: String) -> UserDTO {
        UserDTO(id: id, name: name, email: email, createdAt: createdAt)
    }
}

// Collections — return copies
func listUsers() -> [UserDTO] {
    Array(cachedUsers)  // return copy, not internal reference
}
```

Swift structs are value types — naturally immutable with `let`. Use `let` for all properties. Return copies of internal collections. `Codable` for serialization, `Sendable` for concurrency safety.

---

## 7. Dependency Injection with Proper Registration

```swift
// Protocol-based DI with default parameters
final class OrderService {
    private let userRepo: UserFetching
    private let orderRepo: OrderRepository
    private let logger: Logger
    
    init(
        userRepo: UserFetching = UserRepositoryImpl(),
        orderRepo: OrderRepository = OrderRepositoryImpl(),
        logger: Logger = .app
    ) {
        self.userRepo = userRepo
        self.orderRepo = orderRepo
        self.logger = logger
    }
}

// App entry point — composition root
@main
struct MyApp: App {
    let userService: UserService
    let orderService: OrderService
    
    init() {
        let networkClient = URLSessionNetworkClient()
        let userRepo = APIUserRepository(client: networkClient)
        userService = UserService(repository: userRepo)
        orderService = OrderService(userRepo: userRepo)
    }
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environment(userService)
                .environment(orderService)
        }
    }
}
```

App entry point is the composition root. No DI frameworks — Swift initializers with default parameters. Tests override defaults with mocks. SwiftUI `@Environment` for view-level injection.

---

## 8. Centralized Configuration with Validation

```swift
// Configuration.swift — from environment/plist
struct AppConfiguration: Sendable {
    let apiBaseURL: URL
    let apiKey: String
    let environment: Environment
    let timeout: TimeInterval
    
    enum Environment: String, Sendable {
        case development, staging, production
    }
    
    static func load() throws -> AppConfiguration {
        guard let apiBase = ProcessInfo.processInfo.environment["API_BASE_URL"],
              let url = URL(string: apiBase) else {
            throw AppError.validation("API_BASE_URL is required")
        }
        
        guard let apiKey = ProcessInfo.processInfo.environment["API_KEY"],
              !apiKey.isEmpty else {
            throw AppError.validation("API_KEY is required")
        }
        
        return AppConfiguration(
            apiBaseURL: url,
            apiKey: apiKey,
            environment: Environment(rawValue: ProcessInfo.processInfo.environment["ENV"] ?? "development") ?? .development,
            timeout: 30
        )
    }
}
```

All configuration validated at startup. Missing required values fail immediately. Sensible defaults for non-critical values. Build-time configuration via `.xcconfig` files.

---

## 9. Structured File Logging

```swift
// Logger+Extensions.swift
import os

extension Logger {
    private static let subsystem = Bundle.main.bundleIdentifier ?? "com.myapp"
    
    static let app = Logger(subsystem: subsystem, category: "app")
    static let network = Logger(subsystem: subsystem, category: "network")
    static let database = Logger(subsystem: subsystem, category: "database")
    static let auth = Logger(subsystem: subsystem, category: "auth")
}

// Usage — structured with privacy
Logger.network.info("Request: \(method, privacy: .public) \(url, privacy: .public)")
Logger.auth.error("Auth failed: \(error, privacy: .private)")
Logger.database.debug("Query: \(query, privacy: .private(mask: .hash))")
```

Use `os.Logger` (unified logging). Categories for filtering in Console.app. Privacy annotations on ALL interpolated values. `.public` for non-sensitive data, `.private` for PII.

---

## 10. Zero-Dependency Core Module

```
MyApp/
├── Core/           → zero external dependencies, only Foundation
│   ├── Models/     → domain models (structs)
│   ├── Protocols/  → service interfaces
│   └── Errors/     → error types
├── Services/       → depends on Core only
├── Infrastructure/ → depends on Core + external SDKs
└── UI/             → depends on Core + SwiftUI
```

Core contains: protocols, DTOs (structs), enums, error types. No implementations. Every other module imports Core. Core imports only Foundation.

---

## 11. Interface-First Design

```swift
// Protocols in Core — implementations elsewhere
protocol UserRepository: Sendable {
    func findById(_ id: UUID) async throws -> User?
    func save(_ user: User) async throws -> User
}

protocol OrderRepository: Sendable {
    func findByUser(_ userId: UUID) async throws -> [Order]
}
```

Small protocols (1-3 methods). `Sendable` conformance for concurrency. Defined in Core module. Implementations in Infrastructure. Tests use mock conformances.

---

## 12. Internal/Private by Default

```swift
// Default to private, expose only what's needed
public struct UserService {
    private let repository: UserRepository
    private let cache: CacheManager
    
    public init(repository: UserRepository) {
        self.repository = repository
        self.cache = CacheManager()
    }
    
    public func getUser(id: UUID) async throws -> User { ... }
    
    // Internal helpers
    private func validateEmail(_ email: String) -> Bool { ... }
}
```

`private` by default for all properties and helpers. `public` only for the API surface. `internal` (default access) within modules. `fileprivate` only when necessary for extensions in same file.

---

## 13. Test Module per Production Module

```
MyApp/
├── Sources/
│   ├── Core/
│   ├── Services/
│   └── Infrastructure/
└── Tests/
    ├── CoreTests/           → tests Core only
    ├── ServicesTests/       → tests Services only
    └── IntegrationTests/    → tests full stack
```

Each test target tests exactly one source module. A failure tells you which module is broken. Shared test helpers in a `TestSupport` module.

---

## 14. Convention Over Configuration

```swift
// Codable — convention-based serialization
struct UserResponse: Codable {
    let userId: UUID         // auto-maps to "userId" in JSON
    let displayName: String  // auto-maps to "displayName"
}

// Custom key strategy for snake_case APIs
let decoder = JSONDecoder()
decoder.keyDecodingStrategy = .convertFromSnakeCase
decoder.dateDecodingStrategy = .iso8601
```

`Codable` derives handle serialization by convention. Key strategies for API format differences. Module structure mirrors feature areas. SPM target naming implies purpose.

---

## 15. Deterministic Build Output

```bash
# SPM — deterministic by default
swift build -c release

# Xcode — archive for distribution
xcodebuild archive -scheme MyApp -archivePath build/MyApp.xcarchive

# Reproducible builds — pin toolchain
echo "swift-6.0-RELEASE" > .swift-version
```

`Package.resolved` committed for reproducible dependency resolution. `.swift-version` pins the toolchain. Xcode archive for consistent distribution builds. CI uses identical Xcode version.

---

## 16. Black Box Composition

```swift
// Each module is a black box with clear public API
// Sources/OrderModule/OrderModule.swift (public surface)
public struct OrderService { ... }
public struct Order: Sendable { ... }
public struct OrderId: Sendable { ... }
public protocol OrderRepository: Sendable { ... }

// Internal implementation details
internal struct OrderValidator { ... }
internal final class OrderCache { ... }
```

Each SPM target or module is a black box. Public types define the contract. Internal types are hidden. Module boundaries enforce isolation at compile time. Test each module independently via its public API.

---

## Verification Commands

```bash
# Check formatting
swiftformat --lint .

# Check all lints pass
swiftlint lint --strict

# Build clean
swift build -c release 2>&1 | grep -c warning  # must be 0

# Run all tests
swift test

# Run tests with coverage
swift test --enable-code-coverage

# Check for unused imports (if using periphery)
periphery scan

# Xcode archive
xcodebuild archive -scheme MyApp -destination generic/platform=iOS
```
