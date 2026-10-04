---
description: Project Foundations - Python implementation
---
# Project Foundations — Python

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```toml
# pyproject.toml
[tool.mypy]
strict = true
warn_return_any = true
warn_unused_configs = true
disallow_untyped_defs = true

[tool.ruff]
select = ["ALL"]
# Explicit ignores only — everything else is an error

[tool.pytest.ini_options]
filterwarnings = ["error"]  # warnings become test failures
```

```bash
# CI must run all three:
mypy --strict .
ruff check .
pytest -W error
```

`mypy --strict` catches type errors. `ruff` (or `flake8`) with zero tolerance catches style and logic issues. `pytest -W error` turns runtime warnings into test failures.

---

## 2. Central Dependency Management

```toml
# pyproject.toml — single source of truth
[project]
dependencies = [
    "fastapi>=0.115.0,<1.0.0",
    "pydantic>=2.10.0,<3.0.0",
    "sqlalchemy>=2.0.0,<3.0.0",
]

[project.optional-dependencies]
dev = [
    "pytest>=8.0.0",
    "mypy>=1.13.0",
    "ruff>=0.8.0",
]
```

```bash
# Pin exact versions for reproducibility
pip-compile pyproject.toml -o requirements.txt
pip-compile pyproject.toml --extra dev -o requirements-dev.txt
# Or use poetry.lock / uv.lock
```

Always commit lock file. Use `pip install -r requirements.txt` (not `pip install .`) in CI for reproducibility.

---

## 3. Versioning Strategy

```toml
# pyproject.toml
[project]
version = "1.0.42"
# Or use dynamic versioning
dynamic = ["version"]

[tool.setuptools-scm]
# Auto-version from git tags
```

```python
# __init__.py or version.py
__version__ = "1.0.42"

# Or read from importlib.metadata
from importlib.metadata import version
__version__ = version("myapp")
```

Log version at startup. Expose via health endpoint. Bump before every release build.

---

## 4. Structured Error Handling

```python
from enum import StrEnum
from dataclasses import dataclass

class ErrorCode(StrEnum):
    NOT_FOUND = "NOT_FOUND"
    INVALID_PARAMS = "INVALID_PARAMS"
    UNAUTHORIZED = "UNAUTHORIZED"
    TIMEOUT = "TIMEOUT"
    NOT_SUPPORTED = "NOT_SUPPORTED"
    INTERNAL_ERROR = "INTERNAL_ERROR"

@dataclass(frozen=True)
class AppError:
    code: ErrorCode
    message: str
    suggestion: str | None = None
    candidates: tuple[str, ...] = ()

class AppException(Exception):
    def __init__(self, error: AppError) -> None:
        self.error = error
        super().__init__(error.message)
```

**FastAPI integration:**
```python
@app.exception_handler(AppException)
async def handle_app_error(request: Request, exc: AppException) -> JSONResponse:
    return JSONResponse(
        status_code=_code_to_status(exc.error.code),
        content={"code": exc.error.code, "message": exc.error.message,
                 "suggestion": exc.error.suggestion},
    )
```

Never `raise Exception("something went wrong")`. Use typed errors with `ErrorCode` enum.

---

## 5. Test Configuration (Fast/Slow Split)

```python
import pytest

@pytest.mark.slow
class TestDatabaseIntegration:
    ...

@pytest.mark.e2e
class TestEndToEnd:
    ...
```

```toml
# pyproject.toml
[tool.pytest.ini_options]
markers = [
    "slow: marks tests as slow (deselect with '-m \"not slow\"')",
    "e2e: marks tests as end-to-end",
]
addopts = "-m 'not slow and not e2e'"  # fast by default
```

```bash
# Fast tests (default)
pytest
# Slow tests
pytest -m slow
# All tests
pytest -m ""
```

---

## 6. Immutable DTOs

**Pydantic (preferred for API):**
```python
from pydantic import BaseModel

class UserDto(BaseModel):
    model_config = {"frozen": True}
    name: str
    email: str
    created_at: datetime
```

**dataclasses:**
```python
from dataclasses import dataclass

@dataclass(frozen=True)
class UserDto:
    name: str
    email: str
    created_at: datetime

# Derived copies
from dataclasses import replace
updated = replace(user, email="new@email.com")
```

Use `frozen=True` always. Collections via `tuple` (not `list`) and `frozenset` (not `set`) for true immutability.

---

## 7. Dependency Injection with Proper Registration

**FastAPI:**
```python
# Each feature exposes its dependencies
def get_user_service(
    repo: UserRepository = Depends(get_user_repository),
    logger: Logger = Depends(get_logger),
) -> UserService:
    return UserServiceImpl(repo, logger)

# Routes use Depends()
@router.get("/users/{user_id}")
async def get_user(service: UserService = Depends(get_user_service)) -> UserDto:
    ...
```

**Manual DI (composition root):**
```python
# container.py — clean composition
def create_app() -> Application:
    config = load_config()
    db = create_database(config.database_url)
    user_repo = SqlUserRepository(db)
    user_service = UserServiceImpl(user_repo)
    return Application(user_service=user_service)
```

Dependencies explicit via constructor parameters. Composition root is readable in 10 seconds.

---

## 8. Centralized Configuration with Validation

```python
from pydantic_settings import BaseSettings

class AppSettings(BaseSettings):
    model_config = {"env_prefix": "APP_"}

    database_url: str
    data_path: Path
    max_retries: int = 3
    timeout_seconds: int = 30
    log_level: str = "INFO"

# Validate at startup — fail fast
settings = AppSettings()  # reads from env vars, .env file
# Throws ValidationError if DATABASE_URL or DATA_PATH missing
```

