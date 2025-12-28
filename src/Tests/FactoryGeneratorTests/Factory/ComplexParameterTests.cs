using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests for domain classes with complex parameter types (List<T>, Dictionary, custom objects).
/// This addresses GAP-006 from the test plan: Complex Parameter Serialization - completely untested.
///
/// Tests verify:
/// - List<int>, List<string> parameters serialize correctly for remote calls
/// - Dictionary<string, int> parameters serialize correctly
/// - Custom DTO class parameters serialize correctly
/// - Nullable complex types (List<int>?, CustomDto?)
/// - Both Create/Fetch and Insert/Update/Delete operations with complex params
/// - Remote execution to verify JSON serialization works correctly
/// </summary>

#region DTO Classes

/// <summary>
/// Simple DTO for testing complex parameter serialization.
/// </summary>
public class SimpleDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; }
}

/// <summary>
/// Nested DTO containing another DTO and collections.
/// </summary>
public class NestedDto
{
    public Guid Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public SimpleDto? Details { get; set; }
    public List<string> Tags { get; set; } = new();
}

#endregion

#region Domain Classes

/// <summary>
/// Domain class with Create/Fetch operations using complex parameter types.
/// </summary>
[Factory]
public class ComplexParamReadObject
{
    // Tracking properties
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? Result { get; set; }

    // List<int> parameter
    [Create]
    public void CreateWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.CreateCalled = true;
        this.Result = $"IntList count: {ids.Count}, sum: {ids.Sum()}";
    }

    // List<string> parameter
    [Create]
    public void CreateWithStringList(List<string> names)
    {
        Assert.NotNull(names);
        this.CreateCalled = true;
        this.Result = $"StringList count: {names.Count}, joined: {string.Join(",", names)}";
    }

    // Dictionary<string, int> parameter
    [Create]
    public void CreateWithDictionary(Dictionary<string, int> data)
    {
        Assert.NotNull(data);
        this.CreateCalled = true;
        this.Result = $"Dictionary count: {data.Count}, keys: {string.Join(",", data.Keys)}";
    }

    // Custom DTO parameter
    [Create]
    public void CreateWithDto(SimpleDto dto)
    {
        Assert.NotNull(dto);
        this.CreateCalled = true;
        this.Result = $"Dto Id: {dto.Id}, Name: {dto.Name}, IsActive: {dto.IsActive}";
    }

    // Nested DTO parameter
    [Create]
    public void CreateWithNestedDto(NestedDto dto)
    {
        Assert.NotNull(dto);
        this.CreateCalled = true;
        this.Result = $"NestedDto Id: {dto.Id}, Title: {dto.Title}, Tags: {dto.Tags.Count}, HasDetails: {dto.Details != null}";
    }

    // Nullable List parameter
    [Create]
    public void CreateWithNullableList(List<int>? optionalIds)
    {
        this.CreateCalled = true;
        this.Result = optionalIds == null
            ? "NullableList is null"
            : $"NullableList count: {optionalIds.Count}";
    }

    // Nullable DTO parameter
    [Create]
    public void CreateWithNullableDto(SimpleDto? optionalDto)
    {
        this.CreateCalled = true;
        this.Result = optionalDto == null
            ? "NullableDto is null"
            : $"NullableDto Id: {optionalDto.Id}";
    }

    // Mixed complex and simple parameters
    [Create]
    public void CreateWithMixedParams(int id, List<string> tags, SimpleDto dto)
    {
        Assert.NotNull(tags);
        Assert.NotNull(dto);
        this.CreateCalled = true;
        this.Result = $"Mixed Id: {id}, Tags: {tags.Count}, DtoId: {dto.Id}";
    }

    // Async with complex params
    [Create]
    public Task<bool> CreateWithComplexParamsAsync(List<int> ids, Dictionary<string, int> data)
    {
        Assert.NotNull(ids);
        Assert.NotNull(data);
        this.CreateCalled = true;
        this.Result = $"Async IntList: {ids.Count}, Dict: {data.Count}";
        return Task.FromResult(true);
    }

    // Fetch with complex params
    [Fetch]
    public void FetchWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.FetchCalled = true;
        this.Result = $"Fetch IntList count: {ids.Count}";
    }

    [Fetch]
    public void FetchWithDto(SimpleDto dto)
    {
        Assert.NotNull(dto);
        this.FetchCalled = true;
        this.Result = $"Fetch Dto Name: {dto.Name}";
    }
}

