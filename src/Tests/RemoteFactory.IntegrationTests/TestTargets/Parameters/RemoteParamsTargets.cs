using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.Parameters;

/// <summary>
/// Test target for remote factory methods with params parameters.
/// Tests serialization of params arrays over the client-server boundary.
/// </summary>
[Factory]
public partial class RemoteParamsTarget
{
    public bool CreateCalled { get; set; }
    public bool WasCancelled { get; set; }
    public string? Result { get; set; }

    /// <summary>
    /// Remote params int[] parameter.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithParamsInt(params int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        CreateCalled = true;
        Result = $"Remote ParamsInt count: {ids.Length}, sum: {ids.Sum()}";
    }

    /// <summary>
    /// Remote mixed: regular parameter + params.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithMixedParams(int id, params string[] tags)
    {
        ArgumentNullException.ThrowIfNull(tags);
        CreateCalled = true;
        Result = $"Remote Mixed id: {id}, tags: {tags.Length}";
    }

    /// <summary>
    /// Remote params WITH CancellationToken - verifies CT flows through.
    /// </summary>
    [Create]
    [Remote]
    public void CreateRemoteWithParamsAndCancellation(CancellationToken ct, params int[] ids)
    {
        ArgumentNullException.ThrowIfNull(ids);
        CreateCalled = true;
        WasCancelled = ct.IsCancellationRequested;
        Result = $"Remote ParamsWithCT count: {ids.Length}, cancelled: {ct.IsCancellationRequested}";
    }
}
