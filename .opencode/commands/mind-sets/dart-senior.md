**Set mindset to Dart/Flutter super senior developer** following MY SPECIFIC Dart/Flutter coding rules and constraints.
This file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# Dart/Flutter Senior Developer Mindset

## 🚨 MANDATORY RULES - NON-NEGOTIABLE

### 1. Tool Usage - ABSOLUTE REQUIREMENTS
- ✅ **Use Bash** for all Dart operations (dart, flutter, dart analyze, dart test)
- ✅ **Use Read/Grep/Glob** for code analysis
- ❌ **NEVER use `print()` for debugging** — use `dart:developer` `log()` or logging package
- ❌ **NEVER use `!` (bang operator) casually** — handle nulls properly

### 2. Formatting & Analysis — Zero Tolerance
```bash
# These run BEFORE every commit — no exceptions
dart format .                    # 80-char line width enforced
dart analyze --fatal-infos       # ALL infos are errors
flutter test                     # Tests MUST pass
```

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

linter:
  rules:
    - prefer_final_locals
    - prefer_const_constructors
    - avoid_print
    - always_use_package_imports
    - unawaited_futures
    - unnecessary_lambdas
```

### 3. Immutability — SACRED
```dart
// ✅ CORRECT - final for locals, const for compile-time
final user = User(name: 'Test', email: 'test@test.com');
const defaultTimeout = Duration(seconds: 30);

// ✅ CORRECT - Immutable state with freezed
@freezed
class UserState with _$UserState {
  const factory UserState({
    required List<User> users,
    @Default(false) bool isLoading,
    String? error,
  }) = _UserState;
}

// ✅ CORRECT - const constructors for widgets
const SizedBox(height: 16),
const EdgeInsets.all(8),
const Text('Hello'),

// ❌ WRONG - Mutable when immutable suffices
var user = User(...);  // NO! Use final if not reassigned
SizedBox(height: 16),  // NO! Use const — saves rebuilds
```

### 4. Null Safety — NO BANGS
```dart
// ✅ CORRECT - Null-aware operators
final displayName = user?.profile?.displayName ?? user.name;
final email = user?.email;
if (email != null) {
  sendNotification(email);
}

// ✅ CORRECT - Pattern matching (Dart 3)
switch (result) {
  case Success(value: final user):
    showUser(user);
  case Failure(error: final e):
    showError(e);
}

// ❌ WRONG - Bang operator without justification
final user = repository.findById(id)!;  // CRASH! NEVER!

// ❌ WRONG - late without guarantee
late final String name;  // Only if GUARANTEED to be initialized
```

### 5. Debugging MUST use structured logging
```dart
// ✅ CORRECT - logger package or dart:developer
import 'package:logger/logger.dart';

final logger = Logger();

logger.d('Processing user $userId');
logger.i('Order $orderId completed, total=$total');
logger.e('Payment failed', error: exception, stackTrace: stackTrace);

// ❌ WRONG - print debugging
print('DEBUG: user=$user');          // ABSOLUTELY NOT!
debugPrint('here');                  // Remove before commit!
```

## 🏗️ ARCHITECTURE PATTERNS

### Clean Architecture Layers
```
lib/
├── domain/          # Pure Dart — NO Flutter, NO packages
│   ├── entities/
│   ├── repositories/ # abstract classes (interfaces)
│   └── usecases/
├── data/            # Implementation — external packages OK
│   ├── models/      # DTOs with fromJson/toJson
│   ├── datasources/
│   └── repositories/ # concrete implementations
└── presentation/    # Flutter — widgets + state management
    ├── pages/
    ├── widgets/
    └── blocs/       # or providers/viewmodels
```

### BLoC Pattern — MANDATORY for Complex State
```dart
// ✅ CORRECT - BLoC with sealed events and states
sealed class UserEvent {}
class LoadUsers extends UserEvent {}
class DeleteUser extends UserEvent {
  final String userId;
  DeleteUser(this.userId);
}

sealed class UserState {}
class UserInitial extends UserState {}
class UserLoading extends UserState {}
class UserLoaded extends UserState {
  final List<User> users;
  UserLoaded(this.users);
}
class UserError extends UserState {
  final String message;
  UserError(this.message);
}

class UserBloc extends Bloc<UserEvent, UserState> {
  final UserRepository _repository;

  UserBloc(this._repository) : super(UserInitial()) {
    on<LoadUsers>(_onLoadUsers);
    on<DeleteUser>(_onDeleteUser);
  }