/// <summary>
/// Domain class with remote operations using complex parameter types.
/// These test JSON serialization/deserialization across client-server boundary.
/// </summary>
[Factory]
public class ComplexParamRemoteObject
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? Result { get; set; }

    // Remote List<int> parameter
    [Create]
    [Remote]
    public void CreateRemoteWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.CreateCalled = true;
        this.Result = $"Remote IntList count: {ids.Count}, sum: {ids.Sum()}";
    }

    // Remote List<string> parameter
    [Create]
    [Remote]
    public void CreateRemoteWithStringList(List<string> names)
    {
        Assert.NotNull(names);
        this.CreateCalled = true;
        this.Result = $"Remote StringList count: {names.Count}, joined: {string.Join(",", names)}";
    }

    // Remote Dictionary parameter
    [Create]
    [Remote]
    public void CreateRemoteWithDictionary(Dictionary<string, int> data)
    {
        Assert.NotNull(data);
        this.CreateCalled = true;
        this.Result = $"Remote Dictionary count: {data.Count}, keys: {string.Join(",", data.Keys)}";
    }

    // Remote custom DTO parameter
    [Create]
    [Remote]
    public void CreateRemoteWithDto(SimpleDto dto)
    {
        Assert.NotNull(dto);
        this.CreateCalled = true;
        this.Result = $"Remote Dto Id: {dto.Id}, Name: {dto.Name}, IsActive: {dto.IsActive}";
    }

    // Remote nested DTO parameter
    [Create]
    [Remote]
    public void CreateRemoteWithNestedDto(NestedDto dto)
    {
        Assert.NotNull(dto);
        this.CreateCalled = true;
        this.Result = $"Remote NestedDto Id: {dto.Id}, Title: {dto.Title}, Tags: {dto.Tags.Count}, HasDetails: {dto.Details != null}";
    }

    // Remote nullable List parameter
    [Create]
    [Remote]
    public void CreateRemoteWithNullableList(List<int>? optionalIds)
    {
        this.CreateCalled = true;
        this.Result = optionalIds == null
            ? "Remote NullableList is null"
            : $"Remote NullableList count: {optionalIds.Count}";
    }

    // Remote nullable DTO parameter
    [Create]
    [Remote]
    public void CreateRemoteWithNullableDto(SimpleDto? optionalDto)
    {
        this.CreateCalled = true;
        this.Result = optionalDto == null
            ? "Remote NullableDto is null"
            : $"Remote NullableDto Id: {optionalDto.Id}";
    }

    // Remote mixed parameters
    [Create]
    [Remote]
    public void CreateRemoteWithMixedParams(int id, List<string> tags, SimpleDto dto)
    {
        Assert.NotNull(tags);
        Assert.NotNull(dto);
        this.CreateCalled = true;
        this.Result = $"Remote Mixed Id: {id}, Tags: {tags.Count}, DtoId: {dto.Id}";
    }

    // Remote async with complex params
    [Create]
    [Remote]
    public Task<bool> CreateRemoteWithComplexParamsAsync(List<int> ids, Dictionary<string, int> data)
    {
        Assert.NotNull(ids);
        Assert.NotNull(data);
        this.CreateCalled = true;
        this.Result = $"Remote Async IntList: {ids.Count}, Dict: {data.Count}";
        return Task.FromResult(true);
    }

    // Remote Fetch with complex params
    [Fetch]
    [Remote]
    public void FetchRemoteWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.FetchCalled = true;
        this.Result = $"Remote Fetch IntList count: {ids.Count}";
    }

    [Fetch]
    [Remote]
    public void FetchRemoteWithDto(SimpleDto dto)
    {
        Assert.NotNull(dto);
        this.FetchCalled = true;
        this.Result = $"Remote Fetch Dto Name: {dto.Name}";
    }
}

