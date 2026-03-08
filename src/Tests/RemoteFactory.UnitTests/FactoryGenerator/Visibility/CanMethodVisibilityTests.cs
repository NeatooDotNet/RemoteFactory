using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Visibility;

namespace RemoteFactory.UnitTests.FactoryGenerator.Visibility;

/// <summary>
/// Tests for Can* method visibility behavior.
/// Can* methods derive their guard behavior from the auth class methods (not the factory method):
/// - Public auth methods => No IsServerRuntime guard on Can method (regardless of factory method visibility)
/// - Internal auth methods (no [Remote]) => IsServerRuntime guard on Can method
/// - [Remote] auth methods => IsServerRuntime guard and remote routing on Can method
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
    /// Verifies that LocalCanCreate for an internal factory method with PUBLIC auth methods
    /// has NO guard. Can* derives from auth method visibility, not factory method visibility.
    /// </summary>
    [Fact]
    public void InternalFactory_PublicAuth_CanCreate_GeneratedCode_HasNoGuard()
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

        // Find LocalCanCreate method - should NOT have guard (auth is public)
        var localCanCreateStart = generatedSource.IndexOf("LocalCanCreate(");
        Assert.True(localCanCreateStart >= 0, "LocalCanCreate method not found in generated code");

        var methodEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localCanCreateStart);
        if (methodEnd < 0) methodEnd = generatedSource.Length;
        var localCanCreateBlock = generatedSource.Substring(localCanCreateStart, methodEnd - localCanCreateStart);

        Assert.DoesNotContain("NeatooRuntime.IsServerRuntime", localCanCreateBlock);
    }

    #endregion

    #region Auth-Method-Driven Can* Tests

    /// <summary>
    /// Verifies that LocalCanCreate for a [Remote] internal factory method with PUBLIC auth methods
    /// has NO guard. Can* derives from auth method visibility, not factory method visibility.
    /// Validates BR-1, BR-10: public auth + [Remote] internal factory => no guard.
    /// </summary>
    [Fact]
    public void RemoteInternalFactory_PublicAuth_CanCreate_GeneratedCode_HasNoGuard()
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
    public partial class RemoteInternalWithPublicAuth
    {
        [Remote, Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("RemoteInternalWithPublicAuthFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Find LocalCanCreate method - should NOT have guard (auth is public)
        var localCanCreateStart = generatedSource.IndexOf("LocalCanCreate(");
        Assert.True(localCanCreateStart >= 0, "LocalCanCreate method not found in generated code");

        var methodEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localCanCreateStart);
        if (methodEnd < 0) methodEnd = generatedSource.Length;
        var localCanCreateBlock = generatedSource.Substring(localCanCreateStart, methodEnd - localCanCreateStart);

        Assert.DoesNotContain("NeatooRuntime.IsServerRuntime", localCanCreateBlock);
    }

    /// <summary>
    /// Verifies that LocalCanCreate for an internal auth method HAS the guard.
    /// Validates BR-2: internal auth method => Can* has IsServerRuntime guard.
    /// </summary>
    [Fact]
    public void InternalAuth_CanCreate_GeneratedCode_HasGuard()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public class InternalAuthMethods
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        internal bool CanCreate() { return true; }
    }

    [Factory]
    [AuthorizeFactory<InternalAuthMethods>]
    public partial class EntityWithInternalAuth
    {
        [Create]
        public void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("EntityWithInternalAuthFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Find LocalCanCreate method - should HAVE guard (auth method is internal)
        var localCanCreateStart = generatedSource.IndexOf("LocalCanCreate(");
        Assert.True(localCanCreateStart >= 0, "LocalCanCreate method not found in generated code");

        var methodEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localCanCreateStart);
        if (methodEnd < 0) methodEnd = generatedSource.Length;
        var localCanCreateBlock = generatedSource.Substring(localCanCreateStart, methodEnd - localCanCreateStart);

        Assert.Contains("NeatooRuntime.IsServerRuntime", localCanCreateBlock);
    }

    /// <summary>
    /// Verifies that LocalCanCreate for a [Remote] auth method HAS the guard and is async.
    /// Validates BR-3, BR-6: [Remote] auth method => Can* has guard, routes remotely, promoted on interface.
    /// </summary>
    [Fact]
    public void RemoteAuth_CanCreate_GeneratedCode_HasGuard_And_IsAsync()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IRemoteAuth
    {
        [Remote]
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        bool CanCreate();
    }

    public class RemoteAuthImpl : IRemoteAuth
    {
        public bool CanCreate() { return true; }
    }

    [Factory]
    [AuthorizeFactory<IRemoteAuth>]
    public partial class EntityWithRemoteAuth
    {
        [Create]
        public void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("EntityWithRemoteAuthFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Find LocalCanCreate method - should HAVE guard ([Remote] auth method)
        var localCanCreateStart = generatedSource.IndexOf("LocalCanCreate(");
        Assert.True(localCanCreateStart >= 0, "LocalCanCreate method not found in generated code");

        var methodEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localCanCreateStart);
        if (methodEnd < 0) methodEnd = generatedSource.Length;
        var localCanCreateBlock = generatedSource.Substring(localCanCreateStart, methodEnd - localCanCreateStart);

        Assert.Contains("NeatooRuntime.IsServerRuntime", localCanCreateBlock);

        // CanCreate should be async (routes to server)
        Assert.Contains("Task<Authorized>", generatedSource);

        // Remote delegate should be generated
        Assert.Contains("RemoteCanCreate", generatedSource);
    }

    /// <summary>
    /// Verifies that when auth methods have mixed visibility (some public, some [Remote]),
    /// the Can* method gets a guard (most restrictive wins).
    /// Validates BR-8: mixed auth => guard (most restrictive wins for security).
    /// </summary>
    [Fact]
    public void MixedAuth_PublicAndRemote_CanCreate_GeneratedCode_HasGuard()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IMixedAuth
    {
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        bool HasAccess();

        [Remote]
        [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
        bool CanCreate();
    }

    public class MixedAuthImpl : IMixedAuth
    {
        public bool HasAccess() { return true; }
        public bool CanCreate() { return true; }
    }

    [Factory]
    [AuthorizeFactory<IMixedAuth>]
    public partial class EntityWithMixedAuth
    {
        [Create]
        public void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("EntityWithMixedAuthFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Find LocalCanCreate method - should HAVE guard ([Remote] auth method = most restrictive)
        var localCanCreateStart = generatedSource.IndexOf("LocalCanCreate(");
        Assert.True(localCanCreateStart >= 0, "LocalCanCreate method not found in generated code");

        var methodEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localCanCreateStart);
        if (methodEnd < 0) methodEnd = generatedSource.Length;
        var localCanCreateBlock = generatedSource.Substring(localCanCreateStart, methodEnd - localCanCreateStart);

        Assert.Contains("NeatooRuntime.IsServerRuntime", localCanCreateBlock);
    }

    #endregion
}
