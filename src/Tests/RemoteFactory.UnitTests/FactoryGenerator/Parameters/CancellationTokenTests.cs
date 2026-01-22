using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Parameters;

namespace RemoteFactory.UnitTests.FactoryGenerator.Parameters;

/// <summary>
/// Unit tests for factory methods with CancellationToken parameter.
/// CancellationToken is a common async pattern that should be properly
/// supported in factory methods without being serialized.
/// </summary>
public class CancellationTokenTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public CancellationTokenTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region Create/Fetch with CancellationToken

    [Fact]
    public async Task CreateAsync_WithNonCancelledToken_Completes()
    {
        var factory = _provider.GetRequiredService<ICancellableReadTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.CreateAsync(cts.Token);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task CreateAsync_WithAlreadyCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableReadTargetFactory>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.CreateAsync(cts.Token));
    }

    [Fact]
    public async Task FetchAsync_WithNonCancelledToken_Completes()
    {
        var factory = _provider.GetRequiredService<ICancellableReadTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.FetchAsync(cts.Token);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task FetchAsync_WithAlreadyCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableReadTargetFactory>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.FetchAsync(cts.Token));
    }

    [Fact]
    public async Task FetchBoolAsync_WithNonCancelledToken_Completes()
    {
        var factory = _provider.GetRequiredService<ICancellableReadTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.FetchBoolAsync(cts.Token);

        Assert.NotNull(result);
        Assert.True(result!.FetchCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task FetchBoolAsync_WithAlreadyCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableReadTargetFactory>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.FetchBoolAsync(cts.Token));
    }

    #endregion

    #region Insert/Update/Delete with CancellationToken

    [Fact]
    public async Task InsertAsync_WithNonCancelledToken_Completes()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = true;
        using var cts = new CancellationTokenSource();

        // SaveAsync routes to InsertAsync when IsNew=true
        var result = await factory.SaveAsync(obj, cts.Token);

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task InsertAsync_WithAlreadyCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = true;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.SaveAsync(obj, cts.Token));
    }

    [Fact]
    public async Task UpdateAsync_WithNonCancelledToken_Completes()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = false;
        using var cts = new CancellationTokenSource();

        // SaveAsync routes to UpdateAsync when IsNew=false
        var result = await factory.SaveAsync(obj, cts.Token);

        Assert.NotNull(result);
        Assert.True(result!.UpdateCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task UpdateAsync_WithAlreadyCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = false;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.SaveAsync(obj, cts.Token));
    }

    [Fact]
    public async Task DeleteAsync_WithNonCancelledToken_Completes()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteTargetFactory>();
        var obj = factory.Create();
        obj.IsDeleted = true;
        using var cts = new CancellationTokenSource();

        // SaveAsync routes to DeleteAsync when IsDeleted=true
        var result = await factory.SaveAsync(obj, cts.Token);

        // void Delete returns the object (only bool Delete returning false returns null)
        Assert.NotNull(result);
        Assert.True(result!.DeleteCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task DeleteAsync_WithAlreadyCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteTargetFactory>();
        var obj = factory.Create();
        obj.IsDeleted = true;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.SaveAsync(obj, cts.Token));
    }

    #endregion

    #region Mixed Parameters with CancellationToken

    [Fact]
    public async Task CreateAsync_WithParamAndCancellationToken_Works()
    {
        var factory = _provider.GetRequiredService<IMixedParamCancellableTargetFactory>();
        const int expectedParam = 42;
        using var cts = new CancellationTokenSource();

        var result = await factory.CreateAsync(expectedParam, cts.Token);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal(expectedParam, result.BusinessParam);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task FetchAsync_WithParamServiceAndCancellationToken_Works()
    {
        var factory = _provider.GetRequiredService<IMixedParamCancellableTargetFactory>();
        const int expectedParam = 123;
        using var cts = new CancellationTokenSource();

        var result = await factory.FetchAsync(expectedParam, cts.Token);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(expectedParam, result.BusinessParam);
        Assert.True(result.ServiceWasInjected);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task CreateWithServiceAsync_Works()
    {
        var factory = _provider.GetRequiredService<IMixedParamCancellableTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.CreateWithServiceAsync(cts.Token);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.True(result.ServiceWasInjected);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task FetchComplexAsync_WithMultipleParamsServiceAndCancellationToken_Works()
    {
        var factory = _provider.GetRequiredService<IMixedParamCancellableTargetFactory>();
        const int expectedInt = 999;
        const string expectedString = "test";
        using var cts = new CancellationTokenSource();

        var result = await factory.FetchComplexAsync(expectedInt, expectedString, cts.Token);

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal(expectedInt, result.BusinessParam);
        Assert.True(result.ServiceWasInjected);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task CreateAsync_MixedParams_WithCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<IMixedParamCancellableTargetFactory>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.CreateAsync(42, cts.Token));
    }

    [Fact]
    public async Task FetchAsync_MixedParams_WithCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<IMixedParamCancellableTargetFactory>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.FetchAsync(123, cts.Token));
    }

    #endregion

    #region CancellationToken with Bool Return

    [Fact]
    public async Task CreateBoolAsync_ReturnsObject_WhenTrue()
    {
        var factory = _provider.GetRequiredService<ICancellableBoolTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.CreateBoolAsync(cts.Token);

        Assert.NotNull(result);
        Assert.True(result!.CreateCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task FetchBoolAsync_ReturnsObject_WhenTrue()
    {
        var factory = _provider.GetRequiredService<ICancellableBoolTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.FetchBoolAsync(true, cts.Token);

        Assert.NotNull(result);
        Assert.True(result!.ShouldSucceed);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task FetchBoolAsync_ReturnsNull_WhenFalse()
    {
        var factory = _provider.GetRequiredService<ICancellableBoolTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.FetchBoolAsync(false, cts.Token);

        Assert.Null(result);
    }

    [Fact]
    public async Task CreateBoolAsync_WithCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableBoolTargetFactory>();
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.CreateBoolAsync(cts.Token));
    }

    #endregion

    #region Write Operations with CancellationToken and Bool Return

    [Fact]
    public async Task InsertBoolAsync_ReturnsObject_WhenSucceeds()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteBoolTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = true;
        obj.ShouldSucceed = true;
        using var cts = new CancellationTokenSource();

        // SaveBoolAsync routes to InsertBoolAsync when IsNew=true
        var result = await factory.SaveBoolAsync(obj, cts.Token);

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task InsertBoolAsync_ReturnsNull_WhenFails()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteBoolTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = true;
        obj.ShouldSucceed = false;
        using var cts = new CancellationTokenSource();

        // SaveBoolAsync routes to InsertBoolAsync when IsNew=true
        var result = await factory.SaveBoolAsync(obj, cts.Token);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateBoolAsync_ReturnsObject_WhenSucceeds()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteBoolTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = false;
        obj.ShouldSucceed = true;
        using var cts = new CancellationTokenSource();

        // SaveBoolAsync routes to UpdateBoolAsync when IsNew=false
        var result = await factory.SaveBoolAsync(obj, cts.Token);

        Assert.NotNull(result);
        Assert.True(result!.UpdateCalled);
        Assert.True(result.CancellationWasChecked);
    }

    [Fact]
    public async Task UpdateBoolAsync_ReturnsNull_WhenFails()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteBoolTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = false;
        obj.ShouldSucceed = false;
        using var cts = new CancellationTokenSource();

        // SaveBoolAsync routes to UpdateBoolAsync when IsNew=false
        var result = await factory.SaveBoolAsync(obj, cts.Token);

        Assert.Null(result);
    }

    [Fact]
    public async Task InsertBoolAsync_WithCancelledToken_Throws()
    {
        var factory = _provider.GetRequiredService<ICancellableWriteBoolTargetFactory>();
        var obj = factory.Create();
        obj.IsNew = true;
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => factory.SaveBoolAsync(obj, cts.Token));
    }

    #endregion

    #region Default CancellationToken Value

    [Fact]
    public async Task CreateAsync_WithDefaultToken_Completes()
    {
        var factory = _provider.GetRequiredService<IDefaultCancellableTargetFactory>();

        // Call with default token (CancellationToken.None)
        var result = await factory.CreateAsync(CancellationToken.None);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    [Fact]
    public async Task CreateAsync_WithExplicitToken_Completes()
    {
        var factory = _provider.GetRequiredService<IDefaultCancellableTargetFactory>();
        using var cts = new CancellationTokenSource();

        var result = await factory.CreateAsync(cts.Token);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    #endregion
}