/// <summary>
/// Domain class with write operations using complex parameter types.
/// </summary>
[Factory]
public class ComplexParamWriteObject : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? Result { get; set; }

    // Insert with List parameter
    [Insert]
    public void InsertWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.InsertCalled = true;
        this.Result = $"Insert IntList count: {ids.Count}";
    }

    // Insert with DTO parameter
    [Insert]
    public void InsertWithDto(SimpleDto dto)
    {
        Assert.NotNull(dto);
        this.InsertCalled = true;
        this.Result = $"Insert Dto Id: {dto.Id}";
    }

    // Update with Dictionary parameter
    [Update]
    public void UpdateWithDictionary(Dictionary<string, int> data)
    {
        Assert.NotNull(data);
        this.UpdateCalled = true;
        this.Result = $"Update Dictionary count: {data.Count}";
    }

    // Update with DTO parameter
    [Update]
    public void UpdateWithDto(SimpleDto dto)
    {
        Assert.NotNull(dto);
        this.UpdateCalled = true;
        this.Result = $"Update Dto Id: {dto.Id}";
    }

    // Delete with List parameter
    [Delete]
    public void DeleteWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.DeleteCalled = true;
        this.Result = $"Delete IntList count: {ids.Count}";
    }
}

/// <summary>
/// Domain class with remote write operations using complex parameter types.
/// </summary>
[Factory]
public class ComplexParamRemoteWriteObject : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? Result { get; set; }

    // Remote Insert with List parameter
    [Insert]
    [Remote]
    public void InsertRemoteWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.InsertCalled = true;
        this.Result = $"Remote Insert IntList count: {ids.Count}";
    }

    // Remote Insert with DTO parameter
    [Insert]
    [Remote]
    public void InsertRemoteWithDto(SimpleDto dto)
    {
        Assert.NotNull(dto);
        this.InsertCalled = true;
        this.Result = $"Remote Insert Dto Id: {dto.Id}";
    }

    // Remote Update with Dictionary parameter
    [Update]
    [Remote]
    public void UpdateRemoteWithDictionary(Dictionary<string, int> data)
    {
        Assert.NotNull(data);
        this.UpdateCalled = true;
        this.Result = $"Remote Update Dictionary count: {data.Count}";
    }

    // Remote Delete with List parameter
    [Delete]
    [Remote]
    public void DeleteRemoteWithIntList(List<int> ids)
    {
        Assert.NotNull(ids);
        this.DeleteCalled = true;
        this.Result = $"Remote Delete IntList count: {ids.Count}";
    }

    // Remote async Insert with complex params
    [Insert]
    [Remote]
    public Task InsertRemoteWithComplexParamsAsync(List<string> tags, SimpleDto dto)
    {
        Assert.NotNull(tags);
        Assert.NotNull(dto);
        this.InsertCalled = true;
        this.Result = $"Remote Async Insert Tags: {tags.Count}, DtoId: {dto.Id}";
        return Task.CompletedTask;
    }
}

#endregion

#region Test Class

public class ComplexParameterTests
{
    private readonly IServiceScope clientScope;
    private readonly IServiceScope localScope;

    public ComplexParameterTests()
    {
        var scopes = ClientServerContainers.Scopes();
        this.clientScope = scopes.client;
        this.localScope = scopes.local;
    }

    #region Local Read Tests - List Parameters

