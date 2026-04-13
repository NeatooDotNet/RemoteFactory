# Architect — Fix Event Record Trimming

Last updated: 2026-04-13
Current step: Step 6A — Independent post-implementation verification (VERIFIED)

## Re-grade Summary (Step 3)

**Grade: A**
**Verdict: Approved**

All 10 items from prior review addressed. No new material gaps. Minor polish items only. Plan ready for implementation.

(Full Step 3 detail preserved in git history of this file; not duplicated here.)

---

## Step 6A Verification (post-implementation, 2026-04-13)

### Verdict: VERIFIED

Independent build + test + manual trimming verification all pass. Implementation matches the plan's design, all test scenarios are covered by passing tests, and the trimmed Person.Client WASM publish retains event type metadata — the ground-truth verification this entire plan exists for.

### 1. Clean build

| Configuration | Errors | Warnings | Notes |
|---|---|---|---|
| `dotnet build src/Neatoo.RemoteFactory.sln -c Debug` | 0 | 2 | Both pre-existing WASM workload warnings (`@(NativeFileReference)` not linked because `WasmBuildNative` not true) on `OrderEntry.BlazorClient` for net9.0 and net10.0. Unrelated to this plan. |
| `dotnet build src/Neatoo.RemoteFactory.sln -c Release` | 0 | 2 | Same two pre-existing warnings. |

No new warnings, no IL2xxx warnings.

### 2. Full test suite (Debug, both TFMs)

| Project | TFM | Passed | Failed | Skipped | Total | Duration |
|---|---|---|---|---|---|---|
| RemoteFactory.UnitTests | net9.0 | 577 | 0 | 0 | 577 | 3 s |
| RemoteFactory.UnitTests | net10.0 | 577 | 0 | 0 | 577 | 4 s |
| RemoteFactory.IntegrationTests | net9.0 | 582 | 0 | 3 | 585 | 5 s |
| RemoteFactory.IntegrationTests | net10.0 | 582 | 0 | 3 | 585 | 4 s |
| Design.Tests | net9.0 | 74 | 0 | 0 | 74 | 285 ms |
| Design.Tests | net10.0 | 74 | 0 | 0 | 74 | 316 ms |
| **Totals (per TFM)** | | **1,233** | **0** | **3** | **1,236** | |

Skipped tests are the three `ShowcasePerformanceTests.ShowcasePerformance_*` tests (intentional skips, not regressions).

Filter run: `dotnet test --filter "FullyQualifiedName~EventDtoDiscoveryTests"` → **14 passed / 0 failed / 0 skipped** on net10.0. All scenarios 1-8 and 12-17 confirmed running and passing.

### 3. Test scenario coverage cross-check

| Scenario | Test Method in `EventDtoDiscoveryTests.cs` | Status |
|---|---|---|
| 1 | `Scenario1_RecordEventWithParameterizedCtor_IsPreservedUnconditionally` | Present, asserts both Contains AND IsUnconditional |
| 2 | `Scenario2_ParameterlessCtorEvent_UsesPreserveTypeNotRegister` | Present, asserts negative on Register |
| 3 | `Scenario3_NestedPlainDto_UsesRegister` | Present |
| 4 | `Scenario4_NestedParameterizedRecord_UsesPreserveType` | Present, asserts negative on Register |
| 5 | `Scenario5_CollectionAndNullableProperties_AreUnwrapped` | Present (covers `IReadOnlyList<T>` and nullable) |
| 6 | `Scenario6_SelfReferencingEvent_EmitsExactlyOnce` | Present, exact count assertion |
| 7 | `Scenario7_PrimitiveAndFrameworkProperties_AreNotRegistered` | Present, negative assertions for `System.*` |
| 8 | `Scenario8_FactoryAnnotatedPropertyType_IsSkipped` | Present |
| 9 | (compiler/trimmer-enforced via `[DynamicallyAccessedMembers]` on `Raise<T>`) | Confirmed manually via Step 5 below — `PersonDomainModel.dll` retains type names |
| 10 | (existing integration round-trip) | All 582 integration tests pass |
| 11 | `DtoConstructorRegistryTests.cs::PreserveType_DoesNotRegisterConstructor` + `PreserveType_IsIdempotent` | Both present and passing |
| 12 | `Scenario12_AllPrimitiveEvent_EmitsExactlyOnePreserveTypeAndNoRegister` | Present, `Assert.Single` and `Assert.Empty` |
| 13 | `Scenario13_AbstractAndInterfaceProperties_AreSkipped` | Present |
| 14 | `Scenario14_SharedNestedRecord_EmittedOncePerClass` | Present, exact count assertion (`sharedCount == 1`) |
| 15 | `Scenario15_StaticAndInstanceHandlers_BothEmitPreservationUnconditionally` | Present, asserts IsUnconditional for both |
| 16 | `Scenario16_DictionaryValueType_IsNotWalked` | Present (regression-guards the documented limitation) |
| 17 | `Scenario17_MissingHandlerMethod_StillEmitsPreserveType` | Present, asserts BOTH NF0501 diagnostic AND PreserveType emission |

