// =============================================================================
// DESIGN SOURCE OF TRUTH: Interface Factory Tests
// =============================================================================
//
// Tests demonstrating the INTERFACE FACTORY pattern.
// Interface factories create remote proxies for service interfaces.
//
// =============================================================================

using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for INTERFACE FACTORY pattern (IExampleRepository).
/// </summary>
public class InterfaceFactoryTests
{
    /// <summary>
    /// Verifies interface factory methods work through remote proxy.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Interface factories are always remote
    ///
    /// When you resolve IExampleRepository from the client container,
    /// you get a proxy that serializes calls to the server where the
    /// actual ExampleRepository implementation runs.
    ///
    /// This is different from Class Factories where you get the actual
    /// entity back. Interface factories return the interface itself.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_GetAllAsync_ReturnsDataFromServer()
    {
        // Arrange - register the server implementation
        var (client, server, _) = DesignClientServerContainers.Scopes(
            configureServer: services =>
            {
                services.AddScoped<IExampleRepository, ExampleRepository>();
            });

        // Get the interface from client - this is the proxy
        var repository = client.GetRequiredService<IExampleRepository>();

        // Act - call goes through proxy to server
        var result = await repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Contains(result, x => x.Name == "Item 1");
        Assert.Contains(result, x => x.Name == "Item 2");

        client.Dispose();
        server.Dispose();
    }

    /// <summary>
    /// Verifies interface factory method with parameters.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_GetByIdAsync_ReturnsSpecificItem()
    {
        // Arrange
        var (client, server, _) = DesignClientServerContainers.Scopes(
            configureServer: services =>
            {
                services.AddScoped<IExampleRepository, ExampleRepository>();
            });

        var repository = client.GetRequiredService<IExampleRepository>();

        // Act
        var result = await repository.GetByIdAsync(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Item 42", result.Name);

        client.Dispose();
        server.Dispose();
    }

    /// <summary>
    /// Verifies interface factory returns null correctly.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Nullable return types work through serialization
    ///
    /// The proxy correctly handles null responses from the server.
    /// This is important for repository patterns where not-found
    /// is a valid result.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_GetByIdAsync_HandlesNullResult()
    {
        // Arrange - custom implementation that returns null
        var (client, server, _) = DesignClientServerContainers.Scopes(
            configureServer: services =>
            {
                services.AddScoped<IExampleRepository, NullReturningRepository>();
            });

        var repository = client.GetRequiredService<IExampleRepository>();

        // Act
        var result = await repository.GetByIdAsync(999);

        // Assert
        Assert.Null(result);

        client.Dispose();
        server.Dispose();
    }

    /// <summary>
    /// Verifies interface factory works in local mode.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();

        // In local mode, register the implementation directly
        // (normally done in ConfigureContainer, but showing explicit for clarity)
        var localServices = new ServiceCollection();
        localServices.AddScoped<IExampleRepository, ExampleRepository>();
        var localProvider = localServices.BuildServiceProvider();

        var repository = localProvider.GetRequiredService<IExampleRepository>();

        // Act - direct call, no proxy
        var result = await repository.GetAllAsync();

        // Assert
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);

        await localProvider.DisposeAsync();
        local.Dispose();
    }
}

/// <summary>
/// Repository implementation that returns null for testing.
/// </summary>
internal class NullReturningRepository : IExampleRepository
{
    public Task<IReadOnlyList<ExampleDto>> GetAllAsync()
    {
        return Task.FromResult<IReadOnlyList<ExampleDto>>([]);
    }

    public Task<ExampleDto?> GetByIdAsync(int id)
    {
        return Task.FromResult<ExampleDto?>(null);
    }
}
