# TRIM-008 — Registrar-DAM over-preservation fix

**Plan #:** 008
**Date:** 2026-08-12
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-08-12
**Plan-review opt-in:** Yes (generator emission contract change; security-relevant — this is the plan that makes a false IP-protection guarantee true; mirrors TRIM-007, whose review caught a veto that would have broken every consumer build)
**Code-review opt-in:** Yes (behavior-changing generator work)

---

## Scope

Make RemoteFactory's IP-protection guarantee true for the two shapes where it is false: `[Execute]` static factories and `[FactoryEventHandler<T>]` classes. Their generated `[assembly: NeatooFactoryRegistrar(typeof(T))]` names **the consumer's own class**, and the attribute's `DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)` makes ILLink retain every method on it — so `[Remote]` server-only bodies ship to the browser decompilable. Fix by emitting a top-level generated holder type that forwards to the existing registrar, and pointing the attribute at the holder.

Also in scope, because they are inseparable from the fix: correcting the ~40 documentation anchors the defect falsified (the docs are only false while the code is broken, so they must not be separately mergeable); giving the trimming harness the targets it lacks, including closing plan-review finding **B9** in full; and re-deriving the CI absence gate, whose current exemption is justified by a diagnosis this arc disproved.

**Not** in scope: narrowing the DAM itself; the two latent generator bugs surfaced during design; replacing the reflective lookup with `[ModuleInitializer]` registration; cutting the v1.7.0 release (this plan unblocks it, the arc's close-out step cuts it).

---

## Intent

- The guarantee RemoteFactory advertises becomes the guarantee it delivers, for every factory shape rather than two of four.
- The documentation stops asserting something demonstrably false about what ships to a user's browser — including the distributable skill, which travels without the repo.
- The trimming harness gains the ability to *see* this class of defect at all. Today it cannot: the relay-handler leg has no target whatsoever, and the static leg has targets but no assertion.
- The registrar attribute gains a written contract — "the `Type` must be a generated registrar type, never a consumer type" — so the next person cannot reintroduce this by pointing it somewhere convenient.

---

## Framework & Architectural Alignment

- The remedy mirrors TRIM-007's shape (`EventPreservationRenderer.cs:71,83-85`): a top-level `internal static` holder whose only member is `FactoryServiceRegistrar`, so the DAM blast radius is one method. Precisely what CI verifies at HEAD is that such a holder **registers correctly under trimming** — it does *not* verify DAM narrowing, because that holder has one method and nothing to narrow. The shapes are analogous, not identical: TRIM-007 emits one holder **per assembly** in an assembly-derived namespace; TRIM-008 emits one **per type** in the user's namespace.
- It deliberately does **not** use the nested-type variant floated at TRIM-005's review, which depends on an ILLink property (DAM not extending to nested types) that this codebase has never verified. TRIM-005 was abandoned for building on an unverified inherited diagnosis; that lesson applies here.
- The holder **forwards** rather than hosts, because `[Execute]` methods are `private static` by the repo's own design convention (`src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs:362-368`) and the registrar body calls them. This also means no generated member is removed or renamed.
- No model, builder, or transform changes — the incremental-cache surface stays at zero delta, honoring what TRIM-006 just repaired.
- Absence assertions live in CI, not in the harness process: string absence is only observable from outside the process (`plans/004-trimming-harness-ci-gate.md:119`).

---

## Constraints & Invariants

- `FactoryAttributes.cs:148-170` is unchanged apart from XML docs. Narrowing the DAM would silently break consumers referencing a prebuilt library compiled by an older generator — its registrar would no longer be rooted and its factories would fail to register on a trimmed client, with no diagnostic.
- No generated member is removed, renamed, or has its accessibility changed. This is `fix:`, not `feat!:`.
- Class-factory, interface-factory, and event-preservation emission is byte-identical. Static and relay emission changes by design.
- `RelayHandlerModel` and `TypeInfo` gain no fields; `IncrementalCacheTests` stays green.
- Existing tests are not gutted. `AssemblyAttributeEmissionTests.cs:142` pins the defective behavior and is in-scope to retarget, but its intent — "the registrar attribute is emitted and names the correct type" — must be preserved.
- **The holder's method must remain exactly `FactoryServiceRegistrar`.** `AddRemoteFactoryServices.cs:168-170` looks it up by that literal string and calls `method?.Invoke`, so any rename silently stops registration for that type with no diagnostic and no exception.
- The holder name must **not** be a prefix-extension of the user's type. `{TypeName}NeatooFactoryRegistrar` leaves `global::Ns.{TypeName}` a substring of the holder's FQN, which makes the "must not name the consumer's type" assertion a false red and an unclosed `Contains` a false green. Hence the `NeatooFactoryRegistrar_{TypeName}` prefix form, and a distinct prefix per leg so the two never collide on a class carrying both attributes.
- **Design-surface disposition for AC6:** accept-with-reason, mirroring deferred item 11. Over-preservation is no more observable in untrimmed Design tests than preservation was — `RemoteFactory.TrimmingTests` is the verification surface. `CLAUDE-DESIGN.md` is still corrected as documentation (Step 9); what is accepted is the absence of a *demonstrating example*.
- Full suite green on net9.0 and net10.0 across both solutions, plus the trimmed harness exiting 0.

---

## Steps

**Order matters and is not negotiable: every harness target must exist and be probed BEFORE the leg that fixes it.** A marker measured for the first time after the fix cannot distinguish "the fix removed it" from "it was never rooted" — plan review B1, and the third appearance of this arc's signature failure.

1. Give the harness the targets it lacks, before any generator change: a `[FactoryEventHandler<T>]` class calling a server-only service (that leg has no coverage at all), plus interface-factory and Save/Can* targets, closing B9. Each server-only dependency gets a leg-distinct marker so a failure names the culprit.
2. Probe publish-trimmed with the new targets and the generator **unfixed**, recording which markers are present. This is the baseline the later "absent" claims are measured against; without it those claims are unfalsifiable.
3. Emit the forwarding holder for the static-factory leg and retarget its assembly attribute, changing nothing else in that renderer's output. *(Already done and measured — the pre-existing `_DoWork`/`_ProcessRecord` targets gave this leg a valid baseline.)*
4. Do the same for the relay-handler leg, with a distinct holder name shape, and fix its long-standing missing `global::` prefix at the same time.
5. Give the registrar attribute a written contract in its XML doc: the `Type` must be a generated registrar type, never a consumer type, because the DAM retains every method on it, bodies included.
6. Pin the new emission for both legs — including a regression assertion that the attribute does **not** name the user's type, the check whose absence let this ship — and add the relay-handler emission tests that have never existed. Relay tests must assert the output compilation is error-free, not merely that strings appear: relay output bypasses `NormalizeWhitespace` and has no parse-error signal.
7. Re-probe publish-trimmed and compare against step 2, so every marker has a present-before / absent-after pair. Confirm registration still works on both legs — absence assertions pass more easily when registration is silently dead.
8. Tighten the CI gate to what steps 2 and 7 proved — per-pattern messages, a durable positive control so a missing or renamed DLL cannot pass silently, and removal of the `(?<!I)` exemption only if measurement supports it.
9. Correct the documentation in four buckets: the false claims; the four falsified-TRIM-005 artifacts; the places where the affected shapes are documented not at all; and — the bucket the first draft missed — statements that are **accurate today and become inaccurate after the fix** (`docs/trimming.md:222`, `CLAUDE-DESIGN.md:756`, both of which describe "preserve all methods on the referenced type"). `src/Design/` is an explicit target surface here, not an afterthought: repo `CLAUDE.md` makes it the requirements source of truth.
10. Reconcile the container: close the deferred items this plan retires, record the ones it does not.

---

## Acceptance

- [ ] `[Remote]` method bodies on an `[Execute]` static factory are absent from a publish-trimmed client assembly. `[trimmed-harness]`
- [ ] `[Remote]`/handler method bodies on a `[FactoryEventHandler<T>]` class are absent from a publish-trimmed client assembly. `[trimmed-harness]`
- [ ] Both legs' assembly attributes name a generated holder, and provably do not name the consumer's type. `[unit]`
- [ ] The relay-handler assembly attribute is `global::`-qualified. `[unit]`
- [ ] Class-factory, interface-factory, and event-preservation emission is byte-identical before and after. Evidence is an **expected-delta-set equality** check — enumerate the files expected to change, then assert the actual delta set *equals* it. `[explicit-skip: one-off recursive emission diff — `Generated/` is gitignored so `git status` cannot detect drift. Unlike TRIM-006's zero-delta diff, this one has a nonzero expected delta, so "inspect the diff" would not discriminate]`
- [ ] The CI gate fails when either leg regresses, names which leg, and carries a **durable positive control** so a missing or renamed artifact cannot pass silently (confirmed real today: `grep -aq` on an absent path returns non-zero, the `if` is false, and the step prints success). `[explicit-skip: gate is shell in YAML, not unit-testable]`
- [ ] Factory registration still works through the retargeted holder on both legs. `[trimmed-harness]` for static (`TrimmingTests/Program.cs` resolves the delegate and the harness exits non-zero on failure); `[integration]` for relay, which structurally cannot be covered in the trimmed harness because `RelayHandlerRenderer.cs:82` guards every `RegisterHandler` behind `IsServerRuntime` — name the untrimmed suite that covers it. Needed because `AddRemoteFactoryServices.cs:170` uses `method?.Invoke`, so a misnamed holder method fails **silently**, and every other bullet here is an absence assertion that passes *more* easily when registration is dead.
- [ ] Finding B9 is closed: each new harness target's marker is **present pre-fix and absent post-fix** — not merely that the target files exist. `[trimmed-harness]`
- [ ] No documentation in the repo — including the distributable skill — asserts the IP guarantee for a shape that does not deliver it. `[explicit-skip: documentation]`
- [ ] Full solution build/test green (net9.0 + net10.0), both solutions, harness exits 0. `[explicit-skip: build/test gates]`

---

## Current State (Pre-Flight)

Walked 2026-08-12 on branch `TRIM` (`25ac975`). Diagnosis verified at the keyboard against a published trimmed artifact — the explicit lesson from TRIM-005.

- **The defect.** `StaticFactoryRenderer.cs:41` and `RelayHandlerRenderer.cs:32` point the registrar attribute at the user's own class; `ClassFactoryRenderer.cs:54` and `InterfaceFactoryRenderer.cs:48` correctly point at the generated `{X}Factory`. The DAM (`FactoryAttributes.cs:148-170`, on **both** ctor param and `Type` property) then retains every method on the target. `attr.Type` is used for exactly one `GetMethod` and nothing else (`AddRemoteFactoryServices.cs:160-173`).
- **Why the two legs differ.** Class/interface factories get a separate generated type to host `FactoryServiceRegistrar`. Static factories (`StaticFactoryRenderer.cs:53,65,88-90`) and relay handlers (`RelayHandlerRenderer.cs:42,45`) instead re-open the **user's own partial** — there was no other type to name. Introduced in v0.21.2 (2026-03-08); its plan records the intent but not this consequence.
- **Trimmed-artifact probe:** `_DoWork`, `_ProcessRecord`, `IServerOnlyRepository`, `DoServerWork` **present**; `ServerOnlyDirect`, `ServerOnlyHelper`, `ServerOnlyRepository` absent.
- **The remedy cannot be "move the registrar."** `[Execute]` methods are `private static` by convention (`AllPatterns.cs:362-368`; `TrimTestCommands.cs:26,39`) and the registrar body calls them (`StaticFactoryRenderer.cs:193`), so a sibling holder that *hosts* the registrar is CS0122. The relay leg applies no accessibility filter (`FactoryGenerator.RelayHandler.cs:74-114`) despite its doc comment claiming "non-private", so private handlers compile today. Design work surfaced this as a **second, unstated reason** the TRIM-005 advisory may have reached for a nested type; the only written record of that advisory gives a DAM-breadth rationale, and no claim is made here about what its author actually intended. Forwarding satisfies the accessibility constraint without depending on unverified ILLink behavior — that is the load-bearing point, independent of anyone's intent.
- **Harness cannot see either defect.** `[Execute]` targets exist but nothing asserts their absence; `IServerOnlyRepository` is explicitly exempted by the CI grep. There is **no `[FactoryEventHandler<T>]` class in the harness at all** — that leg is unexercised, not merely unasserted.
- **The CI exemption is unjustified.** `.github/workflows/build.yml:109-112` cites "guarded-dead `LocalCreate` bodies the trimmer keeps — tracked as TRIM-005", a cause this arc disproved. Three other artifacts repeat or contradict it: `TrimmingTests/README.md:30-31`, `TrimTestCommands.cs:35`, and `ServerOnlyTypes.cs:4-6` — the last says the interface *should be absent*, contradicting the other three. The deferred-work table listed only three of the four.
- **Doc inventory: ~40 anchors.** Verified do-NOT-touch (class/interface-scoped, still true): `docs/trimming.md:25,27,36` (under "### Class Factories — Conditional Guards" at `:19` and the interface half of `:33`), `skills/.../class-factory.md:318,333-334` (under "## Internal Visibility for Child Entities"), `advanced-patterns.md:227`. The deferred table over-listed the skill entries; abandoned TRIM-005 targeted `:36` while missing `:35`, the actual falsehood.
- **Latent bugs found during design, recorded not fixed:** nested `[Factory]` static classes and nested handler classes emit uncompilable code (simple-name FQN plus a namespace-scope re-declaration of the user's class); a class carrying both `[Factory]`(static) and `[FactoryEventHandler<T>]` emits duplicate registrars (CS0111).
- **`NormalizeWhitespace` has no error signal** (`FactoryRenderer.cs:100-108`, `:55-58`): malformed emission yields mangled output, not an exception. Relay output bypasses normalization entirely (`FactoryGenerator.cs:104`).

---

## Test Evidence

Filled after implementation, before the Step 5 gate.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| | | | |

---

## Plan Amendments

**2026-08-12 — plan review (CONCERNS, 5 veto).** Full record: [`../reviews/008-plan-review.md`](../reviews/008-plan-review.md). Split explicitly **not** recommended, so the scope decisions stand. Changes made before implementation continued:

- **Step order reversed (B1).** Harness targets and their pre-fix probe now come *first*. The original order measured brand-new markers only after the fix, where "absent" is satisfied equally by "the fix worked" and "it was never rooted" — this arc's signature failure for the third time. The static leg is unaffected: its baseline used pre-existing targets.
- **Registration-works Acceptance bullet added (B2).** `AddRemoteFactoryServices.cs:170` uses `method?.Invoke`, so a misnamed holder method kills registration **silently** — and every other bullet is an absence assertion that passes *more* easily when registration is dead. The relay leg structurally cannot be covered in the trimmed harness (its registrations are `IsServerRuntime`-guarded), so the untrimmed suite must be named. Constraint added pinning the method name.
- **Byte-identity evidence respecified (B3).** Expected-delta-set *equality*, not diff inspection — unlike TRIM-006's zero-delta case, this diff is nonzero by design and would not discriminate.
- **Fourth doc bucket added, `src/Design/` named (A1).** Statements accurate today that become inaccurate after the fix — `docs/trimming.md:222`, `CLAUDE-DESIGN.md:756` — fit none of the original three buckets.
- **AC6 Design disposition recorded (A2)** as accept-with-reason, mirroring deferred item 11.
- **Holder renamed to a prefix form (B4)** — `NeatooFactoryRegistrar_{TypeName}`. The suffix form left `global::Ns.{TypeName}` a substring of the holder's FQN, making the "must not name the consumer's type" assertion a false red. Caught in code already written.
- **Three narrative claims tightened (B7, B8):** what CI actually verifies about the TRIM-007 shape, and removal of an assertion about the TRIM-005 advisory author's intent — stated as fact in the same paragraph that indicts the arc for exactly that habit.
- Relay emission tests must assert an error-free output compilation, not just string containment (B5) — relay output bypasses `NormalizeWhitespace` and has no parse-error signal.

Deferred: enumerating the ~40 doc anchors in the plan before the doc pass (A4) — will land as the Step 9 working list rather than bloating this section.

---

## Abandonment / Retirement Reason

<!-- Only if Status becomes Abandoned or Retired. -->

---

## Notes

- Folded into the arc by user decision 2026-08-12, reversing the 2026-08-11 decision to fix it in plan mode outside any todo. The close-out audit had flagged that it was release-blocking with no durable home; this gives it one.
- Adds **AC6** to the todo. This plan is what reopens AC4/AC5 and unblocks zTreatment PCB-003.
- Retires deferred items 1, 6 (B9), 7, and 8 when it lands. Items 3, 5, 9, 10 stay open; items 11, 12, 13 stay accepted-with-reason.
- The verification design pass for this plan died on an API error and returned nothing; the verification approach here is unreviewed by a second party. The plan-review gate is the compensating control.
