---
description: Project Foundations - Dart/Flutter implementation
---
# Project Foundations — Dart/Flutter

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```yaml
# analysis_options.yaml — strict mode
include: package:flutter_lints/flutter.yaml

analyzer:
  errors:
    missing_return: error
    dead_code: error
    unused_import: error
  language:
    strict-casts: true
    strict-inference: true
    strict-raw-types: true
```

```bash
# CI pipeline — zero tolerance
dart analyze --fatal-infos
dart format --set-exit-if-changed .
```

Strict casts, inference, and raw types catch real bugs. Every unused import, dead code path, and missing return is caught. No warnings in CI, ever.

---

## 2. Central Dependency Management

```yaml
# pubspec.yaml — single source for ALL dependencies
name: myapp
environment:
  sdk: ^3.4.0
  flutter: ^3.22.0

dependencies:
  flutter:
    sdk: flutter
  flutter_bloc: ^8.1.0
  freezed_annotation: ^2.4.0
  get_it: ^7.6.0
  dio: ^5.4.0

dev_dependencies:
  flutter_test:
    sdk: flutter
  build_runner: ^2.4.0
  freezed: ^2.4.0
  mocktail: ^1.0.0
  flutter_lints: ^4.0.0
```

```bash
# Keep dependencies audited
dart pub outdated         # Check for updates
dart pub deps             # Dependency tree
flutter pub upgrade       # Upgrade within constraints
```

`pubspec.lock` is ALWAYS committed. Pin major versions with `^`. Upgrading = single-line change in `pubspec.yaml`.

---

## 3. Versioning Strategy

```yaml
# pubspec.yaml
version: 1.2.3+42  # marketing+build number
```

```dart
// Access at runtime
import 'package:package_info_plus/package_info_plus.dart';

final info = await PackageInfo.fromPlatform();
final version = '${info.version}+${info.buildNumber}';
```

Marketing version (1.2.3) for users, build number (+42) for internal tracking. CI increments build number automatically. Version displayed in app settings/about screen.

---

## 4. Structured Error Handling

```dart
// core/errors/app_error.dart — sealed hierarchy
sealed class AppError {
  final String message;
  final StackTrace? stackTrace;
  const AppError(this.message, [this.stackTrace]);
}

class NotFoundError extends AppError {
  final String entity;
  final String id;
  const NotFoundError(this.entity, this.id)
      : super('$entity "$id" not found');
}

class ValidationError extends AppError {
  final Map<String, String> fields;
  const ValidationError(this.fields)
      : super('Validation failed');
}

class NetworkError extends AppError {
  final int? statusCode;
  const NetworkError(super.message, {this.statusCode});
}

// Result type for clean error handling
sealed class Result<T> {
  const Result();
}
class Success<T> extends Result<T> {
  final T value;
  const Success(this.value);
}
class Failure<T> extends Result<T> {
  final AppError error;
  const Failure(this.error);
}
```

Sealed error hierarchy for exhaustive matching. Result type for operations that can fail. Never catch `Exception` without logging. Pattern matching (Dart 3) for clean error handling.

---

## 5. Test Configuration (Fast/Slow Split)

```dart
// Unit tests — always fast
// test/unit/user_bloc_test.dart
@Tags(['unit'])
void main() { ... }

// Integration tests — separate directory
// integration_test/app_test.dart
void main() {
  IntegrationTestWidgetsFlutterBinding.ensureInitialized();
  // ...
}
```

```bash
# Fast tests (default) — seconds
flutter test test/unit/

# Widget tests
flutter test test/widget/

# Integration tests — on device
flutter test integration_test/

# All tests
flutter test
```

---

## 6. Immutable DTOs

