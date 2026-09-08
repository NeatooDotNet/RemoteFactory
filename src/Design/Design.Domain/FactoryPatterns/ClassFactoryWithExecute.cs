// =============================================================================
// DESIGN SOURCE OF TRUTH: [Execute] on Class Factory
// =============================================================================
//
// This file demonstrates [Execute] static methods on non-static [Factory] classes.
// This pattern keeps orchestration logic co-located with the aggregate it operates on.
//
// =============================================================================

using Neatoo.RemoteFactory;
using Design.Domain.FactoryPatterns;

namespace Design.Domain.FactoryPatterns;

/// <summary>
/// Demonstrates: [Execute] on a non-static [Factory] class.
///
/// Key points:
/// - [Execute] methods must be public static
/// - Must return the containing type's service type (the interface if available)
/// - [Service] parameters are injected by the generated factory
/// - The generated factory interface includes the Execute method
/// - Callers use the factory method, not the static method directly
/// - [Remote] decides where it runs: RunCommand crosses to the server,
///   ScoreLocally runs on whichever tier resolves the factory
/// - Visibility follows the ordinary rule: ArchiveOnServer is internal static without
///   [Remote], so it is server-only -- guarded and not promoted
/// </summary>
/// <remarks>
/// DESIGN DECISION: Execute on class factory generates factory interface methods
///
/// Unlike static factory [Execute] (which generates delegate types), class factory
/// [Execute] generates proper factory methods on the IXxxFactory interface. This
/// keeps the calling pattern consistent:
///   var result = await factory.StartForPatient(patientId);
///
/// DESIGN DECISION: Return type must be the containing type
///
/// This keeps the factory interface cohesive -- every method on IXxxFactory
/// deals with the same type. If you need to return a different type, use a
/// static class [Execute] instead.
/// </remarks>
[Factory]
public partial class ClassExecuteDemo
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public ClassExecuteDemo() { }

    /// <summary>
    /// Standard Create method -- establishes that this is a class factory.
    /// </summary>
    [Remote, Create]
    internal Task Create(string name, [Service] IExampleService service)
    {
        Id = service.GenerateId();
        Name = name;
        return Task.CompletedTask;
    }

    /// <summary>
    /// Execute method on a non-static class.
    /// This is the key feature: [Execute] on a class factory.
    /// Returns the containing type (ClassExecuteDemo via the factory interface).
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: For this method, the generator creates:
    /// - Interface method: IClassExecuteDemoFactory.RunCommand(string input)
    /// - Factory implementation with Local/Remote method pair
    /// - Delegate for remote execution
    /// - A forwarding holder, NeatooClassFactoryRegistrar_ClassExecuteDemo, which the
    ///   assembly-level [NeatooFactoryRegistrar] attribute points at
    ///
    /// TRIMMING: class-level [Execute] is emitted `async` unconditionally, which made it
    /// the shape most exposed by the two defects fixed in v1.7.0.
    ///
    /// 1. The guard now lives in a NON-ASYNC wrapper forwarding to a private core:
    ///        public Task&lt;ClassExecuteDemo&gt; LocalRunCommand(...)
    ///        {
    ///            if (!NeatooRuntime.IsServerRuntime) throw new InvalidOperationException(...);
    ///            return LocalRunCommandCore(...);
    ///        }
    ///        private async Task&lt;ClassExecuteDemo&gt; LocalRunCommandCore(...) { ... }
    ///    Inside an `async` method the compiler lowers the whole body -- guard included --
    ///    into the state machine's MoveNext, within the builder's protected region. ILLink
    ///    folds the feature switch there but does not eliminate the unreachable remainder,
    ///    so this body shipped to trimmed Blazor WASM clients, decompilable. A synchronous
    ///    method puts the guard ahead of any protected region, which is why sync operations
    ///    always trimmed correctly and `async` ones did not.
    ///
    /// 2. The registrar attribute's [DynamicallyAccessedMembers] preserves every method on
    ///    the type it names, bodies included. Naming the GENERATED type is necessary but not
    ///    sufficient: {X}Factory hosts every Local* method, so class factories emit a
    ///    single-method holder rather than naming the factory directly. DAM covers
    ///    NonPublicMethods, so the holder alone would still root the private core -- both
    ///    halves are required.
    ///
    /// BEHAVIOUR (v1.7.0): the server-only guard throws synchronously from the wrapper
    /// rather than surfacing as a faulted Task. Awaiting callers are unaffected.
    /// Authorization failures, target casts, and DI resolution failures still surface as
    /// faulted tasks -- only the server-only guard moved.
    ///
    /// Not demonstrated by a Design test: over-preservation is only observable in a
    /// publish-trimmed artifact, and Design.Tests run untrimmed. RemoteFactory.TrimmingTests
    /// is the verification surface; this shape is measured absent there.
    /// </remarks>
    [Remote, Execute]
    public static async Task<ClassExecuteDemo> RunCommand(
        string input, [Service] IExampleService service)
    {
        var instance = new ClassExecuteDemo();
        instance.Id = service.GenerateId();
        instance.Name = $"Executed: {input}";
        return instance;
    }

    /// <summary>
    /// Execute method WITHOUT [Remote]: runs on whichever tier resolves the factory.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: [Execute] obeys [Remote] on class factories too
    ///
    /// GENERATOR BEHAVIOR: For this method, the generator creates:
    /// - Interface method: IClassExecuteDemoFactory.ScoreLocally(string input)
    /// - A Local method only -- unguarded; no remote delegate, no endpoint
    /// - [Service] parameters resolved from the factory's own container, so on the
    ///   client that is client DI (ITextScorer is registered there)
    ///
    /// Two things are unchanged from RunCommand: the method is public static, and it
    /// returns the containing type. Everything about WHERE it runs comes from the
    /// absence of [Remote]. Authorization follows the same placement rule: an
    /// [AuthorizeFactory] auth method without [Remote] runs alongside it, while
    /// [AspAuthorize] or a [Remote] auth method forces the call to the server.
    ///
    /// TRIMMING: no [Remote], so no guard is emitted, so nothing folds and the body
    /// ships to the client -- that is the point. Measured, not inferred: the trimming
    /// harness carries this exact pair on one class (RunBareCommand beside a [Remote]
    /// sibling, differing in the attribute alone), and its CI gate asserts the bare
    /// body PRESENT in a publish-trimmed client and the [Remote] one ABSENT, then
    /// calls the bare method there to prove it still runs. Design.Tests run untrimmed
    /// and cannot observe any of that.
    /// </remarks>
    [Execute]
    public static Task<ClassExecuteDemo> ScoreLocally(string input, [Service] ITextScorer scorer)
    {
        var instance = new ClassExecuteDemo();
        instance.Id = scorer.Score(input);
        instance.Name = $"Scored: {input}";
        return Task.FromResult(instance);
    }

    /// <summary>
    /// Execute method that is internal static WITHOUT [Remote]: server-only.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: internal without [Remote] means server-only, on [Execute] as on every operation
    ///
    /// The three placements on this class read as one table:
    ///   RunCommand       [Remote] public static  -- the client crosses to the server
    ///   ScoreLocally     public static           -- runs on whichever tier resolves the factory
    ///   ArchiveOnServer  internal static         -- server-only; reachable once the boundary is crossed
    ///
    /// GENERATOR BEHAVIOR: For this method, the generator creates:
    /// - Interface member carried with the `internal` modifier on the public
    ///   IClassExecuteDemoFactory (the interface itself is internal only when every member
    ///   is) -- not promoted, because there is no [Remote] to promote it
    /// - A Local method only, whose non-async wrapper carries the IsServerRuntime guard,
    ///   exactly as an internal Create or Fetch does; no remote delegate, no endpoint
    ///
    /// TRIMMING: guarded, so a client published with the feature switch false drops the
    /// body -- the same mechanism [Remote] uses, driven here by `internal`. The renderer's
    /// server-only test is `internal` OR [Remote].
    ///
    /// Not demonstrated by a Design test: the client-side throw. IsServerRuntime is a
    /// process-wide feature switch, so an in-process client container cannot observe the
    /// guard. The generated guard and the `internal` member are pinned in
    /// RemoteFactory.UnitTests (InternalVisibilityTests); ClassFactoryExecuteTests shows
    /// the method resolving and running through the server container.
    /// </remarks>
    [Execute]
    internal static Task<ClassExecuteDemo> ArchiveOnServer(string input, [Service] IExampleService service)
    {
        var instance = new ClassExecuteDemo();
        instance.Id = service.GenerateId();
        instance.Name = $"Archived: {input}";
        return Task.FromResult(instance);
    }
}
