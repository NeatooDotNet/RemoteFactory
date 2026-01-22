using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Parameters;

namespace RemoteFactory.UnitTests.FactoryGenerator.Parameters;

/// <summary>
/// Unit tests for domain classes with complex parameter types (List, Dictionary, custom objects).
/// </summary>
public class ComplexParameterTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public ComplexParameterTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    #region List Parameters

    [Fact]
    public void CreateWithIntList_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.CreateWithIntList(new List<int> { 1, 2, 3, 4, 5 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("IntList count: 5, sum: 15", result.Result);
    }

    [Fact]
    public void CreateWithStringList_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.CreateWithStringList(new List<string> { "Alpha", "Beta", "Gamma" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("StringList count: 3, joined: Alpha,Beta,Gamma", result.Result);
    }

    #endregion

    #region Dictionary Parameters

    [Fact]
    public void CreateWithDictionary_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();
        var data = new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 } };

        var result = factory.CreateWithDictionary(data);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Contains("Dictionary count: 3", result.Result!);
    }

    #endregion

    #region DTO Parameters

    [Fact]
    public void CreateWithDto_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();
        var dto = new SimpleDto { Id = 42, Name = "TestDto", IsActive = true };

        var result = factory.CreateWithDto(dto);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Dto Id: 42, Name: TestDto, IsActive: True", result.Result);
    }

    [Fact]
    public void CreateWithNestedDto_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();
        var dto = new NestedDto
        {
            Id = Guid.NewGuid(),
            Title = "Nested",
            Tags = new List<string> { "tag1", "tag2" },
            Details = new SimpleDto { Id = 1, Name = "Detail", IsActive = true }
        };

        var result = factory.CreateWithNestedDto(dto);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Contains("Tags: 2", result.Result!);
        Assert.Contains("HasDetails: True", result.Result!);
    }

    #endregion

    #region Nullable Parameters

    [Fact]
    public void CreateWithNullableList_WithValue_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.CreateWithNullableList(new List<int> { 1, 2, 3 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableList count: 3", result.Result);
    }

    [Fact]
    public void CreateWithNullableList_WithNull_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.CreateWithNullableList(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableList is null", result.Result);
    }

    [Fact]
    public void CreateWithNullableDto_WithValue_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.CreateWithNullableDto(new SimpleDto { Id = 99, Name = "Test" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableDto Id: 99", result.Result);
    }

    [Fact]
    public void CreateWithNullableDto_WithNull_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.CreateWithNullableDto(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableDto is null", result.Result);
    }

    #endregion

    #region Mixed and Async Parameters

    [Fact]
    public void CreateWithMixedParams_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.CreateWithMixedParams(
            100,
            new List<string> { "tag1", "tag2" },
            new SimpleDto { Id = 50, Name = "Mixed" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Mixed Id: 100, Tags: 2, DtoId: 50", result.Result);
    }

    [Fact]
    public async Task CreateWithComplexParamsAsync_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = await factory.CreateWithComplexParamsAsync(
            new List<int> { 1, 2 },
            new Dictionary<string, int> { { "a", 1 } });

        Assert.NotNull(result);
        Assert.True(result!.CreateCalled);
        Assert.Equal("Async IntList: 2, Dict: 1", result.Result);
    }

    #endregion

    #region Fetch Tests

    [Fact]
    public void FetchWithIntList_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.FetchWithIntList(new List<int> { 7, 8, 9 });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch IntList count: 3", result.Result);
    }

    [Fact]
    public void FetchWithDto_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamReadTargetFactory>();

        var result = factory.FetchWithDto(new SimpleDto { Id = 11, Name = "FetchDto" });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch Dto Name: FetchDto", result.Result);
    }

    #endregion

    #region Write Tests

    [Fact]
    public void SaveInsertWithIntList_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamWriteTargetFactory>();
        var obj = new ComplexParamWriteTarget { IsNew = true };

        var result = factory.SaveWithIntList(obj, new List<int> { 1, 2, 3, 4 });

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Insert IntList count: 4", result.Result);
    }

    [Fact]
    public void SaveInsertWithDto_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamWriteTargetFactory>();
        var obj = new ComplexParamWriteTarget { IsNew = true };

        var result = factory.SaveWithDto(obj, new SimpleDto { Id = 55, Name = "WriteDto" });

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Insert Dto Id: 55", result.Result);
    }

    [Fact]
    public void SaveUpdateWithDictionary_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamWriteTargetFactory>();
        var obj = new ComplexParamWriteTarget { IsNew = false, IsDeleted = false };
        var data = new Dictionary<string, int> { { "update1", 1 }, { "update2", 2 } };

        var result = factory.SaveWithDictionary(obj, data);

        Assert.NotNull(result);
        Assert.True(result!.UpdateCalled);
        Assert.Equal("Update Dictionary count: 2", result.Result);
    }

    [Fact]
    public void SaveDeleteWithIntList_Works()
    {
        var factory = _provider.GetRequiredService<IComplexParamWriteTargetFactory>();
        var obj = new ComplexParamWriteTarget { IsDeleted = true };

        var result = factory.SaveWithIntList(obj, new List<int> { 99, 100 });

        Assert.NotNull(result);
        Assert.True(result!.DeleteCalled);
        Assert.Equal("Delete IntList count: 2", result.Result);
    }

    #endregion
}
