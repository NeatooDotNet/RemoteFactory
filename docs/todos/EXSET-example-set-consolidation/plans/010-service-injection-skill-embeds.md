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

---

## Gate Record

---

## Plan Amendments

---

## Notes

- **Run unattended** under Keith's standing instruction. The defaults follow 007–014: library code becomes prose, and a behaviour that contradicts the old page is stated as it is and filed.
