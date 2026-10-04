---
description: >-
  Cross-language design patterns — skeleton projects, repository pattern, API
  response envelope
---
# Common Design Patterns — All Languages

These patterns apply universally, regardless of language or framework.

## Skeleton Projects — Search Before Building

Before scaffolding a new project from scratch:
1. **Search for battle-tested skeleton projects** — GitHub, framework starters, official templates
2. **Evaluate candidates** for: security posture, extensibility, relevance to your stack, maintenance activity
3. **Clone the best match** as your foundation
4. **Iterate within the proven structure** rather than inventing your own

This saves weeks of boilerplate and inherits community-tested patterns.

## Repository Pattern — Universal Data Access

Encapsulate ALL data access behind a consistent interface:

```
Interface (in domain layer):
  findAll(filter) → List<Entity>
  findById(id) → Entity?
  create(entity) → Entity
  update(entity) → Entity
  delete(id) → void

Implementation (in infrastructure layer):
  Handles actual DB/API/file operations
  Business logic NEVER touches storage directly
```

Benefits:
- Swap storage (SQL → NoSQL → API) without changing business logic
- Test business logic with in-memory fakes
- Enforce data access patterns (caching, transactions) in one place

## API Response Envelope — Consistent Format

Every API endpoint returns the same structure:

```json
{
  "success": true,
  "data": { ... },
  "error": null,
  "meta": {
    "total": 100,
    "page": 1,
    "limit": 20
  }
}
```

```json
{
  "success": false,
  "data": null,
  "error": {
    "code": "NOT_FOUND",
    "message": "User not found"
  },
  "meta": null
}
```

Rules:
- `success` is always present — boolean indicator
- `data` is null on error, populated on success
- `error` is null on success, structured on error (code + message)
- `meta` for paginated responses only
- Never mix: don't put errors in `data` or data in `error`

## Service Layer — Business Logic Isolation

All business logic lives in services, NEVER in:
- Controllers/handlers (HTTP concerns only)
- Repositories (data access only)
- Models/entities (data structure only)

```
Controller → validates input, calls service, formats response
Service    → business logic, orchestrates repositories
Repository → data access, queries, persistence
```

## Error Hierarchy — Domain to Transport

Define errors in the domain layer, map to transport in the handler layer:

```
Domain layer:  NotFound, Validation, Unauthorized, Conflict, InternalError
    ↓ mapped by handler ↓
Transport:     404,      400,        401,          409,      500
```

Never throw HTTP status codes from services. Services throw domain errors. Handlers translate.
