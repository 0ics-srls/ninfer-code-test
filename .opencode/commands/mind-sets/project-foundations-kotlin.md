---
description: Project Foundations - Kotlin/KMP implementation
---
# Project Foundations — Kotlin/KMP

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```kotlin
// build.gradle.kts (root)
kotlin {
    compilerOptions {
        allWarningsAsErrors.set(true)
    }
}
```

Add static analysis that fails the build:
- **detekt**: static analysis for code smells and complexity — fails build on issues
- **ktlint**: code style enforcement aligned with Kotlin coding conventions
- Both configured in the root `build.gradle.kts` to apply to all modules

```kotlin
// build.gradle.kts (root) — detekt + ktlint
plugins {
    id("io.gitlab.arturbosch.detekt") version libs.versions.detekt
    id("org.jlleitschko.ktlint") version libs.versions.ktlint
}

detekt {
    buildUponDefaultConfig = true
    allRules = false
    config.setFrom("$rootDir/config/detekt/detekt.yml")
}

tasks.withType<Detekt>().configureEach {
    reports { html.required.set(true) }
}
```

All configured to fail the build, not just warn.

---

## 2. Central Dependency Management

**Gradle Version Catalog:**
```toml
# gradle/libs.versions.toml
[versions]
kotlin = "2.1.0"
koin = "4.0.0"
ktor = "3.0.0"
kotlinx-coroutines = "1.9.0"
kotlinx-serialization = "1.7.0"
kermit = "2.0.4"
detekt = "1.23.7"

[libraries]
koin-core = { module = "io.insert-koin:koin-core", version.ref = "koin" }
koin-android = { module = "io.insert-koin:koin-android", version.ref = "koin" }
ktor-client-core = { module = "io.ktor:ktor-client-core", version.ref = "ktor" }
kotlinx-coroutines-core = { module = "org.jetbrains.kotlinx:kotlinx-coroutines-core", version.ref = "kotlinx-coroutines" }
kotlinx-serialization-json = { module = "org.jetbrains.kotlinx:kotlinx-serialization-json", version.ref = "kotlinx-serialization" }

[plugins]
kotlinMultiplatform = { id = "org.jetbrains.kotlin.multiplatform", version.ref = "kotlin" }
kotlinxSerialization = { id = "org.jetbrains.kotlin.plugin.serialization", version.ref = "kotlin" }
```

ALL versions declared in `[versions]`, ALL libraries in `[libraries]`. Never hardcode versions in `build.gradle.kts` files. Koin version drives the compatibility matrix for DI across platforms.

---

## 3. Versioning Strategy

```kotlin
// gradle.properties — single source of truth
project.version=1.0.0

// OR root build.gradle.kts
allprojects {
    version = "1.0.0"
}
```

```kotlin
// shared/src/commonMain/kotlin/.../AppVersion.kt
object AppVersion {
    const val NAME = "1.0.0" // synced with gradle.properties via buildConfigField
}
```

The application reports its version at startup. Follow SemVer. For KMP, the version is defined once in the root project and inherited by all modules.

---

## 4. Structured Error Handling

```kotlin
// shared/src/commonMain/kotlin/.../error/AppError.kt
sealed class AppError(val code: ErrorCode, val message: String) {
    class NotFound(message: String, val candidates: List<String> = emptyList()) :
        AppError(ErrorCode.NOT_FOUND, message)
    class InvalidParams(message: String, val fields: List<String> = emptyList()) :
        AppError(ErrorCode.INVALID_PARAMS, message)
    class Unauthorized(message: String) :
        AppError(ErrorCode.UNAUTHORIZED, message)
    class Timeout(message: String) :
        AppError(ErrorCode.TIMEOUT, message)
    class Internal(message: String, val cause: Throwable? = null) :
        AppError(ErrorCode.INTERNAL_ERROR, message)
}

enum class ErrorCode {
    NOT_FOUND, INVALID_PARAMS, UNAUTHORIZED,
    TIMEOUT, NOT_SUPPORTED, INTERNAL_ERROR
}

// Result type in commonMain
sealed class AppResult<out T> {
    data class Success<T>(val data: T) : AppResult<T>()
    data class Failure(val error: AppError) : AppResult<Nothing>()
}
```

Sealed classes for Result types in `commonMain`. Use `expect`/`actual` for platform-specific error mapping. Never throw raw exceptions across module boundaries — return `AppResult`.

