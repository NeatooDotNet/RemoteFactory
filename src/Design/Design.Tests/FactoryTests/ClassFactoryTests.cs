// =============================================================================
// DESIGN SOURCE OF TRUTH: Class Factory Tests
// =============================================================================
//
// Tests demonstrating the CLASS FACTORY pattern behavior.
//
// DESIGN DECISION: Test all three container modes
//
// Each test should verify behavior in:
// - Client (remote): Calls serialize to server
// - Server (local): Direct execution
// - Local (logical): Single-tier, no remote
//
// This ensures consistent behavior across deployment models.
//
// =============================================================================

using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for CLASS FACTORY pattern (ExampleClassFactory).
/// </summary>
public class ClassFactoryTests
{
    /// <summary>
    /// Verifies [Remote, Create] works through client-server serialization.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: This test validates the complete round-trip:
    /// 1. Client calls factory.Create()
    /// 2. Request serializes to JSON
    /// 3. Server deserializes, executes, returns result
    /// 4. Response serializes back to client
    /// 5. Client gets the created instance
    ///
    /// The test proves [Remote] correctly crosses the boundary.
    /// </remarks>
    [Fact]
    public async Task Create_ThroughClient_SerializesToServerAndBack()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IExampleClassFactoryFactory>();

        // Act - This call goes through JSON serialization to the server
        var result = await factory.Create("Test Item");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Test Item", result.Name);
        Assert.True(result.Id > 0, "Server should have assigned an ID");

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies [Remote, Fetch] works through client-server serialization.
    /// </summary>
    [Fact]
    public async Task Fetch_ThroughClient_ReturnsDeserializedInstance()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IExampleClassFactoryFactory>();

        // Act
        var result = await factory.Fetch(42);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(42, result.Id);
        Assert.Equal("Loaded_42", result.Name);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies Create works in local/logical mode (single-tier).
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Logical mode enables single-tier deployment.
    /// The same domain code works in both distributed and monolithic scenarios.
    /// This test proves the pattern works without remote infrastructure.
    /// </remarks>
    [Fact]
    public async Task Create_InLocalMode_WorksWithoutRemote()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IExampleClassFactoryFactory>();

        // Act - Direct execution, no serialization
        var result = await factory.Create("Local Item");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Local Item", result.Name);
        Assert.True(result.Id > 0);

        local.Dispose();
    }

    /// <summary>
    /// Verifies the server container can execute directly.
    /// </summary>
    [Fact]
    public async Task Create_OnServer_ExecutesDirectly()
    {
        // Arrange
        var (server, _, _) = DesignClientServerContainers.Scopes();
        var factory = server.GetRequiredService<IExampleClassFactoryFactory>();

        // Act
        var result = await factory.Create("Server Item");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Server Item", result.Name);

        server.Dispose();
    }
}
