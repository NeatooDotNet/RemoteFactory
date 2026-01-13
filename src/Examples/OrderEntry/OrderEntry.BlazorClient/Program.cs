using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using MudBlazor.Services;
using Neatoo.RemoteFactory;
using OrderEntry.BlazorClient;
using OrderEntry.Domain;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });
builder.Services.AddMudServices();

// Register Neatoo RemoteFactory with RemoteOnly factories from Domain.Client
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IOrder).Assembly);
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("http://localhost:5184/") };
});

await builder.Build().RunAsync();
