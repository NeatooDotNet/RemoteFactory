# TRIM-008 — Registrar-DAM over-preservation fix

**Plan #:** 008
**Date:** 2026-08-12
**Related Todo:** [../todo.md](../todo.md)
**Status:** Done
**Last Updated:** 2026-08-13
**Plan-review opt-in:** Yes (generator emission contract change; security-relevant — this is the plan that makes a false IP-protection guarantee true; mirrors TRIM-007, whose review caught a veto that would have broken every consumer build)
**Code-review opt-in:** Yes (behavior-changing generator work)

---

## Scope

Make RemoteFactory's IP-protection guarantee true for the two shapes where it is false: `[Execute]` static factories and `[FactoryEventHandler<T>]` classes. Their generated `[assembly: NeatooFactoryRegistrar(typeof(T))]` names **the consumer's own class**, and the attribute's `DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)` makes ILLink retain every method on it — so `[Remote]` server-only bodies ship to the browser decompilable. Fix by emitting a top-level generated holder type that forwards to the existing registrar, and pointing the attribute at the holder.

Also in scope, because they are inseparable from the fix: correcting the ~40 documentation anchors the defect falsified (the docs are only false while the code is broken, so they must not be separately mergeable); giving the trimming harness the targets it lacks, including closing plan-review finding **B9** in full; and re-deriving the CI absence gate, whose current exemption is justified by a diagnosis this arc disproved.

**Not** in scope: narrowing the DAM itself; the two latent generator bugs surfaced during design; replacing the reflective lookup with `[ModuleInitializer]` registration; cutting the v1.7.0 release.

**Scope correction, 2026-08-13.** The line above originally read that this plan unblocks the release. It no longer does on its own. This plan's own pre-fix probe found a **third** broken shape — class factories with async write operations, a different mechanism — now carried as [TRIM-009](./009-async-local-method-body-retention.md). AC6 is held whole across both plans (user decision), so v1.7.0 unblocks when TRIM-009 also lands. TRIM-008 delivers two of AC6's three shapes and must not be closed out as delivering AC6.

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
- [x] Class-factory, interface-factory, and event-preservation emission is byte-identical before and after. Evidence is an **expected-delta-set equality** check — enumerate the files expected to change, then assert the actual delta set *equals* it. `[explicit-skip: one-off recursive emission diff — `Generated/` is gitignored so `git status` cannot detect drift. Unlike TRIM-006's zero-delta diff, this one has a nonzero expected delta, so "inspect the diff" would not discriminate]`
  **Measured 2026-08-13.** Full emission tree captured twice over both solutions with `--no-incremental`, once with the generator reverted to `25ac975` (pre-TRIM-008) and once at HEAD, using an identical collection procedure. **732 files emitted; 40 changed (20 relay-handler, 20 static-factory); 692 byte-identical.** No class-factory or interface-factory output appears in the delta. Discharged one level stronger than the bullet asked: rather than only comparing the delta *set* against an expectation, every changed line in every changed file was matched against the two permitted shapes (registrar-attribute retarget, holder block) — **0 lines fell outside them**. Non-vacuity: the delta list held 40 entries, so the "zero unexpected" result came from inspecting real diffs rather than an empty loop.
