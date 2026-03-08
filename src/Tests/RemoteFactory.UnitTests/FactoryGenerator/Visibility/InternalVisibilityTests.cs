using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Visibility;

namespace RemoteFactory.UnitTests.FactoryGenerator.Visibility;

/// <summary>
/// Tests for internal factory method visibility feature.
/// Verifies that the generator correctly handles public, internal, and
/// mixed-visibility factory methods, including IsServerRuntime guard
/// emission and interface visibility.
/// </summary>
public class InternalVisibilityTests
{
    #region All-Internal Factory Tests

    /// <summary>
    /// All-internal factory methods produce an internal factory interface.
    /// This test verifies the interface is resolvable from DI (same assembly).
    /// </summary>
    [Fact]
    public void AllInternal_FactoryResolvesFromDI()
    {
        var provider = new ServerContainerBuilder().Build();

        var factory = provider.GetRequiredService<IAllInternalTargetFactory>();

        Assert.NotNull(factory);
    }

    /// <summary>
    /// All-internal Create method works on the server (IsServerRuntime guard passes).
    /// The ServerContainerBuilder uses NeatooFactory.Server mode where
    /// IsServerRuntime defaults to true.
    /// </summary>
    [Fact]
    public void AllInternal_Create_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IAllInternalTargetFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    /// <summary>
    /// All-internal Fetch method works on the server.
    /// </summary>
    [Fact]
    public void AllInternal_Fetch_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IAllInternalTargetFactory>();

