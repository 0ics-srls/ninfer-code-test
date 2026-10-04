---
description: Project Foundations - TypeScript implementation
---
# Project Foundations — TypeScript

$include: ./project-foundations.md

---

## 1. Zero-Tolerance Warnings

```json
// tsconfig.json
{
  "compilerOptions": {
    "strict": true,
    "noUnusedLocals": true,
    "noUnusedParameters": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "forceConsistentCasingInFileNames": true
  }
}
```

```js
// eslint.config.js (flat config — ESLint 9+)
// Run with: eslint --max-warnings 0
```

`strict: true` enables all strict type checks. ESLint with `--max-warnings 0` treats every warning as a build failure. CI must enforce both.

---

## 2. Central Dependency Management

**npm/pnpm workspaces:**
```json
// root package.json
{
  "workspaces": ["packages/*"],
  "devDependencies": {
    "typescript": "5.7.0"
  }
}
```

**Nx monorepo:**
Single `package.json` at root for shared dependencies. Libraries define `peerDependencies` only.

**Lock files**: Always commit `package-lock.json` / `pnpm-lock.yaml`. Use `npm ci` (not `npm install`) in CI.

---

## 3. Versioning Strategy

```json
// package.json
{
  "version": "1.0.42"
}
```

```typescript
// version.ts — auto-generated or read from package.json
export const APP_VERSION = process.env.npm_package_version ?? 'dev';
```

Expose version via health endpoint or startup log. For monorepos, use `lerna version` or Nx release. The running application must report its version.

---

## 4. Structured Error Handling

```typescript
enum ErrorCode {
  NotFound = 'NOT_FOUND',
  InvalidParams = 'INVALID_PARAMS',
  Unauthorized = 'UNAUTHORIZED',
  Timeout = 'TIMEOUT',
  NotSupported = 'NOT_SUPPORTED',
  InternalError = 'INTERNAL_ERROR',
}

interface AppError {
  readonly code: ErrorCode;
  readonly message: string;
  readonly suggestion?: string;
  readonly candidates?: readonly string[];
}

// Result type for operations that can fail
type Result<T> = { ok: true; data: T } | { ok: false; error: AppError };
```

Never `throw new Error("something went wrong")`. Use typed errors with discriminated unions. Consumers handle errors programmatically via `code`, not string parsing.

---

## 5. Test Configuration (Fast/Slow Split)

**Jest:**
```json
// jest.config.ts
{
  "testPathIgnorePatterns": ["/node_modules/", "/e2e/", ".*\\.slow\\.spec\\.ts$"]
}
```

```bash
# Fast tests (default)
npm test
# Slow/integration tests
npm test -- --testPathPattern="e2e|slow"
```

**Vitest:**
```typescript
// vitest.config.ts
export default defineConfig({
  test: {
    exclude: [...configDefaults.exclude, '**/e2e/**', '**/*.slow.spec.ts'],
  },
});
```

Tag slow tests by filename convention (`*.slow.spec.ts`, `*.e2e.spec.ts`) or directory (`e2e/`, `integration/`).

---

## 6. Immutable DTOs

```typescript
// Readonly interfaces — no accidental mutation
interface UserDto {
  readonly name: string;
  readonly email: string;
  readonly createdAt: Date;
}

// Const assertions for literal types
const ROLES = ['admin', 'user', 'viewer'] as const;
type Role = typeof ROLES[number];

// Derived copies with spread
const updated: UserDto = { ...user, email: 'new@email.com' };
```

Use `readonly` on all DTO properties. Use `Readonly<T>`, `ReadonlyArray<T>` for collections. Never mutate DTOs — create new ones with spread.

---

## 7. Dependency Injection with Proper Registration

**Angular:**
```typescript
// Each feature exposes a module or provider function
export function provideAuth(): Provider[] {
  return [
    { provide: AuthService, useClass: AuthService },
    { provide: HTTP_INTERCEPTORS, useClass: AuthInterceptor, multi: true },
  ];
}

// app.config.ts — clean composition
export const appConfig: ApplicationConfig = {
  providers: [provideRouter(routes), provideAuth(), provideApi()],
};
```

**NestJS:**
```typescript
@Module({
  providers: [UserService, UserRepository],
  exports: [UserService],
})
export class UserModule {}
```

Composition root is readable in 10 seconds. Each feature owns its registrations.

---

## 8. Centralized Configuration with Validation

