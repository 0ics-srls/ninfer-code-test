---
description: Project Foundations - Java/Spring Boot implementation
---
# Project Foundations — Java/Spring Boot

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```xml
<!-- pom.xml — compiler plugin -->
<plugin>
  <groupId>org.apache.maven.plugins</groupId>
  <artifactId>maven-compiler-plugin</artifactId>
  <configuration>
    <compilerArgs>
      <arg>-Xlint:all</arg>
      <arg>-Werror</arg>
    </compilerArgs>
  </configuration>
</plugin>
```

Add static analysis that fails the build:
- **Checkstyle**: code style violations = build failure
- **SpotBugs**: bug patterns detected at compile time
- **ErrorProne**: common Java mistakes caught by the compiler

All configured to fail the build, not just warn.

---

## 2. Central Dependency Management

**Maven BOM (Bill of Materials):**
```xml
<!-- parent pom.xml -->
<dependencyManagement>
  <dependencies>
    <dependency>
      <groupId>org.springframework.boot</groupId>
      <artifactId>spring-boot-dependencies</artifactId>
      <version>3.4.0</version>
      <type>pom</type>
      <scope>import</scope>
    </dependency>
    <!-- All other versions here -->
  </dependencies>
</dependencyManagement>
```

**Gradle platform:**
```kotlin
// build.gradle.kts
dependencies {
    implementation(platform("org.springframework.boot:spring-boot-dependencies:3.4.0"))
}
```

Child modules declare dependencies WITHOUT versions. One place to upgrade, one place to audit.

---

## 3. Versioning Strategy

```xml
<!-- pom.xml -->
<version>1.0.${revision}</version>

<!-- Set via CI or local build -->
<!-- mvn -Drevision=42 package -->
```

Or use `maven-git-commit-id-plugin` to embed git hash. The application reports version at startup:

```java
@Value("${app.version:dev}")
private String version;

@PostConstruct
void logVersion() {
    log.info("Application started, version: {}", version);
}
```

Expose via `/actuator/info` endpoint.

---

## 4. Structured Error Handling

```java
public record AppError(ErrorCode code, String message,
    List<String> candidates, String suggestion) {

    public AppError(ErrorCode code, String message) {
        this(code, message, List.of(), null);
    }
}

public enum ErrorCode {
    NOT_FOUND, INVALID_PARAMS, UNAUTHORIZED,
    TIMEOUT, NOT_SUPPORTED, INTERNAL_ERROR
}

// Global exception handler
@RestControllerAdvice
public class GlobalExceptionHandler {
    @ExceptionHandler(AppException.class)
    ResponseEntity<AppError> handle(AppException ex) {
        return ResponseEntity.status(ex.getStatus()).body(ex.toError());
    }
}
```

Every error has a typed code. `@RestControllerAdvice` ensures consistent formatting across all endpoints.

---

## 5. Test Configuration (Fast/Slow Split)

```java
@Tag("slow")
@SpringBootTest
class DatabaseIntegrationTest { ... }

@Tag("e2e")
class EndToEndTest { ... }
```

```xml
<!-- pom.xml — exclude slow tests by default -->
<plugin>
  <groupId>org.apache.maven.plugins</groupId>
  <artifactId>maven-surefire-plugin</artifactId>
  <configuration>
    <excludedGroups>slow,e2e</excludedGroups>
  </configuration>
</plugin>
```

```bash
# Fast tests (default)
mvn test
# Slow tests only
mvn test -Dgroups=slow
# All tests
mvn test -Dgroups=
```

---

## 6. Immutable DTOs

```java
// Java 16+ records — immutable by design
public record UserDto(String name, String email, Instant createdAt) {}
public record ApiResponse<T>(T data, int totalCount) {}

// Collections always unmodifiable
public record SearchResult(List<UserDto> items, int total) {
    public SearchResult {
        items = List.copyOf(items); // defensive copy
    }
}
```

Use `record` for all DTOs. Collections via `List.of()`, `List.copyOf()`, `Map.of()`. Never expose mutable collections.

---

## 7. Dependency Injection with Proper Registration

```java
// Each feature exposes a @Configuration class
@Configuration
public class SecurityConfig {
    @Bean
    public PasswordEncoder passwordEncoder() {
        return new BCryptPasswordEncoder();
    }

    @Bean
    public AuthService authService(UserRepository repo, PasswordEncoder encoder) {
        return new AuthServiceImpl(repo, encoder);
    }
}

// Main class — clean composition via @Import or component scanning
@SpringBootApplication
@Import({SecurityConfig.class, MessagingConfig.class})
public class Application { }
```

Each `@Configuration` is self-contained. Dependencies between features are explicit via `@Bean` parameters.

---

## 8. Centralized Configuration with Validation

```yaml
# application.yml — ALL values explicit
app:
  data-path: /app/data
  max-retries: 3
  timeout-seconds: 30
```

