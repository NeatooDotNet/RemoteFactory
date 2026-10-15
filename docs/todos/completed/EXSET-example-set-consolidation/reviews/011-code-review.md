# EXSET-011 code review — 2026-10-15

**Reviewer:** code-reviewer (per-plan, Step 5; `a5f81ab` against `a3dcce2`). **Verdict:** APPROVED. No veto-tier findings.

The mechanics landed cleanly:
- the nine new regions are markers only;
- the one behavioural Design edit, `CheckoutOrder.Insert`'s `CancellationToken`, is sound;
- each embedded key is defined once;
- every inbound anchor resolves;
- the DI table, the handler rules, both WRONG blocks, the Decision Guide and the Complete Example match the source.

There are four callouts, all on Must criteria:
- three page statements the runtime does not bear out (1–3). All three are older text that the rewrite kept;
- one ledger outcome that claims more than the page shows (4).

The test review's finding 1 (no Design test pins the Insert's token) and its finding 2 (`events-serveronly`'s Update takes no token) are not raised again here.

**Build.** 0 errors in both builds:
- Main solution: 2 warnings, both the OrderEntry WASM `NativeFileReference` notice (`011-build.log`).
- Design solution: 0 warnings (`011-design-build.log`).

**Tests.** 0 failed on both TFMs:
- Unit 852. Integration 657 + 7 skipped: `ShowcasePerformanceTests` (3), `RelayTimingTests` (2) and `FactoryReferenceIdentityTests.Ordinal_*` (2), each named in `011-test.log`.
- Design 174 + 1 skipped, `SaveTests.AuthorizedSave_NewAndDeleted_ReturnsNull` (#104) (`011-design-test.log`).
- `011-mdsnippets.log` has no warning, error or missing-snippet line (`TreatMissingAsWarning: True`). It extracts all nine `events-*` keys and `attributes-factoryeventhandler`, and ends `Finished`.

**Timestamps.**
- The Design edits are 16:05:58–16:06:56. The logs are 16:09:26–16:09:43, and the commit is 16:10:38.
- `factory-events.md`'s mtime (16:09:43) is the mdsnippets run.
- The generated `CheckoutOrderFactory.g.cs` (16:09) calls `cTarget.Insert(factoryEvents, cancellationToken)` (`:118`), so the logged runs used the new signature.
- `reviews/011-test-review.md` is untracked in the working tree.

The deliverable landed as scoped:

- **Q1. Handler-signature rules (`:101-108`).** They match `TransformRelayHandler` (`FactoryGenerator.RelayHandler.cs`):
  - **Return type.** The method must return exactly `Task` (`:318`, an exact display-string match). `Task<T>` and `void` are skipped.
  - **First parameter.** It is the first parameter that is neither a `[Service]` nor a `CancellationToken`, and its type must equal `T` exactly.
  - **`static`.** A static method registers. An instance method of the same shape emits NF0503 and is dropped.
  - **Accessibility.** There is no filter. The renderer emits the registrar inside the consumer's partial class, so private methods compile (`RelayHandlerRenderer.cs`, the `RenderRegistrarHolder` remarks).
  - **Count.** Zero matches gives NF0501, more than one gives NF0502.
  - **The transform's own summary says "non-private".** That comment is stale, and the page is right.
- **Q2. DI table (`:591-596`) against `AddRemoteFactoryServices.cs:118-194`.**
  - Remote mode: `AddScoped<IFactoryEvents, RemoteFactoryEvents>` and `TryAddSingleton<IFactoryEventRelay, NoOpFactoryEventRelay>`.
  - Server and Logical: `TryAddScoped<IFactoryEvents, FactoryEventsDispatcher>`, plus the scheduler and `IFactoryEventPhaseCoordinator`, both scoped.
  - Server only: `AddScoped<IFactoryEventCollector, FactoryEventCollector>`.
  - Every cell matches. `AddNeatooRemoteFactory` (`:41`, `:58`) and `NeatooFactory` (`:10`) are the real names. The table omits the internal scheduler and the `RaiseFactoryEventRemote` delegate, but it does not claim to list every registration.
- **Q3. WRONG blocks.**
  - **CS0111 (`:522-534`).** `StaticFactoryRenderer.RenderFactoryServiceRegistrar` (`:141-143`) and `RelayHandlerRenderer.Render` both emit `internal static void FactoryServiceRegistrar(...)` on the consumer's partial class. This matches the probe's CS0111 on `ZzProbeHandlers`. The page scopes the claim to a static class, and that is the shape both renderers emit.
  - **`async void` (`:540-551`).** A `void` static method fails the `Task` check. The class has a static method (`hasStaticCandidate`, `:377`), so NF0501 fires, which matches the probe.
  - **The `Task<T>` claim is right.** A static `Task<string>` fails the same exact-match check and reaches the same NF0501. It is pinned by `NF05xxFactoryEventHandlerTests` (`:34-35`, "Static Task<string> triggers the 'static candidate but wrong shape' path").
- **Q4. Decision Guide (`:482-496`).** Every row holds:
  - `FactoryEventBase` is a `public abstract record` (`FactoryEventBase.cs`), so "records only" holds.
  - The new rows (where to raise, no `[Create]`, `static` returning `Task`) agree with D3, C-06 and the generator.
- **Q5. Complete Example (`:609-617`).**
  - `OrderCheckoutCompleted` is in `Design.Domain`.
  - `CheckoutOrder`'s `[Remote, Insert]` raises it.
  - `OrderNotifyHandlers` is a real `OrderPlacedEvent` handler (`FactoryEventHandlerPattern.cs:93`). Design has no `OrderCheckoutCompleted` handler, as the Amendment records.
  - `Relay_EventRaisedInRemoteInsert_DeliveredToConsumer` runs `Create`, then `Save` from the client container, and asserts the relayed event.
- **Q6. The Design change.**
  - `Insert([Service] IFactoryEvents, CancellationToken)` is bound by the generator: `LocalInsertCore` passes the factory's `cancellationToken`, and `Save`'s public signature is unchanged (`CheckoutOrderFactory.g.cs:19`).
  - Every other Design hunk is a `#region`/`#endregion` pair, plus one added comment (`FactoryEventRelayPattern.cs:163-164`). `FactoryEventRelayTests.cs` gains two region pairs, and no line inside a test changed.
  - Rulings: `CheckoutOrder`'s `[Create]` stays local (D14). `IsNew`/`IsDeleted` are public `get; set;` (R-M). No embed throws for a recoverable check (R-E).
- **Q7. Ledger.**
  - C-06 matches: the raisers are `[Remote, Insert]`, `[Remote, Update]` and `InvoiceFinalizer`'s `[Execute] [Remote]`, no `[Create]` raises, and `:490` says a Create should not.
  - I-08 matches `:489` and the Client-Initiated Raise section.
  - T-18 matches `:103`, `:492` and the WRONG blocks.
  - T-19 matches `:589` and `:593`.
  - C-19 claims more than the page shows (callout 4).
  - The re-owned rows (C-22, C-26, I-01, I-10, T-01, T-02, T-07, T-09, T-26, T-27, U-12 → 016) match the Discovery Log's re-split.
- **Q8. Regions and anchors.**
  - Each of the ten keys has one definition outside `obj`/`bin`/`Generated`. The `events-*` keys are embedded only here, and `attributes-factoryeventhandler` also on `attributes-reference.md`.
  - No heading changed in the diff.
  - Inbound anchors from `docs/attributes-reference.md:270, :289`, `docs/decision-guide.md:153`, `docs/interfaces-reference.md:146` and `docs/release-notes/v1.2.0.md:152` all resolve to a heading on the page: `#dispatch-phases`, `#client-side-relay-consumer-implements-ifactoryeventrelay` and `#il-trimming-and-event-records`.
  - All ten in-page anchors resolve, including `#client-initiated-raise`, `#raising-an-operations-event-from-the-client-instead-of-inside-the-method` and `#draining-afterflush--ifactoryeventphasecoordinator`.
  - The outbound `trimming.md#factory-event-type-preservation` resolves (`trimming.md:382`).
- **Embeds.** The page has ten `snippet source` links, all under `/src/Design/`. The only C# fences outside a snippet pair are the three WRONG blocks (`:506`, `:522`, `:540`). `:289` and `:428` are text diagrams.

**Sacred tests.** Nothing under `src/Tests` changed. The only test-file change is the two region pairs in `FactoryEventRelayTests.cs`, which are markers only.

| # | Callout | Affects | Triage |
|---|---|---|---|
| 1 | **"Pass `RaiseOptions.ServerOnly` to suppress the whole batch … the client receives one invocation with `events.Count == 0`." For the page's own example, the batch is not empty.**<br>• `factory-events.md:446`, directly under the diagram whose handler raises `NoticeAcknowledged`.<br>• `ServerOnly` stops only the raised event's own capture (`FactoryEventsDispatcher.cs:52-54`). The generated handler lambda drops `options`, and `ClientRaisedNoticeHandler` raises with `RaiseOptions.None` (`FactoryEventRelayPattern.cs:245`). So a `ServerOnly` raise of `ClientRaisedNotice` relays `[NoticeAcknowledged]`.<br>• The runtime's own comment says the batch holds "the caller's own event (… `ServerOnly` is the opt-out) plus anything its handlers raised" (`MakeRemoteDelegateRequest.cs:166-171`).<br>• The page's own `:409` states it correctly: `ServerOnly` suppresses relay of the caller's own event.<br>• Design's remarks repeat the error ("suppress the relay entirely … the client receives an empty batch", `FactoryEventRelayPattern.cs:229-230`).<br>• No test covers it either way: `ClientRaise_ServerOnly_RelayInvokedWithEmptyBatch` raises an event with no handler.<br>• The sentence predates 011.<br>• Confidence High (code read).<br>• Reachable by: a client that raises `ClientRaisedNotice` (or any event whose handler raises its own) with `ServerOnly` and expects no relay. Its `IFactoryEventRelay` receives the handler's event. | AC-4 · Must (plan Acceptance 2) | **Amend** (fixed): page and Design remarks say ServerOnly keeps only the raised event out; a handler's own raise still relays (`[NoticeAcknowledged]`) |
| 2 | **"The only exception: if batch deserialization throws `UnknownFactoryEventTypeException`, `Relay` is not invoked." A failed round trip is a second exception, and any deserialization throw aborts the batch, not only that one.**<br>• `factory-events.md:368`. The "exactly once per `[Remote]` call" statements at `:316` and `:466` say the same.<br>• `DispatchRelay` runs inside the `try`, after the round trip succeeds (`MakeRemoteDelegateRequest.cs:125`, `:176`). A server handler exception, an HTTP error or a cancellation rethrows before it, so `Relay` is never called. The page says so itself at `:448`.<br>• The deserialization `catch (Exception ex)` aborts on any exception (`:236-240`).<br>• This is older text.<br>• Confidence High.<br>• Reachable by: a consumer that uses the empty-batch call as its "a factory call just returned" signal, which the `events-relay` embed recommends (`FactoryEventRelayPattern.cs:298`). It never gets that signal for a call that throws. One option: "exactly once per successful round trip". | AC-4 · Must (plan Acceptance 2) | **Amend** (fixed): "per successful round trip" at all three sites; a thrown call and any batch deserialization failure produce no Relay call |
| 3 | **"A `[Service]` the handler shares with the factory method is the same instance." That is true only for scoped and singleton services.**<br>• `factory-events.md:99`.<br>• The generated handler resolves each `[Service]` with `sp.GetRequiredService<T>()` from the caller's provider on every dispatch (`RelayHandlerRenderer.cs:227`). A transient registration gives the handler a new instance.<br>• `:24` says the same of a `DbContext`, which `AddDbContext` registers as scoped, so that line holds.<br>• This is older text in substance (the old `:24` and the old handler's comment); 011 restated it as a general rule.<br>• Confidence Medium.<br>• Reachable by: a consumer whose unit of work or connection wrapper is registered transient, who reads `:99` and expects the handler's writes to join the operation's transaction. | AC-4 · Must (plan Acceptance 2) | **Amend** (fixed): scoped or singleton is the same instance, transient is new, at both sites; the shared-scope cause restated (dispatch on the method's provider; theoretical item taken) |
| 4 | **C-19's 011 outcome says the page "embeds interface-first CheckoutOrder (ICheckoutOrder)". The page shows neither the class declaration nor `ICheckoutOrder`.**<br>• `audit-ledger.md:29`. The page embeds two of `CheckoutOrder`'s members (`events-raise`, `events-serveronly`). `ICheckoutOrder` and `internal partial class CheckoutOrder : ICheckoutOrder` (`FactoryEventRelayPattern.cs:106-127`) are in no region, and `grep` finds neither name on the page.<br>• What the outcome can truthfully record: the old site the audit names (`docs/factory-events.md:38`, an internal class with no interface) is gone, and no embed declares a public concrete factory type.<br>• `events-relay` does show `public sealed class InMemoryAggregatorRelay`. It is a consumer service registered as `IFactoryEventRelay`, not a factory type, so it is arguably outside C-19, but the outcome does not say so.<br>• Confidence Medium.<br>• Reachable by: the close-out AC-4 trace, which reads each outcome as the record of what fixed the finding's sites. | AC-4 · Must (C-19) | **Amend** (fixed): C-19's 011 outcome names what the page shows |

## Could not verify

- **The probe's source.** `ZzProbe.cs` was deleted, so the probe's shapes are known only from the class names and diagnostics. The CS0111 and NF0501 claims are confirmed by the generator trace above, independent of the probe.
- **The generated `.g.cs` files** are build output (16:09, the logged Design build's). I read them as that build's output and compiled nothing.

## Theoretical (not triaged)

- **The Complete Example's closing line, "runs the whole flow".** It overstates the test: step 3, a server handler, has no counterpart for `OrderCheckoutCompleted`. The Amendment records the gap, and the step's wording ("like `OrderNotifyHandlers`") does not claim one.
- **`events-phase-handlers` opens after `InvoiceBalanceGuard`'s doc comment** (`FactoryEventPhasesPattern.cs:160`). The Immediate handler therefore renders without its comment, while the other two carry theirs.
- **`events-client-raise` uses the harness idiom `client.ServiceProvider.GetRequiredService<IFactoryEvents>()`.** A Blazor reader would inject it instead.
- **NF0501 needs a static method on the class** (`hasStaticCandidate`). A non-static class whose only handler is an instance `async void` gets no diagnostic at all. The WRONG block is a static class, so the page's claim holds.
- **The anti-pattern's "Why it matters" (`:514`) gives the wrong cause.** It says handlers share the method's scope because the event "is captured by the same request-scoped `IFactoryEventCollector`". They share it because dispatch runs on the caller's provider, and Logical mode has no collector. This is older text.
- **`:42` says "never" and `:490` says "should not"** for a `[Create]` that raises.

## Read report

- **Read beyond the brief:**
  - `reviews/011-test-review.md` (it appeared in the working tree during the review), so its findings 1 and 2 are not repeated here
  - `Renderer/RelayHandlerRenderer.cs`, `Renderer/StaticFactoryRenderer.cs:100-145` and `DiagnosticDescriptors.cs` (NF0501–NF0505 severities)
  - `NF05xxFactoryEventHandlerTests.cs` (grep)
  - `FactoryEventPhaseCoordinator.cs`, `FactoryEventPhaseScheduler.cs` (grep), `FactoryEventCollector.cs`, `HandleRemoteDelegateRequest.cs:205-225`, `RemoteResponse.cs`, `RelayedFactoryEvent.cs`, `IFactoryEventRelay.cs`, `NeatooRuntime.cs` and `FactoryEventBase.cs`
  - Generated `CheckoutOrderFactory.g.cs` (grep)
  - `ClientRaiseRelayTests.cs:78-105`, `FactoryEventHandlerTests.cs` (grep) and `CLAUDE-DESIGN.md` (ServerOnly grep)
  - Every inbound `factory-events.md#` link in the repository outside `docs/todos`
- **Named but unused:**
  - `.claude/rules/design-snippets.md` beyond its region-uniqueness, embed and WRONG-block rules
  - `docs/VisionAlignment.md` beyond D3, D14, R-E and R-M
