/// <summary>
/// Code samples for docs/concepts/three-tier-execution.md
///
/// Snippets in this file:
/// - docs:concepts/three-tier-execution:client-setup
/// </summary>

using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;
using RemoteFactory.Samples.DomainModel.FactoryOperations;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<HeadOutlet>("head::after");

#region docs:concepts/three-tier-execution:client-setup
// Add RemoteFactory in Remote mode
builder.Services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);

// Configure HTTP client for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    // Update this URL to match your server
    return new HttpClient { BaseAddress = new Uri("https://localhost:5001/") };
});
#endregion

await builder.Build().RunAsync();
