using EmployeeManagement.Client;
using EmployeeManagement.Domain.Aggregates;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

#region getting-started-client-program
// Register RemoteFactory for client mode with domain assembly
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Register HttpClient for remote calls to server
// In hosted WASM mode, HostEnvironment.BaseAddress targets the server that hosts the client
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });

// Optional: Register IName -> Name pairs (auth services, etc.) on the client.
// Enables factory Can* methods and Create to run locally without a server round-trip.
// If omitted, these methods will fall back to remote calls.
builder.Services.RegisterMatchingName(typeof(Employee).Assembly);
#endregion

await builder.Build().RunAsync();
