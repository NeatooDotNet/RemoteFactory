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

- The remedy mirrors TRIM-007's proven shape (`EventPreservationRenderer.cs:71,83-85`): a top-level `internal static` holder whose only member is `FactoryServiceRegistrar`, so the DAM blast radius is one method. That shape is verified in CI at HEAD.
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
- Full suite green on net9.0 and net10.0 across both solutions, plus the trimmed harness exiting 0.

---

## Steps

1. Emit the forwarding holder for the static-factory leg and retarget its assembly attribute, changing nothing else in that renderer's output.
2. Do the same for the relay-handler leg, and fix its long-standing missing `global::` prefix at the same time.
3. Give the registrar attribute a written contract in its XML doc: the `Type` must be a generated registrar type, never a consumer type, because the DAM retains every method on it, bodies included.
4. Pin the new emission for both legs — including a regression assertion that the attribute does **not** name the user's type, the check whose absence let this ship — and add the relay-handler emission tests that have never existed.
5. Give the harness the targets it lacks: a `[FactoryEventHandler<T>]` class calling a server-only service (that leg has no coverage at all), plus interface-factory and Save/Can* targets, closing B9. Each server-only dependency gets a leg-distinct marker so a failure names the culprit.
6. Measure before asserting: publish-trimmed and observe which server-only names actually disappear. The prediction that `IServerOnlyRepository` and `DoServerWork` vanish is a hypothesis to test, not a fact to encode.
7. Tighten the CI gate to what step 6 proved — per-pattern messages, a positive control so a missing DLL cannot pass silently, and removal of the `(?<!I)` exemption only if measurement supports it.
8. Correct the documentation: the false claims, the four falsified-TRIM-005 artifacts, and the five places where the affected shapes are documented not at all.
9. Reconcile the container: close the deferred items this plan retires, record the ones it does not.

---

## Acceptance

- [ ] `[Remote]` method bodies on an `[Execute]` static factory are absent from a publish-trimmed client assembly. `[trimmed-harness]`
- [ ] `[Remote]`/handler method bodies on a `[FactoryEventHandler<T>]` class are absent from a publish-trimmed client assembly. `[trimmed-harness]`
- [ ] Both legs' assembly attributes name a generated holder, and provably do not name the consumer's type. `[unit]`
- [ ] The relay-handler assembly attribute is `global::`-qualified. `[unit]`
- [ ] Class-factory, interface-factory, and event-preservation emission is byte-identical before and after. `[explicit-skip: one-off recursive emission diff, per TRIM-006 precedent — `Generated/` is gitignored so `git status` cannot detect drift]`
- [ ] The CI gate fails when either leg regresses, names which leg, and cannot pass against a missing or untrimmed artifact. `[explicit-skip: CI gate; non-vacuity proven by keyboard red-before-green]`
- [ ] Finding B9 is closed: the harness carries relay-handler, interface-factory, and Save/Can* targets. `[trimmed-harness]`
- [ ] No documentation in the repo — including the distributable skill — asserts the IP guarantee for a shape that does not deliver it. `[explicit-skip: documentation]`
- [ ] Full solution build/test green (net9.0 + net10.0), both solutions, harness exits 0. `[explicit-skip: build/test gates]`

---

## Current State (Pre-Flight)

Walked 2026-08-12 on branch `TRIM` (`25ac975`). Diagnosis verified at the keyboard against a published trimmed artifact — the explicit lesson from TRIM-005.

- **The defect.** `StaticFactoryRenderer.cs:41` and `RelayHandlerRenderer.cs:32` point the registrar attribute at the user's own class; `ClassFactoryRenderer.cs:54` and `InterfaceFactoryRenderer.cs:48` correctly point at the generated `{X}Factory`. The DAM (`FactoryAttributes.cs:148-170`, on **both** ctor param and `Type` property) then retains every method on the target. `attr.Type` is used for exactly one `GetMethod` and nothing else (`AddRemoteFactoryServices.cs:160-173`).
- **Why the two legs differ.** Class/interface factories get a separate generated type to host `FactoryServiceRegistrar`. Static factories (`StaticFactoryRenderer.cs:53,65,88-90`) and relay handlers (`RelayHandlerRenderer.cs:42,45`) instead re-open the **user's own partial** — there was no other type to name. Introduced in v0.21.2 (2026-03-08); its plan records the intent but not this consequence.
- **Trimmed-artifact probe:** `_DoWork`, `_ProcessRecord`, `IServerOnlyRepository`, `DoServerWork` **present**; `ServerOnlyDirect`, `ServerOnlyHelper`, `ServerOnlyRepository` absent.
- **The remedy cannot be "move the registrar."** `[Execute]` methods are `private static` by convention (`AllPatterns.cs:362-368`; `TrimTestCommands.cs:26,39`) and the registrar body calls them (`StaticFactoryRenderer.cs:193`), so a sibling holder is CS0122. The relay leg applies no accessibility filter (`FactoryGenerator.RelayHandler.cs:74-114`) despite its doc comment claiming "non-private", so private handlers compile today. **This is why the TRIM-005 advisory proposed a nested type — it was solving accessibility, not DAM breadth. That was never written down.** Forwarding solves both without depending on unverified ILLink behavior.
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

(None yet.)

---

## Abandonment / Retirement Reason

<!-- Only if Status becomes Abandoned or Retired. -->

---

## Notes

- Folded into the arc by user decision 2026-08-12, reversing the 2026-08-11 decision to fix it in plan mode outside any todo. The close-out audit had flagged that it was release-blocking with no durable home; this gives it one.
- Adds **AC6** to the todo. This plan is what reopens AC4/AC5 and unblocks zTreatment PCB-003.
- Retires deferred items 1, 6 (B9), 7, and 8 when it lands. Items 3, 5, 9, 10 stay open; items 11, 12, 13 stay accepted-with-reason.
- The verification design pass for this plan died on an API error and returned nothing; the verification approach here is unreviewed by a second party. The plan-review gate is the compensating control.