---

## 5. Test Configuration (Fast/Slow Split)

```kotlin
// shared/src/commonTest/ — fast tests, no platform dependencies
class UserValidationTest {
    @Test
    fun `valid email passes validation`() {
        val result = validateEmail("user@example.com")
        assertTrue(result.isValid)
    }
}

// shared/src/androidTest/ — platform-specific tests
class AndroidStorageTest { ... }

// shared/src/jvmTest/ — JVM integration tests
@Tag("slow")
class DatabaseIntegrationTest { ... }
```

```kotlin
// build.gradle.kts — exclude slow tests by default
tasks.withType<Test>() {
    useJUnitPlatform {
        excludeTags("slow", "e2e")
    }
}
```

```bash
# Fast tests (default)
./gradlew test
# Slow tests only
./gradlew test -Dinclude.tags=slow
# All tests
./gradlew test -Dinclude.tags=
```

`commonTest` for shared logic (fast, no platform deps). `{platform}Test` for platform-specific tests. Every `{platform}Main` has a corresponding `{platform}Test`.

---

## 6. Immutable DTOs

```kotlin
// data class with val only — immutable by design
@Serializable
data class UserDto(
    val name: String,
    val email: String,
    val createdAt: Instant,
)

@Serializable
data class ApiResponse<T>(
    val data: T,
    val totalCount: Int,
)

// Sealed classes for variants
@Serializable
sealed class PaymentMethod {
    @Serializable data class CreditCard(val last4: String, val brand: String) : PaymentMethod()
    @Serializable data class BankTransfer(val iban: String) : PaymentMethod()
}

// Modifications via copy()
val updated = user.copy(email = "new@email.com")
```

Use `data class` with `val` properties only. `copy()` for modifications. Sealed classes for variants. `kotlinx.serialization` `@Serializable` for JSON.

---

## 7. Dependency Injection with Proper Registration

```kotlin
// shared/src/commonMain/kotlin/.../di/SharedModule.kt
val sharedModule = module {
    single<UserRepository> { UserRepositoryImpl(get()) }
    factory<GetUserUseCase> { GetUserUseCase(get()) }
}

// shared/src/androidMain/kotlin/.../di/PlatformModule.kt
actual val platformModule = module {
    single<DatabaseDriver> { AndroidDatabaseDriver(get()) }
    single<FileStorage> { AndroidFileStorage(get()) }
}

// composeApp/src/commonMain/kotlin/.../App.kt — composition root
fun initKoin() {
    startKoin {
        modules(
            sharedModule,
            platformModule,
            viewModelModule,
        )
    }
}
```

Koin modules in `commonMain` for shared logic. Platform-specific modules in `{platform}Main`. `single {}`, `factory {}`, `viewModel {}` declarations. Module composition at the app entry point — readable in 10 seconds.

---

## 8. Centralized Configuration with Validation

```kotlin
// shared/src/commonMain/kotlin/.../config/AppConfig.kt
data class AppConfig(
    val apiBaseUrl: String,
    val debugMode: Boolean,
    val maxRetries: Int,
) {
    init {
        require(apiBaseUrl.isNotBlank()) { "apiBaseUrl must not be blank" }
        require(maxRetries > 0) { "maxRetries must be positive" }
    }
}

// expect declaration
expect fun createAppConfig(): AppConfig

// shared/src/androidMain/kotlin/.../config/AppConfig.android.kt
actual fun createAppConfig(): AppConfig = AppConfig(
    apiBaseUrl = BuildConfig.API_BASE_URL,
    debugMode = BuildConfig.DEBUG,
    maxRetries = 3,
)

// shared/src/iosMain/kotlin/.../config/AppConfig.ios.kt
actual fun createAppConfig(): AppConfig = AppConfig(
    apiBaseUrl = NSBundle.mainBundle.objectForInfoDictionaryKey("ApiBaseUrl") as String,
    debugMode = Platform.isDebugBinary,
    maxRetries = 3,
)
```

`expect`/`actual` for platform config (API URLs, feature flags). Validation at startup: missing required config = crash with clear message. `AppConfig` data class in `commonMain`, actual implementations per platform.

---

## 9. Structured File Logging

