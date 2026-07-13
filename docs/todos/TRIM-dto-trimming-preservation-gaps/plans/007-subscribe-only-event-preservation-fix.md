# TRIM-007 — Subscribe-only event preservation fix (generator emission)

**Plan #:** 007
**Date:** 2026-07-13
**Related Todo:** [../todo.md](../todo.md)
**Status:** Draft
**Last Updated:** 2026-07-13
**Plan-review opt-in:** Yes (new incremental-generator pipeline branch; generator emission contract change; corrects documented behavior that currently overpromises)
**Code-review opt-in:** Yes (behavior-changing generator work)

---

## Scope

Close the gap TRIM-003 proved: a `FactoryEventBase`-derived record whose only client-side reference is a generic `Subscribe<TEvent>` call site loses its constructor under `PublishTrimmed=true` — inherited `[DynamicallyAccessedMembers]` does not flow to derived types under ILLink. Per the user's direction (2026-07-07), the fix is **generator emission**: a new incremental pipeline branch discovers every concrete `FactoryEventBase` descendant declared in a compilation and emits preservation for it — restoring the v1.4.0 "every descendant is automatically safe" promise for real, with zero consumer action. Each discovered event root goes through the shared `WalkDtoGraph` (TRIM-001's bucketed walk), so the event itself AND its nested property graph bucket by ctor shape into `PreserveType<T>`/`Register<T>` — which also automates the previously-manual "nested reference types in event records" case. Emission lands in a generated per-assembly event-preservation registrar discovered by the existing assembly-level `[NeatooFactoryRegistrar]` mechanism. Owns the doc corrections TRIM-003 surfaced (docs currently overpromise automatic trimming safety; `FactoryEventBase.cs`'s own comment makes the wrong `Inherited = true` claim; stale per-handler-`PreserveType` prose in the Design projects — migrated from TRIM-003 Steps 3–4). Exit condition: TRIM-003's red harness check goes green in the pure, unannotated consumer shape. Does NOT change the runtime relay/registry/deserializer path or `FactoryEventBase` itself (the annotations stay — they're harmless and self-documenting), and does NOT touch producer-side `Raise<T>` guidance.

---

## Intent

- Any record inheriting `FactoryEventBase` becomes genuinely trimming-safe by declaration — no handler attribute, no consumer annotation, no LinkerConfig entry — making the todo's Acceptance Criterion 3 true and letting zTreatment (PCB-003) delete its event-record entries.
- Nested reference types inside event records stop being a documented manual case — the same automatic story as factory-signature and entity-property DTOs.
- The documentation stops overpromising: the trimming story for events becomes "generator-emitted preservation," stated accurately in `docs/trimming.md`, CLAUDE-DESIGN.md, and the Design project comments.

---

## Framework & Architectural Alignment

- Fourth incremental pipeline branch alongside class/interface/relay-handler branches (`FactoryGenerator.cs`) — `CreateSyntaxProvider` (no attribute to key on; descendants carry nothing directly) with the cheapest available syntactic predicate: **records only** (every `FactoryEventBase` descendant is a record — classes cannot inherit records), non-abstract, non-generic, with a base list (plan review B-callout 1/3). Semantic base-chain transform matches the base by fully-qualified metadata name (the base is always a metadata symbol from the referenced assembly — plan review B-callout 2), value-equatable output, non-event results filtered **before** `Collect()`, and collected roots ordered deterministically before render (plan review B-callout 4).
- Discovery/bucketing reuses `DtoTypeWalker.WalkDtoGraph` with one addition the event path uniquely requires (plan review VETO): an **accessibility gate** — event roots are discovered by raw declaration scan (unlike the signature/entity walks, whose inputs are inherently accessible), so roots whose effective accessibility is not internal-or-public within the assembly (private/protected/file-scoped nested records) are skipped; the generated registrar could not legally reference them. In-repo breakers otherwise: `FactoryEventCollectorTests.cs:8-9`, `FactoryEventBaseAttributeTests.cs:14-15` (private nested event records in a project that runs the generator as an analyzer). Roots bucket by ctor shape (typical positional event records → PreserveType bucket).
- The generated registrar rides the existing zero-reflection-for-consumers discovery: assembly-level `[NeatooFactoryRegistrar(typeof(...))]` + static `FactoryServiceRegistrar(IServiceCollection, NeatooFactory)`, invoked by `RegisterFactories` at `AddNeatooRemoteFactory` time — same lifecycle as every factory registrar, unguarded (client and server), idempotent emissions.
- Trimmed verification per the TRIM-004 harness contract; the failing TRIM-003 check is the acceptance pin.

---

## Constraints & Invariants

- No changes to `src/RemoteFactory` runtime types (relay, registry, deserializer, `FactoryEventBase`, its annotations).
- Existing factory/interface/relay-handler pipelines and their emissions are untouched; the new branch adds a file, never modifies theirs.
- Incremental-cache discipline: the new transform output is value-equatable (`EquatableArray`/records); an unrelated edit must not re-render the event registrar.
- No emission when a compilation declares no concrete `FactoryEventBase` descendants (no empty registrar files).
- Duplicate preservation across registrars stays idempotent (`TryAdd` / no-op `PreserveType`); a type reachable via factory signatures, entity properties, AND event graphs emits validly from each site.
- Abstract descendants (consumer intermediate event bases) get no emission themselves, but their concrete descendants do, with inherited properties walked.
- Full suite green both TFMs; CI trimming gate green — including TRIM-003's check in the pure consumer shape.

---

## Steps

1. Add the event-discovery pipeline branch: syntactic predicate (concrete, non-generic record declarations with a base list), semantic transform (base-chain FQN match against `Neatoo.RemoteFactory.FactoryEventBase`, effective-accessibility gate), per-event `WalkDtoGraph` bucketing (root + nested property graph), value-equatable output filtered before `Collect()`.
2. Render the per-assembly event-preservation registrar: assembly-level `[NeatooFactoryRegistrar]` + static `FactoryServiceRegistrar` emitting the two bucket call kinds; unique hint/namespace derived from the assembly; no output when no events.
3. Extend the TRIM-003 harness event with a nested positional-record property (never constructed) so the nested walk is pinned end-to-end; the subscribe-only check must go green unmodified in its pure consumer shape.
4. Keyboard negative control: disable the new pipeline branch, confirm the check reverts to red, restore (TRIM-001/002 precedent).
5. Unit tests in the DtoDiscovery suite: registrar emitted with correct buckets for a subscribe-only event; nested record/DTO properties bucketed; abstract intermediate skipped while its concrete descendant is walked with inherited properties; **private nested event records skipped (accessibility gate) with the generated output still compiling**; generic event records skipped; cross-event dedupe within the registrar; no registrar when no events declared.
6. Docs to shipped behavior — including the design-decision *narrative*, not just the safety claim (plan review A-callout 1): `docs/trimming.md` event section (generator emission story; correct the DAM-inheritance claim; the `:307` "supersedes … less generated code" rationale is now inverted and must be rewritten; "Nested Reference Types in Event Records" becomes automatic; verification pointer → the subscribe-only check), `FactoryEventBase.cs` doc comment, CLAUDE-DESIGN.md `:793-799` (supersession rationale at `:795`, the "exclusively from DAM" nested-walking paragraph at `:799`) + FAQ row, and the stale `FactoryEventHandlerPattern.cs` / `FactoryEventHandlerTests.cs` comments (migrated from TRIM-003). Release notes (todo Acceptance Criterion 4) are explicitly deferred to the todo-level release step spanning all TRIM plans — not owed by this plan (plan review A-callout 2).

---

## Acceptance

- [ ] The generator emits a per-assembly event-preservation registrar whose buckets cover every concrete `FactoryEventBase` descendant and its nested property graph; compilations with no events emit nothing. `[unit]`
- [ ] TRIM-003's subscribe-only harness check passes on the publish-trimmed client in the pure consumer shape (no consumer annotation), including a nested record property on the event. `[trimmed-harness]`
- [ ] The check's sensitivity is re-proven by a keyboard negative control (pipeline disabled → red). `[explicit-skip: one-off keyboard verification, per precedent]`
- [ ] Docs and Design comments describe the generator-emission story accurately; no surviving overpromise about inherited DAM. `[explicit-skip: doc delta, reviewed at code review]`
- [ ] Full solution build/test green (net9.0 + net10.0); CI trimming gate green. `[explicit-skip: build/test/CI gates]`

---

## Current State (Pre-Flight)

Walked 2026-07-07/13 on branch `TRIM-003-verify-event-preservation` (6c892e9):

- The failing pin: `EventSubscribeOnlySmokeTest` — trimmed red (`NotSupportedException` on `TrimSubscribeOnlyEvent` ctor), untrimmed green, DAM-annotated-Subscribe green (triplet in TRIM-003's Test Evidence). `SubscribingRelay.Subscribe<TEvent>` carries the KNOWN-GAP comment and deliberately no annotation.
- Pipeline seams: `FactoryGenerator.cs:16-108` — three branches, all `ForAttributeWithMetadataName`; the new branch needs `CreateSyntaxProvider` (descendants carry no attribute; `[FactoryEvent]` sits on the base and Roslyn symbol `GetAttributes()` does not surface inherited attributes). `Collect()` + single `RegisterSourceOutput` for the per-compilation file.
- Registrar discovery mechanism: `AddRemoteFactoryServices.RegisterFactories` (`:160-173`) reads assembly-level `NeatooFactoryRegistrarAttribute`s and reflection-invokes static `FactoryServiceRegistrar(IServiceCollection, NeatooFactory)` (NonPublic|Public) on each `attr.Type` — the generated registrar rides this as-is. Attribute defined at `FactoryAttributes.cs:155` (verify `AllowMultiple` at the keyboard — factories already emit one per type, so it must be multiple).
- Walk reuse: `DtoTypeWalker.WalkDtoGraph` buckets roots by ctor shape — a positional event record roots into the PreserveType bucket naturally; `IsDtoStructureCandidate` skips abstract types (consumer intermediate event bases) and `[Factory]` types; `WalkProperties` includes the inherited chain, so a concrete descendant of an abstract event base still walks the base's properties.
- Emission call shapes already exist (`Register<T>(() => new T())` / `PreserveType<T>()`); `DtoConstructorRegistry` is `public` in `Neatoo.RemoteFactory.Internal` — generated factory files already emit `using Neatoo.RemoteFactory.Internal;`.
- Hint-name conventions: factory files use `{SafeHintName}Factory.g.cs`; the event registrar needs an assembly-derived unique hint (sanitize invalid identifier chars) — keyboard detail.
- Unit-test home: `RemoteFactory.UnitTests/FactoryGenerator/DtoDiscovery/` — `RunGenerator` + per-tree `FactoryTree` helper pattern (TRIM-002); the event registrar's tree is selected by its hint name.
- Docs to correct (anchors): `docs/trimming.md` "Factory Event Type Preservation" (~280-347, incl. "How RemoteFactory Handles It", "What You Need to Know", "Nested Reference Types in Event Records"); `FactoryEventBase.cs:5-17` comment; CLAUDE-DESIGN.md `:783-796` (event preservation paragraphs) + FAQ; `Design.Domain/FactoryPatterns/FactoryEventHandlerPattern.cs:119-137`; `Design.Tests/FactoryTests/FactoryEventHandlerTests.cs:55-61`.
- TrimmingTests declares two `FactoryEventBase` descendants today (`TrimTestRelayEvent`, `TrimSubscribeOnlyEvent`) — the new registrar will cover both; `EventRelaySmokeTest` stays untouched (its direct-construction shape remains valid as a round-trip test).

---

## Test Evidence

Filled after implementation, before the Step 5 gate.

| Acceptance bullet (short) | Tier declared | Test method | Tier confirmed |
|---|---|---|---|
| — | — | — | — |

---

## Plan Amendments

(None yet.)

---

## Notes

- The consumer-annotation pattern (DAM on a generic subscribe method) remains valid and documented for generic *passthroughs* (the existing IL2091 guidance) — this plan just stops it being *required* for preservation.
- Branch topology: implemented on `TRIM-007-event-preservation-emission` off `TRIM-003-verify-event-preservation`; one PR carries both plans (003's red verification + 007's fix that turns it green).
