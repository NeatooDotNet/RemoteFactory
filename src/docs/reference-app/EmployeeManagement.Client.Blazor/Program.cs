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

#region client-configuration
// Configure RemoteFactory for Remote (client) mode
var domainAssembly = typeof(Employee).Assembly;

builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    domainAssembly);

// Register the keyed HttpClient for RemoteFactory remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri(serverBaseAddress) };
});
#endregion

await builder.Build().RunAsync();
