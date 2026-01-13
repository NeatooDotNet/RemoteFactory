/// <summary>
/// Code samples for docs/reference/factory-modes.md
///
/// Snippets in this file:
/// - docs:reference/factory-modes:remote-configuration
/// - docs:reference/factory-modes:http-client-configuration
/// </summary>

using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.Samples.DomainModel.FactoryOperations;
using System.Net.Http.Headers;

namespace RemoteFactory.Samples.BlazorClient;

/// <summary>
/// HTTP client configuration examples for Remote mode.
/// </summary>
public static class HttpClientConfigSamples
{
    public static void ConfigureRemoteMode(IServiceCollection services)
    {
#region docs:reference/factory-modes:remote-configuration
services.AddNeatooRemoteFactory(NeatooFactory.Remote, typeof(IPersonModel).Assembly);

// Must also configure HTTP client
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://your-server.com/") };
});
#endregion
    }

    public static void ConfigureHttpClientExamples(IServiceCollection services)
    {
#region docs:reference/factory-modes:http-client-configuration
// Basic configuration
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    return new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
});

// With authentication
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    var tokenProvider = sp.GetRequiredService<ITokenProvider>();
    var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
    client.DefaultRequestHeaders.Authorization =
        new AuthenticationHeaderValue("Bearer", tokenProvider.GetToken());
    return client;
});

// With custom headers
services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
{
    var client = new HttpClient { BaseAddress = new Uri("https://api.example.com/") };
    client.DefaultRequestHeaders.Add("X-Api-Key", "your-api-key");
    client.DefaultRequestHeaders.Add("X-Client-Version", "1.0.0");
    return client;
});
#endregion
    }
}

/// <summary>
/// Placeholder interface for token provider example.
/// </summary>
public interface ITokenProvider
{
    string GetToken();
}