```kotlin
// shared/src/commonMain/kotlin/.../logging/AppLogger.kt
import co.touchlab.kermit.Logger
import co.touchlab.kermit.Severity

val appLogger = Logger.withTag("MyApp")

// Usage
appLogger.i { "Processing started for user=$userId" }
appLogger.e(exception) { "Failed to fetch data" }

// Configuration at app entry point
Logger.setMinSeverity(Severity.Info)
Logger.setLogWriters(platformLogWriter()) // platform-specific writer
```

```kotlin
// shared/src/androidMain/ — Logcat writer (default on Android)
// shared/src/iosMain/ — OSLog writer (default on iOS)
// shared/src/jvmMain/ — SLF4J bridge or file writer

// jvmMain custom writer example
actual fun platformLogWriter(): LogWriter = SLF4JLogWriter()
```

Kermit as the KMP-native logging framework. Severity-based filtering. Platform-specific log writers (Logcat for Android, OSLog for iOS, SLF4J for JVM). Code uses Kermit abstractions — never reference platform logging directly.

---

## 10. Zero-Dependency Core Module

```
shared/src/commonMain/kotlin/com/example/project/
  domain/
    model/        -> DTOs (data classes), enums, value objects
    repository/   -> repository interfaces
    usecase/      -> use case interfaces and implementations
    error/        -> error types, error codes (AppError, AppResult)
```

```kotlin
// shared/build.gradle.kts — commonMain dependencies
kotlin {
    sourceSets {
        commonMain.dependencies {
            // Only Kotlin stdlib (implicit) + kotlinx types
            implementation(libs.kotlinx.coroutines.core)
            implementation(libs.kotlinx.serialization.json)
            // NO framework dependencies here
        }
    }
}
```

`shared/src/commonMain/kotlin/.../domain/` contains pure domain models and interfaces. No framework dependencies in the domain layer. Only Kotlin stdlib + kotlinx types. Interfaces for repositories and use cases defined here.

---

## 11. Interface-First Design

```kotlin
// shared/src/commonMain/ — the contracts
interface UserRepository {
    suspend fun getById(id: String): AppResult<UserDto>
    suspend fun save(user: UserDto): AppResult<Unit>
}

interface AuthService {
    suspend fun login(credentials: Credentials): AppResult<AuthToken>
    suspend fun logout(): AppResult<Unit>
}

// shared/src/androidMain/ — platform implementation
internal class AndroidUserRepository(
    private val dao: UserDao
) : UserRepository {
    override suspend fun getById(id: String): AppResult<UserDto> { ... }
    override suspend fun save(user: UserDto): AppResult<Unit> { ... }
}
```

Interfaces in `commonMain`, implementations in `{platform}Main` via `expect`/`actual` or Koin bindings. Repository interfaces in shared domain, implementations with platform data sources. Use cases depend only on interfaces.

---

## 12. Internal/Private by Default

```kotlin
// Public API — explicitly public
public interface UserRepository {
    suspend fun getById(id: String): AppResult<UserDto>
}

public data class UserDto(val name: String, val email: String)

// Module-scoped — internal visibility
internal class UserRepositoryImpl(
    private val dataSource: UserDataSource
) : UserRepository { ... }

// Implementation detail — private
private fun mapToDto(entity: UserEntity): UserDto { ... }
```

`internal` visibility for module-scoped code. Only public API surface is explicitly `public`. Koin modules expose only what is needed. `internal` classes for implementation details. In KMP, `internal` is enforced at the module boundary.

---

## 13. Test Module per Production Module

```
shared/src/
  commonMain/    -> shared business logic
  commonTest/    -> tests for shared logic (mirrors commonMain structure)
  androidMain/   -> Android-specific code
  androidTest/   -> Android-specific tests
  iosMain/       -> iOS-specific code
  iosTest/       -> iOS-specific tests
  jvmMain/       -> JVM-specific code
  jvmTest/       -> JVM-specific tests
composeApp/src/
  commonMain/    -> UI code
  commonTest/    -> UI tests
server/src/
  jvmMain/       -> server code
  jvmTest/       -> server tests
```

```kotlin
// commonTest — fakes with behavior, not empty stubs
class FakeUserRepository : UserRepository {
    private val users = mutableMapOf<String, UserDto>()

    override suspend fun getById(id: String): AppResult<UserDto> =
        users[id]?.let { AppResult.Success(it) }
            ?: AppResult.Failure(AppError.NotFound("User $id not found"))

    override suspend fun save(user: UserDto): AppResult<Unit> {
        users[user.id] = user
        return AppResult.Success(Unit)
    }
}
```