```dart
// freezed for immutable data classes with copyWith
@freezed
class UserDto with _$UserDto {
  const factory UserDto({
    required String id,
    required String name,
    required String email,
    required DateTime createdAt,
  }) = _UserDto;

  factory UserDto.fromJson(Map<String, dynamic> json) =>
      _$UserDtoFromJson(json);
}

// Usage — copyWith for modifications
final updated = user.copyWith(email: 'new@email.com');

// Simple value objects without codegen
class Coordinate {
  final double lat;
  final double lon;
  const Coordinate({required this.lat, required this.lon});
}
```

Use `freezed` for DTOs — gives you `copyWith`, `==`, `hashCode`, `toString`, `fromJson/toJson`. All fields `final`. `const factory` constructors. Never mutate — create copies.

---

## 7. Dependency Injection with Proper Registration

```dart
// di/injection_container.dart — composition root
final sl = GetIt.instance;

Future<void> initDependencies() async {
  // External
  sl.registerLazySingleton(() => Dio()..options.baseUrl = apiBaseUrl);
  sl.registerLazySingleton(() => const FlutterSecureStorage());

  // Data sources
  sl.registerLazySingleton<UserRemoteDataSource>(
    () => UserRemoteDataSourceImpl(sl()),
  );

  // Repositories
  sl.registerLazySingleton<UserRepository>(
    () => UserRepositoryImpl(remote: sl(), local: sl()),
  );

  // BLoCs — factory (new instance each time)
  sl.registerFactory(() => UserBloc(sl()));
}
```

Single composition root in `di/`. `registerLazySingleton` for shared services. `registerFactory` for BLoCs (new per screen). Constructor injection — never call `GetIt.I` deep in widgets.

---

## 8. Centralized Configuration with Validation

```dart
// core/config/app_config.dart
class AppConfig {
  final String apiBaseUrl;
  final String environment;
  final Duration timeout;

  const AppConfig({
    required this.apiBaseUrl,
    required this.environment,
    this.timeout = const Duration(seconds: 30),
  });

  factory AppConfig.fromEnvironment() {
    const apiUrl = String.fromEnvironment('API_URL');
    if (apiUrl.isEmpty) {
      throw StateError('API_URL must be provided via --dart-define');
    }

    return AppConfig(
      apiBaseUrl: apiUrl,
      environment: const String.fromEnvironment('ENV', defaultValue: 'dev'),
    );
  }
}
```

Build-time config via `--dart-define`. Validation at startup — fail fast. Sensible defaults for non-critical values. Access via DI, never globals.

---

## 9. Structured File Logging

```dart
// core/utils/app_logger.dart
import 'package:logger/logger.dart';

final logger = Logger(
  printer: PrettyPrinter(
    methodCount: 0,
    errorMethodCount: 5,
    lineLength: 100,
    colors: true,
    printTime: true,
  ),
  level: kDebugMode ? Level.debug : Level.info,
);

// Production — use file output or crash reporting
class CrashlyticsLogOutput extends LogOutput {
  @override
  void output(OutputEvent event) {
    if (event.level.index >= Level.warning.index) {
      FirebaseCrashlytics.instance.log(event.lines.join('\n'));
    }
  }
}

// Usage
logger.d('Processing order $orderId');
logger.i('User logged in', error: null, stackTrace: null);
logger.e('API call failed', error: exception, stackTrace: stackTrace);
```

Use `logger` package with configurable output. Debug level in development, info+ in production. Crashlytics/Sentry integration for production errors. Never `print()` — always structured logging.

---

## 10. Zero-Dependency Core Module

```
lib/
├── domain/          → ZERO Flutter/package dependencies, pure Dart only
│   ├── entities/    → domain models
│   ├── repositories/ → abstract interfaces
│   └── usecases/    → business logic
```

Domain contains: entity classes, repository interfaces, use case classes, value objects. No Flutter imports. No external package imports. Only `dart:core` and `dart:async`. Every other layer imports domain. Domain imports nothing external.

---

## 11. Interface-First Design

