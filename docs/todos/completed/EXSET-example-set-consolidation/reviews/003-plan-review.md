# EXSET-003 plan review — 2026-10-12

**Reviewer:** plan-reviewer (tight). **Verdict:** APPROVED. No veto-tier findings.

Pass A: the context matches D13 (plain instance verbs, root `Save` reassigned, no instance `[Execute]`); the read model matches R-H, R-I and §3; the renames match D12/#112; constructor-injected factories match T14; deferrals trace to 004, 009, 012 and Todo 2.

Pass B reality check: the context follows `CtorInjectedEntity`'s path (a required-argument constructor skips ordinal, forces a transient registration, and is built through DI on the client); an `IOrder` property crosses through the interface converter; method- and constructor-injected `IOrderFactory` on one class is legal and the nested server Fetch runs locally; record lines on the read model are preserved for trimming; `RemoteCallCount` observes "no server call"; no generator naming collisions; the 10 auth tests keep their intent under a one-for-one rename.

## Callouts (all affect AC-2 · Must)

| # | Finding | Reachable by | Proposed |
|---|---|---|---|
| 1 | The context (public constructor with parameters, no parameterless one) is claimed by `RecordBypassConverterFactory`, so it and its `IOrder` subtree serialize with no reference handling. Likely fine (the Order graph is a tree), but only proven today for scalar properties, and it is the arrangement Anti-Pattern 9's reasoning warns about; Anti-Pattern 9 also uses a `record OrderSummary` that would read as a second `OrderSummary` | Step 4 as written | Amend: the load test asserts the order's lines and values arrive; Step 6 adds Anti-Pattern 9 (state the actual hazard, rename its sample) |
| 2 | `AddProduct` resolving a SKU through `IProductCatalog` is a remote call, so "no server call" fails; `AddLine`/`Submit` need a Draft order | Notes' "find by SKU" + `AddProduct` | Amend: `AddProduct` takes the product record the screen already holds; tests load `DraftOrderId` |
| 3 | Making all three catalog returns records drops Design's only pin of the class-DTO path (list and single return a class with a parameterless constructor; the third returns a primary-constructor record) | "product records" for all three methods | Amend: search and find return a class listing; a details method returns a record |
| 4 | "List orders" for a customer `Guid` has no data: order rows carry only a customer name | implementing it against `IOrderDbContext` | Amend: the directory keeps its own store; "list orders" becomes "list contacts" |
| 5 | `WorksInLocalMode` and `Design.Server` resolve the implementation by `RegisterMatchingName`'s full-name match | naming the moved implementation other than the interface minus `I`, or moving it to another namespace | Amend: implementations named `ProductCatalog` / `CustomerDirectory`, same namespace as their interfaces |

## Theoretical (not triaged)

- A `private set` on a read-model or context property is silently skipped by ordinal serialization.
- `IReadOnlyList<record>` as a property is unpinned (top-level returns already round-trip).
- `OrderCheckout` beside `CheckoutOrder` and `OrderCheckoutCompleted` may confuse readers.
- The directory's static store and auth flags are mutable; no test leaks today.