All 14 unit tests pass; coverage matches plan exactly. No gaps.

### 4. Generated-code spot check (Design.Domain build artifacts)

Forced clean rebuild of Design.Domain (after killing locked dotnet processes) and inspected the emitted `*.RelayHandler.g.cs` files in `src/Design/Design.Domain/Generated/Neatoo.Generator/Neatoo.Factory/`.

**File: `Design.Domain.FactoryPatterns.OrderNotifyHandlers.RelayHandler.g.cs` (static-method handler, single event)**
```csharp
internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
{
    DtoConstructorRegistry.PreserveType<global::Design.Domain.FactoryPatterns.OrderPlacedEvent>();   // line 19
    if (NeatooRuntime.IsServerRuntime)                                                                // line 20
    {
        FactoryEventHandlerRegistry.RegisterHandler<...>(...);
    }
}
```
PreserveType is on line 19, IsServerRuntime guard opens on line 20. PreserveType is OUTSIDE the guard. ✔

**File: `Design.Domain.FactoryPatterns.OrderShippedHandlers.RelayHandler.g.cs` (static-method handler, event with nested record)**
```csharp
DtoConstructorRegistry.PreserveType<global::Design.Domain.FactoryPatterns.OrderShippedEvent>();      // line 19
DtoConstructorRegistry.PreserveType<global::Design.Domain.FactoryPatterns.ShippingAddress>();        // line 20 — nested record
if (NeatooRuntime.IsServerRuntime) { ... }                                                            // line 21
```
Nested `ShippingAddress` record is preserved. Both calls outside the guard. ✔

**File: `Design.Domain.FactoryPatterns.OrderCheckoutViewModel.RelayHandler.g.cs` (instance-method handler, client-side relay)**
```csharp
internal static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
{
    DtoConstructorRegistry.PreserveType<global::Design.Domain.FactoryPatterns.OrderCheckoutCompleted>();  // line 19
    FactoryEventRelayRegistry.RegisterHandlerType(...);                                                    // line 20 — unguarded relay registration
}
```
Instance-method (client-side relay) handler also emits PreserveType unconditionally. There is no `IsServerRuntime` guard at all in this case (relay registration runs on the client). ✔

All three patterns from Scenarios 7, 14-15 confirmed in real generator output.

### 5. Manual trimming verification (THE GROUND TRUTH)

Published `Person.Server` with full IL trimming on the WASM client:

```bash
dotnet publish src/Examples/Person/Person.Server/Person.Server.csproj -c Release -f net10.0
```

Output: `Person.Server -> bin/Release/net10.0/publish/` with `Optimizing assemblies for size.`

Person.Client uses `[FactoryEventHandler<PersonCreatedEvent>]`, `[FactoryEventHandler<PersonUpdatedEvent>]`, `[FactoryEventHandler<PersonDeletedEvent>]` on a client-side instance handler — exactly the scenario this plan was meant to fix.

**Trimmed WASM (`Person.DomainModel.7wvuasuch6.wasm`, 35,605 bytes)**:
```
$ grep -aoE "Person[A-Za-z]+Event" Person.DomainModel.*.wasm | sort -u
PersonCreatedEvent
PersonDeletedEvent
PersonUpdatedEvent
```
All three event type names survive trimming. Byte offsets show they are in a contiguous metadata region (offsets 27840, 27859, 27878 — interned together, not stripped).