        var result = factory.Fetch(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.ReceivedId);
    }

    /// <summary>
    /// Verifies the generated interface is internal by checking it cannot be
    /// resolved by external assemblies. Since we ARE in the same assembly,
    /// we verify the generated code directly contains "internal interface".
    /// </summary>
    [Fact]
    public void AllInternal_GeneratedInterface_IsInternal()
    {
        // The fact that we can resolve IAllInternalTargetFactory proves it exists.
        // We verify the interface accessibility via the DiagnosticTestHelper which
        // runs the generator and lets us inspect the output.
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class InternalOnly
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, outputCompilation, runResult) = DiagnosticTestHelper.RunGenerator(source);

        // Find the generated factory source
        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("InternalOnlyFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("internal interface IInternalOnlyFactory", generatedSource);
    }

    #endregion

    #region All-Public Non-Remote Factory Tests

    /// <summary>
    /// All-public non-[Remote] factory methods produce a public factory interface.
    /// Backward compatibility: identical behavior to pre-internal-visibility.
    /// </summary>
    [Fact]
    public void AllPublicNonRemote_FactoryResolvesFromDI()
    {
        var provider = new ServerContainerBuilder().Build();

        var factory = provider.GetRequiredService<IAllPublicNonRemoteTargetFactory>();

        Assert.NotNull(factory);
    }

    /// <summary>
    /// Public non-[Remote] Create method works on server (no guard to block it).
    /// </summary>
    [Fact]
    public void AllPublicNonRemote_Create_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IAllPublicNonRemoteTargetFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    /// <summary>
    /// Verifies the generated interface is public and Local methods have
    /// NO IsServerRuntime guard.
    /// </summary>
    [Fact]
    public void AllPublicNonRemote_GeneratedCode_HasNoGuard()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class PublicOnly
    {
        [Create]
        public void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("PublicOnlyFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("public interface IPublicOnlyFactory", generatedSource);
        // Local method should NOT have the IsServerRuntime guard
        Assert.DoesNotContain("NeatooRuntime.IsServerRuntime", generatedSource);
    }

    #endregion

    #region Mixed Visibility Factory Tests

    /// <summary>
    /// Mixed visibility factory: public interface with only public methods.
    /// Internal methods are excluded from the interface but present on the
    /// concrete factory class.
    /// </summary>
    [Fact]
    public void MixedVisibility_FactoryResolvesFromDI()
    {
        var provider = new ServerContainerBuilder().Build();

        var factory = provider.GetRequiredService<IMixedVisibilityTargetFactory>();

        Assert.NotNull(factory);
    }

    /// <summary>
    /// Mixed visibility: public Create method works (no guard).
    /// </summary>
    [Fact]
    public void MixedVisibility_PublicCreate_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IMixedVisibilityTargetFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    /// <summary>
    /// Mixed visibility: public interface includes internal methods with 'internal' modifier.
    /// Internal methods appear on the public interface prefixed with 'internal'.
    /// </summary>
    [Fact]
    public void MixedVisibility_GeneratedCode_PublicInterfaceIncludesInternalMethods()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MixedVis
    {
        [Create]
        public void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MixedVisFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        // Interface should be public
        Assert.Contains("public interface IMixedVisFactory", generatedSource);
        // Interface should contain Create (public, no modifier)
        Assert.Contains("MixedVis Create(", generatedSource);
        // Interface should contain Fetch with 'internal' modifier
        var interfaceStart = generatedSource.IndexOf("public interface IMixedVisFactory");
        var interfaceEnd = generatedSource.IndexOf("}", interfaceStart);
        var interfaceBlock = generatedSource.Substring(interfaceStart, interfaceEnd - interfaceStart);
        Assert.Contains("internal MixedVis Fetch(", interfaceBlock);
    }

    /// <summary>
    /// Mixed visibility: LocalCreate (public method) has NO guard,
    /// LocalFetch (internal method) HAS guard.
    /// </summary>
    [Fact]
    public void MixedVisibility_GuardOnlyOnInternalMethod()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MixedGuard
    {
        [Create]
        public void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MixedGuardFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Extract LocalCreate method - should NOT have guard
        var localCreateStart = generatedSource.IndexOf("LocalCreate(");
        var localCreateEnd = generatedSource.IndexOf("LocalFetch(", localCreateStart);
        var localCreateBlock = generatedSource.Substring(localCreateStart, localCreateEnd - localCreateStart);
        Assert.DoesNotContain("NeatooRuntime.IsServerRuntime", localCreateBlock);

        // Extract LocalFetch method - should HAVE guard
        var localFetchStart = generatedSource.IndexOf("LocalFetch(");
        var localFetchEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localFetchStart);
        var localFetchBlock = generatedSource.Substring(localFetchStart, localFetchEnd - localFetchStart);
        Assert.Contains("NeatooRuntime.IsServerRuntime", localFetchBlock);
    }

    #endregion

    #region All-Internal Guard Verification

    /// <summary>
    /// All-internal: both LocalCreate and LocalFetch have IsServerRuntime guards.
    /// </summary>
    [Fact]
    public void AllInternal_AllLocalMethodsHaveGuard()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class AllIntGuard
    {
        [Create]
        internal void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("AllIntGuardFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Both Local methods should have the guard
        var localCreateStart = generatedSource.IndexOf("LocalCreate(");
        var localFetchStart = generatedSource.IndexOf("LocalFetch(");
        var localCreateBlock = generatedSource.Substring(localCreateStart, localFetchStart - localCreateStart);
        var localFetchBlock = generatedSource.Substring(localFetchStart);

        Assert.Contains("NeatooRuntime.IsServerRuntime", localCreateBlock);
        Assert.Contains("NeatooRuntime.IsServerRuntime", localFetchBlock);
    }

    #endregion

    #region DynamicDependency Preservation

    /// <summary>
    /// Internal interface still emits [DynamicDependency] attribute on
    /// the first interface method for IL trimming support.
    /// </summary>
    [Fact]
    public void AllInternal_DynamicDependencyStillEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class IntDynDep
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IntDynDepFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IntDynDepFactory))]", generatedSource);
    }

    #endregion

    #region Internal Class with Matched Interface - All Internal Methods

    /// <summary>
    /// Internal class with all-internal methods and a matching public interface:
    /// generated factory interface should be internal, resolvable from DI in same assembly.
    /// </summary>
    [Fact]
    public void InternalClassAllInternal_FactoryResolvesFromDI()
    {
        var provider = new ServerContainerBuilder().Build();

        var factory = provider.GetRequiredService<IInternalClassTargetFactory>();

        Assert.NotNull(factory);
    }

    /// <summary>
    /// Internal class with matching interface: Create works on server
    /// (IsServerRuntime guard passes in Server mode).
    /// </summary>
    [Fact]
    public void InternalClassAllInternal_Create_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IInternalClassTargetFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    /// <summary>
    /// Internal class with matching interface: Fetch works on server.
    /// </summary>
    [Fact]
    public void InternalClassAllInternal_Fetch_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IInternalClassTargetFactory>();

        var result = factory.Fetch(42);

        Assert.NotNull(result);
        Assert.Equal(42, result.ReceivedId);
    }

    /// <summary>
    /// Internal class with matching interface: factory Create returns the interface type,
    /// not the concrete type. Verifies the naming convention matching.
    /// </summary>
    [Fact]
    public void InternalClassAllInternal_ReturnType_IsInterface()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IInternalClassTargetFactory>();

        IInternalClassTarget result = factory.Create();

        Assert.IsAssignableFrom<IInternalClassTarget>(result);
    }

    /// <summary>
    /// Internal class with all-internal methods and matching interface:
    /// generated factory interface is internal, uses interface type in return,
    /// and all Local* methods have IsServerRuntime guard.
    /// </summary>
    [Fact]
    public void InternalClassAllInternal_GeneratedCode_InternalInterfaceWithInterfaceReturnType()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IIntClsAllInt { }

    [Factory]
    internal partial class IntClsAllInt : IIntClsAllInt
    {
        [Create]
        internal void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IntClsAllIntFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Interface should be internal (all methods are internal)
        Assert.Contains("internal interface IIntClsAllIntFactory", generatedSource);

        // Return types should use the interface, not the concrete class
        Assert.Contains("IIntClsAllInt Create(", generatedSource);
        Assert.Contains("IIntClsAllInt Fetch(", generatedSource);

        // Both Local methods should have the guard
        var localCreateStart = generatedSource.IndexOf("LocalCreate(");
        var localFetchStart = generatedSource.IndexOf("LocalFetch(");
        var localCreateBlock = generatedSource.Substring(localCreateStart, localFetchStart - localCreateStart);
        var localFetchBlock = generatedSource.Substring(localFetchStart);

        Assert.Contains("NeatooRuntime.IsServerRuntime", localCreateBlock);
        Assert.Contains("NeatooRuntime.IsServerRuntime", localFetchBlock);
    }

    #endregion

    #region Internal Class with Matched Interface - All Public Methods (Aggregate Root Pattern)

    /// <summary>
    /// Internal class with all-public methods and a matching public interface:
    /// generated factory interface should be public, resolvable from DI.
    /// </summary>
    [Fact]
    public void InternalClassAllPublic_FactoryResolvesFromDI()
    {
        var provider = new ServerContainerBuilder().Build();

        var factory = provider.GetRequiredService<IInternalClassPublicMethodsFactory>();

        Assert.NotNull(factory);
    }

    /// <summary>
    /// Internal class with all-public methods and matching interface: Create works on server
    /// (no guard to block it since the method is public).
    /// </summary>
    [Fact]
    public void InternalClassAllPublic_Create_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IInternalClassPublicMethodsFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    /// <summary>
    /// Internal class with all-public methods and matching interface: factory Create returns
    /// the interface type, not the concrete type.
    /// </summary>
    [Fact]
    public void InternalClassAllPublic_ReturnType_IsInterface()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IInternalClassPublicMethodsFactory>();

        IInternalClassPublicMethods result = factory.Create();

        Assert.IsAssignableFrom<IInternalClassPublicMethods>(result);
    }

    /// <summary>
    /// Internal class with all-public methods and matching interface:
    /// generated factory interface is public, uses interface type in return,
    /// and Local* methods have NO IsServerRuntime guard.
    /// </summary>
    [Fact]
    public void InternalClassAllPublic_GeneratedCode_PublicInterfaceNoGuard()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IIntClsPub { }

    [Factory]
    internal partial class IntClsPub : IIntClsPub
    {
        [Create]
        public void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IntClsPubFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Interface should be public (has public methods)
        Assert.Contains("public interface IIntClsPubFactory", generatedSource);

        // Return type should use the interface, not the concrete class
        Assert.Contains("IIntClsPub Create(", generatedSource);

        // Local method should NOT have the IsServerRuntime guard
        Assert.DoesNotContain("NeatooRuntime.IsServerRuntime", generatedSource);
    }

    #endregion

    #region Internal Class with Matched Interface - Mixed Visibility

    /// <summary>
    /// Internal class with mixed visibility and a matching public interface:
    /// generated factory interface should be public, resolvable from DI.
    /// </summary>
    [Fact]
    public void InternalClassMixed_FactoryResolvesFromDI()
    {
        var provider = new ServerContainerBuilder().Build();

        var factory = provider.GetRequiredService<IInternalClassMixedFactory>();

        Assert.NotNull(factory);
    }

    /// <summary>
    /// Internal class with mixed visibility: public Create method works on server (no guard).
    /// </summary>
    [Fact]
    public void InternalClassMixed_PublicCreate_WorksOnServer()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IInternalClassMixedFactory>();

        var result = factory.Create();

        Assert.NotNull(result);
        Assert.True(result.CreateCalled);
    }

    /// <summary>
    /// Internal class with mixed visibility and matching interface: factory Create returns
    /// the interface type, not the concrete type.
    /// </summary>
    [Fact]
    public void InternalClassMixed_ReturnType_IsInterface()
    {
        var provider = new ServerContainerBuilder().Build();
        var factory = provider.GetRequiredService<IInternalClassMixedFactory>();

        IInternalClassMixed result = factory.Create();

        Assert.IsAssignableFrom<IInternalClassMixed>(result);
    }

    /// <summary>
    /// Internal class with mixed visibility and matching interface:
    /// public factory interface uses the interface return type,
    /// includes all methods — internal methods get 'internal' modifier.
    /// LocalCreate has NO guard; LocalFetch HAS guard.
    /// </summary>
    [Fact]
    public void InternalClassMixed_GeneratedCode_PublicInterfaceIncludesInternalMethods()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IIntClsMix { }

    [Factory]
    internal partial class IntClsMix : IIntClsMix
    {
        [Create]
        public void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IntClsMixFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Interface should be public (has public methods)
        Assert.Contains("public interface IIntClsMixFactory", generatedSource);

        // Return type should use the interface
        Assert.Contains("IIntClsMix Create(", generatedSource);

        // Interface should contain Fetch with 'internal' modifier
        var interfaceStart = generatedSource.IndexOf("public interface IIntClsMixFactory");
        var interfaceEnd = generatedSource.IndexOf("}", interfaceStart);
        var interfaceBlock = generatedSource.Substring(interfaceStart, interfaceEnd - interfaceStart);
        Assert.Contains("internal IIntClsMix Fetch(", interfaceBlock);
    }

    /// <summary>
    /// Internal class with mixed visibility and matching interface:
    /// LocalCreate (public method) has NO guard, LocalFetch (internal method) HAS guard.
    /// </summary>
    [Fact]
    public void InternalClassMixed_GuardOnlyOnInternalMethod()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IIntClsMixGuard { }

    [Factory]
    internal partial class IntClsMixGuard : IIntClsMixGuard
    {
        [Create]
        public void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IntClsMixGuardFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Extract LocalCreate method - should NOT have guard
        var localCreateStart = generatedSource.IndexOf("LocalCreate(");
        var localFetchStart = generatedSource.IndexOf("LocalFetch(", localCreateStart);
        var localCreateBlock = generatedSource.Substring(localCreateStart, localFetchStart - localCreateStart);
        Assert.DoesNotContain("NeatooRuntime.IsServerRuntime", localCreateBlock);

        // Extract LocalFetch method - should HAVE guard
        var localFetchEnd = generatedSource.IndexOf("public static void FactoryServiceRegistrar", localFetchStart);
        var localFetchBlock = generatedSource.Substring(localFetchStart, localFetchEnd - localFetchStart);
        Assert.Contains("NeatooRuntime.IsServerRuntime", localFetchBlock);
    }

    #endregion

    #region Internal Class with Matched Interface - DynamicDependency Preservation

    /// <summary>
    /// Internal class with matching interface: [DynamicDependency] attribute
    /// is still emitted on the first interface method for IL trimming support.
    /// </summary>
    [Fact]
    public void InternalClassWithInterface_DynamicDependencyStillEmitted()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IIntClsDynDep { }

    [Factory]
    internal partial class IntClsDynDep : IIntClsDynDep
    {
        [Create]
        internal void Create() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IntClsDynDepFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);
        Assert.Contains("[DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(IntClsDynDepFactory))]", generatedSource);
    }

    #endregion

    #region Mixed Visibility - Internal Methods on Public Interface

    /// <summary>
    /// Mixed visibility: internal Fetch method should appear on the public factory
    /// interface with the 'internal' access modifier, not be excluded entirely.
    /// </summary>
    [Fact]
    public void MixedVisibility_InternalFetch_IncludedOnInterfaceAsInternal()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MixedWithInternalFetch
    {
        [Create]
        public void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MixedWithInternalFetchFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Interface should be public
        Assert.Contains("public interface IMixedWithInternalFetchFactory", generatedSource);

        // Interface should contain Create (public, no modifier needed)
        var interfaceStart = generatedSource.IndexOf("public interface IMixedWithInternalFetchFactory");
        var interfaceEnd = generatedSource.IndexOf("}", interfaceStart);
        var interfaceBlock = generatedSource.Substring(interfaceStart, interfaceEnd - interfaceStart);

        Assert.Contains("MixedWithInternalFetch Create(", interfaceBlock);

        // Interface should contain Fetch with 'internal' modifier
        Assert.Contains("internal MixedWithInternalFetch Fetch(", interfaceBlock);
    }

    /// <summary>
    /// Mixed visibility with Save: public Create + internal Insert/Update/Delete.
    /// The Save method should appear on the public interface with 'internal' modifier.
    /// This is the child entity pattern where parent calls factory.Save(child).
    /// </summary>
    [Fact]
    public void MixedVisibility_InternalSave_IncludedOnInterfaceAsInternal()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    [Factory]
    public partial class MixedWithInternalSave : IFactorySaveMeta
    {
        public bool IsDeleted { get; set; }
        public bool IsNew { get; set; }

        [Create]
        public void Create() { }

        [Insert]
        internal void Insert() { }

        [Update]
        internal void Update() { }

        [Delete]
        internal void Delete() { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("MixedWithInternalSaveFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Interface should be public
        Assert.Contains("public interface IMixedWithInternalSaveFactory", generatedSource);

        // Interface should contain Create (public)
        var interfaceStart = generatedSource.IndexOf("public interface IMixedWithInternalSaveFactory");
        var interfaceEnd = generatedSource.IndexOf("}", interfaceStart);
        var interfaceBlock = generatedSource.Substring(interfaceStart, interfaceEnd - interfaceStart);

        Assert.Contains("MixedWithInternalSave Create(", interfaceBlock);

        // Interface should contain Save with 'internal' modifier
        // (not Task-wrapped since the write methods are non-remote, non-async)
        Assert.Contains("internal MixedWithInternalSave? Save(", interfaceBlock);
    }

    /// <summary>
    /// Internal class with mixed visibility and matching public interface:
    /// internal Fetch should appear on the factory interface with 'internal' modifier.
    /// </summary>
    [Fact]
    public void InternalClassMixed_InternalFetch_IncludedOnInterfaceAsInternal()
    {
        var source = @"
using Neatoo.RemoteFactory;

namespace TestNamespace
{
    public interface IIntClsMixInt { }

    [Factory]
    internal partial class IntClsMixInt : IIntClsMixInt
    {
        [Create]
        public void Create() { }

        [Fetch]
        internal void Fetch(int id) { }
    }
}
";
        var (_, _, runResult) = DiagnosticTestHelper.RunGenerator(source);

        var generatedSource = runResult.GeneratedTrees
            .FirstOrDefault(t => t.FilePath.Contains("IntClsMixIntFactory"))
            ?.GetText()
            ?.ToString();

        Assert.NotNull(generatedSource);

        // Interface should be public (has public methods)
        Assert.Contains("public interface IIntClsMixIntFactory", generatedSource);

        // Interface should contain Create (public)
        var interfaceStart = generatedSource.IndexOf("public interface IIntClsMixIntFactory");
        var interfaceEnd = generatedSource.IndexOf("}", interfaceStart);
        var interfaceBlock = generatedSource.Substring(interfaceStart, interfaceEnd - interfaceStart);

        Assert.Contains("IIntClsMixInt Create(", interfaceBlock);

        // Interface should contain Fetch with 'internal' modifier
        Assert.Contains("internal IIntClsMixInt Fetch(", interfaceBlock);
    }

    #endregion
}
