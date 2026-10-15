# EXSET-003 code review — 2026-10-12

**Reviewer:** code-reviewer (tight, per-plan). **Verdict:** CLEAN. No veto-tier findings. Build 0 errors; the 2 main-solution warnings are the OrderEntry WASM notice. Tests 0 failed.

The deliverable landed as scoped and contradicts no ruling. `IProductCatalog` and `ICustomerDirectory` are named domain services and carry the D12 rule. `OrderCheckout` is a D13 context: plain instance verbs on the held order, factories by constructor (T14), one `[Remote, Fetch]`, saved through the order's root, and the test pins one call to load, none for the verbs, one to save. `OrderSummary` is an R-H read model with a `bool` Fetch (R-I). The rewritten Anti-Pattern 9 was checked against `RecordBypassConverterFactory` and `NeatooJsonSerializer` and is accurate. All five plan-review callouts landed.

| # | Callout | Affects | Disposition |
|---|---|---|---|
| 1 | `CLAUDE-DESIGN.md:387`'s decision-table row still gave the old Anti-Pattern 9 rule ("do not mix Neatoo types into record properties"); Anti-Pattern 9's last sentence had the rename's cause backwards | AC-4 · Must | punched: row states the plain-tree rule and points at Anti-Pattern 9; sentence dropped |
| 2 | `InterfaceFactoryAuthorizationTests` header said the impl is registered explicitly and server-only; `CustomerDirectory` now follows the naming convention, so `RegisterMatchingName` registers it and the test's `AddScoped` was redundant | AC-2 · Must | punched (Keith, 2026-10-13): header describes the convention; the redundant registration removed, so the tests run on convention registration alone |

After the callouts: build 0 errors; Design 120, unit 834, integration 642 + 5 skipped, both TFMs. Logs refreshed in `003-build.log`, `003-test.log`.

Theoretical (not triaged): `RenameCustomer` and `CloseAccount` throw `KeyNotFoundException` for a missing customer (R-I; the old `UpdateItem` upserted) with no test on that path; `ICustomerDirectory.ListContacts` is untested (its predecessor `GetItems` was too); `Home.razor` and the tests name `InMemoryOrderDbContext.DraftOrderId` from client code.
