// =============================================================================
// DESIGN SOURCE OF TRUTH: Local Execute Tests
// =============================================================================
//
// Tests demonstrating [Execute] WITHOUT [Remote] on both factory shapes: the
// call runs on the client, resolves its [Service] parameters from client DI,
// and never crosses the wire.
//
// =============================================================================

using Design.Domain.FactoryPatterns;
using Design.Tests.TestInfrastructure;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace Design.Tests.FactoryTests;

/// <summary>
/// Wire stand-in that fails the test if anything tries to cross it.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Prove "no remote request" by making the wire unusable
///
/// Registered on the client through configureClient, after the standard
/// serializing stand-in, so it is the last registration and wins. It throws
/// from the request methods, never from its constructor -- a throwing
/// constructor would fail factory resolution instead of proving the call
/// stayed local. A test that installs it may call only bare [Execute]
/// samples; a [Remote] sample would (correctly) hit it.
/// </remarks>
internal sealed class NoWireDelegateRequest : IMakeRemoteDelegateRequest
{
    public Task<T> ForDelegate<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
        => throw new InvalidOperationException($"No remote request expected, but {delegateType.Name} tried to cross the wire.");

    public Task<T?> ForDelegateNullable<T>(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
        => throw new InvalidOperationException($"No remote request expected, but {delegateType.Name} tried to cross the wire.");

    public Task ForDelegateEvent(Type delegateType, object?[]? parameters, CancellationToken cancellationToken)
        => throw new InvalidOperationException($"No remote request expected, but {delegateType.Name} tried to cross the wire.");
}

/// <summary>
/// Tests for [Execute] without [Remote] (ExampleCommands.ScoreText, ClassExecuteDemo.ScoreLocally).
/// </summary>
public class LocalExecuteTests
{
    private static (IServiceScope server, IServiceScope client) ScopesWithNoWire()
    {
        var (server, client, _) = DesignClientServerContainers.Scopes(
            configureClient: services => services.AddScoped<IMakeRemoteDelegateRequest, NoWireDelegateRequest>());
        return (server, client);
    }

    /// <summary>
    /// Verifies a bare [Execute] on a static factory runs on the client.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: For [Execute] without [Remote] on _ScoreText:
    /// - Creates delegate type: ExampleCommands.ScoreText
    /// - Registers one unguarded local delegate in every factory mode; no remote delegate
    /// - The delegate resolves ITextScorer from whichever container it runs in --
    ///   here the client's, where RegisterMatchingName maps ITextScorer -> TextScorer
    /// </remarks>
    [Fact]
    public async Task Execute_StaticFactory_WithoutRemote_RunsOnClient()
    {
        // Arrange
        var (server, client) = ScopesWithNoWire();
        var scoreText = client.GetRequiredService<ExampleCommands.ScoreText>();

        // Act - the wire stand-in would throw if this left the client
        var score = await scoreText("the quick brown fox");

        // Assert
        Assert.Equal(4, score);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// Verifies a bare [Execute] on a class factory runs on the client.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: For [Execute] without [Remote] on ScoreLocally:
    /// - Interface method: IClassExecuteDemoFactory.ScoreLocally(string input)
    /// - Local method only, unguarded -- no remote delegate, no endpoint
    /// - [Service] parameters resolved from the factory's own container
    /// </remarks>
    [Fact]
    public async Task Execute_ClassFactory_WithoutRemote_RunsOnClient()
    {
        // Arrange
        var (server, client) = ScopesWithNoWire();
        var factory = client.GetRequiredService<IClassExecuteDemoFactory>();

        // Act - the wire stand-in would throw if this left the client
        var result = await factory.ScoreLocally("the quick brown fox");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(4, result.Id);
        Assert.Equal("Scored: the quick brown fox", result.Name);

        server.Dispose();
        client.Dispose();
    }

    /// <summary>
    /// The control for the stand-in: a [Remote, Execute] sample must try to cross the wire
    /// and hit it. Without this, the two tests above could pass with a stand-in that is
    /// never consulted.
    /// </summary>
    [Fact]
    public async Task Execute_WithRemote_StillTriesTheWire()
    {
        // Arrange
        var (server, client) = ScopesWithNoWire();
        var sendNotification = client.GetRequiredService<ExampleCommands.SendNotification>();

        // Act / Assert
        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => sendNotification("test@example.com", "Hello!"));
        Assert.Contains("tried to cross the wire", ex.Message, StringComparison.Ordinal);

        server.Dispose();
        client.Dispose();
    }
}
