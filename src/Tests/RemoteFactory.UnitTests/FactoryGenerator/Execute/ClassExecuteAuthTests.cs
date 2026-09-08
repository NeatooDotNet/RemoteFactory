using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Execute;

namespace RemoteFactory.UnitTests.FactoryGenerator.Execute;

/// <summary>
/// Unit tests for class-level [Execute] under [AuthorizeFactory] (EXRM-002).
/// </summary>
/// <remarks>
/// The generated factory method is declared nullable because a denied check returns
/// <c>Authorized&lt;T&gt;.Result</c>, which is default — the same rule Create and Fetch follow.
/// The signature is pinned by the build, not by a test method: declaring it non-nullable raises
/// CS8603 in the generated file, and this repo compiles with TreatWarningsAsErrors and no NoWarn
/// for it, so <see cref="ClassExecWithAuth"/> would not build. These tests cover the behavior on
/// either side of the check.
/// </remarks>
public class ClassExecuteAuthTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public ClassExecuteAuthTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .WithService<ClassExecAuth, ClassExecAuth>()
            .Build();
        ClassExecAuth.ShouldAllow = true;
    }

    public void Dispose()
    {
        ClassExecAuth.ShouldAllow = true;
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ClassExecuteWithAuth_Allowed_ReturnsInstance()
    {
        var factory = _provider.GetRequiredService<IClassExecWithAuthFactory>();

        var result = await factory.Run("hello");

        Assert.NotNull(result);
        Assert.Equal("Executed: hello", result.Name);
    }

    [Fact]
    public async Task ClassExecuteWithAuth_Denied_ReturnsNull()
    {
        ClassExecAuth.ShouldAllow = false;
        var factory = _provider.GetRequiredService<IClassExecWithAuthFactory>();

        var result = await factory.Run("hello");

        Assert.Null(result);
    }

    /// <summary>
    /// Exercises both authorization outcomes through one resolved factory.
    /// </summary>
    /// <remarks>
    /// What actually pins the nullable signature is the build itself: the generated public
    /// method returns Authorized&lt;T&gt;.Result, so declaring it Task&lt;T&gt; raises CS8603 in
    /// the generated file, and this repo compiles with TreatWarningsAsErrors and no NoWarn for
    /// it -- the target below would not build at all. This test cannot add to that (assigning a
    /// non-nullable result to a nullable local is legal either way); it covers the allowed and
    /// denied paths against a single factory instance instead.
    /// </remarks>
    [Fact]
    public async Task ClassExecuteWithAuth_BothOutcomes_ThroughOneFactory()
    {
        var factory = _provider.GetRequiredService<IClassExecWithAuthFactory>();

        var allowed = await factory.Run("hello");
        ClassExecAuth.ShouldAllow = false;
        var denied = await factory.Run("hello");

        Assert.NotNull(allowed);
        Assert.Equal("Executed: hello", allowed.Name);
        Assert.Null(denied);
    }

    [Fact]
    public void ClassExecuteWithAuth_CanRun_ReflectsAuthState()
    {
        var factory = _provider.GetRequiredService<IClassExecWithAuthFactory>();

        ClassExecAuth.ShouldAllow = true;
        Assert.True(factory.CanRun().HasAccess);

        ClassExecAuth.ShouldAllow = false;
        Assert.False(factory.CanRun().HasAccess);
    }
}