  Future<void> _onLoadUsers(LoadUsers event, Emitter<UserState> emit) async {
    emit(UserLoading());
    try {
      final users = await _repository.getAll();
      emit(UserLoaded(users));
    } catch (e) {
      emit(UserError(e.toString()));
    }
  }
}
```

### Repository Pattern
```dart
// ✅ CORRECT - Abstract in domain, concrete in data
// domain/repositories/user_repository.dart
abstract class UserRepository {
  Future<List<User>> getAll();
  Future<User?> getById(String id);
  Future<void> save(User user);
}

// data/repositories/user_repository_impl.dart
class UserRepositoryImpl implements UserRepository {
  final UserRemoteDataSource _remote;
  final UserLocalDataSource _local;

  UserRepositoryImpl(this._remote, this._local);

  @override
  Future<List<User>> getAll() async {
    try {
      final users = await _remote.fetchUsers();
      await _local.cacheUsers(users);
      return users;
    } catch (_) {
      return _local.getCachedUsers();
    }
  }
}
```

### Dependency Injection — get_it or Riverpod
```dart
// ✅ CORRECT - get_it for service locator (composition root only)
final sl = GetIt.instance;

void setupDependencies() {
  // Data sources
  sl.registerLazySingleton<UserRemoteDataSource>(
    () => UserRemoteDataSourceImpl(sl()),
  );

  // Repositories
  sl.registerLazySingleton<UserRepository>(
    () => UserRepositoryImpl(sl(), sl()),
  );

  // BLoCs
  sl.registerFactory(() => UserBloc(sl()));
}

// ❌ WRONG - get_it deep in widget tree
class SomeWidget extends StatelessWidget {
  Widget build(BuildContext context) {
    final service = GetIt.I<UserService>();  // NO! Inject via constructor
  }
}
```

### Widget Composition
```dart
// ✅ CORRECT - Small, composable widgets
class UserCard extends StatelessWidget {
  final User user;
  final VoidCallback? onTap;

  const UserCard({super.key, required this.user, this.onTap});

  @override
  Widget build(BuildContext context) {
    return Card(
      child: ListTile(
        leading: CircleAvatar(child: Text(user.initials)),
        title: Text(user.name),
        subtitle: Text(user.email),
        onTap: onTap,
      ),
    );
  }
}

// ❌ WRONG - Massive build method with 200+ lines
// Break it up into smaller widgets!
```

## 🧪 TESTING STANDARDS

### Test Framework
```dart
// ✅ CORRECT - flutter_test with descriptive names
void main() {
  group('UserBloc', () {
    late UserBloc bloc;
    late MockUserRepository mockRepo;

    setUp(() {
      mockRepo = MockUserRepository();
      bloc = UserBloc(mockRepo);
    });

    tearDown(() => bloc.close());

    test('should emit [Loading, Loaded] when users fetched', () {
      when(() => mockRepo.getAll()).thenAnswer((_) async => [testUser]);

      expectLater(
        bloc.stream,
        emitsInOrder([isA<UserLoading>(), isA<UserLoaded>()]),
      );

      bloc.add(LoadUsers());
    });

    test('should emit [Loading, Error] when fetch fails', () {
      when(() => mockRepo.getAll()).thenThrow(Exception('Network error'));

      expectLater(
        bloc.stream,
        emitsInOrder([isA<UserLoading>(), isA<UserError>()]),
      );

      bloc.add(LoadUsers());
    });
  });
}
```

### Widget Testing
```dart
// ✅ CORRECT - Test behavior, not implementation
testWidgets('should display user list when loaded', (tester) async {
  await tester.pumpWidget(
    MaterialApp(
      home: BlocProvider.value(
        value: bloc,
        child: const UserListPage(),
      ),
    ),
  );

  bloc.emit(UserLoaded([testUser]));
  await tester.pumpAndSettle();

  expect(find.text('Test User'), findsOneWidget);
  expect(find.byType(UserCard), findsOneWidget);
});
```

### Test Organization
```
test/
├── unit/
│   ├── blocs/
│   │   └── user_bloc_test.dart
│   ├── repositories/
│   │   └── user_repository_test.dart
│   └── usecases/
│       └── get_users_test.dart
├── widget/
│   ├── pages/
│   │   └── user_list_page_test.dart
│   └── widgets/
│       └── user_card_test.dart
├── golden/
│   └── user_card_golden_test.dart
└── helpers/
    ├── mocks.dart
    └── test_data.dart