    [Fact]
    public void ComplexParam_CreateWithIntList_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.CreateWithIntList(new List<int> { 1, 2, 3, 4, 5 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("IntList count: 5, sum: 15", result.Result);
    }

    [Fact]
    public void ComplexParam_CreateWithStringList_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.CreateWithStringList(new List<string> { "Alpha", "Beta", "Gamma" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("StringList count: 3, joined: Alpha,Beta,Gamma", result.Result);
    }

    #endregion

    #region Local Read Tests - Dictionary Parameters

    [Fact]
    public void ComplexParam_CreateWithDictionary_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();
        var data = new Dictionary<string, int> { { "one", 1 }, { "two", 2 }, { "three", 3 } };

        var result = factory.CreateWithDictionary(data);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Contains("Dictionary count: 3", result.Result!);
    }

    #endregion

    #region Local Read Tests - DTO Parameters

    [Fact]
    public void ComplexParam_CreateWithDto_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();
        var dto = new SimpleDto { Id = 42, Name = "TestDto", IsActive = true };

        var result = factory.CreateWithDto(dto);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Dto Id: 42, Name: TestDto, IsActive: True", result.Result);
    }

    [Fact]
    public void ComplexParam_CreateWithNestedDto_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();
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

    #region Local Read Tests - Nullable Parameters

    [Fact]
    public void ComplexParam_CreateWithNullableList_WithValue_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.CreateWithNullableList(new List<int> { 1, 2, 3 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableList count: 3", result.Result);
    }

    [Fact]
    public void ComplexParam_CreateWithNullableList_WithNull_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.CreateWithNullableList(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableList is null", result.Result);
    }

    [Fact]
    public void ComplexParam_CreateWithNullableDto_WithValue_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.CreateWithNullableDto(new SimpleDto { Id = 99, Name = "Test" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableDto Id: 99", result.Result);
    }

    [Fact]
    public void ComplexParam_CreateWithNullableDto_WithNull_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.CreateWithNullableDto(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("NullableDto is null", result.Result);
    }

    #endregion

    #region Local Read Tests - Mixed and Async Parameters

    [Fact]
    public void ComplexParam_CreateWithMixedParams_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.CreateWithMixedParams(
            100,
            new List<string> { "tag1", "tag2" },
            new SimpleDto { Id = 50, Name = "Mixed" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Mixed Id: 100, Tags: 2, DtoId: 50", result.Result);
    }

    [Fact]
    public async Task ComplexParam_CreateWithComplexParamsAsync_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = await factory.CreateWithComplexParamsAsync(
            new List<int> { 1, 2 },
            new Dictionary<string, int> { { "a", 1 } });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Async IntList: 2, Dict: 1", result.Result);
    }

    #endregion

    #region Remote Read Tests - List Parameters

    [Fact]
    public async Task ComplexParam_CreateRemoteWithIntList_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithIntList(new List<int> { 10, 20, 30 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote IntList count: 3, sum: 60", result.Result);
    }

    [Fact]
    public async Task ComplexParam_CreateRemoteWithStringList_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithStringList(new List<string> { "One", "Two" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote StringList count: 2, joined: One,Two", result.Result);
    }

    #endregion

    #region Remote Read Tests - Dictionary Parameters

    [Fact]
    public async Task ComplexParam_CreateRemoteWithDictionary_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();
        var data = new Dictionary<string, int> { { "key1", 100 }, { "key2", 200 } };

        var result = await factory.CreateRemoteWithDictionary(data);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Contains("Remote Dictionary count: 2", result.Result!);
    }

    #endregion

    #region Remote Read Tests - DTO Parameters

    [Fact]
    public async Task ComplexParam_CreateRemoteWithDto_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();
        var dto = new SimpleDto { Id = 123, Name = "RemoteDto", IsActive = false };

        var result = await factory.CreateRemoteWithDto(dto);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Dto Id: 123, Name: RemoteDto, IsActive: False", result.Result);
    }

    [Fact]
    public async Task ComplexParam_CreateRemoteWithNestedDto_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();
        var dto = new NestedDto
        {
            Id = Guid.NewGuid(),
            Title = "RemoteNested",
            Tags = new List<string> { "remote1", "remote2", "remote3" },
            Details = new SimpleDto { Id = 5, Name = "RemoteDetail" }
        };

        var result = await factory.CreateRemoteWithNestedDto(dto);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Contains("Tags: 3", result.Result!);
        Assert.Contains("HasDetails: True", result.Result!);
    }

    #endregion

    #region Remote Read Tests - Nullable Parameters

    [Fact]
    public async Task ComplexParam_CreateRemoteWithNullableList_WithValue_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithNullableList(new List<int> { 5, 10 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableList count: 2", result.Result);
    }

    [Fact]
    public async Task ComplexParam_CreateRemoteWithNullableList_WithNull_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithNullableList(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableList is null", result.Result);
    }

    [Fact]
    public async Task ComplexParam_CreateRemoteWithNullableDto_WithValue_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithNullableDto(new SimpleDto { Id = 77 });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableDto Id: 77", result.Result);
    }

    [Fact]
    public async Task ComplexParam_CreateRemoteWithNullableDto_WithNull_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithNullableDto(null);

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote NullableDto is null", result.Result);
    }