- [ ] The CI gate fails when either leg regresses, names which leg, and carries a **durable positive control** so a missing or renamed artifact cannot pass silently (confirmed real today: `grep -aq` on an absent path returns non-zero, the `if` is false, and the step prints success). `[explicit-skip: gate is shell in YAML, not unit-testable]`
- [ ] Factory registration still works through the retargeted holder on both legs. `[trimmed-harness]` for static (`TrimmingTests/Program.cs` resolves the delegate and the harness exits non-zero on failure); `[integration]` for relay, which structurally cannot be covered in the trimmed harness because `RelayHandlerRenderer.cs:82` guards every `RegisterHandler` behind `IsServerRuntime` — name the untrimmed suite that covers it. Needed because `AddRemoteFactoryServices.cs:170` uses `method?.Invoke`, so a misnamed holder method fails **silently**, and every other bullet here is an absence assertion that passes *more* easily when registration is dead.
- [ ] Finding B9 is closed: every new harness target has a **recorded pre-fix measurement**, and each leg's post-fix result is stated against it — not merely that the target files exist. Measured 2026-08-13, so the required outcome now differs per leg and is fixed in advance rather than read off the result: **relay** present pre-fix → must be absent post-fix; **interface-factory** absent pre-fix → must stay absent (no regression); **Save/Can\*** present pre-fix → **expected to stay present**, because its cause is [TRIM-009](./009-async-local-method-body-retention.md) and not this plan's defect. A post-fix Save/Can\* absence would falsify TRIM-009's diagnosis and must reopen it, not be recorded as a win. `[trimmed-harness]`
- [ ] No documentation in the repo — including the distributable skill — asserts the IP guarantee for **the two shapes this plan delivers** (`[Execute]` static factories, `[FactoryEventHandler<T>]` classes). `[explicit-skip: documentation]`
  **Scoped 2026-08-13 (code review V3).** The bullet was written unqualified and was therefore false at HEAD by this plan's own inventory: eight class-factory anchors remain accurate-only-after-TRIM-009, deliberately deferred rather than edited-then-reverted. Left absolute, the close-out audit could only tick it dishonestly or veto it. The residue is carried in [`../reviews/008-doc-anchor-inventory.md`](../reviews/008-doc-anchor-inventory.md) as a release-blocking table, and the release is already gated on TRIM-009 by AC6 and deferred item 2.
- [ ] Full solution build/test green (net9.0 + net10.0), both solutions, harness exits 0. `[explicit-skip: build/test gates]`

---

## Current State (Pre-Flight)

Walked 2026-08-12 on branch `TRIM` (`25ac975`). Diagnosis verified at the keyboard against a published trimmed artifact — the explicit lesson from TRIM-005.

