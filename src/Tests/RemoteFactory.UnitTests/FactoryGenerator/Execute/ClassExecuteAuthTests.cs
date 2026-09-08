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
/// The signature itself is pinned by <see cref="ClassExecuteWithAuth_FactoryMethod_IsNullable"/>,
/// which would not compile if the generator declared it non-nullable.
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
    /// Pins the generated signature as nullable. The assignment to a nullable local is the
    /// assertion: were the factory method declared Task&lt;ClassExecWithAuth&gt;, the generated
    /// file would not compile (CS8603) and this test could not run at all.
    /// </summary>
    [Fact]
    public async Task ClassExecuteWithAuth_FactoryMethod_IsNullable()
    {
        var factory = _provider.GetRequiredService<IClassExecWithAuthFactory>();

        ClassExecWithAuth? allowed = await factory.Run("hello");
        ClassExecAuth.ShouldAllow = false;
        ClassExecWithAuth? denied = await factory.Run("hello");

        Assert.NotNull(allowed);
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
