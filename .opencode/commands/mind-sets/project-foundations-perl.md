---
description: Project Foundations - Perl implementation
---
# Project Foundations — Perl

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```ini
# .perlcriticrc — severity 3 minimum
severity = 3
theme = core || pbp || security

[TestingAndDebugging::RequireUseStrict]
equivalent_modules = Moo Moose
```

```bash
# CI pipeline — zero tolerance
perlcritic --severity 3 lib/
perltidy --check-syntax lib/
perl -c lib/MyApp/App.pm       # syntax check
```

Perlcritic at severity 3 catches security issues, code smells, and Perl Best Practices violations. Every unused variable, missing strict/warnings, and suspicious construct is caught. No warnings in CI, ever.

---

## 2. Central Dependency Management

```perl
# cpanfile — single source for ALL dependencies
requires 'perl', '>= 5.036';
requires 'Moo', '>= 2.005';
requires 'Types::Standard', '>= 2.0';
requires 'DBI', '>= 1.643';
requires 'Log::Any', '>= 1.0';
requires 'Path::Tiny', '>= 0.144';
requires 'JSON::MaybeXS', '>= 1.004';

on test => sub {
    requires 'Test2::V0';
    requires 'Test::MockModule', '>= 0.177';
    requires 'Test::MockObject';
};

on develop => sub {
    requires 'Perl::Critic';
    requires 'Perl::Tidy';
    requires 'Devel::Cover';
};
```

```bash
# carton for reproducible installs (like bundler/composer)
carton install                  # install from cpanfile
carton exec prove -lr t/        # run with local deps
carton exec perl app.pl         # run app with local deps
```

`cpanfile.snapshot` is ALWAYS committed (like lock files). Upgrading a dependency = single-line change in `cpanfile`. `carton` ensures reproducible installs across environments.

---

## 3. Versioning Strategy

```perl
# lib/MyApp.pm — version in main module
package MyApp;
use v5.36;
our $VERSION = '1.2.3';
```

```perl
# Accessible at runtime
use MyApp;
say "Version: $MyApp::VERSION";

# Health endpoint (if web app)
get '/health' => sub {
    return { status => 'ok', version => $MyApp::VERSION };
};
```

Version in main module `$VERSION`. `perl-reversion` tool for automated bumps. The application logs its version at startup. CI uses version from module.

---

## 4. Structured Error Handling

```perl
# lib/MyApp/Error.pm — typed error hierarchy
package MyApp::Error;
use Moo;
use Types::Standard qw(Str);
extends 'Throwable::Error';

has message => (is => 'ro', isa => Str, required => 1);

package MyApp::Error::NotFound;
use Moo;
extends 'MyApp::Error';
has entity => (is => 'ro', isa => Str, required => 1);
has id     => (is => 'ro', required => 1);

around message => sub ($orig, $self) {
    return sprintf "%s '%s' not found", $self->entity, $self->id;
};

package MyApp::Error::Validation;
use Moo;
extends 'MyApp::Error';

# Usage
sub get_user ($self, $id) {
    my $user = $self->repo->find_by_id($id)
        or die MyApp::Error::NotFound->new(entity => 'User', id => $id);
    return $user;
}
```

Every error has a typed class. `die` with objects for structured errors. `eval { ... }; if (my $err = $@)` for catching. Never `die` with bare strings in libraries.

---

## 5. Test Configuration (Fast/Slow Split)

```
t/
├── unit/              # fast — no external deps
│   └── *.t
├── integration/       # slow — requires DB/services
│   └── *.t
└── author/            # only for developers
    └── critic.t
```

```bash
# Fast tests (default) — seconds
prove -lr t/unit/

# Integration tests — on demand
prove -lr t/integration/

# All tests
prove -lr t/

# Author tests (perlcritic, pod)
AUTHOR_TESTING=1 prove -lr t/author/
```

```perl
# t/integration/user_repo.t — skip if no DB
use Test2::V0;
use DBI;

my $dsn = $ENV{TEST_DATABASE_URL}
    or skip_all 'Set TEST_DATABASE_URL to run integration tests';
```

---

## 6. Immutable DTOs

