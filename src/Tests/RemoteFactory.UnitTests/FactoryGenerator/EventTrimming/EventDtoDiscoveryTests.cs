using System.Text.RegularExpressions;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.FactoryGenerator.EventTrimming;

/// <summary>
/// Verifies that the generator emits IL-trimming preservation calls
/// (DtoConstructorRegistry.PreserveType&lt;T&gt;() and DtoConstructorRegistry.Register&lt;T&gt;(() => new T()))
/// for every type reachable from a [FactoryEventHandler&lt;T&gt;] event root.
/// </summary>
public class EventDtoDiscoveryTests
{
    private static string RunAndGetGeneratedSource(string source)
    {
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);
        return string.Join("\n", runResult.GeneratedTrees.Select(t => t.GetText()?.ToString() ?? ""));
    }

    private static HashSet<string> GetPreserveTypeCalls(string generated)
    {
        var matches = Regex.Matches(generated, @"DtoConstructorRegistry\.PreserveType<(.+?)>\(\)");
        return [.. matches.Select(m => m.Groups[1].Value)];
    }

    private static HashSet<string> GetRegisterCalls(string generated)
    {
        var matches = Regex.Matches(generated, @"DtoConstructorRegistry\.Register<(.+?)>\(\(\)");
        return [.. matches.Select(m => m.Groups[1].Value)];
    }

    /// <summary>
    /// True if the given call appears OUTSIDE any `if (NeatooRuntime.IsServerRuntime)` block.
    /// Crude but sufficient for these tests: looks for the call text and checks that the
    /// most recent opener above it is the FactoryServiceRegistrar method opener, not an
    /// IsServerRuntime guard.
    /// </summary>
    private static bool IsUnconditional(string generated, string callSubstring)
    {
        var idx = generated.IndexOf(callSubstring);
        if (idx < 0) return false;
        var before = generated.Substring(0, idx);
        var lastGuard = before.LastIndexOf("if (NeatooRuntime.IsServerRuntime)");
        var lastMethodOpen = before.LastIndexOf("FactoryServiceRegistrar");
        return lastMethodOpen > lastGuard;
    }

    private const string Preamble = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;
