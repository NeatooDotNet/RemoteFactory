using Neatoo.RemoteFactory;

namespace RemoteFactory.TrimmingTests;

// =============================================================================
// SAVE / CAN* LEG TARGET (TRIM-008, closing plan-review B9)
// =============================================================================
//
// The harness's existing class-factory target (TrimTestEntity) has a single
// [Remote, Create]. That covers the read half of the class-factory shape and
// nothing else. Save routing (IFactorySaveMeta -> Insert/Update/Delete) and the
// generated Can* methods are a materially different emission path — write
// operations, an auth interface, and a Save() dispatcher — and were never
// measured under trimming.
//
// This target carries the write half. The server-only IP lives in the three
// [Remote] write bodies; each gets its own marker literal so a leak names the
// operation, not just the leg.
//
// AUTH IMPLEMENTATION IS DELIBERATELY DEPENDENCY-FREE
//
// The generator emits `services.TryAddTransient<ITrimSaveAuth, TrimSaveAuthRules>()`
// into FactoryServiceRegistrar WITHOUT an IsServerRuntime guard (see any
// generated *Factory.g.cs for a class carrying [AuthorizeFactory<T>]). An auth
// implementation with a server-only constructor dependency would therefore be
// rooted on the client and would fail ValidateOnBuild — a DIFFERENT preservation
// question from the registrar-DAM one under test here, and conflating the two
// would make this target's result unreadable. TrimSaveAuthRules stays trivial;
// whether it survives trimming is recorded as a measurement, not asserted.
// =============================================================================

/// <summary>
/// Authorization contract for <see cref="TrimSaveTarget"/>. Present so the
/// generator emits the Can* surface (CanCreate / CanSave / CanDelete) that this
/// leg exists to measure.
/// </summary>
public interface ITrimSaveAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDeleteTarget();
}

/// <summary>
/// Trivial auth implementation — no constructor dependencies, no server-only
/// reach. See the file header for why.
/// </summary>
public sealed class TrimSaveAuthRules : ITrimSaveAuth
{
    public bool HasAccess() => true;

    public bool CanDeleteTarget() => true;
}

/// <summary>
/// Class factory exercising Save routing and the generated Can* methods.
/// Implements <see cref="IFactorySaveMeta"/> so the generator emits Save(),
/// which dispatches to Insert / Update / Delete on IsNew and IsDeleted.
/// </summary>
[Factory]
[AuthorizeFactory<ITrimSaveAuth>]
public partial class TrimSaveTarget : IFactorySaveMeta
{
    public int Id { get; set; }

    public string? Label { get; set; }

    public bool IsNew { get; set; } = true;

    public bool IsDeleted { get; set; }

    [Remote]
    [Create]
    internal void Create(string label)
    {
        Label = label;
    }

    [Remote]
    [Insert]
    internal Task Insert([Service] ISaveLegPort port)
    {
        IsNew = false;
        return port.SaveLegInvoke("SaveLegInsertBody_MARKER: " + Label);
    }

    [Remote]
    [Update]
    internal Task Update([Service] ISaveLegPort port)
    {
        return port.SaveLegInvoke("SaveLegUpdateBody_MARKER: " + Label);
    }

    [Remote]
    [Delete]
    internal Task Delete([Service] ISaveLegPort port)
    {
        return port.SaveLegInvoke("SaveLegDeleteBody_MARKER: " + Label);
    }
}
