# Code Review — RFSK-004 — 2026-10-16

Written by the orchestrator from the code-reviewer's report.

**Verdict: APPROVED**

**Logs:** builds passed (main: 0 errors, the 2 WASM `NativeFileReference` warnings from `OrderEntry.BlazorClient`; Design: clean). Unit 871 and integration 664 (5 skipped), Design 180 (1 skipped), hierarchy 3/3, each on both TFMs.

## Direction and shape

`polymorphic-hierarchy.md` is three embeds from `Design.Domain/Payments/Payment.cs` and prose that states the generator's rule: `[Factory]` only from the class's own declaration; abstract and generic classes dropped (`FactoryGenerator.cs:19-23`); base operation methods join the derived factory with their attributes, constructors excluded (`FactoryGenerator.Transform.cs:58-67`); the nearest base's `[AuthorizeFactory<T>]` when the class declares none (`Transform.cs:19-31, 310`). That is #149 stated correctly, and `advanced-patterns.md` agrees.

The Payments example follows the doctrine: `Create` builds (D3), local and `public`; `Fetch` is `[Remote] internal bool`, false for a missing or other-kind row (R-I); `IPaymentLookup` is a domain service; `IPaymentLedger` reaches `Fetch` as a `[Service]` and `PaymentLookup` by constructor on the server; Design's comment style.

**Spot checks, all held:** NF0501–NF0505 ids and severities; log events 3008/3009/3011/3012 and 9001–9009; `PreserveProperties<T>()`; the two `CanSave` overloads under target auth; `[AspAuthorize]` forcing the server; the middleware order; the hint-name cap and NF0104 as an error; NF0101 as an error; the trimming shape table, "TrimMode needs no setting" and the DTO walk against the published `docs/trimming.md`. Fences: factory-events 10 + 4 WRONG, trimming 2 + 1, polymorphic-hierarchy 3, advanced-patterns 18. All links and anchors resolve. No LazyLoad, N+1, `JsonInclude`, `DynamicDependency`, CRTP, "business logic" or `[Remote, Create]` on the four pages.

## Veto-tier

None.

## Callouts (both Affect AC-5)

1. `PaymentHierarchyTests.ConcretePayments_LoadAcrossTheWire_WithTheirOwnAndTheBasesData` asserted no call count, and the test containers register `PaymentLedger` on the client too, so dropping `[Remote]` from a concrete `Fetch` would leave all three tests green. **Fixed** (also the test review's theoretical item): the test asserts `callsBefore + 3`.
2. `SKILL.md`'s base-class row said "`[Factory]` on the base does nothing", false for a concrete base, which gets its own factory. **Fixed:** "on an abstract or generic base".

## Could not verify

- No test review existed yet when the review ran. Resolved: `reviews/004-test-review.md`.
- Blazor's default `TrimMode=partial`, taken from `docs/trimming.md:153`. Resolved: the trimming gate publishes at `partial` (`docs/trimming.md:153`; #151 records that CI covers only that mode).

## Theoretical (all taken)

- `factory-events.md` "none is NF0501" holds only when the class declares some `static` method. **Taken:** the condition is stated.
- `trimming.md` dropped the fact that on the named JSON path (the `Named` format, or a constructor that needs arguments) public fields cross the wire. **Taken:** restored; it matches `service-injection.md` and `NeatooJsonSerializer`'s `IncludeFields`.
- `advanced-patterns.md:5` named `references/interface-factory.md` as code text. **Taken:** a link to its authorization section.
