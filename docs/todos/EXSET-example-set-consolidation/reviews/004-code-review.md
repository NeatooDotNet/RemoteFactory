# EXSET-004 code review — 2026-10-13

**Reviewer:** code-reviewer (per-plan, Step 5; `a80eaef` against `c071d43`). **Verdict:** CLEAN. No veto-tier findings. Build: 0 errors. The main solution's 2 warnings are the OrderEntry WASM notice, and the Design solution has 0 warnings. Tests: 0 failed (unit 835, integration 644 + 5 skipped, Design 137; both TFMs). The Design count rose by exactly the 17 tests the plan added (120 → 137), so the Design run exercised this change.

The deliverable landed as scoped and contradicts no ruling:

- **`Order.OpenDraft` (D11).** It is a class-level `[Remote, Execute]` that calls `FetchDraft`, then the local `Create`, through its injected `IOrderFactory`. `FetchDraft` is `internal` without `[Remote]`, and the generated `IOrderFactory` carries it as an `internal` member, so the client cannot call it. `OpenDraft` never inserts, and the test checks the store afterwards.
- **`Money` and `Percentage` (R-F).** Both are plain records whose get-only re-declared property validates in its initializer. That makes a `with` on the validated member a compile error, and the round trip rebuilds the record through its constructor.
- **`TaxRate` (R-F, R-I, D3).** It has a static `[Remote, Fetch]` that returns null for an unknown region. `InMemoryTaxTable` falls outside the naming convention and is registered explicitly in `ServerServices` and in the test server and local containers only.
- **`ClassExecuteDemo` (D3, D14).** `Create` is local and takes no service, and each `[Execute]` builds through the factory.
- **`CheckoutOrder`.** It is an `IFactorySaveMeta` entity. Its `[Remote, Insert]` raises the relayed event and clears `IsNew`; its `[Remote, Update]` raises the ServerOnly one.
- **`InvoiceFinalizer` and `InvoiceArchiver` (D12).** They are static `[Remote, Execute]` commands with the same shape as `QuarterClose`.
- **Ledger and grep.** The C-06 and C-14 ledger rows are recorded. A grep of `src/Design` `[Create]` bodies finds no raise or write.

**Library change.** `HandleRemoteDelegateRequest.cs:130-131` clears the scoped collector after the delegate resolves and before `BeginEntryCall`. Collection at `:210-227` reads the same instance, so a response carries only its own call's events. A failed call's events are dropped by the next call's clear. `IFactoryEventCollector` has one implementation, so the interface addition breaks nothing. Two integration tests and one unit test pin the fix.

**Sacred tests.** Every reshaped Design test is one the plan named. Each keeps its stated intent:

- The ServerOnly test now asserts that exactly the Insert's event arrived (Total 5.00, not the Update's 6.00).
- The NoOp test saves, so its call still reaches the relay.
- Every phase-order assertion is unchanged.

Nothing under `src/Tests` was modified except by addition.

| # | Callout | Affects | Disposition |
|---|---|---|---|
| 1 | `ClassFactoryWithExecute.cs:35` (class remarks, in a file this plan edited) still shows `var result = await factory.StartForPatient(patientId);`, the reference-app sample this plan's Intent replaces. No such method exists in Design, and none will exist anywhere once EXSET-012 retires the reference app. EXSET-012's plan does not name it. Confidence High. Reachable by: a reader of Design's class-level `[Execute]` file looking up the example it cites | AC-2 · Must | punch: name a method in this file (`factory.RunCommand(input)`), or point at `Order.OpenDraft` |

Theoretical (not triaged):

- `CLAUDE-DESIGN.md:246` returns `new FinalizedInvoice(id)` against Design's two-parameter record (`FactoryEventPhasesPattern.cs:264`). The block is a consumer sketch (`AppDbContext`, and a one-argument `InvoiceFinalizedEvent` that predates this plan).
- Constraint 5 said `TaxRate`'s factory would replace the `IMoneyFactory` assertion. `DesignServerCompositionTests` asserts `ITaxTable`, `IOrderFactory` and `IClassExecuteDemoFactory` instead. `ITaxRateFactory` is registered by the same `AddNeatooRemoteFactory` call that registers the asserted factories.
- Two concurrent remote calls in one shared server scope would now clear each other's collected events, where before they were cross-delivered. ASP.NET's per-request scope cannot reach this. The harness's shared scope is #128.
- Two concurrent `OpenDraft` calls for a customer with no draft each return a new order. Saving both leaves two Drafts, and `FindDraftAsync` returns the first. This is inherent in a create-or-fetch that never inserts (Keith, 2026-10-13).

Read beyond the brief:

- the generated `Design.Domain.Aggregates.OrderFactory.g.cs` (`FetchDraft` is `internal` on the interface);
- `DesignClientServerContainers.Scopes()` (fresh providers per call, so the "no draft afterwards" assertion is isolated);
- `InMemoryOrderDbContext`;
- the NF0105 static exemption in `FactoryModelBuilder.cs:195-212`;
- `004-plan-review.md`.

Named but unused: `CLAUDE-DESIGN.md` and `VisionAlignment.md` were read through the diff and targeted greps (D3, D11, D12, D14, R-F, R-I), not in full. There is no calibration or rubric file.
