# Factory Modes

Your domain assembly doesn't change between client, server, and test projects — the same `Employee` class runs everywhere. Modes tell RemoteFactory how to behave when that assembly is loaded in different contexts: serialize and send to a server, execute locally, or handle incoming requests.

## Runtime Modes

The runtime mode is set when you register RemoteFactory in DI. It controls how factory calls execute:

| Mode | What Happens When You Call a Factory Method | Use For |
|------|---------------------------------------------|---------|
| **Server** | Executes locally + handles incoming remote calls | ASP.NET Core server |
| **Remote** | Serializes the call and sends it to the server via HTTP | Client apps (Blazor, MAUI, WPF, console) |
| **Logical** | Executes locally, no HTTP at all | Tests, console apps, single-tier apps |

### Server

The server registers factories that execute locally and handles incoming HTTP requests from Remote clients:

<!-- snippet: modes-server-config -->
<a id='snippet-modes-server-config'></a>
```cs
// Server mode: handles incoming HTTP + local execution
// services.AddNeatooAspNetCore(options, domainAssembly);
// app.UseNeatoo();  // Maps /api/neatoo endpoint
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L30-L34' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-server-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Remote

The client registers factories that serialize calls and POST them to `/api/neatoo` on the server:

<!-- snippet: modes-remote-config -->
<a id='snippet-modes-remote-config'></a>
```cs
// Remote mode: all operations serialize and POST to server
// services.AddNeatooRemoteFactory(NeatooFactory.Remote, options, domainAssembly);
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri(serverUrl) });
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L18-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remote-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

### Logical

Everything executes locally in a single process — no HTTP, no serialization. Useful for tests, console apps, background services, and prototyping:

<!-- snippet: modes-logical-config -->
<a id='snippet-modes-logical-config'></a>
```cs
// Logical mode: local execution, no HTTP - ideal for testing
// services.AddNeatooRemoteFactory(NeatooFactory.Logical, options, domainAssembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L13-L16' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logical-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

For testing patterns using Logical mode and the Client/Server Container approach, see [Service Injection — Client/Server Container Testing](service-injection.md#clientserver-container-testing).

## Compile-Time Modes (Advanced)

Most developers can ignore compile-time modes initially — the default (Full) works everywhere. Compile-time modes are an optimization for client assemblies.

The compile-time mode controls what code the source generator produces. Set via `[assembly: FactoryMode(...)]`:

| Mode | What Gets Generated | When to Use |
|------|---------------------|-------------|
| **Full** (default) | Local execution code + remote request handlers | Server, tests, single-tier apps |
| **RemoteOnly** | HTTP stubs only — no local implementation | Client assemblies that only need to call the server |

<!-- snippet: modes-full-config -->
<a id='snippet-modes-full-config'></a>
```cs
// Full mode (default): no assembly attribute needed
// services.AddNeatooRemoteFactory(NeatooFactory.Server, options, domainAssembly);
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L8-L11' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-full-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

<!-- snippet: modes-remoteonly-config -->
<a id='snippet-modes-remoteonly-config'></a>
```cs
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
// Generates HTTP stubs only - smaller client assemblies
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Domain/Samples/Modes/FactoryModeAttributes.cs#L25-L28' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-remoteonly-config' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

RemoteOnly produces smaller assemblies (~120 KB vs ~450 KB for Full) because it excludes local method implementations and server dependencies. This matters for Blazor WASM where bundle size affects load time.

## Typical Solution Structure

```
MySolution/
├── MySolution.Domain/    # Full (default) — no assembly attribute needed
├── MySolution.Server/    # Server runtime — AddNeatooAspNetCore()
└── MySolution.Client/    # Remote runtime — AddNeatooRemoteFactory(NeatooFactory.Remote, ...)
                          # Optional: [assembly: FactoryMode(RemoteOnly)] for smaller bundle
```

## Debugging

<!-- snippet: modes-logging -->
<a id='snippet-modes-logging'></a>
```cs
// Enable verbose logging to trace factory execution
// services.AddLogging(b => b.AddFilter("Neatoo.RemoteFactory", LogLevel.Trace));
// Logs: "Executing local factory method..." or "Sending remote factory request..."
```
<sup><a href='/src/docs/reference-app/EmployeeManagement.Infrastructure/Samples/FactoryModes/FactoryModeConfigurationSamples.cs#L19-L23' title='Snippet source file'>snippet source</a> | <a href='#snippet-modes-logging' title='Start of snippet'>anchor</a></sup>
<!-- endSnippet -->

## Next Steps

- [Getting Started](getting-started.md) — Configure modes in a new solution
- [Client-Server Architecture](client-server-architecture.md) — How [Remote] controls the boundary
- [Serialization](serialization.md) — Ordinal vs Named formats
- [ASP.NET Core Integration](aspnetcore-integration.md) — Server configuration details
