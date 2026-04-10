using Microsoft.CodeAnalysis;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.Diagnostics;

/// <summary>
/// Tests for [FactoryEventHandler] diagnostics: NF0401, NF0404, NF0405.
/// </summary>
public class NF04xxFactoryEventHandlerTests
{
    // =========================================================================
    // NF0404: Missing CancellationToken
    // =========================================================================

    [Fact]
    public void NF0404_FactoryEventHandler_NoCancellationToken_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [Factory]
    public static partial class TestHandler
    {
        [FactoryEventHandler]
        private static Task _Handle(TestEvent evt)
        {
            return Task.CompletedTask;
        }
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0404", DiagnosticSeverity.Error);
        Assert.Contains("_Handle", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0404_FactoryEventHandler_WithCancellationToken_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [Factory]
    public static partial class TestHandler
    {
        [FactoryEventHandler]
        private static Task _Handle(TestEvent evt, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0404").ToList();
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // NF0401: Wrong return type (Task<T> instead of Task/void)
    // =========================================================================

    [Fact]
    public void NF0401_FactoryEventHandler_ReturnsTaskT_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [Factory]
    public static partial class TestHandler
    {
        [FactoryEventHandler]
        private static Task<string> _Handle(TestEvent evt, CancellationToken ct)
        {
            return Task.FromResult(""result"");
        }
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0401", DiagnosticSeverity.Error);
        Assert.Contains("_Handle", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0401_FactoryEventHandler_ReturnsTask_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [Factory]
    public static partial class TestHandler
    {
        [FactoryEventHandler]
        private static Task _Handle(TestEvent evt, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0401").ToList();
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // NF0405: Must be static (any visibility)
    // =========================================================================

    [Fact]
    public void NF0405_FactoryEventHandler_InstanceMethod_ReportsDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [Factory]
    public partial class TestHandler
    {
        [FactoryEventHandler]
        internal Task Handle(TestEvent evt, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
";

        var diagnostic = DiagnosticTestHelper.AssertHasDiagnostic(source, "NF0405", DiagnosticSeverity.Error);
        Assert.Contains("Handle", diagnostic.GetMessage());
    }

    [Fact]
    public void NF0405_FactoryEventHandler_PublicStaticMethod_NoDiagnostic()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record TestEvent(int Id) : FactoryEventBase;

    [Factory]
    public partial class TestHandler
    {
        [FactoryEventHandler]
        public static Task Handle(TestEvent evt, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
";

        var diagnostics = DiagnosticTestHelper.GetDiagnosticsById(source, "NF0405").ToList();
        Assert.Empty(diagnostics);
    }

    // =========================================================================
    // Valid handler — no diagnostics at all
    // =========================================================================

    [Fact]
    public void FactoryEventHandler_ValidHandler_NoRemoteFactoryDiagnostics()
    {
        var source = @"
using Neatoo.RemoteFactory;
using System.Threading;
using System.Threading.Tasks;

namespace TestNamespace
{
    public record OrderPlaced(int OrderId) : FactoryEventBase;

    [Factory]
    public static partial class OrderHandler
    {
        [FactoryEventHandler]
        private static Task _HandleOrder(OrderPlaced evt, CancellationToken ct)
        {
            return Task.CompletedTask;
        }
    }
}
";

        DiagnosticTestHelper.AssertNoRemoteFactoryDiagnostics(source);
    }
}
