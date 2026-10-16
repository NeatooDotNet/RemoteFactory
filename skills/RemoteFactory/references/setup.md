# Server and Client Setup

## Packages

The server references `Neatoo.RemoteFactory` and `Neatoo.RemoteFactory.AspNetCore`; the Blazor WebAssembly client references `Neatoo.RemoteFactory`. Both target .NET 9.0 and .NET 10.0. The domain assembly, with its `[Factory]` types, is referenced by both.

## Server (ASP.NET Core)

<!-- snippet: getting-started-server-program -->
<a id='snippet-getting-started-server-program'></a>
```cs
var builder = WebApplication.CreateBuilder(args);

// RemoteFactory plus the server-only services [Service] parameters resolve (ServerServices.cs)
builder.Services.AddDesignServer();

var app = builder.Build();

// Serve the hosted Blazor WASM client
app.UseBlazorFrameworkFiles();
app.UseStaticFiles();

// Sign the caller in before the endpoint runs: the authorization checks read HttpContext.User
app.UseAuthentication();
app.UseAuthorization();

// The RemoteFactory endpoint: one route for every factory call, no controllers
app.UseNeatoo();

app.MapFallbackToFile("index.html");

await app.RunAsync();
```
<sup><a href='/src/Design/Design.Server/Program.cs#L61-L83' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-server-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`AddDesignServer()` is the example's own composition method. Its RemoteFactory registration is:

<!-- snippet: readme-server-setup -->
<a id='snippet-readme-server-setup'></a>
```cs
services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L74-L76' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-server-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`AddNeatooAspNetCore` registers in `NeatooFactory.Server` mode; `UseNeatoo()` maps the one endpoint every factory call goes through. The server's other registrations, the services its `[Service]` parameters take, are in [service-injection.md](service-injection.md#registration-on-each-tier).

**Several domain assemblies.** Pass every assembly that declares `[Factory]` types to the one `AddNeatooAspNetCore` call, and the same list to the client's `AddNeatooRemoteFactory`.

**CORS** is needed only when the client and the server are on different origins. Then add a policy that allows the client's origin with `POST` and the `Content-Type` and `X-Correlation-Id` headers, and call `app.UseCors()` before `app.UseNeatoo()`. A hosted Blazor client shares the server's origin and needs none.

## Client (Blazor WebAssembly)

<!-- snippet: getting-started-client-program -->
<a id='snippet-getting-started-client-program'></a>
```cs
// The factory proxies: calls marked [Remote] go to the server
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IOrder).Assembly);

// The HttpClient RemoteFactory sends them with: the server that hosts this client
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
```
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L65-L73' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

The client also registers the services code running on it resolves, and nothing server-only:

<!-- snippet: skill-client-services -->
<a id='snippet-skill-client-services'></a>
```cs
builder.Services.AddScoped<ITextScorer, TextScorer>();
```
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L84-L86' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-client-services' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Factory modes

The client registers in `NeatooFactory.Remote` mode: a call the client makes to a `[Remote]` operation, or to one whose authorization runs on the server (`[AspAuthorize]`, or a `[Remote]` authorization method), crosses to the server; any other call runs on the tier that makes it, which here is the client. `[Remote]` marks the crossing, not where the code lives: the same operation called on the server runs there locally, and an `internal` operation without `[Remote]` is server-only. `NeatooFactory.Logical` runs every operation in one process, with nothing serialized, for tests and single-tier applications:

<!-- snippet: modes-logical-config -->
<a id='snippet-modes-logical-config'></a>
```cs
// One process, no HTTP: every operation runs here, [Remote] ones included
services.AddNeatooRemoteFactory(NeatooFactory.Logical, typeof(IOrder).Assembly);

// So the services a server would supply to [Service] parameters are registered here too
services.AddSingleton<IOrderDbContext, InMemoryOrderDbContext>();
services.RegisterMatchingName(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Tests/FactoryTests/LogicalModeTests.cs#L28-L35' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

In Logical mode, register whatever the `[Service]` parameters need; `IOrderDbContext` is the example's in-memory order store.

## Using factories from a page

A page injects the generated factories:

<!-- snippet: skill-blazor-inject -->
<a id='snippet-skill-blazor-inject'></a>
```razor
@inject IOrderFactory OrderFactory
@inject IOrderLineFactory LineFactory
```
<sup><a href='/src/Design/Design.Client.Blazor/Pages/Home.razor#L17-L20' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-blazor-inject' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

It creates locally, fetches across the wire, builds a child on the client, and reassigns what `Save` returns:

<!-- snippet: skill-blazor-usage -->
<a id='snippet-skill-blazor-usage'></a>
```razor
private void CreateOrder()
{
    try
    {
        messages += "Creating order (local, no server call)...\n";
        currentOrder = OrderFactory.Create("Demo Customer");
        messages += $"Created order: IsNew={currentOrder.IsNew}, Status={currentOrder.Status}\n";
    }
    catch (Exception ex)
    {
        messages += $"Error: {ex.Message}\n";
    }
}

private async Task FetchOrder()
{
    try
    {
        messages += $"Fetching order #{InMemoryOrderDbContext.DraftOrderId}...\n";
        currentOrder = await OrderFactory.Fetch(InMemoryOrderDbContext.DraftOrderId);
        messages += currentOrder == null
            ? "Order not found\n"
            : $"Fetched order: Id={currentOrder.Id}, Lines={currentOrder.Lines.Count}\n";
    }
    catch (Exception ex)
    {
        messages += $"Error: {ex.Message}\n";
    }
}

private void AddLine()
{
    try
    {
        // The client builds the line with the line factory, then hands it to the order
        currentOrder!.AddLine(LineFactory.Create("Demo Widget", 12.50m, 1));
        messages += $"Added line: Lines={currentOrder.Lines.Count}\n";
    }
    catch (Exception ex)
    {
        messages += $"Error: {ex.Message}\n";
    }
}

private async Task SaveOrder()
{
    try
    {
        // Save returns a new instance from the server: reassign it
        currentOrder = await OrderFactory.Save(currentOrder!);
        messages += $"Saved order: Id={currentOrder?.Id}, Lines={currentOrder?.Lines.Count}\n";
    }
    catch (Exception ex)
    {
        messages += $"Error: {ex.Message}\n";
    }
}
```
<sup><a href='/src/Design/Design.Client.Blazor/Pages/Home.razor#L179-L237' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-blazor-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A static command is an injected delegate, called like a method:

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

## Generated code

The generated files are under `obj/{Configuration}/{tfm}/generated/Neatoo.Generator/Neatoo.Factory/`: `{Namespace}.{TypeName}Factory.g.cs` holds the factory interface and implementation, and `{Namespace}.{TypeName}.Ordinal.g.cs` the serializer.
