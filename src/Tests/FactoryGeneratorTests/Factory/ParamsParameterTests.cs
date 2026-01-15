using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for factory methods with params parameters.
/// Investigates GAP: params parameter handling in source generator.
/// </summary>

#region Domain Classes

/// <summary>
/// Domain class with params parameters in factory methods.
/// </summary>
[Factory]
public class ParamsReadObject
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? Result { get; set; }

    // params int[] parameter
    [Create]
    public void CreateWithParamsInt(params int[] ids)
    {
        Assert.NotNull(ids);
        this.CreateCalled = true;
        this.Result = $"ParamsInt count: {ids.Length}, sum: {ids.Sum()}";
    }

    // params string[] parameter
    [Create]
    public void CreateWithParamsString(params string[] names)
    {
        Assert.NotNull(names);
        this.CreateCalled = true;
        this.Result = $"ParamsString count: {names.Length}, joined: {string.Join(",", names)}";
    }

    // Mixed: regular parameter + params
    [Create]
    public void CreateWithMixedParams(int id, params string[] tags)
    {
        Assert.NotNull(tags);
        this.CreateCalled = true;
        this.Result = $"Mixed id: {id}, tags: {tags.Length}";
    }

    // Fetch with params
    [Fetch]
    public void FetchWithParamsInt(params int[] ids)
    {
        Assert.NotNull(ids);
        this.FetchCalled = true;
        this.Result = $"Fetch ParamsInt count: {ids.Length}";
    }
}

/// <summary>
/// Domain class with remote params parameters.
/// </summary>
[Factory]
public class ParamsRemoteObject
{
    public bool CreateCalled { get; set; }
    public bool WasCancelled { get; set; }
    public string? Result { get; set; }

    // Remote params int[] parameter
    [Create]
    [Remote]
    public void CreateRemoteWithParamsInt(params int[] ids)
    {
        Assert.NotNull(ids);
        this.CreateCalled = true;
        this.Result = $"Remote ParamsInt count: {ids.Length}, sum: {ids.Sum()}";
    }

    // Remote mixed: regular parameter + params
    [Create]
    [Remote]
    public void CreateRemoteWithMixedParams(int id, params string[] tags)
    {
        Assert.NotNull(tags);
        this.CreateCalled = true;
        this.Result = $"Remote Mixed id: {id}, tags: {tags.Length}";
    }

    // Remote params WITH CancellationToken - verifies CT flows through
    [Create]
    [Remote]
    public void CreateRemoteWithParamsAndCancellation(CancellationToken ct, params int[] ids)
    {
        Assert.NotNull(ids);
        this.CreateCalled = true;
        this.WasCancelled = ct.IsCancellationRequested;
        this.Result = $"Remote ParamsWithCT count: {ids.Length}, cancelled: {ct.IsCancellationRequested}";
    }
}

#endregion

#region Test Class

public class ParamsParameterTests
{
    private readonly IServiceScope clientScope;
    private readonly IServiceScope localScope;

    public ParamsParameterTests()
    {
        var scopes = ClientServerContainers.Scopes();
        this.clientScope = scopes.client;
        this.localScope = scopes.local;
    }

    #region Local Tests - params modifier is preserved

    /// <summary>
    /// Tests that params methods can be called with variadic syntax.
    /// Note: Due to C# rules, when CancellationToken (optional) comes before params,
    /// you must pass 'default' for CT to use variadic syntax.
    /// Signature: (CancellationToken cancellationToken = default, params int[] ids)
    /// </summary>
    [Fact]
    public void Params_CreateWithParamsInt_LocalExecution_VariadicSyntax()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IParamsReadObjectFactory>();

        // Must pass 'default' for CT to use variadic params
        var result = factory.CreateWithParamsInt(default, 1, 2, 3, 4, 5);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 5, sum: 15", result.Result);
    }

    [Fact]
    public void Params_CreateWithParamsInt_LocalExecution_SingleValue()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IParamsReadObjectFactory>();

        var result = factory.CreateWithParamsInt(default, 42);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 1, sum: 42", result.Result);
    }

    [Fact]
    public void Params_CreateWithParamsInt_LocalExecution_NoValues()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IParamsReadObjectFactory>();

        // Empty params call - still need to pass CT or call with no args
        var result = factory.CreateWithParamsInt();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 0, sum: 0", result.Result);
    }

    [Fact]
    public void Params_CreateWithParamsString_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IParamsReadObjectFactory>();

        var result = factory.CreateWithParamsString(default, "Alpha", "Beta", "Gamma");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsString count: 3, joined: Alpha,Beta,Gamma", result.Result);
    }

    [Fact]
    public void Params_CreateWithMixedParams_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IParamsReadObjectFactory>();

        // Mixed: regular param, then CT, then variadic params
        var result = factory.CreateWithMixedParams(42, default, "tag1", "tag2", "tag3");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Mixed id: 42, tags: 3", result.Result);
    }

    [Fact]
    public void Params_FetchWithParamsInt_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IParamsReadObjectFactory>();

        var result = factory.FetchWithParamsInt(default, 10, 20, 30);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch ParamsInt count: 3", result.Result);
    }

    [Fact]
    public void Params_CreateWithParamsInt_LocalExecution_WithCancellationToken()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IParamsReadObjectFactory>();
        var cts = new CancellationTokenSource();

        // Pass actual CancellationToken, then params values
        var result = factory.CreateWithParamsInt(cts.Token, 1, 2, 3);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("ParamsInt count: 3, sum: 6", result.Result);
    }

    #endregion

    #region Remote Tests - Serialization with params arrays

    [Fact]
    public async Task Params_CreateRemoteWithParamsInt_RemoteExecution_SingleValue()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IParamsRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithParamsInt(default, 1);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote ParamsInt count: 1, sum: 1", result.Result);
    }

    [Fact]
    public async Task Params_CreateRemoteWithParamsInt_RemoteExecution_MultipleValues()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IParamsRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithParamsInt(default, 1, 2, 3, 4, 5);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote ParamsInt count: 5, sum: 15", result.Result);
    }

    [Fact]
    public async Task Params_CreateRemoteWithParamsInt_RemoteExecution_NoValues()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IParamsRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithParamsInt();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote ParamsInt count: 0, sum: 0", result.Result);
    }

    [Fact]
    public async Task Params_CreateRemoteWithMixedParams_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IParamsRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithMixedParams(42, default, "tag1", "tag2");

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Mixed id: 42, tags: 2", result.Result);
    }

    /// <summary>
    /// Tests that CancellationToken flows correctly when domain method has BOTH CT and params.
    /// Domain: CreateRemoteWithParamsAndCancellation(CancellationToken ct, params int[] ids)
    /// </summary>
    [Fact]
    public async Task Params_CreateRemoteWithParamsAndCancellation_RemoteExecution_CTFlows()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IParamsRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithParamsAndCancellation(default, 1, 2, 3);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.False(result.WasCancelled);
        Assert.Equal("Remote ParamsWithCT count: 3, cancelled: False", result.Result);
    }

    #endregion
}

#endregion
