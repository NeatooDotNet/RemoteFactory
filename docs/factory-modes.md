# Factory Modes

Your domain assembly doesn't change between client, server and test projects: the same `Order` class is loaded everywhere. The mode tells RemoteFactory what that assembly's factories do in the process that registers them.

## Runtime Modes

The mode is set when you register RemoteFactory in DI:

| Mode | What a factory call does | Use for |
|------|--------------------------|---------|
| **Server** | Runs every operation here, and handles the `[Remote]` calls clients send | ASP.NET Core server |
| **Remote** | Serializes each `[Remote]` operation and sends it to the server over HTTP; runs every other operation here | Client apps (Blazor, MAUI, WPF, console) |
| **Logical** | Runs every operation here, `[Remote]` ones included, with nothing serialized | Tests, console tools, single-tier apps |

### Server

`AddNeatooAspNetCore` registers the factories in Server mode, and `UseNeatoo()` maps the endpoint Remote clients post to:

<!-- snippet: readme-server-setup -->
<a id='snippet-readme-server-setup'></a>
```cs
services.AddNeatooAspNetCore(typeof(IOrder).Assembly);
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L72-L74' title='Snippet source file'>snippet source</a> | <a href='#snippet-readme-server-setup' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

See [ASP.NET Core Integration](aspnetcore-integration.md) for the endpoint and its middleware.

### Remote

The client registers the factories in Remote mode, with a keyed `HttpClient` pointed at the server:

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
<sup><a href='/src/Design/Design.Client.Blazor/Program.cs#L64-L72' title='Snippet source file'>snippet source</a> | <a href='#snippet-getting-started-client-program' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

`[Remote]` marks the operations that cross. An operation without it, such as a local `Create` or a `Can*` check, runs on the client. The `[Service]` parameters it takes must therefore be registered in the client's container.

### Logical

Everything runs in one process: no HTTP, no serialization. The services a server would supply are registered in the same container:

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

For tests that do need the round trip, the client/server containers serialize each `[Remote]` call; see [ASP.NET Core Integration — Testing](aspnetcore-integration.md#testing).

## Typical Solution Structure

```
MySolution/
├── MySolution.Domain/    # Shared domain assembly — referenced by both server and client
├── MySolution.Server/    # Server runtime — AddNeatooAspNetCore()
└── MySolution.Client/    # Remote runtime — AddNeatooRemoteFactory(NeatooFactory.Remote, ...)
```

## Debugging

To see where a call ran, log at `Information`:

- **The client's remote calls:** `[{CorrelationId}] Remote delegate call started: {DelegateType}`, under `Neatoo.RemoteFactory.Internal.MakeRemoteDelegateRequest`.
- **The server's handling:** `[{CorrelationId}] Handling remote request for delegate {DelegateType}`, under `Neatoo.RemoteFactory.Server`.
- **Each class factory operation, on the tier that runs it:** `[{CorrelationId}] Factory operation {Operation} started for {TypeName}`, under the generated factory's type name.

Design.Server's filters:

<!-- snippet: aspnetcore-logging -->
<a id='snippet-aspnetcore-logging'></a>
```cs
services.AddLogging(logging => logging
    .AddFilter("Neatoo.RemoteFactory", LogLevel.Information)
    .AddFilter("RemoteFactory.Trace", LogLevel.Warning)   // Trace: every request's (de)serialization
    .AddFilter("Design", LogLevel.Information));          // the generated class factories
```
<sup><a href='/src/Design/Design.Server/ServerServices.cs#L96-L101' title='Snippet source file'>snippet source</a> | <a href='#snippet-aspnetcore-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

A call that logs a client remote call and then a server "Handling" entry crossed; an operation that logs only a factory entry on the client ran there.

## Next Steps

- [Getting Started](getting-started.md): configure modes in a new solution
- [Client-Server Architecture](client-server-architecture.md): how `[Remote]` controls the boundary
- [IL Trimming](trimming.md): remove server-only code from published client bundles
- [Serialization](serialization.md): Ordinal and Named formats
- [ASP.NET Core Integration](aspnetcore-integration.md): server configuration details
