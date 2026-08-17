# PHASE-005 Test Review (Step 5 Gate)

**Plan:** [../plans/005-design-docs-skill.md](../plans/005-design-docs-skill.md)
**Logs:** `005-build.log`, `005-test.log` (round 2 overwrote round 1 — noted in the log headers), `005-redproof.log`

---

## Round 1 — 2026-08-17

**Verdict: the gate can close at must-cover. Zero must-cover findings. Test Evidence map verified honest** (the reviewer re-ran the sweep greps, re-checked the counts against the logs, verified all cited test names, and checked the prose claims against shipped code — 9001–9007 rows against `Log.cs`, DI rows against `AddRemoteFactoryServices`, whitelist exception against the coordinator).

Notable verification beyond the map's own claims:

- **Log integrity checked first** (because RP-0 was a log-topology failure): Design total read 91 on both TFMs; both solutions built; build timestamps precede the `--no-build` test run — the RP-0 trap verified closed from the logs alone.
- The reviewer traced all five expected sequences under a generator phase-pass-through regression (every handler collapsing to `Immediate`): **all five go red** — the plan's charter (attribute-declared phases through the generated registrar) is pinned, not assumed.
- RP-1's inheritance argument confirmed, and strengthened: the sabotage's observed order is exactly the fail-open test's expected order, so RP-1 measures that discriminator from the other side.
- Bonus coverage worth keeping: asserting immediately after `await` with no polling pins "the remote call stays open through the entry-call completion drain."

### Findings and disposition

| # | Tier | Finding | Disposition |
|---|------|---------|-------------|
| 1 | should-cover (plan) | Discard demonstration had no positive control — `pay-flush`/`pay-commit` handlers never observed running anywhere in the Design assembly; deleting one leaves the test green | **Closed round 2:** `PaymentIntake._Record` gained a `reject` flag + success path (drain, `pay-done`); `PaymentIntake_FailedThenSuccessfulCall_SameScope_DiscardsRatherThanLeaks` asserts all four success markers |
| 2 | should-cover (plan) | "Discarded" vs. "leaked" not discriminated at this tier — no second call follows in the scope | **Closed round 2** by the same test: rejected call, then accepted call in the same server scope; the rejected trail must not grow |
| 3 | nice-to-have (plan) | 9007 claimed in Design-tier prose but unobservable (NullLogger harness) | **Taken (soften):** test doc-comment now states the emission is pinned by the unit/integration suites and only marker order is observed here |
| 4 | nice-to-have (plan) | Remote-mode "coordinator not registered" DI-table row unasserted | **Taken:** `Coordinator_NotRegisteredInTheRemoteClientContainer` (null in client, non-null in server + local) |
| 5 | nice-to-have (plan) | Finalize tests discard the returned entity | **Taken:** remote test asserts `Id`/`Total` round-tripped |
| 6 | nice-to-have (plan) | Mode parity — scenarios 2–4 Remote-only vs. the harness's three-mode remark | **Declined, recorded:** upstream both-mode pins exist; Design demonstrates each contract once, plus `Finalize` in both modes |
| 7 | should-cover (tech debt) | Design.Server registers 3 services; Design.Domain `[Service]`-injects `INotificationService` (pre-existing) and now `IPhaseAuditService` — no composition test exists | **Routed to PHASE-007** (composition test resolving every `[Service]` parameter type of Design.Domain factory methods from Design.Server's collection) |
| 8 | should-cover (tech debt) | `FactoryEventHandlerTests.cs` `Assert.True(true)` trio — now the Design tier's nominal `Immediate` pin, asserting nothing | **Routed to PHASE-007** |
| 9 | nice-to-have (tech debt) | `*.log` gitignore excludes every arc's `reviews/*.log` evidence — the files exist on one machine while todo docs cite them | **Escalated to the user** (affects the imminent close-out audit; fix is an ignore exception or `.md` extension — arc-level call) |

## Round 2 — 2026-08-17

Closures: 2 new tests (Design 91 → 93, green both TFMs, logs overwritten with round-2 runs; unit 705×2 and integration 587×2 +5 skips unchanged). Red-proof addendum records why the no-leak discriminator is argued from PHASE-003's measured production sabotage rather than re-measured, and that the positive-control half is new-code-verified.

**Round-2 reviewer verification:** *(pending — appended below when the reviewer returns)*
