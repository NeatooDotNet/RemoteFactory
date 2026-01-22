using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.InterfaceFactory;

#region Test Target Classes

/// <summary>
/// Interface factory for remote execution of methods.
/// The [Factory] attribute indicates this interface should be generated as a factory
/// with remote execution capabilities.
/// </summary>
[Factory]
public interface IExecuteMethods
{
    Task<bool> BoolMethod(bool a, string b);
    Task<List<string>> StringListMethod(List<string> a, int b);
}

/// <summary>
/// Implementation of IExecuteMethods that only exists on the server side.
/// The implementation can have dependencies that are only available on the server.
/// </summary>
public class ExecuteMethods : IExecuteMethods
{
    private readonly IServerOnlyService serverOnlyService;

    public ExecuteMethods(IServerOnlyService serverOnlyService)
    {
        this.serverOnlyService = serverOnlyService;
    }

    public Task<bool> BoolMethod(bool a, string b)
    {
        Assert.NotNull(serverOnlyService);
        return Task.FromResult(a);
    }

    public Task<List<string>> StringListMethod(List<string> a, int b)
    {
        Assert.NotNull(serverOnlyService);
        return Task.FromResult(a);
    }
}

#endregion

/// <summary>
/// Tests for interface factories - interfaces decorated with [Factory] that
/// generate proxy implementations for remote execution.
/// </summary>
public class InterfaceFactoryTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IExecuteMethods _factory;

    public InterfaceFactoryTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes(
            configureServer: services =>
            {
                services.AddScoped<IExecuteMethods, ExecuteMethods>();
            });

        _clientScope = client;
        _serverScope = server;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IExecuteMethods>();
    }

    [Fact]
    public async Task InterfaceFactory_BoolMethod_ReturnsCorrectValue()
    {
        var result = await _factory.BoolMethod(true, "Keith");

        Assert.True(result);
    }

    [Fact]
    public async Task InterfaceFactory_BoolMethod_WithFalse_ReturnsCorrectValue()
    {
        var result = await _factory.BoolMethod(false, "Test");

        Assert.False(result);
    }

    [Fact]
    public async Task InterfaceFactory_StringListMethod_ReturnsSameList()
    {
        var inputList = new List<string> { "Keith", "Neatoo" };

        var result = await _factory.StringListMethod(inputList, 42);

        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains("Keith", result);
        Assert.Contains("Neatoo", result);
    }

    [Fact]
    public async Task InterfaceFactory_StringListMethod_WithEmptyList()
    {
        var inputList = new List<string>();

        var result = await _factory.StringListMethod(inputList, 0);

        Assert.NotNull(result);
        Assert.Empty(result);
    }
}