- **The defect.** `StaticFactoryRenderer.cs:41` and `RelayHandlerRenderer.cs:32` point the registrar attribute at the user's own class; `ClassFactoryRenderer.cs:54` and `InterfaceFactoryRenderer.cs:48` correctly point at the generated `{X}Factory`. The DAM (`FactoryAttributes.cs:148-170`, on **both** ctor param and `Type` property) then retains every method on the target. `attr.Type` is used for exactly one `GetMethod` and nothing else (`AddRemoteFactoryServices.cs:160-173`).
- **Why the two legs differ.** Class/interface factories get a separate generated type to host `FactoryServiceRegistrar`. Static factories (`StaticFactoryRenderer.cs:53,65,88-90`) and relay handlers (`RelayHandlerRenderer.cs:42,45`) instead re-open the **user's own partial** — there was no other type to name. Introduced in v0.21.2 (2026-03-08); its plan records the intent but not this consequence.
- **Trimmed-artifact probe (as of the 2026-08-12 walk, before any fix):** `_DoWork`, `_ProcessRecord`, `IServerOnlyRepository`, `DoServerWork` **present**; `ServerOnlyDirect`, `ServerOnlyHelper`, `ServerOnlyRepository` absent. *(Superseded by measurement, kept as the pre-flight baseline: after Step 3 all four of the "present" names went absent. The 2026-08-13 probe is the current picture.)*
- **The remedy cannot be "move the registrar."** `[Execute]` methods are `private static` by convention (`AllPatterns.cs:362-368`; `TrimTestCommands.cs:26,39`) and the registrar body calls them (`StaticFactoryRenderer.cs:193`), so a sibling holder that *hosts* the registrar is CS0122. The relay leg applies no accessibility filter (`FactoryGenerator.RelayHandler.cs:74-114`) despite its doc comment claiming "non-private", so private handlers compile today. Design work surfaced this as a **second, unstated reason** the TRIM-005 advisory may have reached for a nested type; the only written record of that advisory gives a DAM-breadth rationale, and no claim is made here about what its author actually intended. Forwarding satisfies the accessibility constraint without depending on unverified ILLink behavior — that is the load-bearing point, independent of anyone's intent.
- **Harness cannot see either defect.** `[Execute]` targets exist but nothing asserts their absence; `IServerOnlyRepository` is explicitly exempted by the CI grep. There is **no `[FactoryEventHandler<T>]` class in the harness at all** — that leg is unexercised, not merely unasserted.
- **The CI exemption is unjustified.** `.github/workflows/build.yml:109-112` cites "guarded-dead `LocalCreate` bodies the trimmer keeps — tracked as TRIM-005", a cause this arc disproved. Three other artifacts repeat or contradict it: `TrimmingTests/README.md:30-31`, `TrimTestCommands.cs:35`, and `ServerOnlyTypes.cs:4-6` — the last says the interface *should be absent*, contradicting the other three. The deferred-work table listed only three of the four.
- **Doc inventory: ~40 anchors.** Verified do-NOT-touch (class/interface-scoped, still true): `docs/trimming.md:25,27,36` (under "### Class Factories — Conditional Guards" at `:19` and the interface half of `:33`), `skills/.../class-factory.md:318,333-334` (under "## Internal Visibility for Child Entities"), `advanced-patterns.md:227`. The deferred table over-listed the skill entries; abandoned TRIM-005 targeted `:36` while missing `:35`, the actual falsehood.
- **Latent bugs found during design, recorded not fixed:** nested `[Factory]` static classes and nested handler classes emit uncompilable code (simple-name FQN plus a namespace-scope re-declaration of the user's class); a class carrying both `[Factory]`(static) and `[FactoryEventHandler<T>]` emits duplicate registrars (CS0111).
- **`NormalizeWhitespace` has no error signal** (`FactoryRenderer.cs:100-108`, `:55-58`): malformed emission yields mangled output, not an exception. Relay output bypasses normalization entirely (`FactoryGenerator.cs:104`).

---

## Test Evidence

Filled 2026-08-13, after implementation, before the gates.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| `[Execute]` bodies absent from trimmed client | `[trimmed-harness]` | `verify-trimmed.sh` "static factory" block. **Discriminators [D]:** `_DoWork`, `_ProcessRecord`, `IServerOnlyRepository`, `DoServerWork` — all four recorded PRESENT in the pre-fix walk (this plan's Current State) and absent in the current probe. `IServerOnlyRepository`'s flip is the evidence that justifies deleting the `(?<!I)` exemption, so it is emphatically a discriminator. **New baseline [N]:** `_DoAsyncWork`, `StaticAsyncBody_MARKER` — no pre-fix measurement (the target did not exist), but on the consumer's class, so genuine evidence the fix generalizes to async `[Execute]`. **No-regression [R]:** `ServerOnlyRepository_MARKER`, `ServerOnlyDirect`, `ServerOnlyHelper`, `ServerOnlyHelper_MARKER`. Corrected twice: 2026-08-13 (N5) to introduce the distinction, then again (third-pass N1/Q2) because that first correction put `DoServerWork` and `IServerOnlyRepository` in `[R]`, contradicting both the script and this plan's own Current State | `[trimmed-harness]` |
| Handler bodies absent from trimmed client | `[trimmed-harness]` | `verify-trimmed.sh` — **four discriminators** `RelayLegHandlerBody`, `IRelayLegPort`, `RelayLegInvoke`, `RelayLegHandlerBody_MARKER`, each **present pre-fix and absent post-fix** (`probe-prefix.txt` / `probe-postfix.txt`). `RelayLegBackend` is a **no-regression** check, not fix evidence: `probe-prefix.txt` recorded it absent pre-fix too (nothing ever rooted it — it is DI-registered only behind the harness's own guard). Corrected 2026-08-13; the row previously folded it into the present→absent set | `[trimmed-harness]` |
| Both attributes name a generated holder, not the consumer type | `[unit]` | `AssemblyAttributeEmissionTests.StaticFactory_EmitsAssemblyAttribute`, `.StaticFactory_AssemblyAttribute_DoesNotNameConsumerType`, `.RelayHandler_EmitsAssemblyAttribute`, `.RelayHandler_AssemblyAttribute_DoesNotNameConsumerType` | `[unit]` |
| Relay attribute is `global::`-qualified | `[unit]` | `AssemblyAttributeEmissionTests.RelayHandler_AssemblyAttribute_DoesNotNameConsumerType` — asserts absence of `NeatooFactoryRegistrar(typeof(TestNamespace.` (unqualified form) | `[unit]` |
| Class/interface/event-preservation emission byte-identical | `[explicit-skip: one-off emission diff]` | Discharged by measurement instead of skipped — 732 emitted / 40 changed / 692 identical, every changed line matched against two permitted shapes, 0 outside. See the Acceptance bullet | **Upgraded** from explicit-skip to measured |
| CI gate fails per-leg, durable positive control | `[explicit-skip: shell in YAML]` | **Skip retired.** Gate moved to `verify-trimmed.sh` and exercised against archived artifacts: fails naming the relay leg (`prefix-relayleg.dll`), fails naming the static leg (`baseline-prefix.dll`), fails on a missing path, passes on the fixed artifact | **Upgraded** from explicit-skip to executed |
| Registration still works through the holder | `[trimmed-harness]` static / `[integration]` relay | Static + both interface variants + save: `TrimmingTests/Program.cs` resolves `TrimTestCommands.DoWork`, `TrimTestCommands.DoAsyncWork`, `ITrimIfaceQueryFactory`, `ITrimAsyncIfaceQueryFactory`, `ITrimSaveTargetFactory`; harness exits non-zero on failure, observed exit 0. Relay (structurally impossible in the trimmed harness — registrations are `IsServerRuntime`-guarded): `RemoteFactory.IntegrationTests` `TestTargets/Events/FactoryEventHandlerTargets.cs` family, **561 passing per TFM**. Corrected 2026-08-13 (N5): count was stale, and the async interface variant had no resolution check — its type name survives via DAM regardless of whether registration works, so asserting it PRESENT in the gate without resolving it was the B2 hazard applied to a positive control. **Known gap (N4):** the second handler class `TrimAsyncRelayHandlers` has no live registration signal — the integration suite covers its own handlers, not this harness class, and the control now names each holder in full, so the holder TYPE is covered — but DAM roots that type regardless, so what remains uncovered is whether the handler REGISTRATION fires, which a client-side harness structurally cannot signal | `[trimmed-harness]` + `[integration]` |
| B9 closed with pre-fix measurement per leg | `[trimmed-harness]` | the pre-fix probe vs the current post-fix probe. Relay present→absent; interface absent→absent; Save/Can\* present→present as TRIM-009 predicts. Probe self-validated against the untrimmed assembly — **53/53 markers PRESENT** (the current self-check) — before any result was used. Corrected 2026-08-13 (N5): the row previously cited superseded probe filenames and marker counts; artifacts are now cited by role, see the manifest below. **Caveat on the interface rows:** their markers sit behind an interface hop, so absent-both-times is a no-regression result and cannot report on body elimination at all — see `ITrimAsyncIfaceQuery` remarks and deferred item 19 | `[trimmed-harness]` |
| No doc asserts the guarantee for a shape that lacks it | `[explicit-skip: documentation]` | `reviews/008-doc-anchor-inventory.md`; residual class-factory anchors deferred to TRIM-009, enumerated and release-blocking | `[explicit-skip: documentation]` |
| Full build/test green both solutions | `[explicit-skip: build/test gates]` | Gate run, **nothing filtered**: main **611+611 unit / 561+561 integration, 0 failed** (5 skips per TFM, all pre-existing — 3 `ShowcasePerformanceTests`, 2 `RelayTimingTests` from deferred item 10); Design 86+86; harness exit 0. Corrected 2026-08-13: this row previously read "554+554 … `FactoryEventRelayTests` excluded", which understated the evidence and left a "we filtered the suite" claim behind. That family flakes under parallel load and was separately proven pre-existing (identical failures with this branch's changes stashed), but it was **not** excluded from the gate run and passed in it | `[explicit-skip: build/test gates]` |

**Not covered, deliberately:** no test asserts that a *consumer* pointing `[NeatooFactoryRegistrar]` at their own type is rejected — the contract added in Step 5 is documentation, not a diagnostic. A generator change that repoints the attribute at a user class is caught by the two `DoesNotNameConsumerType` tests; a hand-written attribute in consumer code is not caught at all. Recorded rather than silently absent.

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

**2026-08-13 — Steps 1–2 executed; the pre-fix probe found a third broken leg.** Full record in the todo's Discovery Log entry of the same date.

- **Steps 1–2 complete.** Three harness legs added (`TrimRelayHandlers`, `ITrimIfaceQuery`, `TrimSaveTarget`) with per-leg server-only ports, then probed publish-trimmed with the relay leg unfixed. Harness exits 0; both new positive controls (`ITrimIfaceQueryFactory`, `ITrimSaveTargetFactory`) resolve, so the new legs are live rather than inert fixtures.
- **Relay baseline captured**: `RelayLegHandlerBody`, `IRelayLegPort`, `RelayLegInvoke`, `RelayLegHandlerBody_MARKER` all present pre-fix. Step 4's "absent" claim is now falsifiable.
- **Interface-factory leg measured clean** — all five markers absent. The arc's long-standing structural claim about this leg is now an empirical result.
- **Save/Can\* leg measured BROKEN, and it is not this plan's defect.** Async generated `Local*` methods retain their server-only bodies; sync ones in the same assembly do not. Rooted by the class-factory registrar's own *unguarded* delegate-registration closures, so the forwarding-holder fix does not reach it. Carried as **deferred item 18**.
- **Consequence for AC6 — decided.** AC6 says "every factory shape". This plan can deliver `[Execute]` and `[FactoryEventHandler<T>]`; it cannot deliver the class-factory shape, which measurement moved from "believed safe" to "known broken". **User decision 2026-08-13: AC6 is held whole, not narrowed.** Item 18 becomes [TRIM-009](./009-async-local-method-body-retention.md) and the release waits for both plans. TRIM-008's own scope is unchanged — it continues at Step 4 — but it no longer closes AC6 by itself, and its close-out must not claim to.
- **Acceptance bullet for B9 rewritten** to fix each leg's required post-fix outcome in advance. Left as "present pre-fix → absent post-fix" it would have been *failed* by the interface leg (correctly absent both times) and would have quietly accepted whatever the Save/Can\* leg did.

---

## Abandonment / Retirement Reason

<!-- Only if Status becomes Abandoned or Retired. -->

---

## Notes

- Folded into the arc by user decision 2026-08-12, reversing the 2026-08-11 decision to fix it in plan mode outside any todo. The close-out audit had flagged that it was release-blocking with no durable home; this gives it one.
- Adds **AC6** to the todo. This plan is what reopens AC4/AC5 and unblocks zTreatment PCB-003.
- Retires deferred items 1, 6 (B9), 7, and 8 when it lands. Items 3, 5, 9, 10 stay open; items 11, 12, 13 stay accepted-with-reason.
- The verification design pass for this plan died on an API error and returned nothing; the verification approach here is unreviewed by a second party. The plan-review gate is the compensating control.

**2026-08-13 — Steps 5 and 8 executed.**

- **Step 5 (attribute contract).** `NeatooFactoryRegistrarAttribute` now carries the rule that was only ever implicit: the `Type` must be a generated registrar type, never a consumer's class, because the DAM retains every method on it, bodies included. Documented on the type, the ctor parameter, and the `Type` property, with the reason narrating the actual defect rather than stating a rule with no motive. Also records the two things a future editor would otherwise re-litigate: why the DAM is deliberately not narrowed (no sub-method granularity; narrowing unroots prebuilt-library registrars), and that the method name is looked up as a literal and invoked null-conditionally, so a rename fails silently.
- **Step 8 (CI gate) — the exemption is gone.** `(?<!I)ServerOnlyRepository` is deleted, and `IServerOnlyRepository` is asserted absent like any other marker. Measurement supports it: the interface survived only because the static-factory registrar attribute named the consumer's class, so DAM retained a body that referenced it. That was the *actual* cause; the exemption's cited rationale ("guarded-dead `LocalCreate` bodies", TRIM-005) was the disproven one.
- **The gate moved out of YAML into `verify-trimmed.sh`.** Not cosmetic. The acceptance bullet had this tier'd `[explicit-skip: gate is shell in YAML, not unit-testable]`, and that skip is what let the previous gate ship with an exemption nobody could test and a `grep -aq` that printed success on a missing path. As a script it was run against archived artifacts and **observed failing** four ways: pre-fix relay DLL → fails naming the relay leg; pre-static-fix DLL → fails naming the static leg; missing path → fails loudly; fixed DLL → passes. The skip is retired; the bullet is now `[trimmed-harness]`-adjacent evidence rather than an untested assertion.
- **The gate searches two encodings.** Metadata names are UTF-8; string literals from method bodies are UTF-16LE. `grep -a` on raw bytes can never match a UTF-16 literal, so a gate written the obvious way would report every literal marker absent and pass unconditionally. `tr -d '\000'` gives a second searchable view. Three positive controls cover both extraction paths independently, so a failure in either is caught instead of silently disabling half the gate.
- **Save/Can\* markers are asserted PRESENT, on purpose.** Omitting them would leave the gate quietly passing after TRIM-009 lands. Asserted, CI fails the moment the leak is fixed, and the failure message says to promote them into the absence block. A pending marker, not an endorsement.
- **`.gitattributes` added** pinning `*.sh` to LF, so a CRLF checkout cannot break the gate with a "bad interpreter" error that reads like a missing shell.

Remaining: Steps 9 (docs) and 10 (container reconciliation), then the test-review and code-review gates.

**2026-08-13 — Steps 9 and 10 executed. Implementation complete; gates remain.**

- **Anchor inventory rebuilt from the files, not the plan** (A4), recorded at [`../reviews/008-doc-anchor-inventory.md`](../reviews/008-doc-anchor-inventory.md). It falsified this plan's own "verified do-NOT-touch" list: the class-factory anchors there are false today because of TRIM-009. Deferred to TRIM-009 with the eight anchors enumerated and release-blocking, rather than edited and reverted.
- **Buckets 1–4 worked.** Static/relay per-shape claims corrected and the holder mechanism documented (`docs/trimming.md`, `CLAUDE-DESIGN.md` including its attribute-target table, which had documented the defect as intended design). Bucket 2's four artifacts closed — `ServerOnlyTypes.cs` needed no change, having been right all along. Bucket 3's five silences filled (`ExecuteAttribute` and `FactoryEventHandlerAttribute<T>` XML docs, `attributes-reference.md`, the skill's static-factory and trimming pages, `AllPatterns.cs`). Bucket 4's two anchors rewritten so "preserve all methods on the referenced type" now says which type that is and why it is never the consumer's.
- **A verification trap was documented for users, not just for us.** `docs/trimming.md` and the skill both told readers to `grep -a` the published DLL for server-only names. That works for type and method names (UTF-8 metadata) and silently fails for string literals (UTF-16), which is how our own probe nearly reported a broken leg as clean. Both pages now carry the `tr -d '\000'` step and the instruction to prove the check against an untrimmed build first.
- **Design-surface disposition honored.** `CLAUDE-DESIGN.md` and `AllPatterns.cs` are corrected as documentation; the absence of a *demonstrating Design test* is accepted with reason, recorded in `AllPatterns.cs` itself so the next reader sees why rather than assuming an oversight.
- **Step 10 container reconciliation:** deferred items 6 and 7 CLOSED, item 8 partially closed with its premise corrected. Items 1 and 2 remain open pending merge and TRIM-009.
- **Verification:** main solution 611+611 unit / 561+561 integration green on net9.0 and net10.0, nothing filtered; Design solution 86+86 green; trimmed harness exits 0; `verify-trimmed.sh` passes on the fixed artifact and fails on both archived pre-fix artifacts. `FactoryEventRelayTests` was separately proven a pre-existing flake (identical failures with this branch's changes stashed) but was NOT excluded from the gate runs and passes in them.

**Gates: both closed.** [`../reviews/008-test-review.md`](../reviews/008-test-review.md) and [`../reviews/008-code-review.md`](../reviews/008-code-review.md), three passes each, all findings closed or accepted with reason. Evidence archived in [`../reviews/008-evidence/`](../reviews/008-evidence/). This plan delivers two of AC6's three shapes and must not be closed out as delivering AC6.

**2026-08-13 — gate findings addressed.** `test-reviewer` (4 must-cover, 7 should-cover) and `code-reviewer` (3 veto, 8 callout) both ran; every checkable finding was independently re-verified at the keyboard before being accepted, and all of them held.

**The measurement that changed a diagnosis.** Closing P2/S7 — adding async targets to three legs — produced a result that contradicts TRIM-009's stated cause. Async `[Execute]`, async relay handlers, and an async interface-factory method **all trim clean**; only the class-factory write path leaks. It narrows rather than falsifies: none of the three clean shapes is the leaking shape (static/relay use a *wrapping* guard inside a non-async registrar, and the interface leg reaches its implementation through an interface so no body is statically reachable). The leak needs an early-throw guard **and** `async` **and** a direct call on a concrete type. TRIM-009's stub now carries the five-shape table and an explicit instruction not to treat "async is the cause" as settled.

**Veto-tier:**
- **V1 — the rewritten gate had silently dropped a marker the old gate carried.** `grep -F "IServerOnlyRepository"` cannot match the bare implementation name that `(?<!I)ServerOnlyRepository` was there to catch, so `ServerOnlyRepository` and its body literal were asserted nowhere. A coverage regression inside the artifact this plan holds up as its Step-8 deliverable, while narrating the change as strictly strengthening. `ServerOnlyRepository_MARKER` restored, measured PRESENT untrimmed first.
- **V2/P4 — `RelayLegBackend` was reported as a present→absent pair; the probe recorded it absent both times.** Four relay markers are genuine discriminators, not five. The gate now labels every marker `[D]` discriminator or `[R]` no-regression, and the Test Evidence row is corrected. The blanket "measured PRESENT before, ABSENT after" header was wrong for 8 of 16 markers — this arc's signature failure, re-entering a CI artifact written to prevent it.
- **V3 — two forward-looking claims added by this plan were unregistered.** Both said, in effect, that naming a *generated* type is what makes a leg safe. It isn't: `{X}Factory` is generated and hosts every `Local*` method. Reworded to state the real property (a holder has exactly one method). The unqualified "no documentation asserts the guarantee for a shape that lacks it" bullet is now scoped to the two shapes this plan delivers.

**Must-cover / callout:**
- **P1/C2 — the holder's method name was not pinned**, though the plan's Constraint and the test's own remarks claimed it was. The user's partial emits a byte-identical signature, so `Assert.Contains` passed regardless. Both tests now anchor the signature to the holder's class declaration; proven by renaming *only* the holder's method in the generator and observing RED, then restoring.
- **P3 — `ServerOnlyHelper` had zero references**, so its absence was unconditional and it could never go red, while sitting in the gate as evidence. Wired into `ServerOnlyRepository.DoServerWork`, so the transitive-removal property its comment claimed is now actually tested.
- **S8** — `TrimTestEntity` gets its own `IClassLegPort`; a class-factory leak no longer reports as a static-factory failure, which matters immediately because TRIM-009 changes that leg.
- **S5** — the relay compile test passed on zero generated trees; now asserts the tree exists. **S6** — private-handler relay fixture added (the accessibility case that is the reason the holder forwards rather than hosts). **S9** — the both-attributes claim, asserted in three comments and tested nowhere, is now pinned as CS0111-without-CS0101.
- **C4/S10** — integration counts corrected to 561+561 with nothing filtered.
- **C5** — all four gate runs captured: the passing gate run, `gate-red-prefix-relayleg.txt`, `gate-red-baseline-prefix.txt`, `gate-red-missing.txt`.

**Accepted with reason:**
- **C3** — the gate has only been exercised on `win-x64`; CI publishes `linux-x64`. The two net-new CI-only failure surfaces are the positive controls and the assert-PRESENT block. Accepted: the merge run is the first linux exercise, and a failure there is informative rather than silent. Recorded so a red merge run is not misread as a TRIM-009 falsification.
- **C6** — `BuildReferences()` widening means fixtures whose `Task<T>`/`IServiceCollection`/`ILogger<T>` were previously *error types* now bind, so the generator's semantic model sees different symbols suite-wide. Strengthening, not weakening (it is why `StaticFactorySource` needed its usings), but every pre-TRIM-008 green was obtained under a narrower compilation.
- **C7** — nested types, global namespace, and generic containing types were **traced** in the generator rather than assumed: name derivation is unchanged in kind, the holder binds exactly where the old attribute target did, and no new error is added to the already-broken shapes (deferred items 14/15).
- **Structural corroboration for byte-identity:** `git diff --name-only 25ac975..HEAD -- src/Generator/` returns exactly the two renderers. Because `Generated/` is gitignored, the 732/40/692 measurement is not re-derivable by a later reader; this one-command check is, and it makes the constraint structural rather than observational.
