# Domain service, read model, context

**Plan #:** 003
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-2, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-12
**Plan-review opt-in:** Yes (three new Design shapes later plans embed; documented rules D12, D13, R-H, R-I; renames 15 pinned tests)
**Code-review opt-in:** Yes (behavior-changing: new Design types and a renamed authorized service)
**Branch:** EXSET-003-service-readmodel-context
**PR:** —

---

## Scope

Give Design the three shapes VisionAlignment §3 lists as missing or misnamed. (a) Replace the two repository-named interface factories with domain services named for what they do (D12, #112): `IExampleRepository` becomes a product catalog the client queries, `IAuthorizedRepository` becomes an authorized customer directory with its own auth interface, each keeping what its tests pin (list, single, record and null returns; parameterless, per-id and Read-scope denials; `Can{Method}` shape; a denial crossing the wire). (b) Add a read model: a `[Fetch]`-only `[Factory]` class the client holds, loaded by one `[Remote, Fetch]` that returns `false` when the order is missing, carrying plain records as properties (R-H, R-I). (c) Add a context (D13): a `[Factory]` instance a screen binds to, loaded with its `Order` in one round trip, whose verbs are plain instance methods that call the held order's verbs and build lines through the line factory, and which persists through the root's `Save` using constructor-injected factories. Repository wording goes from CLAUDE-DESIGN, the Blazor page and the three files' comments. Does NOT touch `docs/` pages, the skill, `src/Design/README.md` (EXSET-012), `Money` (004) or the authorized orders (009).

---

## Intent

- A reader asking "how does the client call the repository?" (T5) finds the answer in Design: it doesn't; a repository is `[Service]` on a server operation, and what the client calls is a domain service.
- A reader asking "where does my application service go?" (T6) and "I'll return a DTO from a controller for this screen?" (T20) finds the read model and the context as compiled, tested classes, in the Orders domain, instead of nothing.
- Plans 005, 007 and 010 can embed these three shapes instead of the reference app's query services and DTO-returning samples.

---

## Framework & Architectural Alignment

- D12: a static command for a command; an interface factory is a domain service the client calls; a repository is never an interface factory.
- D13: a context when the screen binds to the result and later verbs act on it; its verb is a plain instance method calling the held aggregate's verb; it persists through that root's `Save()`; never an instance `[Execute]` (NF0103/NF0201).
- R-H (a `[Fetch]`-only class when the client holds an instance; records are not DTOs in the forbidden sense), R-I (`bool`/`false` Fetch), R-F (a record crosses as a property with no factory), T14 (constructor injection is both tiers: the context takes the generated factories in its constructor so its verbs work on the client).
- EXSET-002's `Order`, `IOrderDbContext` and seed data are the substrate; the catalog's products are what `OrderLine.Create` takes (name, price).
- `.claude/rules/design-snippets.md`.

---

## Constraints & Invariants

- The 15 interface-factory tests keep their intent one-for-one; only names and domain vocabulary change. `WorksInLocalMode`, which today pins nothing (bare `ServiceCollection`, `local` unused), is rewritten to resolve the service through the Logical container, keeping its stated intent.
- The authorized service's methods all share the auth-matched parameter type (the generator matches auth parameters by type; a heterogeneous set fails CS7036).
- Interface-first throughout; no `[Remote]` on anything but the entry points; no method-injected service stored in a field; no `new` on a `[Factory]` type.
- `Design.Server` composition still resolves every service (`DesignServerCompositionTests`).
- Both solutions green.

---

## Steps

1. Rename the plain interface factory to a product catalog in the Orders domain (search and find return a listing class, a details method returns a record, so both serialization paths stay pinned; implementation named for the interface without its `I`, beside it), and move its implementation, types, tests and composition assertion with it; the "repository" framing in its comments becomes the domain-service rule (D12) with the WRONG counterpart named.
2. Rename the authorized interface factory and its auth interface to a customer directory on its own store (get, rename, close the account, list contacts), keeping the three auth scopes and the homogeneous `Guid` parameter; closing an account returns plain `Task` once the interface-factory `Task` fix ships, so the `Task<bool>` sentinel goes; retarget its 10 tests.
3. Add the read model: a per-order summary the client holds, `[Remote, Fetch]` returning `false` for a missing order, reading `IOrderDbContext` and carrying line summaries as plain records; interface-first; no `[Create]`.
4. Add the context: a checkout session holding an `IOrder`, loaded by one `[Remote, Fetch]` through `IOrderFactory` on the server, with verbs to add a product the screen already holds (building the line through the constructor-injected line factory, no catalog call), submit, and save through the constructor-injected order factory with the result reassigned; interface-first.
5. Tests on the client container for the read model (found, missing, records arrive) and the context (loads the Draft order with its lines and their values, verbs act on the held order on the client with no server call, save persists through the root and the context holds the saved order).
6. Update the Blazor page's interface-factory section to the catalog and add a context demo; update CLAUDE-DESIGN (decision table, Pattern 2/2a, Anti-Pattern 2, Anti-Pattern 9 (state its actual hazard; rename its `OrderSummary` sample), Critical Rule 4, checklist, file index) and `ServerServices.cs` comments.
7. Record ledger outcomes (C-18, C-27, U-06 and what this plan advances of C-19, T-22).

---

## Acceptance

- [ ] From the client, the catalog returns a list, a single record, and null for an unknown product `[integration]` · Must
- [ ] From the client, the customer directory denies by parameterless, per-id and Read-scope auth, exposes `Can{Method}` with the auth-matched parameter only, and a server-side denial crosses as `NotAuthorizedException` `[integration]` · Must
- [ ] From the client, the order summary loads with its line records for a seeded order and is null for a missing one `[integration]` · Must
- [ ] From the client, the context loads with its order in one call; adding a product and submitting act on the held order with no server call; saving persists through the order's root Save and the context holds the saved order `[integration]` · Must
- [ ] No Design type, comment, test or page calls an interface factory a repository `[explicit-skip: grep for "repository" across src/Design excluding README.md and the [Service] IOrderDbContext/IOrderRepository rule statements]` · Should
- [ ] Both solutions green `[explicit-skip: meta-bullet]` · Must

---

## Current State (Pre-Flight)

- `IExampleRepository` (`AllPatterns.cs:219-246`): `GetAllAsync`, `GetByIdAsync(int)`, `GetRecordByIdAsync(int)`; impl `ExampleRepository` :275-296; types `ExampleDto` :566, `record ExampleRecordResult` :588. Repository framing at :164-180, :182-218, :227, :248-274. Consumers: `InterfaceFactoryTests.cs` (5 tests; `NullReturningRepository` :183-199), `SerializationTests.cs:384-395`, `DesignServerCompositionTests.cs:90`, `ServerServices.cs:116-119` (comment only; `RegisterMatchingName` registers it), `Home.razor:8, 19, 63-91, 124-125, 135, 206-236`, `src/Design/README.md:62-72`, `CLAUDE-DESIGN.md:1087, 1096`.
- `IAuthorizedRepository` (`AuthorizedRepository.cs:245-266`) with `[AuthorizeFactory<IRepositoryAuth>]`: `GetItem(Guid)`, `DeleteItem(Guid)` returning `Task<bool>` (remark says bare `Task` gives CS0738; probe at implementation), `UpdateItem(Guid, string)`, `GetItems(Guid)`; `IRepositoryAuth` :114-168 (`[Execute] HasAccess()`, `[Execute] CanAccessItem(Guid)`, `[Read] CheckReadAccess()`), `RepositoryAuth` :186-212 with static flags, `record RepositoryItem` :217, impl :294-327 (static dictionary: Alpha …01, Beta …02, Denied …42). Consumers: `InterfaceFactoryAuthorizationTests.cs` (10 tests; registers the impl on the server at :48-56), `CLAUDE-DESIGN.md:114-141, 382-383, 1206`, skill `interface-factory.md:241, 246` (Todo 2).
- Interface-factory mechanism: generated `I{X}Factory : I{X}` and `{X}Factory`; Remote registers the proxy as `I{X}`; Server registers the factory and per-method delegates but not the impl (`RegisterMatchingName` or explicit); Logical registers nothing from the generator, `I{X}` resolves to the impl, no auth runs. `Can{Method}` takes the union of auth-method parameters; matching is by type.
- Read model precedent: none in Design; `RemoteFetchTarget` in the unit tests is `[Fetch]`-only. Bool Fetch generates `Task<IX?>` returning `default` on `false` (`OrderFactory.g.cs:20, 136-142`).
- Context precedent: none anywhere (D13 says so). A `[Factory]` class with no write methods generates no `Save`; plain instance methods are ignored by the generator. Constructor injection of a generated factory: `CtorInjectedEntity` (ctor service, registered transient in all modes, deserialized through DI on the named path); the reference app's `OrderLineListFixed([Service] IOrderLineFixedFactory)` calls a ctor-injected factory from an instance method on the client.
- Vocabulary: `Order`, `IOrderLine.Create(productName, unitPrice, quantity)`, `OrderStatus`, `Money`, seed customers `Customer_{id}`, products "Widget A" 10.00 and "Widget B" 25.00; `InMemoryOrderDbContext.DraftOrderId` 300.
- No touched file has a `#region`; renames break no embed.

---

## Punchlist

---

## Test Evidence

| Acceptance bullet (short) | Priority | Tier declared | Test method | Tier confirmed |
|---|---|---|---|---|

---

## Gate Record

---

## Plan Amendments

### 2026-10-12 — Plan-review callouts and the Task fix folded in

- **Section affected:** Steps 1, 2, 4, 5, 6
- **Original said:** product records for all catalog methods; a directory listing a customer's orders; `AddProduct` by SKU; context load test checks the order arrives; Step 6 without Anti-Pattern 9.
- **What changed:** listing class plus details record; directory on its own store with contacts; `AddProduct` takes the held product; load test checks lines and values on the Draft order; Anti-Pattern 9 rewritten; implementations named by the `RegisterMatchingName` convention; `CloseAccount` returns `Task` after the interface-factory `Task` fix (v1.10.4), which Keith chose to ship first.
- **Why:** `reviews/003-plan-review.md` callouts 1–5, accepted by Keith 2026-10-12; the probe of 2026-10-12 confirmed a bare `Task` interface-factory method generates `Task<Task>` (CS0738/CS0266).
- **Discovery Log:** none (detail, not intent)

---

## Notes

- 2026-10-12, Keith: all as proposed. Names `IProductCatalog` (search, find by SKU, product records) and `ICustomerDirectory` with `ICustomerDirectoryAuth` (get, rename, close, list orders; all keyed by customer `Guid`); read model `OrderSummary` (by order id, line records); context `OrderCheckout` (one `[Remote, Fetch]` through `IOrderFactory`, `AddProduct`, `Submit`, `SaveAsync` through the root factory, factories by constructor); the 15 tests renamed one for one, `WorksInLocalMode` rewritten to resolve through the Logical container.
