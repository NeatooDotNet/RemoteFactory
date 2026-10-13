# EXSET-004 test review — 2026-10-13

## Round 1 — CLEAN

**Reviewer:** test-reviewer (tight), against `a80eaef` diffed from `c071d43`. Logs green: main build 0 errors and 2 warnings (the OrderEntry WASM native-reference notice); Design build 0 errors, 0 warnings; unit 835, integration 644 + 5 pre-existing skips, Design 137, each on both TFMs. Counts went 834 → 835, 642 → 644 and 120 → 137, which matches the tests added, so none was deleted.

Every Test Evidence row cites tests that exist, assert the bullet's behavior, and run at the declared tier. The create-or-fetch and `TaxRate` tests run on the client container and assert `RemoteCallCount == 1`. The Money round trip goes through the client: `Order.Total` is computed on the client from each line's `UnitPrice`. The map is honest: row 4 states that "built through the factory" cannot be observed from a test.

Sacred-test check clean, each against the plan's Constraints:
- **Relay trio.** The Insert test, renamed from `…InRemoteCreate…`, keeps the same `OrderId` and `Total` assertions. The ServerOnly test waits for the Insert's event, then updates with a different total. `Assert.Single` with `Total == 5.00` would catch the Update's event if it leaked. The NoOp test still makes a remote call (the Save).
- **Phase tests and the composition test.** Every phase-order assertion is unchanged. `Finalize_Remote` still proves the result crossed the boundary, now through `FinalizedInvoice`'s `Id` and `Total`.
- **`Create_StillWorks_AlongsideExecute`.** It is stronger: it now asserts `Id` too.
- **`DesignServerCompositionTests`.** `IMoneyFactory` went with the factory. `ITaxTable`, `IOrderFactory` and `IClassExecuteDemoFactory` are each method-injected by Design.Domain, which fits the test's stated criterion.

Library change: both new integration tests pin the `Clear()` at the start of each call. `SequentialCalls…` would see the first call's event twice without it, and `FailedCall…` would see the failed call's event. `Relay` runs once per successful call even with an empty batch (`MakeRemoteDelegateRequest.DispatchRelay`), so their waits on `InvocationCount` are sound. The unit test pins `Clear()` itself.

Tech-debt:
- `Relay_ServerOnlyFlag_EventNotRelayedToConsumer` waits a fixed 50 ms before its "no second event" check (inherited). `InMemoryAggregatorRelay` has no invocation count to wait on, as the library tests do.
- `ValueObject_SurvivesSerializationRoundTrip` uses only USD, which is also the default currency, so a `Currency` dropped on the wire would still pass. That the amount crosses is pinned.
- The plan's Constraint says "`TaxRate`'s factory takes [`IMoneyFactory`'s] place". The test asserts `ITaxTable` instead, which is the right assertion; the Constraint's wording is what is off.
- The explicit-skip row says no `[Create]` in this plan's files takes a service. It does not mention `Order.Create`'s `[Service] IOrderLineListFactory`, a client-side child factory that is legal under D3 and D14.

Theoretical: none.
