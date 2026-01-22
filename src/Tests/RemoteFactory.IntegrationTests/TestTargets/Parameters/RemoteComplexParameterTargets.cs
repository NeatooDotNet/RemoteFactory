using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.Parameters;

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

#region Remote Read Operations with Complex Parameters

/// <summary>
/// Test target with remote operations using complex parameter types.
/// Tests JSON serialization/deserialization across client-server boundary.
/// </summary>
[Factory]
public partial class ComplexParamRemoteTarget
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? Result { get; set; }

    [Create]
    public ComplexParamRemoteTarget() { }

    /// <summary>
    /// Remote List&lt;int&gt; parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        CreateCalled = true;
        Result = $"Remote IntList count: {ids.Count}, sum: {ids.Sum()}";
    }

    /// <summary>
    /// Remote List&lt;string&gt; parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithStringList(List<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        CreateCalled = true;
        Result = $"Remote StringList count: {names.Count}, joined: {string.Join(",", names)}";
    }

    /// <summary>
    /// Remote Dictionary parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithDictionary(Dictionary<string, int> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        CreateCalled = true;
        Result = $"Remote Dictionary count: {data.Count}, keys: {string.Join(",", data.Keys)}";
    }

    /// <summary>
    /// Remote custom DTO parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithDto(SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CreateCalled = true;
        Result = $"Remote Dto Id: {dto.Id}, Name: {dto.Name}, IsActive: {dto.IsActive}";
    }

    /// <summary>
    /// Remote nested DTO parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithNestedDto(NestedDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CreateCalled = true;
        Result = $"Remote NestedDto Id: {dto.Id}, Title: {dto.Title}, Tags: {dto.Tags.Count}, HasDetails: {dto.Details != null}";
    }

    /// <summary>
    /// Remote nullable List parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithNullableList(List<int>? optionalIds)
    {
        CreateCalled = true;
        Result = optionalIds == null
            ? "Remote NullableList is null"
            : $"Remote NullableList count: {optionalIds.Count}";
    }

    /// <summary>
    /// Remote nullable DTO parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithNullableDto(SimpleDto? optionalDto)
    {
        CreateCalled = true;
        Result = optionalDto == null
            ? "Remote NullableDto is null"
            : $"Remote NullableDto Id: {optionalDto.Id}";
    }

    /// <summary>
    /// Remote mixed parameters.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithMixedParams(int id, List<string> tags, SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(dto);
        CreateCalled = true;
        Result = $"Remote Mixed Id: {id}, Tags: {tags.Count}, DtoId: {dto.Id}";
    }

    /// <summary>
    /// Remote async with complex params.
    /// </summary>
    [Create]
    [Remote]
    public Task<bool> CreateRemoteWithComplexParamsAsync(List<int> ids, Dictionary<string, int> data)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(data);
        CreateCalled = true;
        Result = $"Remote Async IntList: {ids.Count}, Dict: {data.Count}";
        return Task.FromResult(true);
    }

    /// <summary>
    /// Remote Fetch with complex params.
    /// </summary>
    [Fetch]
    [Remote]
    public void FetchRemoteWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        FetchCalled = true;
        Result = $"Remote Fetch IntList count: {ids.Count}";
    }

    [Fetch]
    [Remote]
    public void FetchRemoteWithDto(SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        FetchCalled = true;
        Result = $"Remote Fetch Dto Name: {dto.Name}";
    }
}

#endregion

#region Remote Write Operations with Complex Parameters

/// <summary>
/// Test target with remote write operations using complex parameter types.
/// </summary>
[Factory]
public partial class ComplexParamRemoteWriteTarget : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? Result { get; set; }

    [Create]
    public ComplexParamRemoteWriteTarget() { }

    /// <summary>
    /// Remote Insert with List parameter.
    /// </summary>
    [Insert]
    [Remote]
    public void InsertRemoteWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        InsertCalled = true;
        Result = $"Remote Insert IntList count: {ids.Count}";
    }

    /// <summary>
    /// Remote Insert with DTO parameter.
    /// </summary>
    [Insert]
    [Remote]
    public void InsertRemoteWithDto(SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        InsertCalled = true;
        Result = $"Remote Insert Dto Id: {dto.Id}";
    }

    /// <summary>
    /// Remote Update with Dictionary parameter.
    /// </summary>
    [Update]
    [Remote]
    public void UpdateRemoteWithDictionary(Dictionary<string, int> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        UpdateCalled = true;
        Result = $"Remote Update Dictionary count: {data.Count}";
    }

    /// <summary>
    /// Remote Delete with List parameter.
    /// </summary>
    [Delete]
    [Remote]
    public void DeleteRemoteWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        DeleteCalled = true;
        Result = $"Remote Delete IntList count: {ids.Count}";
    }

    /// <summary>
    /// Remote async Insert with complex params.
    /// </summary>
    [Insert]
    [Remote]
    public Task InsertRemoteWithComplexParamsAsync(List<string> tags, SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(dto);
        InsertCalled = true;
        Result = $"Remote Async Insert Tags: {tags.Count}, DtoId: {dto.Id}";
        return Task.CompletedTask;
    }
}

#endregion
