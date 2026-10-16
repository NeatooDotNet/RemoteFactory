# Static Factory

Use for a command: a call that returns and is done, with nothing or a simple result. The class is `static partial` with `[Factory]`; each command is a `private static` method named with a leading underscore, and the generator makes a delegate type the caller resolves from DI. A command that returns the aggregate it works on is a class-level `[Execute]` instead ([class-factory.md](class-factory.md#class-level-execute)).

## The commands

<!-- snippet: ordercommands-declaration -->
<a id='snippet-ordercommands-declaration'></a>
```cs
[Factory]
public static partial class OrderCommands
```
<sup><a href='/src/Design/Design.Domain/Services/OrderCommands.cs#L47-L50' title='Snippet source file'>snippet source</a> | <a href='#snippet-ordercommands-declaration' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A command that changes an aggregate fetches it through its factory, calls its verb, and saves through the root factory; it never writes the row. A missing order and an order that is not a draft are results the caller reads:

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

A command with no result returns `Task`. The await confirms the server ran it, and a server exception rethrows there; a missing target is an exception:

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

**Generates** the delegate types `OrderCommands.SubmitOrder(int orderId, CancellationToken cancellationToken = default)` returning `Task<SubmitOutcome>`, and `OrderCommands.CancelOrder(int orderId, CancellationToken cancellationToken = default)` returning `Task`, each registered in DI. The `[Service]` parameters are the server's and do not appear on the delegate.

## The call

The caller injects the delegate and calls it like a method:

<!-- snippet: skill-command-inject -->
<a id='snippet-skill-command-inject'></a>
```razor
[Inject]
private ExampleCommands.SendNotification SendNotification { get; set; } = default!;
```
<sup><a href='/src/Design/Design.Client.Blazor/Pages/Home.razor#L170-L173' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-command-inject' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: skill-command-call -->
<a id='snippet-skill-command-call'></a>
```razor
private async Task ExecuteCommand()
{
    try
    {
        messages += "Executing SendNotification command...\n";
        commandResult = await SendNotification("demo@example.com", "Hello from Blazor!");
        messages += $"Command returned: {commandResult}\n";
    }
    catch (Exception ex)
    {
        messages += $"Error: {ex.Message}\n";
    }
}
```
<sup><a href='/src/Design/Design.Client.Blazor/Pages/Home.razor#L333-L347' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-command-call' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## `[Execute]` obeys `[Remote]`

With `[Remote]`, the client's call crosses to the server. Without it, the delegate runs on whichever tier resolves it, with that tier's services - the shape for client-side computation that must not round-trip:

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

A bare command's `[Service]` dependencies must be registered on the tier that calls it. The client registers the scorer the bare command needs, and nothing server-only:

<!-- snippet: skill-client-services -->
<a id='snippet-skill-client-services'></a>
```cs
builder.Services.AddScoped<ITextScorer, TextScorer>();
```
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L84-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-client-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A bare command that took a server-only service compiles, then fails on the client when it is called.

## Rules

- **`private static`, with a leading underscore.** The generator makes the public delegate from the name without the underscore; a `public` method of the same name conflicts with it.
- **`Task` or `Task<T>`, never `void`** (NF0102). Return `Task` when there is no result; invent no `bool` that is always `true`.
- **`static partial class`.** A static `[Factory]` class without `partial` is NF0101.
- **Value parameters before `[Service]` parameters.** A `[Service]` before a value breaks the generated code today, with no diagnostic ([#135](https://github.com/NeatooDotNet/RemoteFactory/issues/135)).
- **Trimming.** With `[Remote]`, the command's local registration is guarded by `IsServerRuntime`, so a client published with the switch off drops its body and its `[Service]` dependencies. Without `[Remote]` the body ships to the client and runs there, which is the point. A static method is exempt from NF0105; `[Remote]` alone decides where it runs. [trimming.md](trimming.md).

## Commands, events and fire-and-forget

A command is request and response. A domain event raised inside an operation is `IFactoryEvents.Raise` with a `[FactoryEventHandler<T>]` ([factory-events.md](factory-events.md)). Fire-and-forget work (email, a webhook, a queue) is not a command: call `Task.Run` inside the operation with a new scope from `IServiceScopeFactory`.
