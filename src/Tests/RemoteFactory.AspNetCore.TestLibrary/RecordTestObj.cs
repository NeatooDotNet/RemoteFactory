namespace Neatoo.RemoteFactory.AspNetCore.TestLibrary;

/// <summary>
/// Simple record for HTTP integration testing.
/// Tests that records work correctly through the full HTTP pipeline.
/// </summary>
[Factory]
[Create]
public partial record HttpTestRecord(string Name, int Value);

/// <summary>
/// Record with remote fetch operation for HTTP testing.
/// </summary>
[Factory]
[Create]
public partial record HttpFetchableRecord(string Id, string Data)
{
    [Fetch]
    [Remote]
    public static HttpFetchableRecord FetchById(string id)
        => new HttpFetchableRecord(id, $"HttpFetched-{id}");

    [Fetch]
    [Remote]
    public static Task<HttpFetchableRecord> FetchByIdAsync(string id)
        => Task.FromResult(new HttpFetchableRecord(id, $"HttpAsyncFetched-{id}"));
}

/// <summary>
/// Record with ASP.NET Core authorization for HTTP testing.
/// Uses AspAuthorize attribute for policy-based authorization.
/// </summary>
[Factory]
[Create]
public partial record HttpAuthorizedRecord(string Name)
{
    [Fetch]
    [Remote]
    [AspAuthorize("TestPolicy", Roles = "Test role")]
    public static HttpAuthorizedRecord FetchAuthorized(string name)
        => new HttpAuthorizedRecord($"Authorized-{name}");

    [Fetch]
    [Remote]
    [AspAuthorize(Roles = "No auth")]
    public static HttpAuthorizedRecord FetchUnauthorized(string name)
        => new HttpAuthorizedRecord($"Unauthorized-{name}");
}
