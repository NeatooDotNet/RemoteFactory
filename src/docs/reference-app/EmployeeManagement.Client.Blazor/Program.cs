using EmployeeManagement.Client;
using EmployeeManagement.Domain.Aggregates;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure the server base address
var serverBaseAddress = builder.Configuration["ServerUrl"] ?? "http://localhost:5000/";

#region getting-started-client-program
// Register RemoteFactory for client mode with domain assembly
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Register HttpClient for remote calls to server
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
    (sp, key) => new HttpClient { BaseAddress = new Uri(serverBaseAddress) });
#endregion

await builder.Build().RunAsync();
