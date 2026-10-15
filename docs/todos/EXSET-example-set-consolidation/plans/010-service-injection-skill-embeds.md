# Service injection and interface factory pages

**Plan #:** 010
**Date:** 2026-10-11
**Related Todo:** [../todo.md](../todo.md)
**Serves:** AC-3, AC-4
**Status:** In Progress
**Last Updated:** 2026-10-15
**Plan-review opt-in:** No. Most embeds re-point to regions 002–009 built and reviewed. The new regions sit on Design code that tests already pin, and no Design shape is new.
**Code-review opt-in:** Yes. Both pages are restated against the library, and four of the audit's findings are behavioural (U-07, U-08, T-06, T-24).
**Branch:** EXSET-010-service-injection-skill-embeds
**PR:** —

---

## Scope

Re-point every C# block on `docs/service-injection.md` and `docs/interface-factory.md` to `src/Design`, or turn it into prose or a short WRONG block. The skill's `references/service-injection.md` shares three of those regions and is re-pointed with them.

**Split at Step 2 (2026-10-15):** the skill-only `skill-*` embeds (`class-factory.md`, `interface-factory.md`, `advanced-patterns.md`, `setup.md`, `static-factory.md`) move to EXSET-015. The two plans touch no common page or region.

**Reused regions:**
- `aspnetcore-correlation-id`, for multiple services and a scoped service;
- `aspnetcore-service-registration`, for lifetimes and `RegisterMatchingName`;
- `aspnetcore-testing`, for container usage;
- `orderline-child`, for the skill's child entity.

**New Design regions, on code that tests already pin:**
- `service-injection-basic`, on CustomerPreferences' Fetch;
- `service-injection-constructor`, on CtorInjectedEntity's constructor and its read of the service;
- `service-injection-server-only`, on TaxRate's keyed Fetch (replaces the client-supplied query, U-08);
- `service-injection-httpcontext`, on the server's `ICurrentUser` registration;
- `service-injection-testing`, on a test that swaps a server service;
- `clientserver-container-setup`, on the three containers' configuration;
- `interface-factory-contract`, `interface-factory-implementation` and `interface-factory-usage`, on IProductCatalog.

**Becomes prose or WRONG:**
- the serialization caveat's broken shape (WRONG);
- `IServiceProvider` resolution (prose);
- client-side constructor injection with a side effect (prose, with the R-J warning, U-07);
- the interface page's DI registration and its two anti-patterns (WRONG or prose).

**Findings to fix:**
- U-07, U-08, T-06, T-24;
- this page pair's shares of I-02, C-18, C-15, C-19, C-20, C-01, C-02, U-01, U-06 and T-02.

**Delete** the reference-app sources whose every region retires, and strip the markers from any that other code still needs. That closes the duplicate keys (`service-injection-lifetimes`, `-matching-name`, `-httpcontext`, `-testing`).

Does NOT touch:
- the skill's prose beyond the re-pointed embeds (Todo 2);
- `lazyload.md` (EXSET-011).

---

## Intent

Both pages state where a `[Service]` is resolved: in the container of the tier that runs the method. Every example is Design code already pinned by tests, and none teaches a pattern the rulings retired.

---

## Framework & Architectural Alignment

- **Rulings:** D12, D14, R-C, R-H, R-J and R-M.
- **`.claude/rules/design-snippets.md`.**
- **The 006, 007, 009 and 014 pages,** which these pages link rather than restate.

---

## Constraints & Invariants

- **Embeds.** Every C# block on both pages is an embed from `src/Design` or a short WRONG block.
- **Regions.** Each region name is defined once in the repository.
- **The whole still works:** both solutions green, mdsnippets swallows nothing, the reference app builds.

---

## Steps

1. The new Design regions. Where a region needs adjacent lines, reorder statements without changing behaviour.
2. Rewrite `service-injection.md`.
3. Rewrite `interface-factory.md`. Check the "[Factory] on the implementation" claim with a probe compile.
4. Re-point the skill's `service-injection.md` embeds.
5. Delete or strip the reference-app sources, then build `EmployeeManagement.sln`.
6. Verify and record: mdsnippets, both solutions, the ledger.

---

## Acceptance

- [ ] Every C# block on both pages is a Design embed or WRONG. No region is defined twice, and none retired is embedded. The reference app builds. `[explicit-skip: greps; EmployeeManagement.sln build]` · Must
- [ ] The pages' resolution, side-effect and interface-factory statements match the library `[explicit-skip: source cited; probe compile for the implementation-class claim]` · Must
- [ ] The embedded regions are pinned by existing Design tests `[explicit-skip: each region's test named in Test Evidence]` · Must
- [ ] mdsnippets clean; both solutions green `[explicit-skip: meta]` · Must

---