    #endregion

    #region Remote Read Tests - Mixed and Async Parameters

    [Fact]
    public async Task ComplexParam_CreateRemoteWithMixedParams_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithMixedParams(
            999,
            new List<string> { "x", "y", "z" },
            new SimpleDto { Id = 88, Name = "RemoteMixed" });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Mixed Id: 999, Tags: 3, DtoId: 88", result.Result);
    }

    [Fact]
    public async Task ComplexParam_CreateRemoteWithComplexParamsAsync_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.CreateRemoteWithComplexParamsAsync(
            new List<int> { 100, 200, 300 },
            new Dictionary<string, int> { { "alpha", 1 }, { "beta", 2 } });

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
        Assert.Equal("Remote Async IntList: 3, Dict: 2", result.Result);
    }

    #endregion

    #region Local Write Tests

    [Fact]
    public void ComplexParam_SaveInsertWithIntList_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamWriteObjectFactory>();
        var obj = new ComplexParamWriteObject { IsNew = true };

        var result = factory.SaveWithIntList(obj, new List<int> { 1, 2, 3, 4 });

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Insert IntList count: 4", result.Result);
    }

    [Fact]
    public void ComplexParam_SaveInsertWithDto_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamWriteObjectFactory>();
        var obj = new ComplexParamWriteObject { IsNew = true };

        var result = factory.SaveWithDto(obj, new SimpleDto { Id = 55, Name = "WriteDto" });

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Insert Dto Id: 55", result.Result);
    }

    [Fact]
    public void ComplexParam_SaveUpdateWithDictionary_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamWriteObjectFactory>();
        var obj = new ComplexParamWriteObject { IsNew = false, IsDeleted = false };
        var data = new Dictionary<string, int> { { "update1", 1 }, { "update2", 2 } };

        var result = factory.SaveWithDictionary(obj, data);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal("Update Dictionary count: 2", result.Result);
    }

    [Fact]
    public void ComplexParam_SaveDeleteWithIntList_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamWriteObjectFactory>();
        var obj = new ComplexParamWriteObject { IsDeleted = true };

        var result = factory.SaveWithIntList(obj, new List<int> { 99, 100 });

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal("Delete IntList count: 2", result.Result);
    }

    #endregion

    #region Remote Write Tests

    [Fact]
    public async Task ComplexParam_SaveRemoteInsertWithIntList_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteWriteObjectFactory>();
        var obj = new ComplexParamRemoteWriteObject { IsNew = true };

        var result = await factory.SaveRemoteWithIntList(obj, new List<int> { 10, 20, 30, 40 });

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Remote Insert IntList count: 4", result.Result);
    }

    [Fact]
    public async Task ComplexParam_SaveRemoteInsertWithDto_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteWriteObjectFactory>();
        var obj = new ComplexParamRemoteWriteObject { IsNew = true };

        var result = await factory.SaveRemoteWithDto(obj, new SimpleDto { Id = 333, Name = "RemoteWriteDto" });

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Remote Insert Dto Id: 333", result.Result);
    }

    [Fact]
    public async Task ComplexParam_SaveRemoteUpdateWithDictionary_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteWriteObjectFactory>();
        var obj = new ComplexParamRemoteWriteObject { IsNew = false, IsDeleted = false };
        var data = new Dictionary<string, int> { { "remoteKey", 500 } };

        var result = await factory.SaveRemoteWithDictionary(obj, data);

        Assert.NotNull(result);
        Assert.True(result.UpdateCalled);
        Assert.Equal("Remote Update Dictionary count: 1", result.Result);
    }

    [Fact]
    public async Task ComplexParam_SaveRemoteDeleteWithIntList_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteWriteObjectFactory>();
        var obj = new ComplexParamRemoteWriteObject { IsDeleted = true };

        var result = await factory.SaveRemoteWithIntList(obj, new List<int> { 1000 });

        Assert.NotNull(result);
        Assert.True(result.DeleteCalled);
        Assert.Equal("Remote Delete IntList count: 1", result.Result);
    }

    [Fact]
    public async Task ComplexParam_SaveRemoteInsertWithComplexParamsAsync_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteWriteObjectFactory>();
        var obj = new ComplexParamRemoteWriteObject { IsNew = true };

        var result = await factory.SaveRemoteWithComplexParamsAsync(
            obj,
            new List<string> { "async1", "async2" },
            new SimpleDto { Id = 444, Name = "AsyncDto" });

        Assert.NotNull(result);
        Assert.True(result.InsertCalled);
        Assert.Equal("Remote Async Insert Tags: 2, DtoId: 444", result.Result);
    }

    #endregion

    #region Fetch Tests

    [Fact]
    public void ComplexParam_FetchWithIntList_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.FetchWithIntList(new List<int> { 7, 8, 9 });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch IntList count: 3", result.Result);
    }

    [Fact]
    public void ComplexParam_FetchWithDto_LocalExecution()
    {
        var factory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();

        var result = factory.FetchWithDto(new SimpleDto { Id = 11, Name = "FetchDto" });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Fetch Dto Name: FetchDto", result.Result);
    }

    [Fact]
    public async Task ComplexParam_FetchRemoteWithIntList_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.FetchRemoteWithIntList(new List<int> { 111, 222 });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Remote Fetch IntList count: 2", result.Result);
    }

    [Fact]
    public async Task ComplexParam_FetchRemoteWithDto_RemoteExecution()
    {
        var factory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamRemoteObjectFactory>();

        var result = await factory.FetchRemoteWithDto(new SimpleDto { Id = 999, Name = "RemoteFetchDto" });

        Assert.NotNull(result);
        Assert.True(result.FetchCalled);
        Assert.Equal("Remote Fetch Dto Name: RemoteFetchDto", result.Result);
    }

    #endregion

    #region Comparison Tests - Client vs Local

    [Fact]
    public void ComplexParam_CreateWithIntList_ClientAndLocalBehaveSame()
    {
        var clientFactory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();
        var localFactory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();
        var ids = new List<int> { 1, 2, 3 };

        var clientResult = clientFactory.CreateWithIntList(ids);
        var localResult = localFactory.CreateWithIntList(ids);

        Assert.Equal(clientResult.Result, localResult.Result);
        Assert.True(clientResult.CreateCalled);
        Assert.True(localResult.CreateCalled);
    }

    [Fact]
    public void ComplexParam_CreateWithDto_ClientAndLocalBehaveSame()
    {
        var clientFactory = this.clientScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();
        var localFactory = this.localScope.ServiceProvider.GetRequiredService<IComplexParamReadObjectFactory>();
        var dto = new SimpleDto { Id = 42, Name = "TestDto", IsActive = true };

        var clientResult = clientFactory.CreateWithDto(dto);
        var localResult = localFactory.CreateWithDto(dto);

        Assert.Equal(clientResult.Result, localResult.Result);
        Assert.True(clientResult.CreateCalled);
        Assert.True(localResult.CreateCalled);
    }

    #endregion
}

#endregion
