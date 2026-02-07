using EmployeeManagement.Domain.Samples.GettingStarted;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Client.Samples;

// Removed: duplicate snippet moved to Program.cs
public static class ClientProgram
{
    public static async Task ConfigureClient(string[] args, string serverBaseAddress)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);
        builder.Services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            typeof(EmployeeModel).Assembly);
        builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient { BaseAddress = new Uri(serverBaseAddress) });
        await builder.Build().RunAsync();
    }
}