**Trimmed DLL (`Person.DomainModel.dll`, 38,400 bytes)**:
```
$ grep -aoE "Person[A-Za-z]+Event|get_Id" Person.DomainModel.dll | sort -u
PersonCreatedEvent
PersonDeletedEvent
PersonUpdatedEvent
get_Id
```
All three event type names AND the `get_Id` property accessor (the constructor parameter property on each event) are preserved. This confirms `[DynamicallyAccessedMembers(All)]` applied via `PreserveType<T>()` is doing its job.

**Re-published with verbose output**: `0 Warning(s)` — no `IL2xxx` trimming warnings emitted during publish.

The trimming-preservation pipeline works end-to-end for the original bug case.

### 6. Design.Tests pre-existing fix review

`src/Design/Design.Tests/TestInfrastructure/DesignClientServerContainers.cs::ForDelegateEvent` (lines 120-130):

```csharp
public async Task ForDelegateEvent(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
{
    var remoteRequest = _neatooJsonSerializer.ToRemoteDelegateRequest(delegateType, parameters);
    var json = JsonSerializer.Serialize(remoteRequest);
    var remoteRequestOnServer = JsonSerializer.Deserialize<RemoteRequestDto>(json)!;

    await _serviceProvider
        .GetRequiredService<ServerServiceProvider>()
        .ServerProvider
        .GetRequiredService<HandleRemoteDelegateRequest>()(remoteRequestOnServer, cancellationToken);
}
```

Compared against:
- Interface `src/RemoteFactory/Internal/MakeRemoteDelegateRequest.cs:21`: `Task ForDelegateEvent(Type delegateType, object?[]? parameters, CancellationToken cancellationToken);` ✔ matches
- Sibling integration-tests version `src/Tests/RemoteFactory.IntegrationTests/TestContainers/ClientServerContainers.cs:86-96`: identical structure, threads token through to `HandleRemoteDelegateRequest` invocation ✔ matches

The fix is minimal, correct, and matches both the interface contract and the sibling implementation. It is NOT a no-op — the token IS threaded through to `HandleRemoteDelegateRequest`, exactly as the integration-tests version does.

### Pre-existing warnings (acknowledged, not introduced)

The two warnings observed during build/Release-build are pre-existing and unrelated to this plan:
- `WorkloadManifest.targets(124,5): warning : @(NativeFileReference) is not empty, but the native references won't be linked in, because neither $(WasmBuildNative), nor $(RunAOTCompilation) are 'true'.` on `OrderEntry.BlazorClient` (net9.0 and net10.0 each emit this once). Caused by SQLite native references in WASM project being skipped because non-AOT build. Independent of factory events / trimming.

### Summary of evidence

- Builds: Debug + Release both succeed with 0 errors and only pre-existing warnings.
- Tests: 1,233 passing across 6 project×TFM combinations, 0 failures, 3 intentionally-skipped performance tests.
- Scenario coverage: All 14 plan scenarios (1-8, 12-17) have dedicated test methods in `EventDtoDiscoveryTests.cs`; scenarios 9, 10, 11 covered by trimming verification, integration tests, and `DtoConstructorRegistryTests.cs` respectively.
- Generated code: Real Design.Domain output confirms PreserveType is emitted unconditionally (outside `IsServerRuntime` guard) for both static-method handlers and instance-method (client-side relay) handlers, including nested records (`ShippingAddress`).
- Manual trimming: The Person example's `PersonCreatedEvent`/`PersonUpdatedEvent`/`PersonDeletedEvent` type names AND the `get_Id` accessor survive in the trimmed WASM client (35,605 bytes) — the original bug condition is demonstrably fixed.
- Pre-existing fix: `DesignClientServerContainers.ForDelegateEvent` properly threads the `CancellationToken` and matches the interface signature and the integration-tests sibling.

### Verdict

**VERIFIED.** Proceed to Step 6B (requirements verification).

No follow-up items for the developer. No design concerns. No regressions detected.
