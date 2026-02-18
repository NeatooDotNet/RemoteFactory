using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Execute;

namespace RemoteFactory.UnitTests.FactoryGenerator.Execute;

/// <summary>
/// Unit tests for [Execute] methods on non-static [Factory] classes.
/// Verifies that the generated factory interface and methods work correctly
/// in Server mode (local execution, no serialization).
/// </summary>
public class ClassExecuteTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public ClassExecuteTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .WithService<IService2, Service2>()
            .WithService<IService3, Service3>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public async Task ClassExecute_WithService_ReturnsInstance()
    {
        var factory = _provider.GetRequiredService<IClassExecTargetFactory>();

        var result = await factory.RunWithSvc("hello");

        Assert.NotNull(result);
        Assert.Equal("Executed: hello", result.Name);
    }

    [Fact]
    public async Task ClassExecute_NoService_ReturnsInstance()
    {
        var factory = _provider.GetRequiredService<IClassExecNoSvcFactory>();

        var result = await factory.RunNoService("world");

        Assert.NotNull(result);
        Assert.Equal("NoSvc: world", result.Value);
    }

    [Fact]
    public async Task ClassExecute_MultipleServices_ReturnsInstance()
    {
        var factory = _provider.GetRequiredService<IClassExecMultiSvcFactory>();

        var result = await factory.RunMulti("test");

        Assert.NotNull(result);
        Assert.Equal("test-42", result.Result);
    }

    [Fact]
    public async Task ClassExecute_CreateStillWorks_AlongsideExecute()
    {
        var factory = _provider.GetRequiredService<IClassExecTargetFactory>();

        var result = await factory.Create("from create");

        Assert.NotNull(result);
        Assert.Equal("from create", result.Name);
    }

    [Fact]
    public void ClassExecute_FactoryInterface_CanBeResolved()
    {
        var factory = _provider.GetService<IClassExecTargetFactory>();

        Assert.NotNull(factory);
    }
}
