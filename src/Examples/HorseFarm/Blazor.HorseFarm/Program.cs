using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Blazor.HorseFarm;
using Neatoo.RemoteFactory;
using HorseFarm.DomainModel;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

builder.Services.AddBlazorBootstrap();
builder.Services.AddScoped(sp => new HttpClient { BaseAddress = new Uri(builder.HostEnvironment.BaseAddress) });


// Incorporate Neatoo RemoteFactory
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IHorseFarmFactory).Assembly);
builder.Services.AddKeyedScoped(Neatoo.RemoteFactory.RemoteFactoryServices.HttpClientKey, (sp, key) => new HttpClient { BaseAddress = new Uri("http://localhost:5129/") });


await builder.Build().RunAsync();
