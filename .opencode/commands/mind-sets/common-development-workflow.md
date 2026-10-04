---
description: >-
  Cross-language development workflow — research-first, 5-phase pipeline,
  pre-review checklist
---
# Common Development Workflow — All Languages

This workflow applies to ALL feature development, regardless of language.

## Phase 0: Research & Reuse (MANDATORY)

Before writing ANY new code, follow this search hierarchy:
1. **Search the codebase** — similar patterns, base classes, utilities already built
2. **Check framework/library docs** — does the framework already provide this?
3. **Check package registries** — npm/pip/cargo/pub/composer for battle-tested libraries
4. **Search GitHub** — proven implementations, skeleton projects, reference architectures
5. **Web search LAST** — only if above sources are insufficient

**Rule: Prefer adopting or adapting a proven approach over writing net-new code when it meets 80%+ of the requirement.**

## Phase 1: Plan First

- Break the feature into discrete, testable steps
- Identify dependencies and risks BEFORE coding
- For complex features: use TDDAB planning methodology
- For simple features: a mental checklist is sufficient
- **WAIT** for confirmation before proceeding to implementation

## Phase 2: TDD Approach

1. **RED** — Write a failing test that defines the expected behavior
2. **GREEN** — Write the minimum code to make the test pass
3. **REFACTOR** — Clean up without changing behavior
4. Target **80%+ code coverage** for business logic
5. Commit after each RED→GREEN→REFACTOR cycle (checkpoint evidence)

## Phase 3: Code Review

- Review your own code BEFORE declaring done
- Check security: SQL injection, XSS, secrets, auth bypasses
- Check quality: error handling, edge cases, performance
- Address CRITICAL and HIGH issues immediately
- Fix MEDIUM when possible, document LOW as TODOs

## Phase 4: Commit & Push

- Atomic commits — one logical change per commit
- Conventional commit messages: `feat:`, `fix:`, `refactor:`, `test:`, `docs:`
- Verify CI/CD passes before requesting review
- Resolve merge conflicts before pushing

## Pre-Review Checklist
Before declaring any feature "done":
- [ ] All tests pass
- [ ] No warnings in build output
- [ ] New code has tests
- [ ] Error handling covers edge cases
- [ ] No hardcoded secrets or values
- [ ] Input validation at all boundaries
- [ ] Documentation updated if needed