## Current State (Pre-Flight)

- **`service-injection.md`:** 15 embeds, all from the reference app. Four keys are defined twice there.
- **`interface-factory.md`:** six hand-written fences and no embeds. It shows:
  - `RegisterMatchingName<T>()`, which does not exist (T-06);
  - "causes duplicate generation" where the library emits NF0106 (T-24);
  - "returns DTOs";
  - `partial` "Required" on the class (T-02).
- **The skill's `service-injection.md`:** 3 embeds from the reference app.
- **Design has the shapes:**
  - `TaxRate.Fetch`: keyed, with a server-only `ITaxTable`;
  - `CtorInjectedEntity`: its constructor-injected service is read on the client after the round trip;
  - `IProductCatalog` and `ProductCatalog`;
  - `ServerServices`' registrations;
  - `DesignClientServerContainers.Scopes`.

---

## Punchlist

---

## Test Evidence

| Bullet | Pinned by | Tier |
|---|---|---|
| 1. Pages and sources | explicit-skip: both pages have no C# fence outside a snippet pair except three WRONG blocks, and every `snippet source` link is under `src/Design`. The skill's `service-injection.md` embeds only Design regions. No `service-injection-*`, `clientserver-container-*` or `interface-factory-*` key is defined twice. `SerializationCaveatSamples.cs` and `ClientServerContainerSamples.cs` deleted; markers stripped from `ServiceInjectionSamples.cs`, `ServiceRegistrationSamples.cs`, `TestingSamples.cs` and `ChildEntitySamples.cs`, whose types other reference-app code uses; `reviews/010-refapp-build.log` (0 errors) | explicit-skip |
| 2. Statements match the library | explicit-skip: `[Service]` resolution from the generated code (`cc.GetRequiredService`, `ExampleCommands` factory); `RegisterMatchingName` (`AddRemoteFactoryServices.cs:256-276`); the request scope (`WebApplicationExtensions.cs:56`); the interface registrations and auth order (`IProductCatalogFactory.g.cs`, `ICustomerDirectoryFactory.g.cs`); NF0106 (`DiagnosticDescriptors.cs:65`); `partial` (`FactoryModelBuilder.cs:68`, `:309`); `[Factory]` on the implementation: probe compile, CS0101 and CS0111 (Plan Amendments) | explicit-skip |
| 3. Embedded regions pinned | `service-injection-basic` (`OperationsTests.Fetch_StoredCustomer_ReturnsTheSavedValues`, `Fetch_UnknownCustomer_ReturnsNull`), `service-injection-constructor` (`SerializationTests.CtorInjectedEntity_ServiceArrivesOnEachSideAfterRoundTrip`), `service-injection-server-only` (`ValueObjectTests.TaxRate_LoadsFromTheServer_AndAppliesToMoney`), `service-injection-httpcontext` (`AuthorizeFactoryHttpTests`, over the real Design.Server host), `service-injection-testing` (`StaticFactoryTests.Execute_ReturningTask_RunsOnServer`, its own region), `clientserver-container-setup` (every test through `Scopes()`), `interface-factory-contract`/`-implementation`/`-usage` (`InterfaceFactoryTests`). The reused regions keep the tests 002–009 cite | explicit-skip |
| 4. Meta | explicit-skip: `reviews/010-mdsnippets.log`; the shared pages change only in attribution, and `advanced-patterns.md` only in the re-pointed `aspnetcore-testing` embed. Logs: `010-build.log`, `010-test.log`, `010-design-build.log`, `010-design-test.log` (unit 852; integration 657 + 7 skips; Design 173 + 1 known-bug skip; unchanged; both TFMs) | explicit-skip |

---

## Gate Record

---

## Plan Amendments

- **2026-10-15 · Implementation:**
  - **The skill's `clientserver-container-usage` embed** (`advanced-patterns.md`) is re-pointed to `aspnetcore-testing` here, not in EXSET-015: its source file is deleted in this plan.
  - **Two files are deleted.** `SerializationCaveatSamples.cs` and `ClientServerContainerSamples.cs` go; the four files with types other reference-app code uses keep their code, with the markers stripped.
  - **The probe compile** for `[Factory]` on both the interface and its implementation failed with CS0101 and CS0111, the class's factory taking the interface factory's names. The page says so, in place of "duplicate registration".
  - **`DesignClientServerContainers.Scopes`** moves the client's two registrations up beside the mode configuration, so that `clientserver-container-setup` is one tight region. Registration order is unchanged in effect.
  - **Design's AllPatterns comments** that called method injection "server-only" are corrected (I-02).

---

## Notes

- **Run unattended** under Keith's standing instruction. The defaults follow 007–014: library code becomes prose, and a behaviour that contradicts the old page is stated as it is and filed.
