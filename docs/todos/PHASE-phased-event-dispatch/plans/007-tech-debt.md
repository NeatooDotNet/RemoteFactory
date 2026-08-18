# Test Harness Debt, Emission Pins, and the Coordinator's Silent Short-Circuit

**Plan #:** 007
**Date:** 2026-08-18
**Related Todo:** [../todo.md](../todo.md)
**Status:** Implemented — gate pending
**Last Updated:** 2026-08-18
**Plan-review opt-in:** No (test infrastructure and pins; the one behavior addition is a Debug log event; blast radius bounded by the sacred-tests rule)
**Code-review opt-in:** Yes (touches sacred harness files broadly; one runtime addition; a possible scheduler storage tweak)

---

## Scope

Pay down the debt every PHASE gate routed here: pin the phase log events that ship
unasserted (9002/9004/9006, plus 9002's collapsed count), pin the accepted-but-undocumented
behaviors (undefined-phase silent no-op, `Enqueue` null-handler guard, NF0505's attribute
location, coalescing's runtime inertness on an unqueued path, a production-shaped
warn-merge variant), give the coordinator's silent short-circuit a Debug event id so a
consumer draining outside an entry call gets a signal instead of a 9007 telling them to do
what they just did, add the first Design.Server test (a composition check that its
container can resolve every `[Service]` parameter type Design.Domain's factory methods
declare) and replace the `Assert.True(true)` trio, consolidate the duplicated test
harnesses (relay wait helper, `ScopesWithLogging`, tuple-order divergence,
`CapturingLoggerProvider` snapshot accessor, `IEventTestService`/registry isolation
discipline — written down, whichever way each lands), defuse the `SingleEventRelay` 2s
poll flake, restore O(1) dequeue for the scheduler's default path (or formally accept the
List cost), and record the `Design.sln` build requirement where verification instructions
live. This plan does NOT touch the generator's emission (PHASE-008), does NOT build the
scheduler concurrency harness (re-split to PHASE-009), and changes no consumer-facing
contract beyond the one new Debug log id.

---

## Inherited (routed here by the 001–006 gates — provenance per item)

- 9002/9004/9006 positive emission pins; harness exists since 004 (`CapturingLoggerProvider`). *(003 gate)*
- 9002's drained count under collapse. *(006 gate)*
- Undefined-phase silent no-op documenting pin. *(002 discovery, accepted; 004 routing)*
- `Enqueue` null-handler guard pin. *(004 gate)*
- NF0505 location pin — it deliberately points at the attribute, diverging from the class-identifier convention, and the better choice is unprotected. *(006 gate)*
- Coalescing runtime-inertness pin on an unqueued path (XML claims it; only emission is pinned). *(006 gate)*
- Production-shaped warn-merge variant — the shipped pin drives `Enqueue(Immediate, …)`, which the dispatcher never produces. *(006 code review C5)*
- Coordinator short-circuit observability — new Debug event id. *(004 code review / discovery)*
- Design.Server composition test — it registers 3 services while Design.Domain `[Service]`-injects `INotificationService` and `IPhaseAuditService`; no Design.Server test exists. *(005 gate)*
- `FactoryEventHandlerTests` `Assert.True(true)` trio — after 005's rescope it is the Design tier's nominal `Immediate` pin and asserts nothing. *(005 gate)*
- Registry test-isolation: `Clear()` internal and uncalled; every test invents unique event types; 006 added five more. *(001 routing; 006 dependents note)*
- `ClientServerContainers` tuple-order divergence + `ScopesWithLogging` duplication + cross-container log attribution; another divergent call site in 006's relay-pin helper. *(003 gate; 006 note)*
- `IEventTestService` shared-singleton Guid-filter discipline. *(004 routing)*
- `CapturingLoggerProvider.Entries` snapshot accessor; 006 added three more pins on the raw list. *(004 routing; 006 note)*
- `SingleEventRelay` hard 2s poll flaking under full-parallel runs; 006 duplicated the harness. *(004 discovery; 006 note)*
- Scheduler storage shape — O(n) front-dequeue paid by the non-opted-in path on an unenforced smallness assumption. *(006 code review C3)*
- Explicit `Design.sln` build in verification docs — the main solution omits it. *(005 RP-0)*