```java
@ConfigurationProperties(prefix = "app")
@Validated
public record AppProperties(
    @NotBlank String dataPath,
    @Min(1) int maxRetries,
    @Min(1) int timeoutSeconds
) {}

// Enable in main class
@EnableConfigurationProperties(AppProperties.class)
```

Validation at startup via `@Validated` + Jakarta annotations. Missing or invalid config = fail fast with clear message. Consumed via constructor injection of `AppProperties`.

---

## 9. Structured File Logging

```yaml
# application.yml — Logback configured via Spring
logging:
  level:
    root: INFO
    com.myapp: DEBUG
  file:
    name: logs/app.log
  logback:
    rollingpolicy:
      max-file-size: 10MB
      max-history: 30
```

```java
// In code — SLF4J only, never reference Logback directly
@Slf4j  // Lombok
public class MyService {
    public void process() {
        log.info("Processing started");
    }
}

// Or without Lombok
private static final Logger log = LoggerFactory.getLogger(MyService.class);
```

Only `application.yml` configures logging. Code uses SLF4J abstractions only. In tests, set level to WARN to reduce noise.

---

## 10. Zero-Dependency Core Module

```
myapp-core/
  src/main/java/com/myapp/core/
    model/        → DTOs (records), enums
    service/      → service interfaces
    error/        → error types, error codes
    repository/   → repository interfaces
```

```xml
<!-- myapp-core/pom.xml — minimal dependencies -->
<dependencies>
  <!-- Only Jakarta validation annotations if needed -->
  <dependency>
    <groupId>jakarta.validation</groupId>
    <artifactId>jakarta.validation-api</artifactId>
  </dependency>
</dependencies>
```

Core has interfaces, records, enums. No Spring, no JPA, no implementation dependencies. Every other module depends on core.

---

## 11. Interface-First Design

```java
// In core — the contract
public interface UserService {
    Optional<UserDto> getById(Long id);
    UserDto create(CreateUserDto data);
}

public interface UserRepository {
    Optional<User> findById(Long id);
    User save(User user);
}
```

Every significant service has an interface in core. Implementations in separate modules annotated with `@Service` / `@Repository`. Testing uses Mockito against interfaces.

---

## 12. Internal/Private by Default

```java
// Package-private class (no access modifier)
class UserServiceImpl implements UserService { ... }

// Only interfaces and DTOs are public
public interface UserService { ... }
public record UserDto(...) { }

// Module system (Java 9+)
module com.myapp.core {
    exports com.myapp.core.model;
    exports com.myapp.core.service;
    // internals NOT exported
}
```

Default to package-private. Only `public` what consumers need. Use `module-info.java` for strong encapsulation in modular projects.

---

## 13. Test Module per Production Module

```
myapp-core/     → src/test/java/  (unit tests for core)
myapp-server/   → src/test/java/  (integration tests)
myapp-plugin/   → src/test/java/  (plugin tests)
```

Maven convention: `src/test/java/` mirrors `src/main/java/`. Each module's tests are self-contained. A failure identifies the broken module.

---

## 14. Convention Over Configuration

Examples:
- Spring component scanning discovers `@Service`, `@Repository`, `@Controller` automatically
- `application-{profile}.yml` for environment-specific config — no code changes
- Spring Data repositories: interface extends `JpaRepository<T, ID>` → implementation auto-generated
- Configuration properties: class name + prefix = auto-binding

---

## 15. Deterministic Build Output

```xml
<!-- pom.xml -->
<build>
  <directory>${project.basedir}/target</directory>
  <finalName>${project.artifactId}-${project.version}</finalName>
</build>
```

```bash
# Always clean before release build
mvn clean package
```

Output always in `target/`. Final artifact name is predictable. Use `mvn clean` to avoid stale classes.

---

## 16. Black Box Composition

```java
// Public interface in core module
public interface OrderProcessor {
    OrderResult process(OrderRequest request);
}

// Package-private implementation in its own module
class OrderProcessorImpl implements OrderProcessor { ... }

// Module system for strong encapsulation (Java 9+)
module com.myapp.orders {
    exports com.myapp.orders.api;  // only interfaces + DTOs
    // implementation packages NOT exported
}
```

Each black box is a Maven module with `module-info.java` controlling exports. Only interfaces and DTOs are `public`. Implementations are package-private. Test each box in isolation via its public API.

---

## Verification Commands

```bash
# Check compiler warnings-as-errors
grep -r "Werror\|TreatWarningsAsErrors" pom.xml
# Check BOM is used
grep -r "dependencyManagement" pom.xml
# Build clean
mvn clean compile -Werror
# Run fast tests
mvn test
# Run all tests including slow
mvn test -Dgroups=
# Check for scattered config access
grep -rn "System.getenv\|System.getProperty" --include="*.java" src/ | grep -v Properties
```
