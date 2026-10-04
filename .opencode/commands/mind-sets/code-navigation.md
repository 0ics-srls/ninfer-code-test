# Code Navigation (shared rule — LSAI / vs-mcp / xmp4 / grep)

How to look up symbols, usages, and "does X exist" reliably, in ANY language.
A skill that trusts a nav tool WITHOUT proving it is present + ready + queried
correctly will silently fall back to grep, or read an empty answer as "0 found"
-- a false negative. This rule is referenced by `j-review-plan`, `audit`,
`j-develop`, `j-bug`, `j-new-feature`. Settings (which tool, scoping paths) come
from `j-settings.md`; repairing stale settings is `/j-setup`'s job, not this rule.

## GATE -- print this PREFLIGHT block before the first code-nav lookup

This is NOT a suggestion. Output the filled block below before any symbol/usage/
existence query. A missing block or a `no` answer is visible -- that is the point.

```
CODE-NAV PREFLIGHT
- tool: <@code-nav-local: lsai | vs-mcp | grep>  -> available this session? <yes|no>
- ready: <Ready | not-ready: state>  ; if not-ready -> waited + re-checked? <yes>
- scope: <own-code roots used>  ; dependency/build/system paths excluded? <yes>
```

You may not start querying until this block is printed.

## The discipline (per-query -- re-apply to EVERY lookup, not once)

1. **Per-query invariant.** A lookup you scoped/checked a minute ago does NOT make
   the next one safe. Re-apply this whole rule to each query. (Skipping it after
   the first query is the #1 way grep creeps back in.)
2. **Availability + readiness.** The tool (`@code-nav-local`: lsai or vs-mcp) must
   be present this session AND its index/solution must be ready. An empty result
   while it is loading/indexing means "not ready", NOT "0 matches" -- wait and
   re-check. A present-but-dead tool returning nothing is indistinguishable from
   "absent": confirm it is live before trusting silence.
3. **Scope to own code.** The index covers the whole build graph -- your code PLUS
   dependencies (node_modules, NuGet packages, `target/`, `vendor/`, go module
   cache, `.venv`, build dirs) PLUS framework/system code. An unscoped search of a
   common term is dependency noise BY DESIGN. Scope by `@own-code-roots` /
   `@vendored-globs` from settings, or the generic excludes above if unset.
4. **Tool by question.** Existence / signature -> the precise "info / outline"
   query (lsai_info/lsai_outline; vs-mcp FindSymbols/GetDocumentOutline). References
   -> "usages / callers" on a real symbol. Fuzzy discovery -> "search", and ONLY
   scoped. Do not use a fuzzy search to answer "does X exist".
5. **Negative verdict needs citation.** "Absent / 0 uses / doesn't exist" is valid
   ONLY from a precise info/outline query or a path-scoped search, with the tool
   call + result quoted. Report a have/total ratio as **Partial**, never collapse
   it to **Missing**. No quoted result = not a verdict, it is a guess -> STOP.
6. **Division of labor.** own code -> `@code-nav-local` (lsai|vs-mcp) · third-party
   library API -> `@code-nav-external` (xmp4, if set) · non-code files + last-resort
   -> grep. Fallback to grep triggers when the tool is **absent or not-ready at
   runtime** (not merely when `@code-nav-local` is configured as `grep`), and must
   be flagged low-confidence.

## Indexer blind spots (confirm, don't trust silence)

Compiler-expanded / generated constructs (C/C++ macros, Rust proc-macros, C#
source generators, Go codegen) may be under-indexed -- a lookup can return
"not found" for something that exists. For those, confirm with grep or a build
with the generators run. Per-language specifics live in that language's overlay,
not here.
