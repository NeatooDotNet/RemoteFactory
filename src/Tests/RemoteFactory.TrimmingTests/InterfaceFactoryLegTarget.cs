using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

// =============================================================================
// INTERFACE-FACTORY LEG TARGET (TRIM-008, closing plan-review B9)
// =============================================================================
//
// The interface-factory leg is the one the arc has always CLAIMED is safe, on
// the structural grounds that its assembly attribute names the GENERATED proxy
// (TrimIfaceQueryFactory) rather than any user type — so DAM has nothing of the
// consumer's to over-retain.
//
// That claim was never measured. This target measures it, which is the whole
// point of B9: an arc that asserts two legs are safe while only ever probing one
// of them is asserting, not verifying.
//
// Note the shape difference from the broken legs. There is no [Remote] method
// body here to strip — an interface factory's client side is a proxy that
// serializes calls. The server-only IP lives in the IMPLEMENTATION class
// (TrimIfaceServerSide), which carries no factory attribute at all and is
// registered only behind the harness's IsServerRuntime guard. If the interface
// leg is genuinely clean, nothing roots that class and its body is absent.
//
// NF0106 TRAP: operation attributes ([Fetch], [Execute], ...) on an interface-
// factory member are a diagnostic, not a no-op — the member degrades and the
// generated factory silently loses it. No operation attributes below; [Factory]
// on the interface is the whole contract.
// =============================================================================

/// <summary>
/// Interface factory. The generator emits <c>ITrimIfaceQueryFactory</c> and a
/// <c>TrimIfaceQueryFactory</c> proxy; the assembly attribute names the proxy.
/// </summary>
[Factory]
public interface ITrimIfaceQuery
{
    Task<string> LookupAsync(string key);
}

/// <summary>
/// Server-side implementation. Deliberately carries NO factory attribute — the
/// [Factory] on the interface is sufficient, and adding one here would cause
/// duplicate registration.
/// </summary>
/// <remarks>
/// Every name in this class is an absence marker: the type name, its
/// <see cref="IIfaceLegPort"/> dependency, and the literal in the body. All three
/// should be gone from a trimmed client publish.
/// </remarks>
public sealed class TrimIfaceServerSide : ITrimIfaceQuery
{
    private readonly IIfaceLegPort port;

    public TrimIfaceServerSide(IIfaceLegPort port)
    {
        this.port = port;
    }

    public Task<string> LookupAsync(string key)
    {
        // Concatenated, not interpolated — see the note in RelayHandlerLegTarget.
        return Task.FromResult(port.IfaceLegInvoke("IfaceLegServerBody_MARKER: " + key));
    }
}

// =============================================================================
// ASYNC INTERFACE-FACTORY VARIANT
// =============================================================================
//
// The target above measures only the SYNCHRONOUS emission branch.
// InterfaceFactoryRenderer emits `async` for a method only when the model's IsAsync is set,
// and FactoryModelBuilder sets that from
//     method.AuthMethodInfos.Any(m => m.IsTask) || method.AspAuthorizeCalls.Any()
// — so an interface factory with no authorization can never produce an async Local* method.
//
// That distinction is not cosmetic here. TRIM-009 found that async generated Local* methods
// retain their server-only bodies while sync ones do not. Claiming "the interface leg is
// clean" off a sync-only measurement generalizes across exactly the boundary that broke the
// class-factory leg. This variant carries a Task-returning auth method so the generated
// LocalQueryAsync really is async, and gets its own marker.
// =============================================================================

/// <summary>
/// Authorization contract whose method returns <c>Task&lt;bool&gt;</c>, which is what makes the
/// generated interface-factory method <c>async</c>.
/// </summary>
public interface ITrimAsyncIfaceAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    Task<bool> CanQuery();
}

/// <summary>
/// Trivial auth implementation — no server-only reach, for the reason recorded in
/// SaveCanLegTarget.cs (auth registrations are emitted unguarded).
/// </summary>
public sealed class TrimAsyncIfaceAuth : ITrimAsyncIfaceAuth
{
    public Task<bool> CanQuery() => Task.FromResult(true);
}

/// <summary>
/// Interface factory whose generated local method is <c>async</c>.
/// </summary>
/// <remarks>
/// WHAT THIS TARGET CAN AND CANNOT MEASURE — read before citing its result.
/// <para>
/// Its markers live on <see cref="TrimAsyncIfaceServerSide"/>, which the generated
/// <c>LocalQueryAsync</c> reaches only through the interface
/// (<c>GetRequiredService&lt;ITrimAsyncIfaceQuery&gt;()</c> then <c>target.QueryAsync(...)</c>).
/// So those markers are absent by fixture shape whether or not the generated body survives
/// trimming. Their absence is **not** evidence that the feature-switch fold eliminated
/// anything on this leg. It is a no-regression check that the implementation stays off the
/// client, which is worth having and is all it is.
/// </para>
/// <para>
/// This is structural, not a fixture defect that could be tidied up. An interface factory
/// reaches everything through interfaces by design, so no server-only *implementation* name
/// can appear directly in its generated local body. The obvious fix — a <c>[Service]</c>
/// parameter, which would put <c>GetRequiredService&lt;IAsyncLegPort&gt;()</c> straight into
/// the body — was tried and does not compile: the generator strips the service parameter from
/// the proxy's implementing method while the interface still declares it, so the emitted
/// factory fails CS0535. Recorded as Deferred Work item 19; nothing else in the repo uses
/// that shape, which is why it was never caught.
/// </para>
/// <para>
/// Consequence: the async-interface result contributes nothing to the question of whether
/// async bodies fold. That question rests on the static and relay async targets and on the
/// class-factory sync-vs-async pair.
/// </para>
/// </remarks>
[Factory]
[AuthorizeFactory<ITrimAsyncIfaceAuth>]
public interface ITrimAsyncIfaceQuery
{
    Task<string> QueryAsync(string key);
}

/// <summary>
/// Server-side implementation for the async interface-factory variant.
/// </summary>
public sealed class TrimAsyncIfaceServerSide : ITrimAsyncIfaceQuery
{
    private readonly IAsyncLegPort port;

    public TrimAsyncIfaceServerSide(IAsyncLegPort port)
    {
        this.port = port;
    }

    public Task<string> QueryAsync(string key)
    {
        return port.AsyncLegInvoke("IfaceAsyncBody_MARKER: " + key);
    }
}
