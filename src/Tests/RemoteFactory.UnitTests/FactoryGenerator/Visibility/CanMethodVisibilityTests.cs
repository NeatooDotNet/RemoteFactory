using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Visibility;

namespace RemoteFactory.UnitTests.FactoryGenerator.Visibility;

/// <summary>
/// Tests for Can* method visibility behavior.
/// Can* methods inherit their guard behavior from their parent factory method:
/// - Public parent method => No IsServerRuntime guard on Can method
/// - Internal parent method => IsServerRuntime guard on Can method
/// </summary>
public class CanMethodVisibilityTests : IDisposable
{
    private readonly IServiceProvider _provider;

    public CanMethodVisibilityTests()
    {
        _provider = new ServerContainerBuilder()
            .WithService<VisibilityTestAuth, VisibilityTestAuth>()
            .Build();
        VisibilityTestAuth.ShouldAllow = true;
    }

    public void Dispose()
    {
        VisibilityTestAuth.ShouldAllow = true;
        (_provider as IDisposable)?.Dispose();
    }

    #region Public Method Can* Tests

    /// <summary>
    /// CanCreate on a public method works on server (no guard to block it).
    /// </summary>
    [Fact]
    public void PublicMethod_CanCreate_WorksOnServer()
    {
        var factory = _provider.GetRequiredService<IPublicMethodWithAuthFactory>();

        var result = factory.CanCreate();

        Assert.True(result.HasAccess);
    }

    /// <summary>
    /// CanCreate on a public method reflects authorization state.
    /// </summary>
    [Fact]
    public void PublicMethod_CanCreate_ReflectsAuthState()
    {
        var factory = _provider.GetRequiredService<IPublicMethodWithAuthFactory>();

        VisibilityTestAuth.ShouldAllow = true;
        Assert.True(factory.CanCreate().HasAccess);

        VisibilityTestAuth.ShouldAllow = false;
        Assert.False(factory.CanCreate().HasAccess);
    }

    /// <summary>
    /// Verifies that LocalCanCreate for a public method has NO guard
    /// in the generated code.
    /// </summary>
    [Fact]
    public void PublicMethod_CanCreate_GeneratedCode_HasNoGuard()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class TestAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanCreate() { return true; }
    }

    [Factory]
    [AuthorizeFactory<TestAuth>]
    public partial class PublicWithAuth
    {
        [Create]
        public void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("PublicWithAuthFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Find LocalCanCreate method - should NOT have guard
        var localCanCreateStart = generatedSource.IndexOf("LocalCanCreate(");
        Assert.True(localCanCreateStart >= 0, "LocalCanCreate method not found in generated code");

        // Get the method body from LocalCanCreate to the next method or end of class
        var methodEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localCanCreateStart);
        if (methodEnd < 0) methodEnd = generatedSource.Length;
        var localCanCreateBlock = generatedSource.Substring(localCanCreateStart, methodEnd - localCanCreateStart);

        Assert.DoesNotContain("NeatooRuntime.IsServerRuntime", localCanCreateBlock);
    }

    #endregion

    #region Internal Method Can* Tests

    /// <summary>
    /// CanCreate on an internal method works on server (guard passes because
    /// NeatooRuntime.IsServerRuntime defaults to true).
    /// </summary>
    [Fact]
    public void InternalMethod_CanCreate_WorksOnServer()
    {
        var factory = _provider.GetRequiredService<IInternalMethodWithAuthFactory>();

        var result = factory.CanCreate();

        Assert.True(result.HasAccess);
    }

    /// <summary>
    /// CanCreate on an internal method reflects authorization state.
    /// </summary>
    [Fact]
    public void InternalMethod_CanCreate_ReflectsAuthState()
    {
        var factory = _provider.GetRequiredService<IInternalMethodWithAuthFactory>();

        VisibilityTestAuth.ShouldAllow = true;
        Assert.True(factory.CanCreate().HasAccess);

        VisibilityTestAuth.ShouldAllow = false;
        Assert.False(factory.CanCreate().HasAccess);
    }

    /// <summary>
    /// Verifies that LocalCanCreate for an internal method HAS the
    /// IsServerRuntime guard in the generated code.
    /// </summary>
    [Fact]
    public void InternalMethod_CanCreate_GeneratedCode_HasGuard()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class TestAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        public bool CanCreate() { return true; }
    }

    [Factory]
    [AuthorizeFactory<TestAuth>]
    public partial class InternalWithAuth
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("InternalWithAuthFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Find LocalCanCreate method - should HAVE guard
        var localCanCreateStart = generatedSource.IndexOf("LocalCanCreate(");
        Assert.True(localCanCreateStart >= 0, "LocalCanCreate method not found in generated code");

        var methodEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localCanCreateStart);
        if (methodEnd < 0) methodEnd = generatedSource.Length;
        var localCanCreateBlock = generatedSource.Substring(localCanCreateStart, methodEnd - localCanCreateStart);

        Assert.Contains("NeatooRuntime.IsServerRuntime", localCanCreateBlock);
    }

    #endregion
}
