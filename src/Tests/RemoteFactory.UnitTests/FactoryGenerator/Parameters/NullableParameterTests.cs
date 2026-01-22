using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Parameters;

namespace RemoteFactory.UnitTests.FactoryGenerator.Parameters;

/// <summary>
/// Unit tests for factory methods with nullable parameters.
/// </summary>
public class NullableParameterTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public NullableParameterTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public void Create_WithNullValue_Works()
    {
        var factory = _provider.GetRequiredService<INullableParameterTargetFactory>();

        var result = factory.Create(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    [Fact]
    public void Create_WithValue_Works()
    {
        var factory = _provider.GetRequiredService<INullableParameterTargetFactory>();

        var result = factory.Create(42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }
}
