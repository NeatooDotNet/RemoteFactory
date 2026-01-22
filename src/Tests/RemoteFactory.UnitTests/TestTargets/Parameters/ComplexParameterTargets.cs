using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Parameters;

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

#region Read Operations with Complex Parameters

/// <summary>
/// Test target with Create/Fetch operations using complex parameter types.
/// </summary>
[Factory]
public partial class ComplexParamReadTarget
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? Result { get; set; }

    /// <summary>
    /// List&lt;int&gt; parameter.
    /// </summary>
    [Create]
    public void CreateWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        CreateCalled = true;
        Result = $"IntList count: {ids.Count}, sum: {ids.Sum()}";
    }

    /// <summary>
    /// List&lt;string&gt; parameter.
    /// </summary>
    [Create]
    public void CreateWithStringList(List<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        CreateCalled = true;
        Result = $"StringList count: {names.Count}, joined: {string.Join(",", names)}";
    }

    /// <summary>
    /// Dictionary&lt;string, int&gt; parameter.
    /// </summary>
    [Create]
    public void CreateWithDictionary(Dictionary<string, int> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        CreateCalled = true;
        Result = $"Dictionary count: {data.Count}, keys: {string.Join(",", data.Keys)}";
    }

    /// <summary>
    /// Custom DTO parameter.
    /// </summary>
    [Create]
    public void CreateWithDto(SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CreateCalled = true;
        Result = $"Dto Id: {dto.Id}, Name: {dto.Name}, IsActive: {dto.IsActive}";
    }

    /// <summary>
    /// Nested DTO parameter.
    /// </summary>
    [Create]
    public void CreateWithNestedDto(NestedDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        CreateCalled = true;
        Result = $"NestedDto Id: {dto.Id}, Title: {dto.Title}, Tags: {dto.Tags.Count}, HasDetails: {dto.Details != null}";
    }

    /// <summary>
    /// Nullable List parameter.
    /// </summary>
    [Create]
    public void CreateWithNullableList(List<int>? optionalIds)
    {
        CreateCalled = true;
        Result = optionalIds == null
            ? "NullableList is null"
            : $"NullableList count: {optionalIds.Count}";
    }

    /// <summary>
    /// Nullable DTO parameter.
    /// </summary>
    [Create]
    public void CreateWithNullableDto(SimpleDto? optionalDto)
    {
        CreateCalled = true;
        Result = optionalDto == null
            ? "NullableDto is null"
            : $"NullableDto Id: {optionalDto.Id}";
    }

    /// <summary>
    /// Mixed complex and simple parameters.
    /// </summary>
    [Create]
    public void CreateWithMixedParams(int id, List<string> tags, SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(tags);
        ArgumentNullException.ThrowIfNull(dto);
        CreateCalled = true;
        Result = $"Mixed Id: {id}, Tags: {tags.Count}, DtoId: {dto.Id}";
    }

    /// <summary>
    /// Async with complex params.
    /// </summary>
    [Create]
    public Task<bool> CreateWithComplexParamsAsync(List<int> ids, Dictionary<string, int> data)
    {
        ArgumentNullException.ThrowIfNull(ids);
        ArgumentNullException.ThrowIfNull(data);
        CreateCalled = true;
        Result = $"Async IntList: {ids.Count}, Dict: {data.Count}";
        return Task.FromResult(true);
    }

    /// <summary>
    /// Fetch with complex params.
    /// </summary>
    [Fetch]
    public void FetchWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        FetchCalled = true;
        Result = $"Fetch IntList count: {ids.Count}";
    }

    [Fetch]
    public void FetchWithDto(SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        FetchCalled = true;
        Result = $"Fetch Dto Name: {dto.Name}";
    }
}

#endregion

#region Write Operations with Complex Parameters

/// <summary>
/// Test target with write operations using complex parameter types.
/// </summary>
[Factory]
public partial class ComplexParamWriteTarget : IFactorySaveMeta
{
    public bool IsNew { get; set; }
    public bool IsDeleted { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public string? Result { get; set; }

    /// <summary>
    /// Insert with List parameter.
    /// </summary>
    [Insert]
    public void InsertWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        InsertCalled = true;
        Result = $"Insert IntList count: {ids.Count}";
    }

    /// <summary>
    /// Insert with DTO parameter.
    /// </summary>
    [Insert]
    public void InsertWithDto(SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        InsertCalled = true;
        Result = $"Insert Dto Id: {dto.Id}";
    }

    /// <summary>
    /// Update with Dictionary parameter.
    /// </summary>
    [Update]
    public void UpdateWithDictionary(Dictionary<string, int> data)
    {
        ArgumentNullException.ThrowIfNull(data);
        UpdateCalled = true;
        Result = $"Update Dictionary count: {data.Count}";
    }

    /// <summary>
    /// Update with DTO parameter.
    /// </summary>
    [Update]
    public void UpdateWithDto(SimpleDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        UpdateCalled = true;
        Result = $"Update Dto Id: {dto.Id}";
    }

    /// <summary>
    /// Delete with List parameter.
    /// </summary>
    [Delete]
    public void DeleteWithIntList(List<int> ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        DeleteCalled = true;
        Result = $"Delete IntList count: {ids.Count}";
    }
}

#endregion
