using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Parameters;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.Parameters;

/// <summary>
/// Integration tests for remote operations with complex parameter types.
/// Verifies JSON serialization/deserialization across client-server boundary.
/// </summary>
public class RemoteComplexParameterTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IComplexParamRemoteTargetFactory _readFactory;
    private readonly IComplexParamRemoteWriteTargetFactory _writeFactory;

    public RemoteComplexParameterTests()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        _serverScope = server;
        _clientScope = client;
        _readFactory = _clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteTargetFactory>();
        _writeFactory = _clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteWriteTargetFactory>();
    }

    #region List Parameters

    [Fact]
    public async Task CreateRemoteWithIntList_Works()
    {
        var result = await _readFactory.CreateRemoteWithIntList(new List<int> { 10, 20, 30 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote IntList count: 3, sum: 60", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithStringList_Works()
    {
        var result = await _readFactory.CreateRemoteWithStringList(new List<string> { "One", "Two" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote StringList count: 2, joined: One,Two", result.Result);
    }

    #endregion

    #region Dictionary Parameters

    [Fact]
    public async Task CreateRemoteWithDictionary_Works()
    {
        var data = new Dictionary<string, int> { { "key1", 100 }, { "key2", 200 } };

        var result = await _readFactory.CreateRemoteWithDictionary(data);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Contains("Remote Dictionary count: 2", result.Result!);
    }

    #endregion

    #region DTO Parameters

    [Fact]
    public async Task CreateRemoteWithDto_Works()
    {
        var dto = new SimpleDto { Id = 123, Name = "RemoteDto", IsActive = false };

        var result = await _readFactory.CreateRemoteWithDto(dto);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Dto Id: 123, Name: RemoteDto, IsActive: False", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithNestedDto_Works()
    {
        var dto = new NestedDto
        {
            Id = Guid.NewGuid(),
            Title = "RemoteNested",
            Tags = new List<string> { "remote1", "remote2", "remote3" },
            Details = new SimpleDto { Id = 5, Name = "RemoteDetail" }
        };

        var result = await _readFactory.CreateRemoteWithNestedDto(dto);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Contains("Tags: 3", result.Result!);
        Assert.Contains("HasDetails: True", result.Result!);
    }

    #endregion

    #region Nullable Parameters

    [Fact]
    public async Task CreateRemoteWithNullableList_WithValue_Works()
    {
        var result = await _readFactory.CreateRemoteWithNullableList(new List<int> { 5, 10 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableList count: 2", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithNullableList_WithNull_Works()
    {
        var result = await _readFactory.CreateRemoteWithNullableList(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableList is null", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithNullableDto_WithValue_Works()
    {
        var result = await _readFactory.CreateRemoteWithNullableDto(new SimpleDto { Id = 77 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableDto Id: 77", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithNullableDto_WithNull_Works()
    {
        var result = await _readFactory.CreateRemoteWithNullableDto(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableDto is null", result.Result);
    }

    #endregion

    #region Mixed and Async Parameters

    [Fact]
    public async Task CreateRemoteWithMixedParams_Works()
    {
        var result = await _readFactory.CreateRemoteWithMixedParams(
            999,
            new List<string> { "x", "y", "z" },
            new SimpleDto { Id = 88, Name = "RemoteMixed" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Mixed Id: 999, Tags: 3, DtoId: 88", result.Result);
    }

    [Fact]
    public async Task CreateRemoteWithComplexParamsAsync_Works()
    {
        var result = await _readFactory.CreateRemoteWithComplexParamsAsync(
            new List<int> { 100, 200, 300 },
            new Dictionary<string, int> { { "alpha", 1 }, { "beta", 2 } });

        Assert.NotNull(result);
        Assert.True(result!.CreateCalled);
        Assert.Equal("Remote Async IntList: 3, Dict: 2", result.Result);
    }

    #endregion

    #region Fetch Tests

    [Fact]
    public async Task FetchRemoteWithIntList_Works()
    {
        var result = await _readFactory.FetchRemoteWithIntList(new List<int> { 111, 222 });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Remote Fetch IntList count: 2", result.Result);
    }

    [Fact]
    public async Task FetchRemoteWithDto_Works()
    {
        var result = await _readFactory.FetchRemoteWithDto(new SimpleDto { Id = 999, Name = "RemoteFetchDto" });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Remote Fetch Dto Name: RemoteFetchDto", result.Result);
    }

    #endregion

    #region Write Tests

    [Fact]
    public async Task SaveRemoteInsertWithIntList_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsNew = true;

        var result = await _writeFactory.SaveRemoteWithIntList(obj, new List<int> { 10, 20, 30, 40 });

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Remote Insert IntList count: 4", result.Result);
    }

    [Fact]
    public async Task SaveRemoteInsertWithDto_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsNew = true;

        var result = await _writeFactory.SaveRemoteWithDto(obj, new SimpleDto { Id = 333, Name = "RemoteWriteDto" });

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Remote Insert Dto Id: 333", result.Result);
    }

    [Fact]
    public async Task SaveRemoteUpdateWithDictionary_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsNew = false;
        obj.IsDeleted = false;
        var data = new Dictionary<string, int> { { "remoteKey", 500 } };

        var result = await _writeFactory.SaveRemoteWithDictionary(obj, data);

        Assert.NotNull(result);
        Assert.True(result!.UpdateCalled);
        Assert.Equal("Remote Update Dictionary count: 1", result.Result);
    }

    [Fact]
    public async Task SaveRemoteDeleteWithIntList_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsDeleted = true;

        var result = await _writeFactory.SaveRemoteWithIntList(obj, new List<int> { 1000 });

        Assert.NotNull(result);
        Assert.True(result!.DeleteCalled);
        Assert.Equal("Remote Delete IntList count: 1", result.Result);
    }

    [Fact]
    public async Task SaveRemoteInsertWithComplexParamsAsync_Works()
    {
        var obj = _writeFactory.Create();
        obj.IsNew = true;

        var result = await _writeFactory.SaveRemoteWithComplexParamsAsync(
            obj,
            new List<string> { "async1", "async2" },
            new SimpleDto { Id = 444, Name = "AsyncDto" });

        Assert.NotNull(result);
        Assert.True(result!.InsertCalled);
        Assert.Equal("Remote Async Insert Tags: 2, DtoId: 444", result.Result);
    }

    #endregion
}