```dart
// domain/repositories — abstract interfaces
abstract class UserRepository {
  Future<List<User>> getAll();
  Future<User?> getById(String id);
  Future<void> save(User user);
  Future<void> delete(String id);
}

abstract class AuthRepository {
  Future<Result<User>> signIn(String email, String password);
  Future<void> signOut();
  Stream<User?> get authStateChanges;
}
```

Abstract classes in domain. Concrete implementations in data layer. BLoCs/ViewModels depend on abstractions. Tests mock the abstractions with `mocktail`.

---

## 12. Internal/Private by Default

```dart
// Private with underscore prefix
class UserBloc extends Bloc<UserEvent, UserState> {
  final UserRepository _repository;  // private

  // Public API
  UserBloc(this._repository) : super(UserInitial());

  // Private implementation
  Future<void> _onLoadUsers(LoadUsers event, Emitter emit) async { ... }
  bool _isValidEmail(String email) => ...;  // private helper
}

// Library-level privacy with part/show
// Export only what's needed
export 'src/user_service.dart' show UserService;
// Internal implementation details stay in src/
```

Underscore prefix for all private members. Export only public API from package. `src/` directory for internal implementation. `show` directive to limit exports.

---

## 13. Test Module per Production Module

```
test/
├── unit/
│   ├── blocs/
│   │   └── user_bloc_test.dart      → tests UserBloc
│   ├── repositories/
│   │   └── user_repo_test.dart      → tests UserRepositoryImpl
│   └── usecases/
│       └── get_users_test.dart      → tests GetUsersUseCase
├── widget/
│   └── pages/
│       └── user_list_test.dart      → tests UserListPage
```

Each test file tests exactly one production class. A failing test tells you which component is broken. Unit for BLoCs/services, widget for pages/components.

---

## 14. Convention Over Configuration

```dart
// Naming conventions imply purpose
// user_bloc.dart → BLoC for user feature
// user_repository.dart → Repository interface
// user_repository_impl.dart → Implementation
// user_model.dart → Data layer DTO
// user_entity.dart → Domain entity

// Route naming by convention
GoRouter(routes: [
  GoRoute(path: '/users', builder: (_, __) => const UserListPage()),
  GoRoute(path: '/users/:id', builder: (_, state) => UserDetailPage(id: state.pathParameters['id']!)),
]);
```

File names mirror class names in snake_case. Directory structure mirrors architecture layers. Widget names end with `Page`, `View`, `Card`, etc. BLoC names match feature name.

---

## 15. Deterministic Build Output

```bash
# Reproducible builds
flutter build apk --release --obfuscate --split-debug-info=build/debug-info
flutter build ios --release --obfuscate --split-debug-info=build/debug-info

# Lock Flutter version
# .fvmrc or .flutter-version
3.22.0
```

`pubspec.lock` committed for deterministic dependency resolution. Flutter version pinned via FVM. `--obfuscate` for release builds. Debug info stored separately for crash symbolication.

---

## 16. Black Box Composition

```dart
// Each feature is a black box
// lib/features/user/
//   ├── domain/       → public interfaces
//   ├── data/         → private implementations
//   ├── presentation/ → public widgets
//   └── user.dart     → barrel file (public API)

// user.dart — barrel export (public surface)
export 'domain/entities/user.dart';
export 'domain/repositories/user_repository.dart';
export 'presentation/pages/user_list_page.dart';
export 'presentation/blocs/user_bloc.dart';
// Internal details NOT exported
```

Each feature exports only its public API via barrel file. Internal data sources, models, and helpers stay private. Features communicate via domain interfaces. Test each feature independently.

---

## Verification Commands

```bash
# Check formatting
dart format --set-exit-if-changed .

# Check all analysis passes
dart analyze --fatal-infos

# Run all tests
flutter test

# Run tests with coverage
flutter test --coverage
genhtml coverage/lcov.info -o coverage/html

# Check for outdated dependencies
dart pub outdated

# Run code generation
dart run build_runner build --delete-conflicting-outputs

# Build release
flutter build apk --release --obfuscate --split-debug-info=build/debug-info
```
