# PHASE-003 Code Review (per-plan, findings-only) — 2026-08-14

**Plan:** [../plans/003-aftercommit-entry-call-drain.md](../plans/003-aftercommit-entry-call-drain.md)
**Verdict shape:** 1 veto (V1), 11 callouts (C1–C11). Build/test/red-proof logs verified
by the reviewer (2666 passing, red-proofs genuine, backward-compat claim confirmed).

## Veto

**V1 — Interface leg's wrapper split moved previously-eliminable bodies into a
DAM-rooted, unguarded core.** The assembly attribute pointed at `{Impl}Factory` (hosting
every `Local*`), whose `[DynamicallyAccessedMembers(PublicMethods | NonPublicMethods)]`
roots the new private cores with their bodies — the configuration TRIM-009 *measured* as
insufficient on the class leg. The plan's "TRIM item 20 status unchanged" was honest
about ignorance but silent about direction.
**Disposition: FIXED** — the interface leg now emits a single-method registrar holder
(`NeatooInterfaceFactoryRegistrar_` prefix, distinct per the item-15 convention) and the
assembly attribute points at it, aligning all three legs on the measured-good shape.
`InterfaceFactory_EmitsAssemblyAttribute` amended to pin the holder + the
does-not-name-the-factory regression assertion (intent preserved: the attribute names
the correct type; the correct type changed). Elimination on this leg remains UNVERIFIED
(TRIM item 20 — the fixture is still blocked by item 19), but the shape is no longer the
measured-bad one. Recorded in the Discovery Log.

## Callouts and dispositions

| # | Finding | Disposition |
|---|---|---|
| C1 | `EndEntryCallAsync` check-then-decrement spanned two lock acquisitions — an interleaving could reach depth 0 having neither drained nor cleared | **Fixed** — single lock block |
| C2 | A handler's OCE at the entry drain fails an already-succeeded call (chartered by the todo's AC, but in tension with the no-token rationale) | **Recorded**; the "swallow OCE at a post-completion drain?" question handed to PHASE-004 (its stub carries it) |
| C3 | Sync block-drain can deadlock under a captured SynchronizationContext | **Fixed (docs)** — caveat added to `FactoryEntryCall.Run` XML; PHASE-005 carries the guidance |
| C4 | 9006 asserted a cause (`did not complete successfully`) the shared `ClearAtExit` cannot guarantee (also fires on post-OCE success-path cleanup) | **Fixed** — renamed `FactoryEventPhaseDiscardedAtExit`, message + CLAUDE-DESIGN row reworded |
| C5 | Every factory call now pays closure + delegate + state machine; value-object `Create` pays a null `GetService` per call client-side; the only perf suite is skipped | **Recorded** (plan Notes); revisit on consumer report |
| C6 | `ref`/`in`/`out`/ref-struct factory parameters would now fail to compile in generated code (lambda capture); no diagnostic | **Recorded** (plan Notes) — if the shape turns out to compile today, a generator diagnostic gets its own Draft row |
| C7 | `Local{X}Core` name-collision surface widened (a user `FetchCore` factory method beside `Fetch`) | **Recorded** (plan Notes) |
| C8 | Static-leg delegates resolved from the root provider now touch a scoped service (`ValidateScopes` throws) | **Recorded** (plan Notes) — delegates are meant to be scope-resolved |
| C9 | `FactoryEntryCall` emitted unqualified (CS0104 risk against consumer types) | **Fixed** — `global::`-qualified in all three renderers; emission pins updated |
| C10 | (a) Cross-plan outcomes unrecorded for RFEF and TRIM; (b) four stale "inside guard" comments | **Fixed** — two Discovery Log entries added; comments corrected |
| C11 | `Can*` authorization probes are now entry calls (harmless empty drain today; under RFEF a read-only probe would open a transaction) | **Recorded** in the RFEF-substrate Discovery Log entry |

Checked-and-clear list from the reviewer retained in full in the agent transcript;
highlights: AspForbid reaches `End(false)` before the success-shaped return; handlers
invoked outside the lock (no re-entrant deadlock); both choke-point registrations pass
the scoped provider; every Test Evidence citation exists at its declared tier (19/19
spot-checked).

**Post-fix suites:** unit 668×2, integration 579×2 +5 skipped, Design 86×2 — 0 failures
(logs regenerated).