using System.Collections.Generic;
";

    #region Scenario 1 — Record event with parameterized ctor is preserved (Rules 1, 7)

    [Fact]
    public void Scenario1_RecordEventWithParameterizedCtor_IsPreservedUnconditionally()
    {
        var source = Preamble + @"
namespace TestNs;

public record OrderPlaced(System.Guid Id, decimal Total) : FactoryEventBase;

[FactoryEventHandler<OrderPlaced>]
public partial class OrderHandler
{
    public static Task Handle(OrderPlaced evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);

        Assert.Contains("global::TestNs.OrderPlaced", preserve);
        Assert.True(IsUnconditional(generated, "PreserveType<global::TestNs.OrderPlaced>()"),
            "PreserveType<OrderPlaced>() must be emitted outside any IsServerRuntime guard");
    }

    #endregion

    #region Scenario 2 — Event with parameterless ctor ALSO uses PreserveType (Rule 1)

    [Fact]
    public void Scenario2_ParameterlessCtorEvent_UsesPreserveTypeNotRegister()
    {
        var source = Preamble + @"
namespace TestNs;

public class SimpleEvent : FactoryEventBase
{
    public string Name { get; set; } = """";
}

[FactoryEventHandler<SimpleEvent>]
public partial class SimpleHandler
{
    public static Task Handle(SimpleEvent evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.SimpleEvent", preserve);
        Assert.DoesNotContain("global::TestNs.SimpleEvent", register);
    }

    #endregion

    #region Scenario 3 — Nested parameterless-ctor DTO goes to Register (Rules 1, 2)

    [Fact]
    public void Scenario3_NestedPlainDto_UsesRegister()
    {
        var source = Preamble + @"
namespace TestNs;

public class Address
{
    public string Street { get; set; } = """";
}

public record OrderPlaced(Address ShippingAddress) : FactoryEventBase;

[FactoryEventHandler<OrderPlaced>]
public partial class OrderHandler
{
    public static Task Handle(OrderPlaced evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.OrderPlaced", preserve);
        Assert.Contains("global::TestNs.Address", register);
    }

    #endregion

    #region Scenario 4 — Nested parameterized record uses PreserveType (Rules 1, 2)

    [Fact]
    public void Scenario4_NestedParameterizedRecord_UsesPreserveType()
    {
        var source = Preamble + @"
namespace TestNs;

public record LineItemDetail(int Qty, decimal Price);

public record OrderPlaced(LineItemDetail First) : FactoryEventBase;

[FactoryEventHandler<OrderPlaced>]
public partial class OrderHandler
{
    public static Task Handle(OrderPlaced evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.OrderPlaced", preserve);
        Assert.Contains("global::TestNs.LineItemDetail", preserve);
        Assert.DoesNotContain("global::TestNs.LineItemDetail", register);
    }

    #endregion

    #region Scenario 5 — Collection / nullable unwrapping (Rule 2)

    [Fact]
    public void Scenario5_CollectionAndNullableProperties_AreUnwrapped()
    {
        var source = Preamble + @"
namespace TestNs;

public class LineItem { public int Id { get; set; } }
public class Coupon { public string Code { get; set; } = """"; }

public record Batch(
    System.Collections.Generic.IReadOnlyList<LineItem> Items,
    Coupon? Optional) : FactoryEventBase;

[FactoryEventHandler<Batch>]
public partial class BatchHandler
{
    public static Task Handle(Batch evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.LineItem", register);
        Assert.Contains("global::TestNs.Coupon", register);
    }

    #endregion

    #region Scenario 6 — Cycle suppression (Rule 3)

    [Fact]
    public void Scenario6_SelfReferencingEvent_EmitsExactlyOnce()
    {
        var source = Preamble + @"
namespace TestNs;

public record Node(Node? Next) : FactoryEventBase;

[FactoryEventHandler<Node>]
public partial class NodeHandler
{
    public static Task Handle(Node evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserveOccurrences = Regex.Matches(generated, @"PreserveType<global::TestNs\.Node>\(\)").Count;

        Assert.Equal(1, preserveOccurrences);
    }

    #endregion

    #region Scenario 7 — Framework/primitive properties are skipped

    [Fact]
    public void Scenario7_PrimitiveAndFrameworkProperties_AreNotRegistered()
    {
        var source = Preamble + @"
namespace TestNs;

public record PrimEvent(
    string Name,
    int Count,
    System.DateTime When,
    System.Guid Id,
    decimal Amount) : FactoryEventBase;

[FactoryEventHandler<PrimEvent>]
public partial class PrimHandler
{
    public static Task Handle(PrimEvent evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.PrimEvent", preserve);
        Assert.DoesNotContain(preserve, t => t.StartsWith("global::System"));
        Assert.DoesNotContain(register, t => t.StartsWith("global::System"));
    }

    #endregion

    #region Scenario 8 — [Factory]-annotated property types are skipped

    [Fact]
    public void Scenario8_FactoryAnnotatedPropertyType_IsSkipped()
    {
        var source = Preamble + @"
namespace TestNs;

[Factory]
public partial class Order
{
    public partial int Id { get; set; }
    [Create] internal void Create() { }
}

public record OrderShipped(Order Target) : FactoryEventBase;

[FactoryEventHandler<OrderShipped>]
public partial class ShipHandler
{
    public static Task Handle(OrderShipped evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.OrderShipped", preserve);
        Assert.DoesNotContain("global::TestNs.Order", preserve);
        Assert.DoesNotContain("global::TestNs.Order", register);
    }

    #endregion

    #region Scenario 12 — Event with zero reference-type properties (negation of Rule 2)

    [Fact]
    public void Scenario12_AllPrimitiveEvent_EmitsExactlyOnePreserveTypeAndNoRegister()
    {
        var source = Preamble + @"
namespace TestNs;

public record MinimalEvent(int Count, string Tag) : FactoryEventBase;

[FactoryEventHandler<MinimalEvent>]
public partial class MinHandler
{
    public static Task Handle(MinimalEvent evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Single(preserve);
        Assert.Contains("global::TestNs.MinimalEvent", preserve);
        Assert.Empty(register);
    }

    #endregion

    #region Scenario 13 — Abstract/interface property types are skipped

    [Fact]
    public void Scenario13_AbstractAndInterfaceProperties_AreSkipped()
    {
        var source = Preamble + @"
namespace TestNs;

public interface IAnimal { }
public abstract class AbstractBase { }

public record Event(IAnimal Pet, AbstractBase Base) : FactoryEventBase;

[FactoryEventHandler<Event>]
public partial class EvHandler
{
    public static Task Handle(Event evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.Event", preserve);
        Assert.DoesNotContain("global::TestNs.IAnimal", preserve);
        Assert.DoesNotContain("global::TestNs.IAnimal", register);
        Assert.DoesNotContain("global::TestNs.AbstractBase", preserve);
        Assert.DoesNotContain("global::TestNs.AbstractBase", register);
    }

    #endregion

    #region Scenario 14 — Multi-attribute cross-event dedupe (Rule 4)

    [Fact]
    public void Scenario14_SharedNestedRecord_EmittedOncePerClass()
    {
        var source = Preamble + @"
namespace TestNs;

public record SharedNestedRecord(int Val);

public record EventA(SharedNestedRecord Shared) : FactoryEventBase;
public record EventB(SharedNestedRecord Shared) : FactoryEventBase;

[FactoryEventHandler<EventA>]
[FactoryEventHandler<EventB>]
public partial class MultiHandler
{
    public static Task HandleA(EventA evt) => Task.CompletedTask;
    public static Task HandleB(EventB evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);

        var sharedCount = Regex.Matches(generated, @"PreserveType<global::TestNs\.SharedNestedRecord>\(\)").Count;
        Assert.Equal(1, sharedCount);

        var preserve = GetPreserveTypeCalls(generated);
        Assert.Contains("global::TestNs.EventA", preserve);
        Assert.Contains("global::TestNs.EventB", preserve);
    }

    #endregion

    #region Scenario 15 — Static-method handler AND instance-method handler both preserve unconditionally

    [Fact]
    public void Scenario15_StaticAndInstanceHandlers_BothEmitPreservationUnconditionally()
    {
        var staticSource = Preamble + @"
namespace TestNs;

public record EventStatic(int V) : FactoryEventBase;

[FactoryEventHandler<EventStatic>]
public partial class StaticHandler
{
    public static Task Handle(EventStatic evt) => Task.CompletedTask;
}
";
        var instanceSource = Preamble + @"
namespace TestNs;

public record EventInstance(int V) : FactoryEventBase;

[FactoryEventHandler<EventInstance>]
public partial class InstanceHandler
{
    public Task Handle(EventInstance evt) => Task.CompletedTask;
}
";
        var staticGen = RunAndGetGeneratedSource(staticSource);
        var instanceGen = RunAndGetGeneratedSource(instanceSource);

        Assert.True(IsUnconditional(staticGen, "PreserveType<global::TestNs.EventStatic>()"),
            "Static-method handler: PreserveType must be outside IsServerRuntime guard");
        Assert.True(IsUnconditional(instanceGen, "PreserveType<global::TestNs.EventInstance>()"),
            "Instance-method handler: PreserveType must be outside IsServerRuntime guard");
    }

    #endregion

    #region Scenario 16 — Known gap: Dictionary value type is NOT discovered (regression guard)

    [Fact]
    public void Scenario16_DictionaryValueType_IsNotWalked()
    {
        var source = Preamble + @"
namespace TestNs;

public record Payload(int V);

public record Cache(System.Collections.Generic.Dictionary<string, Payload> Items) : FactoryEventBase;

[FactoryEventHandler<Cache>]
public partial class CacheHandler
{
    public static Task Handle(Cache evt) => Task.CompletedTask;
}
";
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);
        var register = GetRegisterCalls(generated);

        Assert.Contains("global::TestNs.Cache", preserve);
        // Documented known limitation — Dictionary<K,V> value type is NOT walked.
        Assert.DoesNotContain("global::TestNs.Payload", preserve);
        Assert.DoesNotContain("global::TestNs.Payload", register);
    }

    #endregion

    #region Scenario 17 — NF0501 (no matching handler) still emits preservation (Rule 8)

    [Fact]
    public void Scenario17_MissingHandlerMethod_StillEmitsPreserveType()
    {
        var source = Preamble + @"
namespace TestNs;

public record OrphanEvent(int V) : FactoryEventBase;

[FactoryEventHandler<OrphanEvent>]
public partial class OrphanHandler
{
    public static Task WrongSignature(int notTheEventType) => Task.CompletedTask;
}
";
        var (diags, _, _) = DiagnosticTestHelper.RunGenerator(source);
        var generated = RunAndGetGeneratedSource(source);
        var preserve = GetPreserveTypeCalls(generated);

        Assert.Contains(diags, d => d.Id == "NF0501");
        Assert.Contains("global::TestNs.OrphanEvent", preserve);
    }

    #endregion
}
