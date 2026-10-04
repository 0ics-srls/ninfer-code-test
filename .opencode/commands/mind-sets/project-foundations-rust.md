---
description: Project Foundations - Rust implementation
---
# Project Foundations — Rust

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```toml
# Cargo.toml — deny ALL warnings
[lints.rust]
unsafe_code = "forbid"
missing_docs = "warn"

[lints.clippy]
all = { level = "deny" }
pedantic = { level = "warn" }
nursery = { level = "warn" }
```

```bash
# CI pipeline — zero tolerance
cargo clippy -- -D warnings
cargo fmt -- --check
```

Clippy pedantic catches real design issues. `unsafe_code = "forbid"` prevents accidental unsafe. Every unused import, dead code path, and suspicious pattern is caught at compile time.

---

## 2. Central Dependency Management

```toml
# Cargo.toml (workspace root)
[workspace]
members = ["crates/*"]

[workspace.dependencies]
serde = { version = "1.0", features = ["derive"] }
tokio = { version = "1", features = ["full"] }
sqlx = { version = "0.8", features = ["runtime-tokio", "postgres"] }
tracing = "0.1"
anyhow = "1"
thiserror = "2"
```

```toml
# crates/myapp/Cargo.toml — inherit from workspace
[dependencies]
serde = { workspace = true }
tokio = { workspace = true }
```

One version per dependency across the entire workspace. `Cargo.lock` is ALWAYS committed for applications. Upgrading a dependency = single-line change in workspace root.

---

## 3. Versioning Strategy

```toml
# Cargo.toml
[package]
version = "1.2.3"
```

```rust
// Built into binary via env! macro
const VERSION: &str = env!("CARGO_PKG_VERSION");

// Or with build-time git info
fn version_info() -> String {
    format!("{} ({})", env!("CARGO_PKG_VERSION"), env!("GIT_HASH"))
}
```

```rust
// build.rs — embed git hash
fn main() {
    let output = std::process::Command::new("git")
        .args(["rev-parse", "--short", "HEAD"])
        .output()
        .unwrap();
    let hash = String::from_utf8(output.stdout).unwrap().trim().to_string();
    println!("cargo::rustc-env=GIT_HASH={hash}");
}
```

The application reports its version at startup and via a `/health` endpoint. `cargo-release` for automated version bumps and publishing.

---

## 4. Structured Error Handling

```rust
// error.rs — thiserror for typed errors
#[derive(Debug, thiserror::Error)]
pub enum AppError {
    #[error("not found: {entity} {id}")]
    NotFound { entity: &'static str, id: String },
    
    #[error("validation: {0}")]
    Validation(String),
    
    #[error("unauthorized")]
    Unauthorized,
    
    #[error(transparent)]
    Database(#[from] sqlx::Error),
    
    #[error(transparent)]
    Internal(#[from] anyhow::Error),
}

impl AppError {
    pub fn not_found(entity: &'static str, id: impl ToString) -> Self {
        Self::NotFound { entity, id: id.to_string() }
    }
}

// Axum integration — convert to HTTP response
impl IntoResponse for AppError {
    fn into_response(self) -> Response {
        let (status, message) = match &self {
            Self::NotFound { .. } => (StatusCode::NOT_FOUND, self.to_string()),
            Self::Validation(msg) => (StatusCode::BAD_REQUEST, msg.clone()),
            Self::Unauthorized => (StatusCode::UNAUTHORIZED, "unauthorized".into()),
            _ => (StatusCode::INTERNAL_SERVER_ERROR, "internal error".into()),
        };
        (status, Json(json!({ "error": message }))).into_response()
    }
}
```

Every error has a typed variant. `thiserror` for library/domain errors. `anyhow` for application-level context chaining. `?` with `.context()` propagates with full call chain. Never expose internal details in API responses.

---

## 5. Test Configuration (Fast/Slow Split)

```rust
// Unit tests — always run, in same file
#[cfg(test)]
mod tests {
    use super::*;
    
    #[test]
    fn should_validate_email() { ... }
}

// Integration tests — separate directory, feature-gated
// tests/api_test.rs
#[cfg(feature = "integration")]
#[tokio::test]
async fn should_create_user_via_api() { ... }
```

```toml
# Cargo.toml
[features]
integration = []
```

