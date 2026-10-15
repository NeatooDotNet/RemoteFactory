# Static Factory Pattern

Use for stateless commands. No instance state — pure functions with side effects via services.

## Execute Commands (Request-Response)

Use `[Execute]` for operations that return a result. The client awaits the response. `[Execute]` obeys `[Remote]` like every other operation: with it the call crosses to the server; without it the delegate runs on whichever tier resolves it, with that tier's services — the shape for client-side computation that must not round-trip.

> **Note:** `[Execute]` also works on non-static `[Factory]` classes when the operation returns the containing type. See `references/class-factory.md` for that pattern.

<!-- snippet: ordercommands-declaration -->
<a id='snippet-ordercommands-declaration'></a>
```cs
[Factory]
public static partial class OrderCommands
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L47-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-ordercommands-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: operations-execute -->
<a id='snippet-operations-execute'></a>
```cs
[Remote, Execute]
private static async Task<SubmitOutcome> _SubmitOrder(
    int orderId,
    [Service] IOrderFactory orderFactory,
    CancellationToken cancellationToken)
{
    var order = await orderFactory.Fetch(orderId, cancellationToken);
    if (order == null)
        return SubmitOutcome.NotFound;
    if (!order.CanSubmit)
        return SubmitOutcome.NotSubmittable;

    order.Submit();
    await orderFactory.Save(order, cancellationToken);
    return SubmitOutcome.Submitted;
}
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L60-L77' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: operations-execute-command -->
<a id='snippet-operations-execute-command'></a>
```cs
[Remote, Execute]
private static async Task _CancelOrder(int orderId, [Service] IOrderFactory orderFactory)
{
    var order = await orderFactory.Fetch(orderId)
        ?? throw new KeyNotFoundException($"Order {orderId} not found");
    if (order.Status == OrderStatus.Cancelled)
        return;   // already cancelled: a retried call is not an error

    order.Cancel();
    await orderFactory.Save(order);
}
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L82-L94' title='Snippet source file'>snippet source</a> | <a href='#snippet-operations-execute-command' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

**Generates**:
- the `OrderCommands.SubmitOrder(orderId)` delegate type, returning `SubmitOutcome`
- the `OrderCommands.CancelOrder(orderId)` delegate type; each also takes an optional `CancellationToken`, and is resolved from DI

**Usage**:
```csharp
var success = await SkillEmployeeCommands.SendNotification("admin@example.com", "Hello!");
var summary = await SkillEmployeeCommands.GetEmployeeSummary(employeeId);
```

> **Looking for domain events?** See `references/factory-events.md` for `IFactoryEvents.Raise`, the `[FactoryEventHandler<T>]` class attribute, `RaiseOptions.ServerOnly`, and `IFactoryEventRelay`. That is RemoteFactory's only event-shaped abstraction; the former `[Event]` method attribute was removed in v1.5.0.

---

## Critical Rules

### Methods must be `private static` with underscore prefix

```csharp
// WRONG - conflicts with generated code
[Remote, Execute]
public static Task<bool> SendNotification(...) { }

// RIGHT - private with underscore
[Remote, Execute]
private static Task<bool> _SendNotification(...) { }
```

The generator creates the public method. Your code provides the private implementation.

`private static` also matters for trimming: with `[Remote]`, the generated local registration is guarded by `NeatooRuntime.IsServerRuntime`, so on a Blazor WASM client published with the feature switch set to `false`, the method body, its `[Service]` dependencies, and their transitive references are removed. Without `[Remote]` there is no guard — the body ships to the client and runs there, which is the point of a bare `[Execute]`. Static factories are exempt from the NF0105 `[Remote] public` check; `[Remote]` alone decides placement. See `references/trimming.md`, which documents what each factory shape does and does not remove.

### Bare [Execute] runs where it is called

Leave `[Remote]` off when the command must run on the tier that calls it — a client-side engine, a scoring rule, anything that must not round-trip. The generator emits one unguarded local delegate in every mode and no remote delegate; `[Service]` parameters resolve from that tier's container, so the dependency must be registered there (a bare command that takes a server-only repository compiles, then fails on the client at call time).

<!-- snippet: attributes-execute-local -->
<a id='snippet-attributes-execute-local'></a>
```cs
[Execute]
private static Task<int> _ScoreText(string text, [Service] ITextScorer scorer)
{
    return Task.FromResult(scorer.Score(text));
}
```
<sup><a href='/src/Design/Design.Domain/FactoryPatterns/AllPatterns.cs#L507-L513' title='Snippet source file'>snippet source</a> | <a href='#snippet-attributes-execute-local' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`ITextScorer` is the example's pure-computation scoring service. The client registers it, so the bare command runs there with no server call.

### [Execute] returns `Task` or `Task<T>`, never `void`

```csharp
// WRONG - NF0102
[Remote, Execute]
private static void _DoSomething(...) { }

// RIGHT - no result: the await confirms completion and rethrows a server exception
[Remote, Execute]
private static Task _DoSomething(...) { ... }

// RIGHT - the caller needs the value
[Remote, Execute]
private static Task<Summary> _Summarize(...) { ... }
```

A `Task`-returning `[Execute]` generates a `Task` delegate and round-trips with no result. Until the #113 fix it generated a `Task<Task>` delegate that did not compile.

### Static classes must be `partial`

```csharp
// WRONG - missing partial
[Factory]
public static class Commands { }

// RIGHT
[Factory]
public static partial class Commands { }
```

---

## When to Use Static Factory

- **Stateless commands** — No instance state needed
- **Request-response operations** — Clean function-style API
- **Cross-cutting operations** — Notifications, auditing, logging (invoked via `[Execute]` request-response)

For fire-and-forget work (email, webhooks, queue publishes), call `Task.Run` directly inside the factory method with a fresh scope from `IServiceScopeFactory.CreateScope()`. For transactional domain events, use `IFactoryEvents.Raise` + `[FactoryEventHandler<T>]` — see `references/factory-events.md`.
