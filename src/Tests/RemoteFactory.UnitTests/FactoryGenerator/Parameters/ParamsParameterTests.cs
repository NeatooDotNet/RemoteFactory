using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Parameters;

namespace RemoteFactory.UnitTests.FactoryGenerator.Parameters;

/// <summary>
/// Unit tests for factory methods with params parameters.
/// Verifies that params modifier is preserved in generated factory interface.
/// </summary>
public class ParamsParameterTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public ParamsParameterTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region params int[] Tests

    /// <summary>
    /// Tests that params methods can be called with variadic syntax.
    /// Note: Due to C# rules, when CancellationToken (optional) comes before params,
    /// you must pass 'default' for CT to use variadic syntax.
    /// </summary>
    [Fact]
    public void CreateWithParamsInt_VariadicSyntax_Works()
    {
        var factory = _provider.GetRequiredService<IParamsReadTargetFactory>();

        // Must pass 'default' for CT to use variadic params
        var result = factory.CreateWithParamsInt(default, 1, 2, 3, 4, 5);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 5, sum: 15", result.Result);
    }

    [Fact]
    public void CreateWithParamsInt_SingleValue_Works()
    {
        var factory = _provider.GetRequiredService<IParamsReadTargetFactory>();

        var result = factory.CreateWithParamsInt(default, 42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 1, sum: 42", result.Result);
    }

    [Fact]
    public void CreateWithParamsInt_NoValues_Works()
    {
        var factory = _provider.GetRequiredService<IParamsReadTargetFactory>();

        // Empty params call - still need to pass CT or call with no args
        var result = factory.CreateWithParamsInt();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 0, sum: 0", result.Result);
    }

    [Fact]
    public void CreateWithParamsInt_WithCancellationToken_Works()
    {
        var factory = _provider.GetRequiredService<IParamsReadTargetFactory>();
        var cts = new CancellationTokenSource();

        // Pass actual CancellationToken, then params values
        var result = factory.CreateWithParamsInt(cts.Token, 1, 2, 3);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 3, sum: 6", result.Result);
    }

    #endregion

    #region params string[] Tests

    [Fact]
    public void CreateWithParamsString_MultipleValues_Works()
    {
        var factory = _provider.GetRequiredService<IParamsReadTargetFactory>();

        var result = factory.CreateWithParamsString(default, "Alpha", "Beta", "Gamma");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsString count: 3, joined: Alpha,Beta,Gamma", result.Result);
    }

    #endregion

    #region Mixed Parameters Tests

    [Fact]
    public void CreateWithMixedParams_RegularParamPlusParams_Works()
    {
        var factory = _provider.GetRequiredService<IParamsReadTargetFactory>();

        // Mixed: regular param, then CT, then variadic params
        var result = factory.CreateWithMixedParams(42, default, "tag1", "tag2", "tag3");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Mixed id: 42, tags: 3", result.Result);
    }

    #endregion

    #region Fetch Tests

    [Fact]
    public void FetchWithParamsInt_Works()
    {
        var factory = _provider.GetRequiredService<IParamsReadTargetFactory>();

        var result = factory.FetchWithParamsInt(default, 10, 20, 30);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch ParamsInt count: 3", result.Result);
    }

    #endregion
}
