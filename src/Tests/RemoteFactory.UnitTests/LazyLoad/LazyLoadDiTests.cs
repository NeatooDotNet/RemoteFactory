using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.LazyLoad;

/// <summary>
/// DI registration and <see cref="ILazyLoadFactory"/> tests verifying
/// correct registration and factory behavior.
/// </summary>
public class LazyLoadDiTests
{
    /// <summary>
    /// Use the RemoteFactory assembly which has no [Factory] types,
    /// so RegisterFactories is a no-op and the test isolates core
    /// service registration behavior.
    /// </summary>
    private static readonly System.Reflection.Assembly CoreAssembly = typeof(ILazyLoadFactory).Assembly;

    /// <summary>
    /// TS-LL-011 (BR-LL-011): Factory Create with loader returns unloaded instance.
    /// </summary>
    [Fact]
    public void Factory_CreateWithLoader_IsLoadedFalse()
    {
        var factory = new LazyLoadFactory();

        var ll = factory.Create<string>(() => Task.FromResult<string?>("test"));

        Assert.False(ll.IsLoaded);
        Assert.Null(ll.Value);
    }

    /// <summary>
    /// TS-LL-012 (BR-LL-012): Factory Create with value returns loaded instance.
    /// </summary>
    [Fact]
    public void Factory_CreateWithValue_IsLoadedTrue()
    {
        var factory = new LazyLoadFactory();

        var ll = factory.Create<string>("preloaded");

        Assert.Equal("preloaded", ll.Value);
        Assert.True(ll.IsLoaded);
    }

    /// <summary>
    /// TS-LL-020 (BR-LL-024): AddNeatooRemoteFactory registers ILazyLoadFactory.
    /// </summary>
    [Fact]
    public void AddNeatooRemoteFactory_RegistersILazyLoadFactory()
    {
        var services = new ServiceCollection();
        services.AddNeatooRemoteFactory(NeatooFactory.Logical, CoreAssembly);
        using var sp = services.BuildServiceProvider();

        var factory = sp.GetRequiredService<ILazyLoadFactory>();

        Assert.NotNull(factory);
        Assert.IsType<LazyLoadFactory>(factory);
    }
}
