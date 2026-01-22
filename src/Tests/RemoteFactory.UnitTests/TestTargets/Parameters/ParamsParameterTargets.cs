using Neatoo.RemoteFactory;

namespace RemoteFactory.UnitTests.TestTargets.Parameters;

/// <summary>
/// Test target for factory methods with params parameters.
/// </summary>
[Factory]
public partial class ParamsReadTarget
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public string? Result { get; set; }

    /// <summary>
    /// params int[] parameter.
    /// </summary>
    [Create]
    public void CreateWithParamsInt(params int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        CreateCalled = true;
        Result = $"ParamsInt count: {ids.Length}, sum: {ids.Sum()}";
    }

    /// <summary>
    /// params string[] parameter.
    /// </summary>
    [Create]
    public void CreateWithParamsString(params string[] names)
    {
        ArgumentNullException.ThrowIfNull(names);
        CreateCalled = true;
        Result = $"ParamsString count: {names.Length}, joined: {string.Join(",", names)}";
    }

    /// <summary>
    /// Mixed: regular parameter + params.
    /// </summary>
    [Create]
    public void CreateWithMixedParams(int id, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        CreateCalled = true;
        Result = $"Mixed id: {id}, tags: {tags.Length}";
    }

    /// <summary>
    /// Fetch with params.
    /// </summary>
    [Fetch]
    public void FetchWithParamsInt(params int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        FetchCalled = true;
        Result = $"Fetch ParamsInt count: {ids.Length}";
    }
}
