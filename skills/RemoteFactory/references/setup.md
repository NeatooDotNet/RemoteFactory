# Server and Client Setup

## NuGet Packages

```xml
<!-- Server project -->
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
<PackageReference Include="Neatoo.RemoteFactory.AspNetCore" Version="x.y.z" />

<!-- Client project (Blazor WASM) -->
<PackageReference Include="Neatoo.RemoteFactory" Version="x.y.z" />
```

---

## Server Setup (ASP.NET Core)

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

### Multiple Assemblies

Pass every assembly that declares `[Factory]` types to the **one** `AddNeatooAspNetCore` call: `AddNeatooAspNetCore(typeof(IOrder).Assembly, typeof(IInvoice).Assembly)`. A second call replaces the first call's assembly list instead of adding to it. The client's `AddNeatooRemoteFactory` takes the same list.

### CORS Configuration (Non-Hosted Deployments Only)

CORS is only needed when the client and server are on different origins (e.g., standalone Blazor WASM hosted separately from the API). In hosted WASM mode, CORS is unnecessary because both share the same origin. When it is needed, add a policy that allows the client's origin with `POST` and the `Content-Type` and `X-Correlation-Id` headers, and call `app.UseCors()` before `app.UseNeatoo()`.

---

## Client Setup (Blazor WASM)

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

### Factory Modes

The client above registers in `NeatooFactory.Remote` mode: `[Remote]` operations, and operations whose authorization runs on the server (`[AspAuthorize]`, or a `[Remote]` authorization method), go to the server; everything else runs on the client. `NeatooFactory.Logical` runs every operation in one process, with nothing serialized, for tests and single-tier apps:

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

`IOrderDbContext` is the example's order store, an in-memory stand-in for a database: in Logical mode, register whatever your `[Service]` parameters need. The server's `AddNeatooAspNetCore` registers in `NeatooFactory.Server` mode.

---

## Using Factories

### Inject and Use

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
        // The client builds the line with the line factory, then hands it to the order (R-C)
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
<sup><a href='/src/Design/Design.Client.Blazor/Pages/Home.razor#L175-L233' title='Snippet source file'>snippet source</a> | <a href='#snippet-skill-blazor-usage' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Static Factory Commands

```csharp
// No injection needed - static delegates
await SkillEmployeeCommands.SendNotification("admin@example.com", "Employee updated!");

// Fire-and-forget events
_ = SkillEmployeeEvents.OnEmployeeCreatedEvent(employeeId, "John Doe");
```

---

## Framework Support

RemoteFactory supports:
- **.NET 9.0** (STS)
- **.NET 10.0** (LTS)

Both frameworks are included in the NuGet packages.

---

## Generated Code Location

Generated code appears in:
```
obj/Debug/{tfm}/generated/Neatoo.Generator/Neatoo.Factory/
```

Files generated:
- `{Namespace}.{TypeName}Factory.g.cs` - Factory interface and implementation
- `{Namespace}.{TypeName}.Ordinal.g.cs` - Serialization implementation
