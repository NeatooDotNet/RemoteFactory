using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

// =============================================================================
// CLASS-LEVEL [Execute] LEG TARGET (TRIM-009, closing plan-review A3)
// =============================================================================
//
// `[Execute]` on a NON-static [Factory] class is a distinct emission path from
// `[Execute]` on a static class:
//
//   static class  -> StaticFactoryRenderer          (covered by TrimTestCommands)
//   [Factory] class -> ClassFactoryRenderer.RenderClassExecuteLocalMethod
//
// The class path has no synchronous variant to fall back to: its body is always
// emitted async. Before TRIM-009 the `if (!IsServerRuntime) throw` guard sat
// inside that async body, which is the H1 mechanism in full -- the [Remote] body,
// its [Service] interface, and its literals all shipped to a trimmed client.
// TRIM-009 split the emission: the guard now sits on a NON-async `Local{X}`
// wrapper ahead of any protected region, and `async` applies only to the private
// `Local{X}Core` it forwards to (ClassFactoryRenderer.RenderLocalMethodOpening).
// Corrected 2026-09-08 (EXRM-003 plan review, callout B3): this header still
// described the pre-TRIM-009 shape, and it is the sentence a reader of the pair
// below relies on to know what actually varies between the two halves.
//
// It had NO harness coverage until this file. That mattered because it is a
// Design source-of-truth pattern -- see Design.Domain/FactoryPatterns/
// ClassFactoryWithExecute.cs, which demonstrates exactly this shape -- and AC6
// requires every factory shape be "proven in the trimmed harness, not inferred".
// Found at TRIM-009's plan review, before the plan could close AC6 over it.
//
// MARKER PLACEMENT: the literal lives inside the [Execute] body, which is what
// must disappear. ExecLegBackend_MARKER lives in the implementation and only
// proves the implementation was rooted -- a weaker property, recorded separately.
// =============================================================================

/// <summary>
/// Class factory carrying a class-level <c>[Execute]</c> method.
/// </summary>
/// <remarks>
/// The <c>[Create]</c> method establishes this as a class factory; <c>RunExecCommand</c>
/// is the shape under test.
/// </remarks>
[Factory]
public partial class TrimExecTarget
{
    public string Label { get; set; } = string.Empty;
    public string ExecResult { get; set; } = string.Empty;

    public TrimExecTarget() { }

    /// <summary>
    /// Establishes this type as a class factory. Synchronous on purpose: it is the
    /// in-file control showing that the sync path on this same type stays clean.
    /// </summary>
    [Remote]
    [Create]
    internal void Create(string label)
    {
        Label = label;
    }

    /// <summary>
    /// Class-level <c>[Execute]</c> — emitted <c>async</c> unconditionally by
    /// <c>RenderClassExecuteLocalMethod</c>, with the feature-switch guard inside.
    /// </summary>
    /// <remarks>
    /// <c>public static</c> matches the Design pattern. <c>[Remote]</c> is what makes the
    /// generator emit the guard; without it the method would run on both sides and the
    /// body would legitimately survive.
    /// </remarks>
    [Remote]
    [Execute]
    public static async Task<TrimExecTarget> RunExecCommand(
        string input,
        [Service] IExecLegPort execPort)
    {
        var instance = new TrimExecTarget();
        instance.Label = input;
        instance.ExecResult = await execPort.ExecLegInvoke("ClassExecBody_MARKER: " + input);
        return instance;
    }

    /// <summary>
    /// The BARE half of the class pair (EXRM-003) — the same shape as
    /// <see cref="RunExecCommand"/> with <c>[Remote]</c> removed, and nothing else changed.
    /// </summary>
    /// <remarks>
    /// What the absent attribute changes, measured from the emitted factory: the bare
    /// method gets no delegate type, no <c>…Property</c> fork in either constructor, no
    /// delegate registration, and — the part this leg exists to observe — no
    /// <c>IsServerRuntime</c> guard on its <c>Local{X}</c> wrapper. Nothing folds, so
    /// <c>BareClassBody_MARKER</c> is expected PRESENT in the trimmed client while
    /// <c>ClassExecBody_MARKER</c> above is expected ABSENT.
    /// <para>
    /// Both halves share this class, its factory, its registrar holder, and its
    /// <c>public static</c> shape, so <c>[Remote]</c> is the only variable — the
    /// controlled-pair method TRIM-009 used for sync-vs-async, applied to the attribute.
    /// </para>
    /// <para>
    /// The <c>[Service]</c> is <see cref="IClientTallyPort"/>, registered outside the
    /// harness's feature-switch guard: a bare <c>[Execute]</c> resolves services from
    /// whichever container runs it, and Program.cs calls this on the trimmed client to
    /// prove the body did not merely survive as unreachable metadata.
    /// </para>
    /// </remarks>
    [Execute]
    public static Task<TrimExecTarget> RunBareCommand(
        string input,
        [Service] IClientTallyPort tallyPort)
    {
        var instance = new TrimExecTarget();
        instance.Label = input;
        instance.ExecResult = tallyPort.ClientTallyCompute("BareClassBody_MARKER: " + input);
        return Task.FromResult(instance);
    }
}