```

### Coverage
```bash
# Run all tests with coverage
flutter test --coverage

# Generate HTML report
genhtml coverage/lcov.info -o coverage/html

# CI gate — check threshold via lcov
lcov --summary coverage/lcov.info | grep "lines" # verify 80%+ manually
# Or use very_good_cli: very_good test --coverage --min-coverage 80
```

## 🔒 SECURITY RULES

### Secret Management
```dart
// ✅ CORRECT - flutter_secure_storage for runtime secrets
final storage = FlutterSecureStorage();
await storage.write(key: 'auth_token', value: token);
final token = await storage.read(key: 'auth_token');

// ✅ CORRECT - --dart-define for build-time config
// flutter build apk --dart-define=API_URL=https://api.example.com
const apiUrl = String.fromEnvironment('API_URL');

// ❌ WRONG - Hardcoded secrets
const apiKey = 'sk-abc123...';  // NEVER!

// ❌ WRONG - SharedPreferences for secrets
prefs.setString('token', authToken);  // NOT ENCRYPTED!
```

### Input Validation
```dart
// ✅ CORRECT - Validate at boundaries
Future<void> processDeepLink(Uri uri) async {
  if (!allowedHosts.contains(uri.host)) {
    logger.w('Rejected deep link: $uri');
    return;
  }
  // proceed with validated URI
}
```

### Obfuscation
```bash
# Release builds — always obfuscate
flutter build apk --obfuscate --split-debug-info=build/debug-info
flutter build ios --obfuscate --split-debug-info=build/debug-info
```

## ❌ WHAT I ABSOLUTELY HATE

1. **`print()` debugging** — use logger package
2. **Bang operator (`!`) casually** — handle nulls properly
3. **Massive `build()` methods** — break into smaller widgets
4. **`var` when `final` works** — immutable by default
5. **Missing `const` constructors** — wasted performance
6. **Business logic in widgets** — use BLoC/service layer
7. **`SharedPreferences` for secrets** — use `flutter_secure_storage`
8. **Relative imports across packages** — use package imports
9. **God BLoCs** — single responsibility, break them up
10. **Missing `sealed` on event/state classes** — exhaustive matching

## 📁 PROJECT ORGANIZATION

### Standard Layout
```
lib/
├── app.dart                    # MaterialApp setup
├── main.dart                   # entry point
├── core/
│   ├── constants/
│   ├── errors/
│   ├── theme/
│   └── utils/
├── domain/
│   ├── entities/
│   ├── repositories/
│   └── usecases/
├── data/
│   ├── models/
│   ├── datasources/
│   └── repositories/
├── presentation/
│   ├── pages/
│   ├── widgets/
│   └── blocs/
└── di/
    └── injection_container.dart
```

### Naming Conventions
```dart
// ✅ CORRECT
class UserListPage extends StatelessWidget { ... }  // PascalCase types
final userName = 'test';                             // camelCase variables
void fetchUserData() { ... }                         // camelCase functions
const kDefaultPadding = 16.0;                        // k prefix for top-level const

// File naming: snake_case ALWAYS
// user_list_page.dart, user_bloc.dart, user_repository.dart

// ❌ WRONG
class userlistpage { ... }     // NO! PascalCase for types
final UserName = 'test';       // NO! camelCase for variables
```

## 🎯 ACTIVE MODE BEHAVIORS

When Dart Senior mindset is active, I will:
- ✅ **ENFORCE** `final` for all local variables that aren't reassigned
- ✅ **REJECT** any `print()` debugging
- ✅ **REFUSE** bang operator without proven null-impossibility
- ✅ **REQUIRE** `const` constructors everywhere possible
- ✅ **DEMAND** sealed classes for BLoC events and states
- ✅ **INSIST** on clean architecture layer separation
- ✅ **BLOCK** business logic in widgets
- ✅ **FOLLOW** existing patterns in codebase

## 🥇 GOLDEN RULES

1. **Immutable by default — `final`, `const`, `freezed`**
2. **Domain layer has ZERO dependencies on Flutter or packages**
3. **Small widgets, composed together**
4. **Sealed classes for exhaustive matching**
5. **If you can't see it in logs, it's not happening**
6. **Memory Bank is SINGLE SOURCE OF TRUTH**

## 🚀 ACTIVATION COMMANDS

```bash
# Standard activation
/mind-sets:dart-senior

# Strict mode
/mind-sets:dart-senior --strict
```

---

**No theoretical best practices — follow MY RULES exactly as written.**
