// =============================================================================
// DESIGN SOURCE OF TRUTH: Interface Factory Tests
// =============================================================================
//
// Tests demonstrating the INTERFACE FACTORY pattern: a domain service the
// client calls. Interface factories create remote proxies for service
// interfaces.
//
// =============================================================================

using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for INTERFACE FACTORY pattern (IProductCatalog).
/// </summary>
/// <remarks>
/// ProductCatalog is registered by RegisterMatchingName on every container
/// (IProductCatalog -> ProductCatalog). On the client the generated proxy is
/// registered first, so the client resolves the proxy.
/// </remarks>
public class InterfaceFactoryTests
{
    /// <summary>
    /// Verifies interface factory methods work through the remote proxy.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Interface factories are always remote
    ///
    /// When you resolve IProductCatalog from the client container, you get a
    /// proxy that serializes calls to the server where the actual
    /// ProductCatalog implementation runs.
    ///
    /// This is different from Class Factories where you get the actual
    /// entity back. Interface factories return the interface itself.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_SearchAsync_ReturnsDataFromServer()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();

        #region interface-factory-usage
        // Get the interface from client - this is the proxy
        var catalog = client.GetRequiredService<IProductCatalog>();

        // Act - call goes through proxy to server
        var result = await catalog.SearchAsync("Widget");
        #endregion

        // Assert
        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains(result, x => x.Name == "Widget A");
        Assert.Contains(result, x => x.Name == "Widget B");
        Assert.Equal(1, client.ServiceProvider.GetRequiredService<ServerServiceProvider>().RemoteCallCount);

        client.Dispose();
        server.Dispose();
    }

    /// <summary>
    /// Verifies interface factory method with parameters.
    /// </summary>
    [Fact]
    public async Task InterfaceFactory_FindBySkuAsync_ReturnsSpecificItem()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var catalog = client.GetRequiredService<IProductCatalog>();

        // Act
        var result = await catalog.FindBySkuAsync("WB-200");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WB-200", result.Sku);
        Assert.Equal("Widget B", result.Name);
        Assert.Equal(25.00m, result.UnitPrice);

        client.Dispose();
        server.Dispose();
    }

    /// <summary>
    /// Verifies interface factory can return a record with a primary constructor.
    /// </summary>
    /// <remarks>
    /// Records with primary constructors are serialized without $id/$ref metadata,
    /// avoiding the System.Text.Json parameterized constructor limitation.
    /// This test validates the full client-server round-trip for plain records.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_GetDetailsAsync_ReturnsRecordFromServer()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var catalog = client.GetRequiredService<IProductCatalog>();

        // Act - record round-trips through serialization
        var result = await catalog.GetDetailsAsync("WA-100");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("WA-100", result.Sku);
        Assert.Equal("The standard widget.", result.Description);

        client.Dispose();
        server.Dispose();
    }

    /// <summary>
    /// Verifies interface factory returns null correctly.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Nullable return types work through serialization
    ///
    /// The proxy correctly handles null responses from the server. Not found
    /// is a valid answer from a lookup.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_FindBySkuAsync_HandlesNullResult()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var catalog = client.GetRequiredService<IProductCatalog>();

        // Act
        var result = await catalog.FindBySkuAsync("NO-SUCH-SKU");

        // Assert
        Assert.Null(result);

        client.Dispose();
        server.Dispose();
    }

    /// <summary>
    /// Verifies interface factory works in local mode.
    /// </summary>
    /// <remarks>
    /// In Logical (single-tier) mode the generator registers no proxy:
    /// IProductCatalog resolves straight to ProductCatalog through
    /// RegisterMatchingName, and the call is a direct call.
    /// </remarks>
    [Fact]
    public async Task InterfaceFactory_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var catalog = local.GetRequiredService<IProductCatalog>();

        // Act - direct call, no proxy
        var result = await catalog.SearchAsync("Widget");

        // Assert
        Assert.IsType<ProductCatalog>(catalog);
        Assert.Equal(3, result.Count);

        local.Dispose();
    }
}
