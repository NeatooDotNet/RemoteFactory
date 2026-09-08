# EXRM-002 — Plan Review

**Date:** 2026-09-08
**Reviewer:** plan-reviewer (budget: tight)
**Object:** `plans/002-local-execute-coverage.md` as drafted at Step 2, Current State empty
**Verdict:** APPROVED — no veto-tier; one Must callout, five lesser

## Pass A — documented requirements

**Veto-tier:** none. `CLAUDE-DESIGN.md:374` (Execute must return `Task<T>`) and NF0106's interface rule are both respected by Constraints. The only rule the plan overwrites is the decorative-`[Remote]` note, which the todo Goal authorizes.

## Pass B — codebase

**Veto-tier:** none. Direction is sound and confirmed against code: `constraints` strings are read into `Constraints` at `CombinationGenerator.cs:147` and consumed nowhere, so retiring `always_remote` is inert; `CombinationInfo.cs:21` puts `ExecutionMode` in the class name, so Local targets cannot collide; `RemoteCallCounter` is registered in `RegisterFactoryTypes` (`ClientServerContainers.cs:444`), which the `Scopes(configure…)` overload reaches via `ConfigureContainer:355`, so Step 4's overload has the counter; `ConfigureContainer:358` skips explicit services on Remote but `:369 RegisterMatchingName` still gives the client `IService`→`Service` and not `IServerOnlyService`→`ServerOnly`, so the Step 3 positive/negative pair works as described; `[AuthorizeFactory]` matches class Execute by operation flag (`FactoryGenerator.Types.cs:609`), and `IAspAuthorize` is registered only by AspNetCore (`ServiceCollectionExtensions.cs:30`), so a server fake has no competitor.

**Callout B1 (Must).** `ExecuteBehaviorTests.cs:109-173` — the five *existing* tests are already named `Execute_TaskTResult_{None,Single,Multiple,Service,Mixed}_Local_*` and resolve the `_Remote` targets from `_localScope`. Step 2's new client-scope bare tests want exactly those names, and Constraint line 45 forbids renaming the incumbents → CS0111. "Local" now means two things in one file (Logical *scope* vs bare *target*); the region comment at `:102-107` says the counter there "documents the expectation rather than discriminating it" — Step 2's new legs are the ones that actually discriminate. Affects AC-1 (Must). Reachable by writing Step 2's region with the natural names.

**Callout B2.** `ClassFactoryRenderer.cs:319` and `:827` both emit `return (await Local…).Result;` and `Authorized<T>.Result` (`Authorized.cs:77`) is `default` on denial — bullet 4's "as it does for a denied Create" is **true but means returns null**, not throws; only write paths throw (`:1087`, `:1533`). Affects AC-4 (Should). Reachable by asserting `ThrowsAsync<NotAuthorizedException>`.

**Callout B3.** `ClassFactoryModel.cs:62` — if the Step 5 target's only factory method is `internal static [Execute]`, `AllMethodsInternal` is true, so `ClassFactoryRenderer.cs:113` renders `internal interface` with **no** per-member `internal` prefix (`:124`). Affects bullet 7 (Could). Reachable by a single-method target plus asserting `internal Task<T> …` in generated source.

**Callout B4.** Bullet 7 advances no criterion in **Serves** (AC-1/2/4); it is todo Punchlist row 3, tagged AC-5 · Must — which plan line 88 says was *not* pulled down. Affects AC-5 (Must). Either Serves gains AC-5 or the triage line is wrong.

**Callout B5.** Step 6 rewrites `AllPatterns.cs:368-371`, whose last sentence ("The guard is what makes the body trimmable, not the attribute") is EXRM-003/AC-3's finding, not yet measured. Affects AC-3 (Must). Reachable by rewriting the whole note.

**Callout B6.** `AllPatterns.cs:380-388` documents static-factory `[Execute]` as `private static _Name` ("COMMON MISTAKE: Making the method public"); Step 6's new sample must follow it, not the combination targets' `public static`. Affects AC-5 (Must).

**Theoretical (not triaged).** A throwing `IMakeRemoteDelegateRequest` via Design's `configureClient` (`DesignClientServerContainers.cs:259` then `:263`) disables the wire for every call in that test, and throwing in its constructor would fail factory resolution rather than the assertion. Noted for pre-flight: the stand-in throws from `ForDelegate*`, never the constructor, and the test calls only the bare sample.

**Read report.** Beyond the brief: `Authorized.cs`, `ClassFactoryRenderer.cs` (:100-330, :1400-1540), `ClassFactoryModel.cs`, `FactoryGenerator.Types.cs:575-615`, `FactoryModelBuilder.cs:1025-1050`, `CombinationInfo.cs`, `ExecuteBehaviorTests.cs`, `CLAUDE-DESIGN.md` (grep). Named but unused: `ClassExecuteRoundTripTests`, `ShowcaseReadTests.cs`, `001-*-review.md`, reference-app `ExecuteSamples.cs`.

## Triage (proposed 2026-09-08; user decisions recorded below)

| # | Finding | Affects | Priority | Size | Disposition |
|---|---|---|---|---|---|
| 1 | B1 — Step 2's natural test names collide with the existing `_Local_` tests | AC-1 | Must | punch | **Amend** Step 2: new region named for bare targets from the client scope, tests prefixed `BareExecute_…`; incumbents untouched |
| 2 | B4 — bullet 7 serves no listed criterion; it backs Punchlist row 3 (AC-5) | AC-5 | Must (row) / Could (pin) | punch | **Amend**: move bullet 7 to EXRM-004, which writes the sentence the pin backs; 002's Serves unchanged |
| 3 | B5 — Step 6 rewrites the trimming sentence EXRM-003 has not yet measured | AC-3 | Must | punch | **Amend** Step 6: replace the decorative claim only; the trimming sentence is pointed at EXRM-003 for rewrite after measurement |
| 4 | B6 — the static Design sample must be `private static _Name` | AC-5 | Must | punch | **Dismiss**: the existing Design convention already binds the sample; noted for pre-flight |
| 5 | B2 — a denied `[AuthorizeFactory]` on Execute returns null, not throws | AC-4 | Should | punch | **Amend** bullet 4: "denied returns null, as a denied Create does" |
| 6 | B3 — a single-method internal target makes the whole interface internal | — | Could | punch | Folds into row 2: bullet 7 lands in 004 worded "not promoted to public" |

**User decisions:** Accept all as proposed (2026-09-08); commit on the plan branch, no push until Done.