---

## Intent

- Every phase log event id (9001–9008, plus the new short-circuit id) has at least one
  positive emission pin, so a wording or wiring regression is caught by a test rather
  than a consumer.
- Accepted behaviors stop being oral tradition: each "we decided not to change this"
  from the arc's gates gets a documenting pin that fails if the behavior drifts silently.
- A consumer whose transaction abstraction wraps the factory call from outside gets a
  Debug breadcrumb at the moment their drain did nothing, instead of only a 9007 later
  telling them to do what they just did.
- Design.Server stops being the one project with zero tests while its container silently
  cannot serve the domain it hosts.
- The test harnesses this arc leaned on (and duplicated under deadline) become single,
  documented seams; the isolation disciplines the suite depends on are written where the
  next test author will see them.
- The full suite stops having a known flake, and the scheduler's default path stops
  paying for a feature it doesn't use — or the acceptance is recorded formally.

---

## Framework & Architectural Alignment

- **Existing tests are sacred:** harness consolidation preserves every existing
  assertion's intent; mechanical call-site alignment only where names/positions change.
- **Log-event conventions (this arc's):** new id continues the 9xxx block; `LoggerMessage`
  partial in `Internal/Log.cs`; documented in the three log tables (CLAUDE-DESIGN,
  docs/factory-events.md, skill) in the same change.
- **Red-proof discipline:** documenting pins state what makes them discriminating; any
  claim of "would go red" is measured or labeled derived.
- **`Internal` namespace policy:** any scheduler storage change keeps the public/unsealed
  shape and the pinned semantics (FIFO, counts, warn-merge).
- **Skill self-containment** for any skill-table row added.

---

## Constraints & Invariants

- No consumer-facing contract change except the one new Debug log event (additive).
- The 729/591/94 suites stay green with existing assertions intact; totals only grow.
- The coordinator's short-circuit semantics do not change — only its observability.
- The scheduler's pinned semantics (FIFO order, collapsed counts, warn-merge, discard)
  are invariant under any storage change; the 006 pins are the proof.
- Design.Server gains a test project reference only if the composition test needs it —
  no production restructuring of the sample server.
- Harness changes may move/rename test-infrastructure code but never weaken an assertion;
  where a divergence is documented rather than fixed, the doc lives in the code.

---

## Steps

1. **Emission pins:** positive unit pins for 9002 (incl. the collapsed drained count),
   9004, and 9006 message/level/shape, using the capture harness — with the snapshot
   accessor added first so these pins don't grow the raw-list dependency.
2. **Documenting pins:** undefined-phase silent no-op; `Enqueue` null-handler guard;
   NF0505's attribute location; runtime inertness of the coalesce flag on an unqueued
   path; the production-shaped warn-merge variant (AfterFlush trigger, not
   `Enqueue(Immediate, …)`).
3. **Coordinator short-circuit observability:** new Debug event in `Log.cs` emitted when
   `DrainAsync(AfterFlush)` returns without draining because no entry call is active;
   pinned (fires there, silent when a drain proceeds); the three log tables gain its row.
4. **Design.Server composition test:** resolve every `[Service]` parameter type declared
   by Design.Domain factory methods from Design.Server's registered collection; fix the
   two known missing registrations it will catch (`INotificationService`,
   `IPhaseAuditService`) in Design.Server's Program.
5. **`Assert.True(true)` trio:** real assertions preserving each demonstration's intent
   (dispatch-to-all-handlers observed through a recording seam).
6. **Harness consolidation:** one relay-wait helper; `ScopesWithLogging` and the
   coalescing relay helper folded to single seams; tuple-order divergence either aligned
   or documented at the declaration; `IEventTestService` and registry-isolation
   disciplines written at their seams (isolate-vs-document decided at the keyboard).
7. **Flake defusal:** the `SingleEventRelay` poll made deterministic or its timeout made
   proportionate to parallel load; recorded either way.
8. **Scheduler default-path dequeue:** restore O(1) head removal (cursor or equivalent)
   with the 006 pins proving semantics unchanged, or record formal acceptance in the
   storage comment and the todo.
9. **Docs:** `Design.sln` build added to the verification commands in CLAUDE.md (and
   anywhere else build instructions live); the new log-event row in all three tables.
10. **Gate:** Test Evidence with expected totals, single build+test run to logs (both
    solutions), test-reviewer; code review (opted in).

---

## Acceptance

- [x] 9002, 9004, and 9006 each have a positive unit pin (message, level, and the
      count where the message carries one), and 9002's count reflects a collapsed
      queue. `[unit]`
- [x] The accepted behaviors are pinned as documented: undefined-phase registration
      never drains (silent no-op), `Enqueue` null-handler throws, NF0505 locates at
      the attribute, the coalesce flag is runtime-inert on an unqueued dispatch path,
      and the warn-merge holds in a production-shaped ordering. `[unit]`
- [x] The coordinator short-circuit emits the new Debug event exactly when it returns
      without draining, and not when a drain proceeds; the id appears in all three
      log tables. `[unit]` (tables `[explicit-skip: prose]`)
- [x] A Design.Server composition test resolves every `[Service]` parameter type of
      Design.Domain's factory methods; it fails before the Program registrations are
      fixed and passes after (measured, not asserted). `[integration]`
- [x] The three `FactoryEventHandlerTests` demonstrations assert observable dispatch
      behavior; their original intent statements are preserved. `[integration]`
- [x] One relay-wait helper and one logging-scopes helper exist; the divergences that
      stay are documented at their declarations. `[explicit-skip: harness refactor —
      verified by the suite staying green and by review]`
- [x] A full-parallel (default `-m`) solution test run passes with the relay tests
      included. `[explicit-skip: run evidence in the gate log, noted alongside the
      -m:1 run]`
- [x] The scheduler's default-path dequeue decision is executed or formally recorded;
      either way the 006 coalescing/ordering/count pins are untouched and green.
      `[unit]` (acceptance-of-record variant `[explicit-skip: recorded decision]`)
- [x] Full suites green both solutions, expected totals reconciled. `[explicit-skip:
      meta-bullet, satisfied by the gate run]`

---

## Current State (Pre-Flight)

Read before the first edit. Five findings changed the plan; they are carried into
Plan Amendments below.

**Log-event coverage is not what the Inherited list assumed.** 9005 is *already*
positively pinned (`FactoryEventsDispatcherPhaseTests.cs:253` — id + Debug level).
9002 has no assertion anywhere in either suite. 9004's only mention
(`FactoryEventPhaseEntryTests.cs:226`) matches `9005 || 9004`, so it cannot
discriminate between the two fallbacks and is not a pin. 9006 has PHASE-006's two
count pins (`FactoryEventPhaseCoalescingTests.cs:356,375`) asserting the message
text, but no level assertion. So the real gap is 9002 (nothing), 9004 (nothing
discriminating), 9006 (level only) — not the three-way hole the row implied.
`Log.cs` tops out at 9008, so **9009 is free** for the coordinator short-circuit.

**Design.Server's drift is wider than "two missing registrations."** It registers
three services (`IOrderRepository`, `IExampleRepository`, `IExampleService`) and
calls neither `RegisterMatchingName` (which `Person.Server/Program.cs:11` does) nor
anything else. Design.Domain `[Service]`-injects four more:
`INotificationService`, `IProductReviewService`, `IPhaseAuditService`, and —
through constructor injection on `CtorInjectedEntity`/`ExecCtorEntity` —
`ITenantTokenService`. Two of those (`IOrderRepository` → `InMemoryOrderRepository`,
`IProductReviewService` → `InMemoryProductReviewService`) break the `IFoo`→`Foo`
convention, so `RegisterMatchingName` alone does not close the gap.

**The composition test cannot be built the way the Acceptance bullet described.**
"Resolve every `[Service]` parameter type declared by Design.Domain factory methods"
is a reflective enumeration, and the standing rule is no reflection without approval
(global CLAUDE.md). Non-reflective alternatives split into a hand-maintained mirror
of Program.cs (which cannot go red when Program.cs drifts — the arc's own recurring
defect, deliberately introduced) and a shared compiled seam. **Measured:** adding a
`ProjectReference` from Design.Tests to Design.Server builds clean despite the
Blazor WASM chain (`dotnet build src/Design/Design.sln -c Release` → Build
succeeded), so the shared seam is available and the mirror is not needed.

**`Enqueue`'s null-handler guard is genuinely unpinned** — `Enqueue_NullEvent_Throws`
(`FactoryEventPhaseSchedulerTests.cs:486`) covers only the event argument. The
NF0504 location assertion at `AssemblyAttributeEmissionTests.cs:1036`
(`duplicate.Location.SourceSpan`) is the pattern NF0505's location pin follows.

**Harness duplication is three-way, not two-way.** `WaitForAsync` is byte-identical
in `FactoryEventRelayTests.cs:23` and `FactoryEventCoalescingTests.cs:31`;
`ScopesWithRelay` exists in both with *different return arities*; and
`ClientServerContainers` publishes **three** tuple orders — `Scopes()` and
`ScopesWithLogging` return `(server, client, local)` while
`Scopes(configureClient, configureServer, configureLocal)` returns
`(client, server, local)`. The two relay helpers both consume the odd one out, which
is why both carry a comment about it.

Scheduler storage as it stands: `Dictionary<DispatchPhase, List<QueuedDispatch>>`
with `queue[0]` + `RemoveAt(0)` in `TryDequeueThrough`.

---

## Test Evidence

Counts, both TFMs: unit 729 → **743** (+14), integration 591 → **595** (+4), Design
94 → **98** (+4). Baselines are PHASE-006's closing totals. Build and test logs for
both solutions in the gate record; +3 of the unit delta are gate-round-1 closures.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| 9002 pinned (message, level, count) | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_LogsTheDrainedCountAndPhase` — asserts Debug, phase, and "Drained **3**" where the requested phase held 2 (the third is the swept earlier phase, which is the count's whole point); plus `DrainAsync_NothingDrained_LogsNo9002` for the other direction | ✓ |
| 9004 pinned, discriminated from 9005 | `[unit]` | `FactoryEventsDispatcherPhaseTests.Raise_PhasedHandlerWithNoQueueInScope_DispatchesImmediatelyRatherThanVanishing` — id, Debug, event type, phase, **and `DoesNotContain(9005)`**; the 9005 pin gained the mirror `DoesNotContain(9004)`. The pre-existing integration assertion matched `9005 \|\| 9004` and could not tell them apart | ✓ |
| 9006 level and cross-phase total | `[unit]` | `FactoryEventPhaseSchedulerTests.EndEntryCallAsync_Failed_LogsTheDiscardedTotalAcrossAllPhases` (PHASE-006 already pinned the collapsed count; level and the across-queues total were unasserted) | ✓ |
| Undefined-phase silent no-op documented | `[unit]` | `FactoryEventPhaseSchedulerTests.DrainAsync_UndefinedPhaseRegistration_IsASilentNoOp` | ✓ |
| `Enqueue` null-handler guard | `[unit]` | `FactoryEventPhaseSchedulerTests.Enqueue_NullHandler_ThrowsAtEnqueueNotAtDrain` | ✓ |
| NF0505 locates at the attribute | `[unit]` | `NF0505CoalesceOnImmediateTests.NF0505_DiagnosticIsLocatedAtTheAttributeNotTheClass` — went red on first run against a wrong expectation (bracketed text vs. the `AttributeSyntax` span) and was corrected to the real location | ✓ |
| Coalesce flag runtime-inert on unqueued paths | `[unit]` | `FactoryEventsDispatcherPhaseTests.Raise_CoalescingHandlerWithNoQueueInScope_RunsPerRaiseNotOnce` (9004 path) and `…OutsideAnyFactoryCall_RunsPerRaiseNotOnce` (9005 path) | ✓ |
| Coordinator short-circuit emits 9009, and only there | `[unit]` | `FactoryEventPhaseCoordinatorTests.DrainAsync_OutsideAnyEntryCall_LogsTheShortCircuit` + `DrainAsync_InsideAnEntryCall_LogsNoShortCircuit`; both measured red under RP-1. **Gate round 1 (must-cover):** both construct the coordinator by hand, so the DI wiring was unpinned — `DrainAsync_ResolvedFromDI_OutsideAnyEntryCall_LogsTheShortCircuit` added and measured (RP-5: dropping the logger factory from the registration reds it alone, the two hand-built pins stay green) | ✓ |
| 9009 in all three log tables | `[explicit-skip: prose]` | CLAUDE-DESIGN.md, docs/factory-events.md, skill reference — no test asserts published prose | n/a |
| Design.Server composition resolves every server-only dependency | `[integration]` | `DesignServerCompositionTests.ServerComposition_ResolvesEveryServerOnlyDependency` + `…ResolvesThePhaseDispatchServices`. Fails before the fix and passes after — **measured** (RP-3: deleting one registration reds this test alone, 97/98, while all 94 pre-existing Design tests stay green) | ✓ |
| …and actually runs the domain | `[integration]` | `…RunsAPhasedFactoryOperationEndToEnd` (three phase markers + the method-done marker) and `…RunsTheOrderAggregateSavePath`. The first is the one that caught the transient-lifetime defect the resolution tests could not see | ✓ |
| `Assert.True(true)` trio asserts observable dispatch | `[integration]` | `FactoryEventHandlerTests.Raise_DispatchesToAllHandlers` (both handlers, both messages exact), `…Raise_NoHandlers_CompletesWithoutError` (nothing dispatched — the no-op observed, not merely un-thrown), `…Raise_EventWithNestedRecord_DispatchesSuccessfully` (reads through the nested record into the message) | ✓ |
| One relay-wait helper and one scopes helper | `[explicit-skip: harness refactor]` | `RelayTestHarness` replaces both copies; verified by the suite staying green and by `ClientServerContainersOrderTests.RelayTestHarness_ScopesWithRelay_ReturnsServerClientRelay` | n/a |
| Tuple-order divergence documented **and pinned** | `[integration]` | `ClientServerContainersOrderTests` (4 tests) — went beyond the bullet, which offered "aligned or documented". RP-4 measured 35 integration reds from a one-line reorder | ✓ (upgraded) |
| Full-parallel run passes with the relay tests | `[explicit-skip: run evidence]` | Recorded in the gate record alongside the `-m:1` run | n/a |
| Warn-merge holds in a production-shaped ordering | `[unit]` | `Coalesce_AbortedConsumerDrain_ThenPreDrainRaiseCollapses_TheSurvivorStillWarns9007` — consumer AfterFlush drain aborted by a handler exception, then a later identical raise; every state framework-produced. **Added at gate round 1**: the draft map had no row for this clause of Acceptance bullet 2 and no `MISSING —` row either; it was disposed of in Amendment A4's prose, which the gate then falsified | ✓ |
| Scheduler default-path dequeue decision executed | `[unit]` | Head cursor implemented (amortized O(1)); the PHASE-006 ordering/collapse/count pins are untouched and green, and `Coalesce_RaiseAfterTheDispatchWasTaken_WithWorkStillQueuedBehindIt_StartsAFreshDispatch` is new coverage the refactor required (RP-2). **Gate round 1 (must-cover + should-cover):** the refactor's own arithmetic was unpinned — `PhaseQueue.Replace`'s head offset (closed by the production-shaped merge pin above, RP-6) and `Count`'s head subtraction (closed by `EndEntryCallAsync_Failed_AfterAnAbortedDrain_DiscardCountExcludesWhatAlreadyRan`, RP-7, whose first version could not go red because a fully-drained queue resets the cursor) | ✓ |
| `Design.sln` build in verification docs | `[explicit-skip: prose]` | CLAUDE.md's Key Build Commands now builds and tests both solutions, with the false-green symptom named | n/a |

---

## Plan Amendments

**A1 — The composition test is not the reflective drift-detector the Acceptance bullet
described.** The bullet said "resolve every `[Service]` parameter type declared by
Design.Domain factory methods," which is a reflective enumeration, and the standing
rule is no reflection without approval. The two non-reflective options were a
hand-maintained mirror of `Program.cs` (green no matter what the server does — the
arc's own recurring defect, deliberately built) and a shared compiled seam. Measured
that Design.Tests can reference Design.Server despite the Blazor WASM chain, so the
seam won: `Design.Server/ServerServices.cs` holds `AddDesignServerServices`, `Program.cs`
calls it, and the test calls the same method. **Residual, stated rather than papered
over:** the test covers the services it names and the operations it runs. A new
`[Service]` type used by no covered operation would still slip. RP-3 measures what it
does catch.

**A2 — The Constraint "no production restructuring of the sample server" was relaxed,
deliberately.** Extracting seven inline `AddScoped` lines into a named method is a
restructuring, and without it there is no seam to test. `Program.cs` keeps its teaching
comments and gains one naming the seam and saying to add services there. The
alternative that honored the Constraint literally was the mirror test A1 rejects.

**A3 — The drift was four services, not two, and there was a second defect class.**
Pre-flight found `INotificationService`, `IProductReviewService`, `IPhaseAuditService`
and `ITenantTokenService` missing, not the two the row recorded. Then the end-to-end
test found the lifetime problem: `RegisterMatchingName` registers **transient**, so
closing the gap by convention alone gave the factory method, each handler, and the
assertion a different `IPhaseAuditService`. Stateful services are now registered
explicitly as scoped, with the failure mode written down where the next person will
add a service.

**A4 — RETRACTED at the gate. C5's complaint stood; the production-shaped pin was
owed and is now written.** The original amendment argued that C5's suggested remedy
(an AfterFlush trigger) fails — true, it is consumed by the same sweep it would
trigger from — and concluded that **both** merge orderings are therefore unreachable,
resting on "every drain sweeps earliest-phase-first and runs until empty." That
premise is false, and the gate caught it. The in-transaction branch of `DrainAsync`
has no catch, so a handler exception propagates and abandons the rest of the queue
(pinned since PHASE-001 by
`DrainAsync_InTransaction_PropagatesHandlerExceptionAndAbortsRemaining`); a cancelled
drain does the same. Either leaves mid-drain-stamped work pending behind a non-zero
head cursor — exactly the state the merge needs. Verified against the source before
acting on it. `Coalesce_AbortedConsumerDrain_ThenPreDrainRaiseCollapses_TheSurvivor-
StillWarns9007` is the production-shaped variant, measured red under both the deleted
merge and a mis-offset `Replace` (RP-6). Worth keeping: this is the arc's
reasoning-dressed-as-evidence failure mode occurring *inside a correction of that same
failure mode* — the first attempt replaced C5's wrong remedy with a wrong claim rather
than with a test.

**A5 — The tuple-order bullet was over-delivered, and one live mislabel was fixed.**
The bullet offered "aligned or documented." Aligning is a one-line change no compiler
checks — RP-4 measured 35 integration tests silently exercising the wrong container —
so the orders are documented at the declarations *and* pinned by
`ClientServerContainersOrderTests`, which is what would make a future alignment safe.
Separately, `FactoryEventHandlerLocalTests.CreateScopes` declared its tuple
`(server, client, local)` while returning `(client, server, local)`; every test in the
file destructured positionally and was accidentally correct, but the signature was
lying. Corrected.

**A6 — The scheduler dequeue was executed, not accepted, and the refactor needed a new
test.** Head cursor (`PhaseQueue`), amortized O(1), semantics unchanged. RP-2's first
two rounds came back green against a red prediction, which surfaced that the taken-entry
boundary had no coverage at all and that two independent guards protect it. New pin
added; the comment and the test remarks now state the measured redundancy instead of the
reasoning that turned out to be wrong.

---

## Notes

- **Branch stack:** `PHASE-007-tech-debt` is stacked on `PHASE-006-coalescing` (PR #84,
  open at branch time) — several items pin 006's code. Merge order: #84, then this plan.
- **Re-split executed at drafting:** the scheduler concurrency harness (zero coverage
  against its own shared-scope contract; both 006 reviewers recommended a dedicated
  deterministic harness, not a `Task.WhenAll` race) is **PHASE-009**, stubbed in this
  drafting commit. It is deliberately not in this plan's Scope.
- The new Debug id will be 9009 if free at pre-flight (Log.cs topped at 9008 when 006
  closed).
- PHASE-008 (generator emission hygiene) still owns everything about emitted-token
  qualification and generator-test harness defects — nothing here overlaps.
