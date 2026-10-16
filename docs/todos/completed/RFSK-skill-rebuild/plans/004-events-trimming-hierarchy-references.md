# Events, trimming, hierarchy, advanced references

**Plan #:** 004
**Date:** 2026-10-16
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-5, AC-9
**Status:** Done
**Last Updated:** 2026-10-16
**Plan-review opt-in:** No (no public API; one new Design example and its tests, no library change)
**Code-review opt-in:** Yes (four pages restate generator and runtime behaviour, and a new Design example states the inheritance rule #149 is about)
**Branch:** RFSK-004-events-trimming-hierarchy-references
**PR:** #169

---

## Scope

Rebuild `factory-events.md` (24 hand-written blocks, including a `[Remote, Create]` that persists), `trimming.md` (the `[DynamicDependency]`/`[JsonInclude]` recipe), `polymorphic-hierarchy.md` (a Neatoo `EntityBase<T>` example, a `PlanRoutingService` that does not compile, Key Rule 4 backwards, #149) and `advanced-patterns.md` so that every C# block is a `src/Design` region or a WRONG block, and the pages say what the generator does. Design has no class hierarchy, so the hierarchy page gets one: an abstract base whose operation joins each concrete type's factory, two `[Factory]` concrete types, and a domain service that picks which to load, pinned by tests on the real containers. `SKILL.md`'s quick-reference row on generic bases, which states Neatoo's CRTP need as RemoteFactory's, is corrected. Does NOT touch the six core pages (003).

---

## Intent

- A reader of the hierarchy page learns RemoteFactory's actual rule: each concrete type carries its own `[Factory]`, the base's operation methods join each one's factory, and `[Factory]` on an abstract or generic class does nothing.
- The events page shows no operation that persists inside a `[Create]`, and the trimming page prescribes no private-setter recipe and no `TrimMode`.
- No page in the skill keeps a hand-written example to copy from.

---

## Framework & Architectural Alignment

- `SKILL.md`'s model; VisionAlignment sections 0 (D3, R-K, R-M, R-N, R-O), 2 and 5 (F3, F4).
- `docs/attributes-reference.md` "Attributes and Inheritance" (EXSET-014, checked against the generator) is the rule the hierarchy page and its example follow; #141 records the misleading `Inherited = true`.
- The published `docs/factory-events.md` and `docs/trimming.md`, rebuilt by EXSET from the same regions, are the prose these pages align with; the skill pages stay self-contained.
- R-A and `.claude/rules/design-snippets.md`; R-O: the new example is in the Orders domain.

---

## Constraints & Invariants

- No existing region changes; the new ones are prefixed `skill-hierarchy-*` and each is pinned by a Design test on the real containers.
- No library or generator change. If the hierarchy example shows the runtime does not do what the docs say, that is a finding to stop on, not to work around.
- Mechanism facts survive when true: the three server invariants, the phase contract, coalescing, the relay ordering guarantee, the diagnostics and log-event tables, the trimming shape table, the DTO-walk reach, the authorization `CanSave` overloads.
- Both solutions build and test green; `mdsnippets` changes nothing else but attribution lines.

---

## Steps

1. Add the hierarchy example to Design: an abstract `Payment` base with the shared shape and an operation, two concrete `[Factory]` payments with their own data and their own load, and a domain service the client calls that decides once which concrete type to load. Register what the server needs.
2. Pin it: the inherited operation on each concrete factory, each concrete type's load across the wire, and the domain service returning each concrete type through the base interface with its own data intact.
3. Rebuild `factory-events.md` around Design's checkout, phases and relay regions; fire-and-forget becomes prose.
4. Rebuild `trimming.md` with "persistence code", no `TrimMode` setting, and `IFactorySaveMeta` as public setters.
5. Rebuild `polymorphic-hierarchy.md` around the new example and the generator's inheritance rule.
6. Rebuild `advanced-patterns.md`: embeds for the parameterized and target authorization, a troubleshooting section with no hand-written fix and no N+1.
7. Correct `SKILL.md`'s generic-base row.
8. Run `mdsnippets`, check the diff, build and test both solutions.

---

## Acceptance

- [ ] A concrete payment's factory carries the base's `[Create]`, which runs on the client `[integration]`
- [ ] Each concrete payment loads across the wire as its own type, with its own and the base's data `[integration]`
- [ ] The payment domain service, called from the client, returns each concrete type through the base interface with its data intact `[integration]`
- [ ] `polymorphic-hierarchy.md` states attribute inheritance as the generator applies it `[explicit-skip: code review against the generator]`
- [ ] Every C# block in the four pages is an embed or a short WRONG block `[explicit-skip: fence count]`
- [ ] No page prescribes the private-setter recipe, a `TrimMode`, persistence inside a `[Create]`, LazyLoad, or N+1 as a reason `[explicit-skip: grep; code review]`
- [ ] `mdsnippets` runs clean; both solutions build and test green `[explicit-skip: meta-bullet]`

---

## Current State (Pre-Flight)

Walked 2026-10-16 on the arc at `7be38fe`.

- `factory-events.md` 719 lines, 24 C# fences, none embedded. Wrong in it: the lead example's `[Remote, Create]` writes the row and flushes (D3: a Create builds; the published page now raises from an `[Insert]`); the drain example is a `[Remote, Create]` too. Regions available: `events-event`, `events-raise`, `attributes-factoryeventhandler`, `events-phase-handlers`, `events-phase-drain`, `events-coalesce`, `events-relay`, `events-relay-register`, `events-serveronly`, `events-client-raise`.
- `trimming.md` 355 lines: says "server-only business logic" (R-N: persistence code), "Confirm `TrimMode` is `full`" (the gate publishes at `partial` with `IsTrimmable`; `docs/trimming.md:153`), and prescribes `[JsonInclude]` plus `[DynamicDependency]` for private setters (R-M; a private setter is skipped by the ordinal converter). C# fences: the explicit-preservation calls, two event records, the IL2091 wrapper and its fixes, the `IFactorySaveMeta` pair and the private-setter `Employee`. Regions: `trimming-event-nested-record`, `save-ifactorysavemeta`.
- `polymorphic-hierarchy.md` 107 lines: a Neatoo `EntityBase<T>` hierarchy (`PlanBase<T>`, CRTP for Neatoo's `InitializePropertyBackingFields`), Rule 4 "`[Create]`/`[Fetch]` are NOT inherited, `[Factory]` and `[Remote]` ARE" - the reverse of the generator (`FactoryGenerator.Transform.cs:58-67`; `FactoryGenerator.cs:19-23` drops abstract and generic classes), a `PlanRoutingService` with an undeclared `_repository`, and a "Tested In" section pointing at another repository.
- `advanced-patterns.md` 579 lines: hand-written blocks at the parameterized auth (`authorization-parameterized` covers it), target auth (`authorization-target`), and a troubleshooting section with four hand-written fixes and an N+1 item.
- Design has no class hierarchy. `SKILL.md`'s quick-reference row says "`[Factory]` on each concrete type, with a CRTP constraint on the base"; CRTP is Neatoo's requirement, not RemoteFactory's.
- No runtime test pins a base class's operation joining a derived factory; EXSET-014 verified it from the generator source and a probe compile.

---

## Punchlist

- 

---

## Test Evidence

The flip step was not run (temporary production edits are refused by the permission classifier). Each new test's assertions fail if the behaviour it pins breaks: a base `[Create]` missing from a concrete factory is a compile error in the test; a base property dropped on the wire fails the `Amount` asserts; a lookup that returned the base type fails `IsAssignableFrom<ICardPayment>`; a `Create` that crossed fails the call count.

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|
| Base `[Create]` on each concrete factory, runs on the client | Required | `[integration]` | `Design.Tests.FactoryTests.PaymentHierarchyTests.ConcreteFactories_CarryTheBasesCreate_AndRunItOnTheClient` | ✓ |
| Each concrete payment loads across the wire as its own type | Required | `[integration]` | `…PaymentHierarchyTests.ConcretePayments_LoadAcrossTheWire_WithTheirOwnAndTheBasesData` | ✓ |
| The lookup returns each concrete type through `IPayment` | Required | `[integration]` | `…PaymentHierarchyTests.PaymentLookup_ReturnsEachConcreteType_ThroughTheBaseInterface` | ✓ |
| The hierarchy page states inheritance as the generator does | Required | `[explicit-skip]` | code review against `FactoryGenerator.cs:19-23` and `FactoryGenerator.Transform.cs:19-31, 58-67` | — |
| Every C# block an embed or WRONG | Required | `[explicit-skip]` | fences = embeds + WRONG: factory-events 14 = 10 + 4, trimming 3 = 2 + 1, polymorphic-hierarchy 3 = 3 + 0, advanced-patterns 18 = 18 + 0, SKILL.md 8 = 8 + 0 | ✓ |
| No private-setter recipe, `TrimMode`, Create-persistence, LazyLoad, N+1 | Required | `[explicit-skip]` | grep of the four pages: none of `JsonInclude`, `DynamicDependency`, `lazy`, `N+1`, `CRTP`, "business logic"; the one `TrimMode` line says it needs no setting; code review | — |
| mdsnippets clean; both green | Required | `[explicit-skip]` | `reviews/004-*.log`: unit 871, integration 664 (5 skipped), Design 180 (1 skipped; +3), both TFMs. No file outside the skill changed | ✓ |

---

## Gate Record

- Round 1 (2026-10-16): test review APPROVED, its theoretical item taken (test 2 asserts the call count) — `reviews/004-test-review.md`. Code review APPROVED, two callouts fixed (the same call count; the `SKILL.md` base-class row limited to abstract or generic bases) and three theoretical items taken (the NF0501 condition, the public-field fact, a code-text path made a link) — `reviews/004-code-review.md`
- No round 2: no veto — Done

---

## Plan Amendments

---

## Notes

- Loaded: `SKILL.md`, VisionAlignment sections 0, 2, 5, `docs/attributes-reference.md`, `docs/factory-events.md`, `docs/trimming.md`, `.claude/rules/design-snippets.md`.
