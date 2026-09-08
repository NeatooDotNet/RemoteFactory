# EXRM-001 — Test Review

**Date:** 2026-09-08
**Reviewer:** test-reviewer (budget: tight)
**Object:** `plans/001-static-delegates-obey-remote.md` Acceptance + Test Evidence, the new and rewritten tests, `reviews/001-build.log`, `reviews/001-test.log`

## Round 1 — CLEAN

Every Acceptance bullet is pinned by a real, non-vacuous test at its declared tier; the sacred rewrite added assertions only; both logs are green (build 0 errors, 4 pre-existing warnings; tests 2960 passed, 0 failed, 10 pre-existing skips across both frameworks).

**Veto-tier:** none. The three sacred files named as affected (`RemoteExecuteServiceTests.cs`, `ClassExecuteRoundTripTests.cs`, `FactoryEventHandlerClientServerTests.cs`) are byte-identical to the arc. `ExecuteBehaviorTests.cs` keeps its ten tests; every hunk adds a `RemoteCallCounter` assertion after the existing ones, none removed; the header and region names now state the follows-`[Remote]` rule.

**Must-cover:** none. Bullet 1 asserts the client-side result and counter 0; bullet 6 asserts the exception and counter 0; bullet 3 resolves and runs in all three containers; bullet 2 asserts a result only server DI can produce and counter 1, which is what makes it non-vacuous. The counter is registered in every container path and incremented only by the client stand-in, so a local-scope zero is meaningful. Bullet 2's guard half is an honest ✓: the bullet text stops at "executes on the server", the trimmed-client measurement is AC-3 and EXRM-003's.

**Should-cover:** none. Bullet 4's refusal is confirmed server-side in the handler, before deserialization, off the registry, with the paired positive control ruling out a blanket refusal.

**Tech-debt (2):**
1. The server-only failure test asserted only the exception type. **Punched inline:** the test now also asserts the message names `IServerOnlyService`; rerun green (appended to `001-test.log`).
2. `LocalOnlyDelegateRegistryTests` leaves probe types in the process-global registry. **Dismissed** on the todo: harmless, no test asserts registry contents.

**Theoretical (not triaged):** Server-container absence of the remote registration is proven via Logical (same renderer branch); removing the guard from a remote delegate's local registration would stay green here until EXRM-003's trimming gate lands.

**Closing tier:** all bullets `[integration]` as declared, one `[explicit-skip]` meta-bullet; nothing accepted as MISSING.
