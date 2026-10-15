// =============================================================================
// DESIGN SOURCE OF TRUTH: The Correlation ID in a Server Operation
// =============================================================================
//
// Every remote call carries an X-Correlation-Id header. The client generates
// the ID once per DI scope (in Blazor WASM, once per app session) and sends it
// on each call; UseNeatoo() on the server puts it in the request's
// ICorrelationContext and echoes it on the response. RemoteFactory's own log
// lines for the call carry it as a [{CorrelationId}] message parameter.
//
// DESIGN DECISION: Hand the correlation ID to the user as a support reference
//
// The user who reports a problem quotes the reference; support searches the
// server's logs for it and finds this session's calls. The ID is the one thing
// both tiers already share.
//
// The in-process test harness has no HTTP and no middleware, so there the
// context is empty; the HTTP tests (Design.Tests/Http) are where this is pinned.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Services;

/// <summary>
/// Problem reports a user sends from the client.
/// </summary>
[Factory]
public static partial class SupportRequests
{
    /// <summary>
    /// Records a problem report on the server and returns its reference.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: generates the delegate SupportRequests.ReportProblem(int, string);
    /// the [Service] parameters are resolved on the server.
    /// </remarks>
    #region aspnetcore-correlation-id
    [Remote, Execute]
    private static async Task<string> _ReportProblem(
        int orderId,
        string description,
        [Service] ICorrelationContext correlation,
        [Service] ISupportLog supportLog)
    {
        // The same ID tags the server's log lines for this call
        var reference = correlation.CorrelationId ?? "unknown";
        await supportLog.RecordAsync(new SupportEntry(reference, orderId, description));
        return reference;
    }
    #endregion
}

/// <summary>
/// One problem report, keyed by the call's correlation ID.
/// </summary>
public record SupportEntry(string Reference, int OrderId, string Description);

/// <summary>
/// Where problem reports are kept. Server-only.
/// </summary>
public interface ISupportLog
{
    Task RecordAsync(SupportEntry entry);
    IReadOnlyList<SupportEntry> Entries { get; }
}

/// <summary>
/// In-memory support log standing in for a ticketing system.
/// </summary>
public class InMemorySupportLog : ISupportLog
{
    private readonly List<SupportEntry> entries = [];
    private readonly Lock gate = new();

    public IReadOnlyList<SupportEntry> Entries
    {
        get { lock (gate) return entries.ToList(); }
    }

    public Task RecordAsync(SupportEntry entry)
    {
        lock (gate) entries.Add(entry);
        return Task.CompletedTask;
    }
}
