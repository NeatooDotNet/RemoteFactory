using EmployeeManagement.Domain.Samples.GettingStarted;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Client.Samples;

#region getting-started-client-program
public static class ClientProgram
{
    public static async Task ConfigureClient(string[] args, string serverBaseAddress)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Configure RemoteFactory for Remote (client) mode with domain assembly
        builder.Services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(EmployeeModel).Assembly);

        // Register keyed HttpClient for RemoteFactory remote calls
        builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
        {
            return new HttpClient { BaseAddress = new Uri(serverBaseAddress) };
        });

        await builder.Build().RunAsync();
    }
}
#endregion