```bash
# Fast tests (default) — seconds
cargo test

# Integration tests — on demand
cargo test --features integration

# All with coverage
cargo llvm-cov --features integration
```

```makefile
test:
	cargo test
test-integration:
	cargo test --features integration
test-cover:
	cargo llvm-cov --html --fail-under-lines 80
```

---

## 6. Immutable DTOs

```rust
// Immutable by default in Rust — fields are private + no setters
#[derive(Debug, Clone, serde::Serialize, serde::Deserialize)]
pub struct UserDto {
    pub id: i64,
    pub name: String,
    pub email: String,
    pub created_at: DateTime<Utc>,
}

// Derive Copy for small value objects
#[derive(Debug, Clone, Copy, PartialEq, Eq, Hash)]
pub struct Coordinate {
    pub lat: f64,
    pub lon: f64,
}

// Use builder or constructor — no field mutation after creation
impl UserDto {
    pub fn new(id: i64, name: String, email: String) -> Self {
        Self { id, name, email, created_at: Utc::now() }
    }
}
```

Rust structs are immutable by default unless you hold `&mut`. Use `#[derive(Clone)]` for copies. No setter methods — construct once, use builder for complex cases. Collections return iterators or cloned `Vec`, never `&mut Vec`.

---

## 7. Dependency Injection with Proper Registration

```rust
// main.rs — explicit wiring, no frameworks
#[tokio::main]
async fn main() -> Result<()> {
    let config = Config::from_env()?;
    
    // Infrastructure
    let pool = PgPoolOptions::new()
        .max_connections(config.db_max_conn)
        .connect(&config.database_url)
        .await?;
    
    // Repositories
    let user_repo = PostgresUserRepo::new(pool.clone());
    let order_repo = PostgresOrderRepo::new(pool.clone());
    
    // Services
    let user_service = Arc::new(UserService::new(user_repo));
    let order_service = Arc::new(OrderService::new(order_repo, user_service.clone()));
    
    // HTTP
    let app = routes(user_service, order_service);
    axum::serve(listener, app).await?;
    Ok(())
}
```

`main()` is the composition root. No DI frameworks — Rust constructors + `Arc` for shared ownership. Each `::new()` declares its dependencies. Generic type parameters for testability (`UserService<R: UserRepository>`).

---

## 8. Centralized Configuration with Validation

```rust
// config.rs
#[derive(Debug, serde::Deserialize)]
pub struct Config {
    #[serde(default = "default_port")]
    pub port: u16,
    pub database_url: String,
    pub api_key: String,
    #[serde(default = "default_timeout")]
    pub timeout_seconds: u64,
}

fn default_port() -> u16 { 8080 }
fn default_timeout() -> u64 { 30 }

impl Config {
    pub fn from_env() -> Result<Self> {
        dotenvy::dotenv().ok();
        envy::from_env::<Self>()
            .context("failed to load configuration from environment")
    }
}
```

All configuration via environment variables. Deserialization validates types at startup. Missing required fields fail immediately with clear messages. Sensible defaults for non-critical values.

---

## 9. Structured File Logging

```rust
// tracing setup in main
use tracing_subscriber::{fmt, prelude::*, EnvFilter};

fn setup_tracing() -> tracing_appender::non_blocking::WorkerGuard {
    let file_appender = tracing_appender::rolling::daily("logs", "app.log");
    let (non_blocking, guard) = tracing_appender::non_blocking(file_appender);
    
    tracing_subscriber::registry()
        .with(EnvFilter::try_from_default_env()
            .unwrap_or_else(|_| EnvFilter::new("info")))
        .with(fmt::layer().json().with_writer(non_blocking))
        .with(fmt::layer().with_writer(std::io::stderr))
        .init();
    
    guard  // MUST keep alive for the duration of the program
}

// Usage — structured key-value pairs
use tracing::{info, error, instrument};

#[instrument(skip(repo))]
async fn get_user(repo: &impl UserRepository, id: UserId) -> Result<User> {
    info!(user_id = %id, "fetching user");
    // ...
}
```

Use `tracing` crate (not `log`). JSON format for production. `#[instrument]` for automatic span creation. `EnvFilter` for runtime-configurable log levels via `RUST_LOG`.

---

## 10. Zero-Dependency Core Module

```
crates/
├── core/         → zero external dependencies (only std)
├── domain/       → depends on core only
├── infra/        → depends on domain + external crates
└── api/          → depends on domain + infra
```

