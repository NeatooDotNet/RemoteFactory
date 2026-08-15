using Microsoft.Extensions.DependencyInjection;

namespace Neatoo.RemoteFactory.Internal;

/// <summary>
/// Entry-call wrapper invoked by generated factory code around the local execution of a
/// factory method. Marks the call on the scope's <see cref="IFactoryEventPhaseScheduler"/>
/// so phased event dispatches queue while the call is in flight, drains
/// <see cref="DispatchPhase.AfterCommit"/> at the outermost successful completion, and
/// discards deferred work when the call fails.
/// </summary>
/// <remarks>
/// <para>
/// Public because generated factory code calls it; it lives in the <c>Internal</c>
/// namespace alongside <see cref="IFactoryEventPhaseScheduler"/> for the same reason.
/// </para>
/// <para>
/// Resolution is null-tolerant by design: Remote-mode (client) containers register no
/// scheduler, and some generated callers (value-object factories) run client-side with
/// no <c>IsServerRuntime</c> guard — there the wrapper reduces to invoking the body.
/// Keeping these methods non-<see langword="async"/> at the generated call site also
/// preserves the IL-trimming wrapper shape: the caller's runtime guard stays in the
/// outer method rather than being lowered into an async state machine.
/// </para>
/// </remarks>
public static class FactoryEntryCall
{
    /// <summary>Runs a <see cref="Task{T}"/>-returning factory body as an entry call.</summary>
    public static async Task<T> RunAsync<T>(IServiceProvider serviceProvider, Func<Task<T>> body)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(body);

        var scheduler = serviceProvider.GetService<IFactoryEventPhaseScheduler>();
        if (scheduler == null)
        {
            return await body().ConfigureAwait(false);
        }

        scheduler.BeginEntryCall();
        try
        {
            var result = await body().ConfigureAwait(false);
            await scheduler.EndEntryCallAsync(success: true).ConfigureAwait(false);
            return result;
        }
        catch
        {
            await scheduler.EndEntryCallAsync(success: false).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>Runs a <see cref="Task"/>-returning factory body as an entry call.</summary>
    public static async Task RunAsync(IServiceProvider serviceProvider, Func<Task> body)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(body);

        var scheduler = serviceProvider.GetService<IFactoryEventPhaseScheduler>();
        if (scheduler == null)
        {
            await body().ConfigureAwait(false);
            return;
        }

        scheduler.BeginEntryCall();
        try
        {
            await body().ConfigureAwait(false);
            await scheduler.EndEntryCallAsync(success: true).ConfigureAwait(false);
        }
        catch
        {
            await scheduler.EndEntryCallAsync(success: false).ConfigureAwait(false);
            throw;
        }
    }

    /// <summary>
    /// Runs a synchronous factory body as an entry call. If the body deferred phased
    /// work (a fire-and-forget <c>Raise</c> inside a synchronous factory method), the
    /// completion drain blocks rather than losing that work — the no-silent-loss
    /// invariant outranks staying non-blocking on this edge. With nothing deferred, the
    /// completion is fully synchronous.
    /// </summary>
    /// <remarks>
    /// The blocking drain can deadlock under a captured <see cref="SynchronizationContext"/>
    /// (e.g. Logical mode inside a Blazor Server circuit) if a drained handler awaits
    /// without <c>ConfigureAwait(false)</c> — the continuation posts to the context this
    /// call is blocking. Handlers registered at a non-Immediate phase should not assume
    /// a context, and synchronous factory methods should not raise phased events on
    /// context-bound scopes.
    /// </remarks>
    public static T Run<T>(IServiceProvider serviceProvider, Func<T> body)
    {
        ArgumentNullException.ThrowIfNull(serviceProvider);
        ArgumentNullException.ThrowIfNull(body);

        var scheduler = serviceProvider.GetService<IFactoryEventPhaseScheduler>();
        if (scheduler == null)
        {
            return body();
        }

        scheduler.BeginEntryCall();
        try
        {
            var result = body();
            scheduler.EndEntryCallAsync(success: true).GetAwaiter().GetResult();
            return result;
        }
        catch
        {
            // Completes synchronously: the failure path clears without running handlers.
            scheduler.EndEntryCallAsync(success: false).GetAwaiter().GetResult();
            throw;
        }
    }
}
