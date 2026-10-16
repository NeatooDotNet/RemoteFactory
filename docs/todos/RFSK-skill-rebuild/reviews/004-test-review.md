# Test Review — RFSK-004 — 2026-10-16 — Round 1

Written by the orchestrator from the test-reviewer's report.

**Verdict: APPROVED**

**Logs:** main build 0 errors, 2 warnings (the WASM `NativeFileReference` warning on `OrderEntry.BlazorClient`, not this plan); Design build clean. Unit 871 and integration 664 (5 skipped) per TFM; Design 180 (1 skipped) per TFM, up 3 from RFSK-003's 177; the hierarchy filter 3/3 per TFM.

## Veto-tier

None.

- **Bullet 1:** `PaymentHierarchyTests.ConcreteFactories_CarryTheBasesCreate_AndRunItOnTheClient` resolves both factories from the client on `DesignClientServerContainers`, asserts the base's `OrderId`/`Amount` and an unchanged `RemoteCallCount`. It fails to compile if the base `Create` leaves a concrete factory, and the counter catches a crossing.
- **Bullet 2:** `…ConcretePayments_LoadAcrossTheWire_WithTheirOwnAndTheBasesData` asserts base and own data for both kinds, and null across kinds.
- **Bullet 3:** `…PaymentLookup_ReturnsEachConcreteType_ThroughTheBaseInterface` asserts the concrete type of each result, its data, null for an unknown order, and `callsBefore + 3` - one call per lookup; a lookup run on the client would count +2.
- **Counter use** is right: `ServerServiceProvider` is client-scoped (`DesignClientServerContainers.cs:52`, `:259`) and counts once per remote request (`:97`, `:123`).
- **Fence counts honest:** factory-events 14 = 10 + 4 WRONG; trimming 3 = 2 + 1; polymorphic-hierarchy 3 = 3; advanced-patterns 18 = 18; `SKILL.md` 8 = 8.
- **Explicit-skip reasons honest;** no existing test or region changed.

## Callouts

None.

## Could not verify

- The mdsnippets log was not provided. Resolved: the diff touches nothing outside the skill and Design.

## Theoretical (taken)

- Test 2 had no call-count assertion, so it would pass if `Fetch` stopped crossing (the client container has `PaymentLedger` too, through `RegisterMatchingName`). **Taken:** test 2 now asserts `callsBefore + 3`; Design 180, green on both TFMs.
