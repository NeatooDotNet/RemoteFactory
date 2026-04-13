using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// Authorization class that tracks how many times each method is called.
/// Used to verify auth methods are not called multiple times due to
/// failed deduplication in BuildSaveMethodFromGroup.
/// </summary>
public class AuthTriplicationTestAuth
{
    public static int CanWriteCallCount { get; set; }

    public static void ResetCounts()
    {
        CanWriteCallCount = 0;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite()
    {
        CanWriteCallCount++;
        return true;
    }
}

/// <summary>
/// Target class with Insert, Update, and Delete methods that all share
/// the same auth class. The generated Save method merges auth from all
/// three write methods via Distinct(). Before the fix, Distinct() failed
/// because AuthMethodCall used reference equality for its Parameters list,
/// resulting in 3x duplicate auth calls.
/// </summary>
[Factory]
[AuthorizeFactory<AuthTriplicationTestAuth>]
public class AuthTriplicationTarget : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    [Create]
    public AuthTriplicationTarget() { }

    [Insert]
    public void Insert() { }

    [Update]
    public void Update() { }

    [Delete]
    public void Delete() { }
}

#endregion

/// <summary>
/// Regression test for auth method triplication in generated Save methods.
/// Bug: BuildSaveMethodFromGroup merges auth from Insert, Update, Delete via
/// SelectMany + Distinct. AuthMethodCall is a record with IReadOnlyList Parameters,
/// and record equality uses reference equality for collections, so Distinct()
/// failed to deduplicate — each auth method was called 3 times.
/// Fix: Override Equals/GetHashCode on AuthMethodCall to use SequenceEqual.
/// </summary>
public class AuthMethodTriplicationTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IAuthTriplicationTargetFactory _factory;

    public AuthMethodTriplicationTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<AuthTriplicationTestAuth, AuthTriplicationTestAuth>()
            .Build();
        _factory = _provider.GetRequiredService<IAuthTriplicationTargetFactory>();
        AuthTriplicationTestAuth.ResetCounts();
    }

    public void Dispose()
    {
        AuthTriplicationTestAuth.ResetCounts();
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public void CanSave_CallsAuthMethodExactlyOnce()
    {
        // Arrange
        AuthTriplicationTestAuth.ResetCounts();

        // Act
        var result = _factory.CanSave();

        // Assert — auth method should be called once, not 3 times
        Assert.True(result.HasAccess);
        Assert.Equal(1, AuthTriplicationTestAuth.CanWriteCallCount);
    }
}
