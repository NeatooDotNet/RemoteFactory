# TRIM-007 Plan Review — 2026-07-13

**Reviewer:** plan-reviewer agent (two-pass).
**Verdict: CONCERNS** — one veto-tier Pass B finding, addressed in the draft before implementation; five callouts folded in.

## Pass A — vs documented requirements

- **No veto.** Re-introducing generator emission reverses the v1.4.0 *mechanism* decision ("annotation supersedes per-handler emission — stronger guarantee, less generated code"), but the reversal is sanctioned: TRIM-003 proved the annotation fails the subscribe-only shape, the user chose generator emission, and 1.x release notes permit surface changes under minor bumps.
- **A1 (callout):** the doc step must rewrite the design-decision *narrative*, not just the safety claim — `CLAUDE-DESIGN.md:795` ("supersedes … less generated code", now inverted in both directions), `:799` ("preservation comes *exclusively* from DAM" + manual nested guidance, both falsified), `docs/trimming.md:307`. Pre-flight anchor extended `:783-796` → `:793-799`. → Folded into Step 6.
- **A2 (callout):** release notes (todo AC4) absent from Step 6 — now explicitly deferred to the todo-level release step. → Folded.

## Pass B — vs codebase

**High-risk item de-risked:** the generated registrar cannot be trimmed away — `NeatooFactoryRegistrarAttribute`'s ctor param and `Type` property carry `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]` (`FactoryAttributes.cs:157-168`), so the assembly-level `typeof` roots `FactoryServiceRegistrar` under `TrimMode=full`; its body then roots the `PreserveType<[DAM(All)] T>` call sites. `AllowMultiple = true` confirmed (`:154`). This is exactly how factory registrars survive today — not via DI references.

- **VETO (B1): `WalkDtoGraph` cannot be reused "unchanged" — event discovery reaches inaccessible types.** The walker has no accessibility gate; that was safe because factory-signature and entity-property inputs are inherently accessible. Event roots come from a raw declaration scan — and the repo itself declares `private` nested event records in a project that runs the generator as an analyzer (`FactoryEventCollectorTests.cs:8-9`, `FactoryEventBaseAttributeTests.cs:14-15`, incl. a two-level inheritance chain). Emitting `PreserveType<T>` for them in a separate registrar file is a build break on day one. → Draft amended: effective-accessibility gate (internal-or-public at every nesting level) on event roots; a unit test pins the gate and that generated output compiles.
- **B-callouts, all folded:** (1) predicate narrowed to records-only (classes can't inherit records) non-abstract non-generic with base list — the cheapest keying since no attribute exists to key on; (2) base match by fully-qualified metadata name, not symbol identity (the base is always a metadata symbol); (3) exclude open-generic event records; (4) deterministic ordering of collected roots before render + filter non-event results before `Collect()` (single-file output makes ordering affect bytes under `ContinuousIntegrationBuild`); (5) first per-assembly registrar has no naming precedent — sanitize assembly-derived identifiers (in-repo names all safe; leading digits/hyphens are the consumer edge); (6) cancellation-token discipline consistent with existing transforms (ignored; shallow walk).
- No existing test asserts a generated-tree count that the new file would break (assertion helpers select trees by name; `Assert.Single` tests use event-free sources).

## Disposition

| # | Finding | Disposition |
|---|---------|-------------|
| B1 (veto) | Accessibility gate required | Framework Alignment + Step 1 + Step 5 amended; test pinned |
| A1 | Design-decision narrative rewrite | Step 6 anchors extended |
| A2 | Release-note deferral unstated | Step 6 states the todo-level deferral |
| B2-B5 | Predicate/matching/ordering/naming | Steps 1-2 amended; keyboard notes |

Calibration: diagnoses adopted; remedies matched the code walk and were adopted as-is.
