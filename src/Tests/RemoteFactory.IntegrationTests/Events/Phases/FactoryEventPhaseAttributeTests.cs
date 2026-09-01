using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Events;

namespace RemoteFactory.IntegrationTests.Events.Phases;

/// <summary>
/// End-to-end coverage for PHASE-002: a phase declared on
/// <c>[FactoryEventHandler&lt;T&gt;]</c> and registered by generated code actually
/// governs when the handler runs.
/// </summary>
/// <remarks>
/// The falsifiability condition these tests rest on: nothing here — and nothing in
/// <c>FactoryEventPhaseAttributeTargets</c> — calls
/// <c>FactoryEventHandlerRegistry.RegisterHandler</c>. Registration happens because
/// the generator emits a registrar, the assembly attribute names it, and
/// <c>AddNeatooRemoteFactory</c> invokes it reflectively at container build. Sibling
/// tests in <c>FactoryEventPhaseEntryTests</c> deliberately hand-register instead, so
/// they would stay green against a generator that ignored the phase entirely; these
/// would not.
/// <para>
/// No constructor call to <c>PhaseHandlerRegistrations.EnsureRegistered()</c>, unlike
/// the PHASE-003 tests — needing one would mean the generated registration was not
/// doing the work.
/// </para>
/// </remarks>
public class FactoryEventPhaseAttributeTests
{
    private static List<string> RecordedFor(IServiceScope scope, Guid id)
    {
        return scope.GetRequiredService<IEventTestService>()
            .GetRecordedEvents()
            .Where(e => e.EntityId == id)
            .Select(e => e.EventName)
            .ToList();
    }

    /// <summary>
    /// HTTP-dispatched <c>[Remote]</c> entry call: the attribute-declared AfterCommit
    /// handler runs after the factory method body returns, the unphased one inline.
    /// </summary>
    [Fact]
    public async Task RemoteCreate_AttributeDeclaredPhases_GovernWhenHandlersRun()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        var factory = client.GetRequiredService<IPhaseAttributeTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        // Raise order was projection-then-atomic; the drain inverts it. A generator that
        // dropped the phase argument would register both at Immediate and produce
        // ["attr-after-commit", "attr-immediate", "attr-method-done"].
        Assert.Equal(["attr-immediate", "attr-method-done", "attr-after-commit"], RecordedFor(server, id));
    }

    /// <summary>
    /// Direct Logical invocation through the generated wrapper seam reaches the same
    /// drain point as the remote choke point.
    /// </summary>
    [Fact]
    public async Task LogicalCreate_AttributeDeclaredPhases_GovernWhenHandlersRun()
    {
        var (_, _, local) = ClientServerContainers.Scopes();
        var factory = local.GetRequiredService<IPhaseAttributeTargetFactory>();
        var id = Guid.NewGuid();

        await factory.Create(id);

        Assert.Equal(["attr-immediate", "attr-method-done", "attr-after-commit"], RecordedFor(local, id));
    }
}
