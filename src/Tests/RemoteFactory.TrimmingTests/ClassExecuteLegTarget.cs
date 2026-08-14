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
// The class path is emitted `async` UNCONDITIONALLY -- there is no sync variant
// to fall back to -- and it carries the same `if (!IsServerRuntime) throw` guard
// inside the async body. So it is subject to TRIM-009's H1 mechanism in full:
// before the fix, its [Remote] body, its [Service] interface, and its literals
// all shipped to a trimmed client.
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
}