Every `{platform}Main` source set has its `{platform}Test` counterpart. `commonTest` mirrors `commonMain` structure. Fakes with behavior (not empty stubs) in `commonTest`.

---

## 14. Convention Over Configuration

Standard KMP source set naming — no custom names:
- `commonMain`, `commonTest` — shared code
- `androidMain`, `androidTest` — Android
- `iosMain`, `iosTest` — iOS (covers `iosArm64`, `iosSimulatorArm64`, etc.)
- `jvmMain`, `jvmTest` — JVM/Desktop

```
com.example.project/
  domain/       -> models, interfaces, use cases
  data/         -> repositories, data sources
  di/           -> Koin modules
  ui/           -> Compose UI (in composeApp)
```

Standard module names: `shared`, `composeApp`, `server`. Package structure follows reverse-DNS: `com.example.project.{module}`. Follow JetBrains standard project structure from kmp.jetbrains.com.

---

## 15. Deterministic Build Output

```kotlin
// gradle/wrapper/gradle-wrapper.properties — committed to VCS
distributionUrl=https\://services.gradle.org/distributions/gradle-8.10-bin.zip
```

```properties
# gradle.properties — reproducible build settings
org.gradle.jvmargs=-Xmx2048M
kotlin.code.style=official
kotlin.mpp.stability.nowarn=true
android.useAndroidX=true
```

```toml
# gradle/libs.versions.toml — exact versions, no dynamic ranges
[versions]
kotlin = "2.1.0"       # exact, not "2.+"
ktor = "3.0.0"         # exact, not "latest.release"
```

Gradle wrapper committed (`gradle/wrapper/`). `libs.versions.toml` pins exact versions (no dynamic ranges like `+` or `latest.release`). `gradle.properties` with reproducible build settings.

---

## 16. Black Box Composition

```
project/
  shared/         -> business logic black box (commonMain + platform specifics)
  composeApp/     -> UI black box (Compose Multiplatform)
  server/         -> backend black box (Ktor, JVM only)
```

```kotlin
// shared/ — public API via explicit exports
// shared/src/commonMain/kotlin/.../SharedApi.kt
public interface SharedApi {
    fun getUserService(): UserService
    fun getAuthService(): AuthService
}

// composeApp/ depends on shared/ — only through interfaces
// build.gradle.kts
kotlin {
    sourceSets {
        commonMain.dependencies {
            implementation(project(":shared"))
        }
    }
}

// server/ — Ktor black box, depends on shared/
// server/build.gradle.kts
dependencies {
    implementation(project(":shared"))
    implementation(libs.ktor.server.core)
}
```

`shared/` = business logic black box (commonMain + platform specifics). `composeApp/` = UI black box (Compose Multiplatform). `server/` = backend black box (Ktor, JVM only). Each module has a clear public API with `internal` implementation. Modules communicate through interfaces defined in the shared domain.

---

## Verification Commands

```bash
# Check allWarningsAsErrors is enabled
grep -r "allWarningsAsErrors" build.gradle.kts */build.gradle.kts
# Check version catalog exists and is used
cat gradle/libs.versions.toml
# Check Gradle wrapper is committed
ls gradle/wrapper/gradle-wrapper.properties
# Check no hardcoded versions in build files
grep -rn "version = \"" --include="*.kts" | grep -v "build.gradle.kts:.*project.version"
# Build with all checks (warnings as errors + detekt + ktlint)
./gradlew build
# Run detekt static analysis
./gradlew detekt
# Run ktlint check
./gradlew ktlintCheck
# Run fast tests only (commonTest + platform tests, excluding slow)
./gradlew test
# Run all tests including slow/integration
./gradlew test -Dinclude.tags=
# Check for scattered config access (should use AppConfig)
grep -rn "System.getenv\|System.getProperty" --include="*.kt" src/ | grep -v Config
# Check domain layer has no framework imports
grep -rn "import android\.\|import io.ktor\.\|import org.koin" shared/src/commonMain/kotlin/**/domain/
# Verify source set structure
find . -name "commonMain" -o -name "commonTest" -o -name "androidMain" -o -name "iosMain" | sort
```
