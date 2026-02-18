// =============================================================================
// DESIGN SOURCE OF TRUTH: Class Factory Execute Tests
// =============================================================================
//
// Tests demonstrating [Execute] on non-static [Factory] classes.
//
// =============================================================================

using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Tests for [Execute] methods on non-static [Factory] classes.
/// </summary>
public class ClassFactoryExecuteTests
{
    /// <summary>
    /// Verifies [Execute] method on class factory works through client-server.
    /// </summary>
    [Fact]
    public async Task Execute_OnClassFactory_WorksThroughClientServer()
    {
        // Arrange
        var (server, client, _) = DesignClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IClassExecuteDemoFactory>();

        // Act
        var result = await factory.RunCommand("test input");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Executed: test input", result.Name);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies Execute works in local/logical mode.
    /// </summary>
    [Fact]
    public async Task Execute_OnClassFactory_WorksInLocalMode()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IClassExecuteDemoFactory>();

        // Act
        var result = await factory.RunCommand("local input");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Executed: local input", result.Name);

        local.Dispose();
    }

    /// <summary>
    /// Verifies that Create still works alongside Execute on the same factory.
    /// </summary>
    [Fact]
    public async Task Create_StillWorks_AlongsideExecute()
    {
        // Arrange
        var (_, _, local) = DesignClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IClassExecuteDemoFactory>();

        // Act
        var result = await factory.Create("test");

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test", result.Name);

        local.Dispose();
    }
}