All config in one class. Pydantic validates types and required fields at startup. No scattered `os.getenv()` across the codebase.

---

## 9. Structured File Logging

```python
import logging
import logging.config

LOGGING_CONFIG = {
    "version": 1,
    "handlers": {
        "file": {
            "class": "logging.handlers.RotatingFileHandler",
            "filename": "logs/app.log",
            "maxBytes": 10_485_760,  # 10MB
            "backupCount": 5,
            "formatter": "json",
        },
    },
    "formatters": {
        "json": {
            "class": "pythonjsonlogger.jsonlogger.JsonFormatter",
            "format": "%(asctime)s %(name)s %(levelname)s %(message)s",
        },
    },
    "root": {"level": "INFO", "handlers": ["file"]},
}

logging.config.dictConfig(LOGGING_CONFIG)
```

**Or with structlog (via stdlib integration):**
```python
import structlog
import logging

logging.basicConfig(
    format="%(message)s",
    handlers=[logging.FileHandler("logs/app.log")],
    level=logging.INFO,
)
structlog.configure(
    processors=[structlog.processors.JSONRenderer()],
    logger_factory=structlog.stdlib.LoggerFactory(),
)
```

Libraries use `logging.getLogger(__name__)` — never configure logging themselves. Only the entry point configures logging. In tests, set level to WARNING.

---

## 10. Zero-Dependency Core Module

```
src/myapp/
  core/
    __init__.py     → public API
    interfaces.py   → Protocol classes (abstract interfaces)
    models.py       → DTOs (frozen dataclasses/Pydantic)
    errors.py       → error types, error codes
    types.py        → type aliases, enums
```

Core uses only stdlib and typing. No third-party imports (except Pydantic for DTOs if used project-wide). Every other module imports from `myapp.core`.

---

## 11. Interface-First Design

```python
from abc import ABC, abstractmethod
# Or Protocol for structural typing

class UserService(Protocol):
    def get_by_id(self, user_id: int) -> UserDto | None: ...
    def create(self, data: CreateUserDto) -> UserDto: ...

class UserRepository(Protocol):
    def find_by_id(self, user_id: int) -> User | None: ...
    def save(self, user: User) -> User: ...
```

Use `Protocol` (structural subtyping) for interfaces — no inheritance required. Implementations satisfy the protocol by having matching methods. Testing uses mocks/fakes against the Protocol.

---

## 12. Internal/Private by Default

```python
# __init__.py — controls public API
from .models import UserDto, CreateUserDto
from .interfaces import UserService

__all__ = ["UserDto", "CreateUserDto", "UserService"]
# Implementation classes NOT exported

# Private by convention
_internal_helper()      # leading underscore = private
class _InternalParser:  # leading underscore = private
    ...
```

Only export from `__init__.py` what consumers need. Use `__all__` to define the public API. Leading underscore convention for internal functions/classes.

---

## 13. Test Module per Production Module

```
src/myapp/
  core/
  services/
  api/
tests/
  test_core/
  test_services/
  test_api/
  conftest.py       → shared fixtures
```

Or co-located:
```
src/myapp/
  core/
    models.py
    test_models.py
```

Each module has corresponding tests. A failure tells you which module is broken.

---

## 14. Convention Over Configuration

Examples:
- FastAPI router discovery: `app.include_router(router, prefix="/api")` with file-based organization
- pytest discovers `test_*.py` files automatically
- Pydantic Settings reads env vars by field name convention
- SQLAlchemy model-to-table name convention

---

## 15. Deterministic Build Output

```toml
# pyproject.toml
[tool.setuptools]
package-dir = {"" = "src"}

[build-system]
requires = ["setuptools>=75.0", "wheel"]
build-backend = "setuptools.build_meta"
```

```bash
# Clean build
rm -rf dist/ build/ *.egg-info
python -m build
```

Output always in `dist/`. Use virtual environments (`venv`, `uv`) to isolate from system Python. `pip freeze` to verify exact installed versions.

---

## 16. Black Box Composition

```python
# core/interfaces.py — public contract
from typing import Protocol

class OrderProcessor(Protocol):
    def process(self, request: OrderRequest) -> OrderResult: ...

# core/__init__.py — controls public API
from .interfaces import OrderProcessor
from .models import OrderRequest, OrderResult

__all__ = ["OrderProcessor", "OrderRequest", "OrderResult"]
```

```python
# orders/_processor.py — internal implementation (leading underscore)
class _OrderProcessorImpl:
    def process(self, request: OrderRequest) -> OrderResult: ...

# orders/__init__.py — only re-exports what consumers need
from ._processor import _OrderProcessorImpl as OrderProcessorImpl

__all__ = ["OrderProcessorImpl"]
```

Each black box is a Python package with `__init__.py` controlling its public API via `__all__`. Internal modules use `_` prefix. Consumers import from the package, never from internal modules. Use `Protocol` for interfaces in core. Test each box via its public interface.

---

## Verification Commands

```bash
# Check strict mypy
grep -r "strict = true" pyproject.toml
# Check pinned dependencies
ls requirements.txt || ls poetry.lock || ls uv.lock
# Type check
mypy --strict src/
# Lint with zero warnings
ruff check src/
# Run fast tests
pytest
# Check for scattered env access
grep -rn "os.getenv\|os.environ" --include="*.py" src/ | grep -v settings
```