```perl
# Moo with ro (read-only) — immutable by design
package MyApp::DTO::User;
use Moo;
use Types::Standard qw(Int Str InstanceOf);

has id         => (is => 'ro', isa => Int, required => 1);
has name       => (is => 'ro', isa => Str, required => 1);
has email      => (is => 'ro', isa => Str, required => 1);
has created_at => (is => 'ro', isa => InstanceOf['DateTime']);

# Copy with modifications
sub with_email ($self, $new_email) {
    return (ref $self)->new(
        %$self,
        email => $new_email,
    );
}
```

All attributes `is => 'ro'` (read-only). Type constraints via Types::Standard. Copy methods for modifications. Never use `rw` (read-write) for DTOs.

---

## 7. Dependency Injection with Proper Registration

```perl
# app.pl or lib/MyApp/Bootstrap.pm — composition root
package MyApp::Bootstrap;
use v5.36;

sub wire () {
    my $config = MyApp::Config->load;
    
    # Infrastructure
    my $dbh = DBI->connect(
        $config->dsn, $config->db_user, $config->db_pass,
        { RaiseError => 1, AutoCommit => 1 }
    );
    
    # Repositories
    my $user_repo  = MyApp::Repository::User->new(dbh => $dbh);
    my $order_repo = MyApp::Repository::Order->new(dbh => $dbh);
    
    # Services
    my $user_svc  = MyApp::Service::User->new(repo => $user_repo);
    my $order_svc = MyApp::Service::Order->new(
        repo     => $order_repo,
        user_svc => $user_svc,
    );
    
    return MyApp::App->new(
        user_service  => $user_svc,
        order_service => $order_svc,
    );
}
```

Bootstrap function is the composition root. Constructor injection via Moo attributes. No service locators or global registries. Each `->new()` declares its dependencies.

---

## 8. Centralized Configuration with Validation

```perl
# lib/MyApp/Config.pm
package MyApp::Config;
use Moo;
use Types::Standard qw(Str Int);

has dsn      => (is => 'ro', isa => Str, required => 1);
has db_user  => (is => 'ro', isa => Str, required => 1);
has db_pass  => (is => 'ro', isa => Str, required => 1);
has port     => (is => 'ro', isa => Int, default => 8080);
has api_key  => (is => 'ro', isa => Str, required => 1);

sub load ($class) {
    return $class->new(
        dsn      => $ENV{DATABASE_URL} // die("DATABASE_URL required\n"),
        db_user  => $ENV{DB_USER}      // die("DB_USER required\n"),
        db_pass  => $ENV{DB_PASS}      // die("DB_PASS required\n"),
        port     => $ENV{PORT}         // 8080,
        api_key  => $ENV{API_KEY}      // die("API_KEY required\n"),
    );
}
```

All configuration via environment variables. Validation at startup — fail fast with clear messages. Sensible defaults for non-critical values. Load once, pass via constructors.

---

## 9. Structured File Logging

```perl
# Log::Any — adapter pattern
use Log::Any qw($log);
use Log::Any::Adapter;

# Configure adapter at startup
Log::Any::Adapter->set('File', '/var/log/myapp/app.log',
    log_level => 'info',
);

# Or JSON via Log::Dispatch adapter
use Log::Any::Adapter ('Dispatch',
    outputs => [[ 'File', min_level => 'info', filename => '/var/log/myapp/app.log',
                   newline => 1, mode => '>>' ]],
);

# Usage — structured context
$log->info("Order processed", { order_id => $id, total => $total });
$log->error("Payment failed", { order_id => $id, error => "$err" });
$log->debug("User lookup", { user_id => $uid });
```

Use Log::Any (adapter pattern — like SLF4J). Libraries use `$log`, application configures adapter. JSON format for production. Never `print STDERR` or `warn` for logging.

---

## 10. Zero-Dependency Core Module

```
lib/MyApp/
├── Model/         → zero external dependencies (only core modules)
│   ├── User.pm    → Moo class (Moo is the one exception)
│   └── Order.pm
├── Service/       → depends on Model only
├── Repository/    → depends on Model + DBI
└── App.pm         → depends on everything
```

Model contains: Moo classes, value objects, error classes. No DBI, no web framework. Services contain business logic with repository dependencies. Models import only Moo + Types::Standard.

---

## 11. Interface-First Design

