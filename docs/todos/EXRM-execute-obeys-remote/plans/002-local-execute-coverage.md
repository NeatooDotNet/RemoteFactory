# Local Execute Proven on Both Shapes, Plus Auth

**Plan #:** 002
**Date:** 2026-09-08
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-1, AC-2, AC-4
**Status:** Draft
**Last Updated:** 2026-09-08
**Plan-review opt-in:** Yes — the Design samples this plan adds are the requirements source of truth that EXRM-004 will document; `plan-reviewer` (Pass A carries the business-requirements check)
**Code-review opt-in:** Yes — user decision 2026-09-08 (orchestrator proposed No: no generator or library change); runs at Step 5 beside the test review
**Branch:** exrm-002-local-execute-coverage — cut from the arc at Step 2
**PR:** —

---

## Scope

Prove the new contract from the client side for both shapes: the combination generator gains a Local execution mode for Execute and the Execute behavior tests gain local legs with a wire-crossing negative control; class-level bare `[Execute]` gets client round-trip tests beside the existing `[Remote, Execute]` ones; the interplay of `[AspAuthorize]` and `[AuthorizeFactory]` with a bare class-level `[Execute]` is pinned; and the Design projects gain bare `[Execute]` samples with tests, as this repo's requirements verification. Does not change the generator or the library beyond what EXRM-001 landed, and does not touch published docs, the skill, or the trimming harness.

---

## Intent

- After EXRM-001 the static shape is pinned but the class shape rests on the reading that its renderer already obeys `[Remote]`. This plan turns that reading into tests: a bare class-level `[Execute]` observed running on the client, resolving its services there, and never touching the wire.
- The combination suite currently proves Execute only in its `[Remote]` form. Opening a Local mode makes bare Execute a first-class combination like every other operation, so future regressions in either direction are caught by the same generated matrix.
- The authorization terms that force remoteness (`[AspAuthorize]`, a `[Remote]` auth method) and the one that does not (`[AuthorizeFactory]` with a client-side auth method) are pinned on the bare shape, so AC-4 stops being an inference from `BuildClassExecuteMethod`.
- The Design projects show a consumer what a client-runnable `[Execute]` looks like on each shape — zTreatment's driving case — and Design.Tests prove it stays on the client, replacing the "[Remote] is decorative" note with the real rule.

---

## Framework & Architectural Alignment

