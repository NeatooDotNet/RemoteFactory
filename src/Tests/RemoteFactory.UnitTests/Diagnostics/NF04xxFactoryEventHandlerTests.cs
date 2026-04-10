using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for [FactoryEventHandler&lt;T&gt;] diagnostics: NF0501, NF0502, NF0101.
/// </summary>
public class NF05xxFactoryEventHandlerTests
{
    // =========================================================================
    // NF0501: No matching handler method
    // =========================================================================

    [Fact]
    public void NF0501_NoMatchingMethod_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<TestEvent>]
    public partial class TestHandler
    {
        // No matching method — should produce NF0501
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0501", DiagnosticSeverity.Error);
        Assert.Contains("TestHandler", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0501_MethodWrongReturnType_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<TestEvent>]
    public partial class TestHandler
    {
        // Returns Task<string> not Task — no match
        public Task<string> Handle(TestEvent evt) => Task.FromResult(""x"");
    }
}
";

        DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0501", DiagnosticSeverity.Error);
    }

    [Fact]
    public void NF0501_MethodWrongParamType_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEventA(int Id) : FactoryEventBase;
    public record TestEventB(int Id) : FactoryEventBase;

    [FactoryEventHandler<TestEventA>]
    public partial class TestHandler
    {
        // Handles TestEventB, not TestEventA — no match
        public Task Handle(TestEventB evt) => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0501", DiagnosticSeverity.Error);
    }

    // =========================================================================
    // NF0502: Ambiguous — multiple matching methods
    // =========================================================================

    [Fact]
    public void NF0502_MultipleMatchingMethods_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<TestEvent>]
    public partial class TestHandler
    {
        public Task HandleA(TestEvent evt) => Task.CompletedTask;
        public Task HandleB(TestEvent evt) => Task.CompletedTask;
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0502", DiagnosticSeverity.Error);
        Assert.Contains("TestHandler", diagnostic.GetMessage());
    }

    // =========================================================================
    // NF0101: Class must be partial
    // =========================================================================

    [Fact]
    public void NF0101_NotPartial_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<TestEvent>]
    public class TestHandler
    {
        public Task Handle(TestEvent evt) => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0101", DiagnosticSeverity.Error);
    }

    // =========================================================================
    // Valid handlers — no diagnostics
    // =========================================================================

    [Fact]
    public void Valid_InstanceMethod_NoDiagnostics()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<TestEvent>]
    public partial class TestHandler
    {
        public Task Handle(TestEvent evt) => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }

    [Fact]
    public void Valid_StaticMethodWithServices_NoDiagnostics()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public interface IMyService { }
    public record TestEvent(int Id) : FactoryEventBase;

    [FactoryEventHandler<TestEvent>]
    public partial class TestHandler
    {
        internal static Task Handle(TestEvent evt, [Service] IMyService svc, CancellationToken ct)
            => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }

    [Fact]
    public void Valid_MultipleEventTypes_NoDiagnostics()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record EventA(int Id) : FactoryEventBase;
    public record EventB(int Id) : FactoryEventBase;

    [FactoryEventHandler<EventA>]
    [FactoryEventHandler<EventB>]
    public partial class TestHandler
    {
        public Task HandleA(EventA evt) => Task.CompletedTask;
        public Task HandleB(EventB evt) => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }
}
