using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for [FactoryEventHandler&lt;T&gt;] diagnostics: NF0501, NF0502, NF0101.
///
/// After the client-relay redesign, only static-method handlers are generated; instance
/// methods are silently skipped (Rule 16). Diagnostics therefore trigger on STATIC
/// candidates that don't match the expected shape. Instance-only handler classes
/// compile without diagnostics (and without generated code).
/// </summary>
public class NF05xxFactoryEventHandlerTests
{
    // =========================================================================
    // NF0501: No matching static handler method when a static candidate exists
    // =========================================================================

    [Fact]
    public void NF0501_StaticMethod_WrongReturnType_ReportsDiagnostic()
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
        // Static Task<string> triggers the 'static candidate but wrong shape' path.
        public static Task<string> Handle(TestEvent evt) => Task.FromResult(""x"");
    }
}
";

        DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0501", DiagnosticSeverity.Error);
    }

    [Fact]
    public void NF0501_StaticMethod_WrongParamType_ReportsDiagnostic()
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
        // Static, returns Task — triggers the 'static candidate but event-type mismatch' path.
        public static Task Handle(TestEventB evt) => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0501", DiagnosticSeverity.Error);
    }

    // =========================================================================
    // NF0502: Ambiguous — multiple matching static methods
    // =========================================================================

    [Fact]
    public void NF0502_MultipleMatchingStaticMethods_ReportsDiagnostic()
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
        public static Task HandleA(TestEvent evt) => Task.CompletedTask;
        public static Task HandleB(TestEvent evt) => Task.CompletedTask;
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0502", DiagnosticSeverity.Error);
        Assert.Contains("TestHandler", diagnostic.GetMessage());
    }

    // =========================================================================
    // Silently-unused: classes with only instance methods no longer emit diagnostics
    // =========================================================================

    [Fact]
    public void InstanceMethodHandler_ReportsNF0503Warning()
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
        // Rule 16 (upgraded): instance-method handler is ignored at codegen but emits
        // NF0503 Warning so consumers who didn't migrate from the old client-relay
        // pattern get a loud signal at compile time.
        public Task Handle(TestEvent evt) => Task.CompletedTask;
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0503", DiagnosticSeverity.Warning);
        Assert.Contains("TestHandler", diagnostic.GetMessage());
        Assert.Contains("Handle", diagnostic.GetMessage());
    }

    [Fact]
    public void EmptyHandler_SilentlyUnused_NoDiagnostic()
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
        // No handler method at all — no static candidate, so silent per Rule 16.
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }

    // =========================================================================
    // NF0101: Class must be partial (still applies to static-handler classes)
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
        public static Task Handle(TestEvent evt) => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0101", DiagnosticSeverity.Error);
    }

    // =========================================================================
    // Valid handlers — no diagnostics
    // =========================================================================

    [Fact]
    public void Valid_StaticMethod_NoDiagnostics()
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
        public static Task Handle(TestEvent evt) => Task.CompletedTask;
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
        public static Task HandleA(EventA evt) => Task.CompletedTask;
        public static Task HandleB(EventB evt) => Task.CompletedTask;
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }
}