- Two-container client/server simulation (`ClientServerContainers.Scopes()`), with the `RemoteCallCounter` EXRM-001 added as the wire-crossing discriminator; the Design counterpart uses its `configureClient` hook to install a wire stand-in that fails the test if reached.
- `RegisterMatchingName` name-convention DI: `IService` resolves on the client, `IServerOnlyService` (implementation named `ServerOnly`) does not — the positive/negative pair from `ShowcaseReadTests`.
- `[AuthorizeFactory]` static-flag toggling (`EnforcementTestAuth.ShouldAllow`, Design's `AuthorizedOrderAuth` flags); `IAspAuthorize` is a core-library interface, so a plain fake on the server container exercises `[AspAuthorize]` without the AspNetCore package.
- Config-driven combination targets (`CombinationDimensions.json` → `CombinationGenerator`), mirroring the Local/Remote split the CRUD operations already declare.
- Design projects as requirements verification (repo CLAUDE.md, Step 7B); Design samples carry the "why" in comments, DDD vocabulary unexplained.
- No reflection in tests; sacred tests receive additions only.

---

## Constraints & Invariants

- No source change under `src/Generator` or `src/RemoteFactory`. A test that needs one has found a bug: stop and report, never work around it in the test.
- `ExecuteBehaviorTests` (ten tests) and `ClassExecuteRoundTripTests` keep every existing test and assertion; new coverage lives in new regions or new files.
- Interface factories stay always-remote and are not touched (todo Out of Scope, NF0106).
- Class-level Execute keeps its return-type rule (the containing type's service type) and its `Task<T>` rule (NF0102).
- `[Remote, Execute]` samples and tests in Design are unchanged; new samples sit beside them.
- Both solutions (`src/Neatoo.RemoteFactory.sln`, `src/Design/Design.sln`) build and test green, with a test-count increase in each.
- Published docs, the skill, `CLAUDE-DESIGN.md`, and the reference app are EXRM-004's; this plan edits Design *code and its comments* only.

---

## Steps

1. Open a Local execution mode for Execute in the combination dimensions so the five parameter shapes exist as bare delegates beside the `[Remote]` five, and retire the "always remote" constraint label the config still carries.
2. Give the Execute behavior tests a new region for the bare targets called from the client scope — each runs there with its declared parameters and service and zero wire crossings — named and prefixed (`BareExecute_…`) so it cannot collide with the existing `_Local_` tests, which read the `[Remote]` targets from the Logical scope and stay as they are.
3. Add class-level bare `[Execute]` targets and client-side tests: client service resolution with no remote call; a server-only service as the call-time negative control on the client and the positive control on the server; a `[Remote, Execute]` sibling that crosses the wire exactly once.
4. Pin the authorization interplay on the class shape: a client-side `[AuthorizeFactory]` check on a bare Execute runs locally, allowed and denied, with no wire; `[AspAuthorize]` on a bare Execute forces the wire and reaches the server's `IAspAuthorize`; a `[Remote]` auth method forces the wire by the same rule.
5. Add a bare `[Execute]` sample to each Design shape — the static commands class and the class-factory demo — taking a service the client container can resolve, with comments stating the contract. Replace only the decorative-`[Remote]` claim in the adjacent note; its trimming sentence belongs to EXRM-003, which rewrites it after measuring the pair.
6. Add Design tests that call both samples from the Design client scope and prove no remote request was made, leaving the existing `[Remote, Execute]` Design tests as the AC-2 control.
7. Build and test both solutions to log files; confirm the test counts rose.

---

## Acceptance

- [ ] Each bare Execute combination target, resolved from the client scope, runs there with its parameters and service and makes zero remote calls `[integration]` · Must
- [ ] A bare class-level `[Execute]` called through the client's factory runs on the client with client-resolved `[Service]` and no remote call; the same shape taking a server-only service fails at call time on the client (no remote call) and succeeds on the server `[integration]` · Must
- [ ] `[Remote, Execute]` on a class factory, called from the client, crosses the wire exactly once and returns the v1.8.1 result `[integration]` · Must
- [ ] A client-side `[AuthorizeFactory]` check on a bare class Execute runs locally: allowed executes with no remote call; denied returns null, as a denied Create does, still with no remote call `[integration]` · Should
- [ ] `[AspAuthorize]` on a bare class Execute forces exactly one wire crossing and the server's `IAspAuthorize` is consulted `[integration]` · Should
- [ ] The Design bare-`[Execute]` samples on both shapes run from the Design client scope with no remote request, and the Design `[Remote, Execute]` samples still round-trip `[integration]` · Must
- [ ] Both solutions build and test green with test counts up `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

_(Step 3)_

---

## Punchlist

Step 2 triage (2026-09-08): the five open todo-level rows are all AC-5 docs/version rows in EXRM-004's and EXRM-005's paths; none lies in this plan's path, none pulled down.

-

---

## Test Evidence

_(after implementation)_

---

## Gate Record

_(Step 5)_

---

## Plan Amendments

_(append-only)_

### 2026-09-08 — Plan-review callouts amended before implementation

- **Section affected:** Steps 2 and 5 (was 6); Acceptance bullets 4 and 7
- **Original said:** Step 2 added "a Local-mode region"; Step 6 replaced the whole decorative note; bullet 4 said denial "surfaces as it does for a denied Create"; bullet 7 pinned the visibility rule at `[unit]`.
- **What changed:** Step 2's region is named for bare targets and prefixed `BareExecute_` (B1 — the natural names collide with the existing `_Local_` tests); Step 5 rewrites the decorative claim only and leaves the trimming sentence to EXRM-003 (B5); bullet 4 says "returns null" (B2 — `Authorized<T>.Result` is default on denial); bullet 7 moved to EXRM-004 beside the Punchlist sentence it backs (B4, B3). B6 dismissed.
- **Why:** `reviews/002-plan-review.md`; user decisions 2026-09-08.
- **Discovery Log:** 2026-09-08 / EXRM-002

---

## Notes

Recon (2026-09-08): the pattern for "ran locally on the client" is `Showcase/ShowcaseReadTests.cs`: a bare `[Create]` taking `[Service] IServerOnlyService` whose body is `Assert.Fail()`, asserted to throw `InvalidOperationException` from the client (negative form), and a bare `[Create]` taking `[Service] IService` resolved on the client through `RegisterMatchingName` (positive form). `ClassExecuteRoundTripTests` and `Design.Tests/ClassFactoryExecuteTests` are all `[Remote, Execute]` and become AC-2 evidence. Static factories carry no authorization path in the builder today, so AC-4 is class-level; `[AspAuthorize]` has no Execute precedent in any test (`AspAuthorizeTestObj.cs` covers Create/Insert only) and the AspNetCore test library is consumed by no test project.

Step 2 recon additions: `IAspAuthorize` lives in core `RemoteFactory`, so the integration tests can host a plain fake through `Scopes(configureServer:)` — no AspNetCore reference needed. `DesignClientServerContainers` has no call counter; its `configureClient` hook is the seam for a throwing wire stand-in. `CombinationDimensions.json` lists Execute with `"constraints": ["requires_static_class", "always_remote"]`; no test consumes the constraint strings. `AllPatterns.cs:368-371` carries the "[Remote] is decorative on [Execute]" note beside the static sample. Design registers its services by `RegisterMatchingName` on all three containers, so a Design sample's `[Service]` resolves on the client whenever the implementation name matches — the sample's comments must say so rather than claim a server-only service.

Could-tier candidate from the 001 plan review (B1) was drafted as Acceptance bullet 7 and moved to EXRM-004 at this plan's review (B4/B3): it backs todo Punchlist row 3, which 004 writes, and a single-method internal target renders the whole interface internal rather than a prefixed member. Pre-flight notes from the review: the static Design sample follows `private static _Name` (`AllPatterns.cs:380-388`); the Design wire stand-in throws from `ForDelegate*`, never its constructor, and its test calls only the bare sample.