```toml
# crates/core/Cargo.toml
[dependencies]
# EMPTY — no external dependencies
```

Core contains: trait definitions, DTOs (structs), error types, constants. Domain contains business logic with trait bounds. Infrastructure implements traits with external crates. Core imports nothing external.

---

## 11. Interface-First Design

```rust
// Traits defined in domain — implemented in infra
#[async_trait]
pub trait UserRepository: Send + Sync {
    async fn find_by_id(&self, id: UserId) -> Result<Option<User>>;
    async fn save(&self, user: &User) -> Result<User>;
    async fn delete(&self, id: UserId) -> Result<()>;
}

// Service is generic over the trait
pub struct UserService<R: UserRepository> {
    repo: R,
}
```

Traits in domain crate. Implementations in infra crate. Services generic over traits. Tests use mock implementations (`mockall`).

---

## 12. Internal/Private by Default

```rust
// Private by default — only pub what's needed
pub struct UserService { ... }           // public type
pub fn new(repo: impl UserRepository)    // public constructor
fn validate_email(email: &str)           // private helper

// pub(crate) for internal sharing
pub(crate) fn hash_password(pw: &str) -> String { ... }

// Re-export from lib.rs for clean public API
pub use domain::{User, UserId};
pub use error::AppError;
```

Everything private by default. `pub` only for the public API. `pub(crate)` for internal cross-module sharing. `lib.rs` re-exports define the crate's public surface.

---

## 13. Test Module per Production Module

```
crates/
├── core/
│   └── src/lib.rs              # contains #[cfg(test)] mod tests
├── domain/
│   └── src/
│       ├── user.rs             # contains #[cfg(test)] mod tests
│       └── order.rs            # contains #[cfg(test)] mod tests
├── infra/
│   └── src/
│       └── postgres.rs         # contains #[cfg(test)] mod tests
└── api/
    └── tests/                  # integration tests
        └── api_test.rs
```

Unit tests in `#[cfg(test)]` modules in the same file — they have access to private items. Integration tests in `tests/` directory — they only see the public API. Each crate has its own test coverage.

---

## 14. Convention Over Configuration

```rust
// Derive macros — convention-based serialization
#[derive(Debug, Clone, Serialize, Deserialize)]
#[serde(rename_all = "camelCase")]
pub struct UserResponse {
    pub user_id: i64,       // serializes as "userId"
    pub display_name: String,
}

// Module structure implies routing
// src/api/users.rs → /users routes
// src/api/orders.rs → /orders routes
```

`serde` derives handle serialization by convention. Module layout mirrors API structure. `#[derive]` macros eliminate boilerplate. Cargo workspace structure implies build graph.

---

## 15. Deterministic Build Output

```toml
# .cargo/config.toml
[build]
target-dir = "target"

[profile.release]
lto = true
codegen-units = 1
strip = true
```

```bash
# Reproducible builds
RUSTFLAGS="-C target-cpu=native" cargo build --release

# Docker multi-stage for consistent output
FROM rust:1.82 AS builder
WORKDIR /app
COPY . .
RUN cargo build --release

FROM debian:bookworm-slim
COPY --from=builder /app/target/release/myapp /usr/local/bin/
```

`Cargo.lock` committed for deterministic dependency resolution. Release profile with LTO + single codegen unit for optimized binaries. Docker multi-stage builds for consistent output.

---

## 16. Black Box Composition

```rust
// Each crate is a black box with a clear public API
// crates/order/src/lib.rs
pub use service::OrderService;
pub use model::{Order, OrderId, OrderState};
pub use error::OrderError;

// Everything else is private
mod service;
mod model;
mod repository;
mod error;
```

Each workspace crate is a black box. `lib.rs` re-exports define the contract. Internal modules are private. Crate boundaries enforce isolation at compile time. Test each crate independently via its public API.

---

## Verification Commands

```bash
# Check formatting
cargo fmt -- --check

# Check all clippy lints pass
cargo clippy -- -D warnings

# Run all tests with race detection (Miri for unsafe)
cargo test
cargo +nightly miri test  # if unsafe code exists

# Check coverage
cargo llvm-cov --fail-under-lines 80

# Check for vulnerabilities
cargo audit

# Check license compliance
cargo deny check

# Verify dependency tree
cargo tree --duplicates

# Build release
cargo build --release
```
