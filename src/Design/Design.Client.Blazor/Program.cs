// =============================================================================
// DESIGN SOURCE OF TRUTH: Blazor Client Configuration (Hosted WASM)
// =============================================================================
//
// Demonstrates RemoteFactory client-side setup with Blazor WebAssembly.
// In hosted mode, the server serves the client, so they share the same origin.
//
// DESIGN DECISION: Client setup is minimal
//
// The client only needs:
// 1. AddNeatooRemoteFactory() - registers factory proxies
// 2. HttpClient using HostEnvironment.BaseAddress - for making remote calls
// 3. Client-safe services for [Execute] commands that run on the client
//
// The registration below is embedded in README and getting started (region
// getting-started-client-program); the reasons are kept here so it stays short.
//
// DESIGN DECISION: AddNeatooRemoteFactory registers client-side proxies
//
// Parameters:
// - NeatooFactory.Remote: [Remote] operations are serialized and sent to the
//   server; everything else runs here, on the client
// - typeof(IOrder).Assembly: the assembly whose generated factories to register
//
// What this does:
// - Registers the generated factories, with each [Remote] operation wired to
//   the HTTP call
// - Does NOT register server-only services (they're not needed on client)
//
// COMMON MISTAKE: Using NeatooFactory.Logical on client
//
// WRONG:
// builder.Services.AddNeatooRemoteFactory(NeatooFactory.Logical, ...);
// // This makes all calls local - no server round-trip!
//
// RIGHT:
// builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, ...);
//
// DESIGN DECISION: Keyed HttpClient for RemoteFactory
//
// RemoteFactory uses a keyed service to find the HttpClient that points
// to the server. The key is RemoteFactoryServices.HttpClientKey.
//
// This allows the app to have multiple HttpClients for different purposes
// while RemoteFactory always uses the correct one.
//
// In hosted WASM mode, use builder.HostEnvironment.BaseAddress so the
// client automatically targets the server that hosts it -- no hardcoded
// URL needed.
//
// =============================================================================

using Design.Client.Blazor;
using Design.Domain.Aggregates;
using Design.Domain.FactoryPatterns;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#region getting-started-client-program
// The factory proxies: calls marked [Remote] go to the server
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IOrder).Assembly);

// The HttpClient RemoteFactory sends them with: the server that hosts this client
builder.Services.AddKeyedScoped(
    RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
#endregion

// -------------------------------------------------------------------------
// DESIGN DECISION: Register the client-safe services the client actually runs
//
// An [Execute] without [Remote] runs on the client and resolves its [Service]
// parameters from THIS container. ITextScorer backs ExampleCommands.ScoreText
// and ClassExecuteDemo.ScoreLocally; it is pure computation, so it belongs
// here. Server-side services (IExampleService, INotificationService) stay out
// -- a bare [Execute] that took one would fail at call time on the client.
// -------------------------------------------------------------------------
builder.Services.AddScoped<ITextScorer, TextScorer>();

// Standard Blazor HttpClient for other purposes
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

await builder.Build().RunAsync();
