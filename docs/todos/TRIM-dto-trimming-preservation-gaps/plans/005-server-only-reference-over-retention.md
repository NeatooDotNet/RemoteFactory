# TRIM-005 — Server-only reference over-retention in trimmed clients

**Plan #:** 005
**Status:** Draft
**Plan-review opt-in:** TBD at draft
**Code-review opt-in:** TBD at draft
**Related Todo:** [../todo.md](../todo.md)

## Scope

Investigate and resolve the trimmed-client over-retention of server-only *references*. At HEAD, a publish-trimmed client keeps the `IServerOnlyRepository` TypeDef name and its `DoServerWork` member reference because generated `LocalCreate` bodies survive ILLink constant folding — the methods are rooted client-side by delegate registration, and the early-`throw` feature-switch guard combined with the `try/catch` region defeats unreachable-code elimination. This contradicts `docs/trimming.md` ("Verifying Trimming Results" says the server-only grep should return no matches; "Class Factories — Conditional Guards" says everything after the throw guard is removed) and the harness README's original claim. Outcome is either a generator fix (a guard shape ILLink folds fully — e.g. if/else around the whole body — so server-only references vanish from client binaries) or a documented acceptance (docs corrected; TRIM-004's implementation-only CI grep stays the contract). Functional boundary enforcement is intact either way — implementations are trimmed; only names/metadata remain — so this is bundle-size and docs-accuracy work, not a correctness fix. Discovered during TRIM-004 (2026-07-06). Does NOT touch DTO preservation (TRIM-001/002) or event verification (TRIM-003).