```perl
# Perl uses duck typing — document the interface
# lib/MyApp/Role/UserStore.pm
package MyApp::Role::UserStore;
use Moo::Role;

requires 'find_by_id';
requires 'save';
requires 'exists_by_email';

# lib/MyApp/Repository/User.pm
package MyApp::Repository::User;
use Moo;
with 'MyApp::Role::UserStore';

has dbh => (is => 'ro', required => 1);

sub find_by_id ($self, $id) { ... }
sub save ($self, $user) { ... }
sub exists_by_email ($self, $email) { ... }
```

Moo::Role for interface contracts (`requires`). Services depend on roles, not concrete classes. Tests provide mock implementations that consume the role.

---

## 12. Internal/Private by Default

```perl
# Private methods — underscore prefix convention
package MyApp::Service::User;
use Moo;

# Public API
sub create ($self, %args) { ... }
sub get_by_id ($self, $id) { ... }

# Private helpers
sub _validate_email ($self, $email) { ... }
sub _hash_password ($self, $password) { ... }

# Private attributes
has _cache => (is => 'ro', init_arg => undef, default => sub { {} });
```

Underscore prefix for all private methods and attributes. `init_arg => undef` for internal-only attributes. Export only what's needed via `@EXPORT_OK`. Document public API, hide internals.

---

## 13. Test Module per Production Module

```
t/
├── unit/
│   ├── service/
│   │   ├── user_service.t       → tests MyApp::Service::User
│   │   └── order_service.t      → tests MyApp::Service::Order
│   └── model/
│       └── user.t               → tests MyApp::Model::User
├── integration/
│   └── repository/
│       └── user_repo.t          → tests MyApp::Repository::User
```

Each test file tests exactly one production module. A failing test tells you which module is broken. Unit tests for services/models, integration for repositories/API.

---

## 14. Convention Over Configuration

```perl
# Module structure mirrors namespace
# lib/MyApp/Service/User.pm → MyApp::Service::User
# lib/MyApp/Repository/User.pm → MyApp::Repository::User

# Test file mirrors module
# t/unit/service/user_service.t → tests MyApp::Service::User

# Naming conventions
# MyApp::Model::*      → data classes
# MyApp::Service::*    → business logic
# MyApp::Repository::* → data access
# MyApp::Error::*      → exception classes
# MyApp::Util::*       → utility functions
```

Namespace structure implies purpose. File paths mirror namespaces. Test paths mirror source paths. Method naming: `verb_noun` (`get_user`, `create_order`).

---

## 15. Deterministic Build Output

```bash
# carton for deterministic installs
carton install --deployment    # from cpanfile.snapshot only
carton exec perl app.pl        # run with exact versions

# Docker
FROM perl:5.40-slim
COPY cpanfile cpanfile.snapshot ./
RUN cpanm --installdeps --notest .
COPY . .
CMD ["perl", "app.pl"]
```

`cpanfile.snapshot` committed for deterministic resolution. `--deployment` flag for exact versions. Docker with pinned Perl version for consistent builds.

---

## 16. Black Box Composition

```perl
# Each namespace is a black box with clear interface
# lib/MyApp/Service/User.pm — public surface
package MyApp::Service::User;
use Moo;

# Public API — documented
sub create ($self, %args) { ... }
sub get_by_id ($self, $id) { ... }
sub delete ($self, $id) { ... }

# Private implementation
sub _validate ($self, %args) { ... }
sub _notify ($self, $user) { ... }
```

Each service exposes a clean public API (no underscore prefix). Internal methods are private (underscore prefix). Services communicate via method calls on injected dependencies. Test each service in isolation via its public methods.

---

## Verification Commands

```bash
# Check modern Perl header
grep -rL "use v5.36\|use v5.38\|use v5.40" lib/ --include="*.pm"  # must be empty

# Check perlcritic passes
perlcritic --severity 3 lib/

# Check formatting (compare output, non-zero if differs)
find lib -name '*.pm' -exec perltidy -st {} \; | diff - {} >/dev/null

# Run all tests
prove -lr t/

# Run tests with coverage
cover -test
cover -report html

# Check for security issues
perlcritic --severity 4 --theme security lib/

# Verify dependencies installed
carton install --deployment    # fails if cpanfile.snapshot mismatch

# Syntax check all modules
find lib -name '*.pm' -exec perl -c {} \;
```