```typescript
// config.ts — validated at startup
import { z } from 'zod';

const configSchema = z.object({
  DATABASE_URL: z.string().url(),
  PORT: z.coerce.number().int().positive().default(3000),
  LOG_LEVEL: z.enum(['debug', 'info', 'warn', 'error']).default('info'),
  API_KEY: z.string().min(1),
});

export const config = configSchema.parse(process.env);
// Throws at startup if invalid — not silently at runtime
```

**Angular:**
```typescript
// environment.ts — typed, no magic strings
export const environment = {
  apiUrl: 'http://localhost:3000',
  production: false,
} as const;
```

All config in one place. Validation at startup. No scattered `process.env.SOMETHING` across the codebase.

---

## 9. Structured File Logging

**winston:**
```typescript
import { createLogger, transports, format } from 'winston';

export const logger = createLogger({
  level: process.env.LOG_LEVEL ?? 'info',
  format: format.combine(format.timestamp(), format.json()),
  transports: [
    new transports.File({ filename: 'logs/app.log' }),
    new transports.File({ filename: 'logs/error.log', level: 'error' }),
  ],
});
```

**pino:**
```typescript
import pino from 'pino';

export const logger = pino({
  level: process.env.LOG_LEVEL ?? 'info',
  transport: {
    target: 'pino/file',
    options: { destination: 'logs/app.log', mkdir: true },
  },
});
```

Libraries accept a logger instance via constructor/parameter — never create their own. In tests, use a no-op logger.

---

## 10. Zero-Dependency Core Module

```
packages/core/     → zero external dependencies
  src/
    interfaces/    → service interfaces
    models/        → DTOs, types, enums
    errors/        → error types, error codes
    index.ts       → public barrel export
```

Core contains: interfaces, types, DTOs, enums, constants. No implementations. Every other package imports from `@myapp/core`. Core imports nothing.

---

## 11. Interface-First Design

```typescript
// In core — the contract
export interface UserService {
  getById(id: string): Promise<UserDto | null>;
  create(data: CreateUserDto): Promise<UserDto>;
}

export const USER_SERVICE = new InjectionToken<UserService>('UserService');
```

Every significant service has an interface in core. Implementations live in their own packages. Testing uses the interface with mocks/stubs.

---

## 12. Internal/Private by Default

```typescript
// index.ts (barrel) — controls public API
export { UserService } from './user.service';
export type { UserDto } from './models';
// NOT exported: internal helpers, utilities, implementation details

// Private by convention — not in barrel
// internal/parser.ts — consumers can't import it
```

Only export from `index.ts` what consumers need. Everything else is internal. Use ESLint `no-restricted-imports` to enforce boundaries.

---

## 13. Test Module per Production Module

```
packages/core/
  src/
  tests/          → or __tests__/ or *.spec.ts co-located
packages/api/
  src/
  tests/
packages/ui/
  src/
  tests/
```

Each package has its own test suite. A failure tells you which package is broken. Co-located spec files (`*.spec.ts` next to source) also acceptable.

---

## 14. Convention Over Configuration

Examples:
- File-based routing (Next.js, Nuxt, Angular file-based routes)
- Auto-import of modules by directory convention
- Naming convention for test files (`*.spec.ts`, `*.test.ts`)
- Barrel exports (`index.ts`) for public API

---

## 15. Deterministic Build Output

```json
// tsconfig.json
{
  "compilerOptions": {
    "outDir": "./dist",
    "rootDir": "./src"
  }
}
```

Build output always in `dist/`. Clean before build: `rm -rf dist && tsc`. For bundlers (webpack, vite, esbuild), output directory is explicit in config. No stale artifacts.

---

## 16. Black Box Composition

```typescript
// packages/core/index.ts — barrel controls public API
export { OrderProcessor } from './order-processor.interface';
export type { OrderRequest, OrderResult } from './models';
// Internal implementation NOT exported

// packages/orders/index.ts — only re-exports what consumers need
export { OrderProcessorImpl } from './order-processor';
```

Each black box is a package with a barrel export (`index.ts`) controlling its public API. Internal files are not exported. Use ESLint `no-restricted-imports` to enforce boundaries between packages. Test each box via its exported interface.

---

## Verification Commands

```bash
# Check strict mode
grep -r '"strict": true' tsconfig.json
# Check lock file committed
ls package-lock.json || ls pnpm-lock.yaml
# Build with zero warnings
npx tsc --noEmit
npx eslint --max-warnings 0 .
# Run fast tests
npm test
# Check for scattered env access
grep -rn "process.env\." --include="*.ts" src/ | grep -v config
```
