using Microsoft.Extensions.DependencyInjection;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;
using RemoteFactory.UnitTests.TestTargets.Core;
using System.Reflection;

namespace RemoteFactory.UnitTests.FactoryGenerator.Core;

/// <summary>
/// Tests that verify the constructor injection detection correctly determines
/// which types should/should not have ordinal serialization generated.
///
/// Types that require DI (have constructors with non-service, non-default parameters)
/// should NOT implement IOrdinalSerializable because they cannot be instantiated
/// via object initializer syntax during deserialization.
/// </summary>
/// <remarks>
/// These tests use reflection to INSPECT generated types, not to invoke methods.
/// This is acceptable for verifying generator output decisions.
/// </remarks>
public class ConstructorInjectionTests
{
    // ============================================================================
    // Types that SHOULD implement IOrdinalSerializable
    // ============================================================================

    [Fact]
    public void SimpleDto_ImplementsIOrdinalSerializable()
    {
        // Arrange - CtorTarget_SimpleDto has parameterless constructor
        var type = typeof(CtorTarget_SimpleDto);

        // Act - Check if type implements IOrdinalSerializable (inspection reflection)
        var implementsInterface = type.GetInterfaces()
            .Any(i => i.Name == "IOrdinalSerializable");

        // Assert
        Assert.True(implementsInterface,
            "CtorTarget_SimpleDto should implement IOrdinalSerializable because it has a parameterless constructor");
    }

    [Fact]
    public void SimpleDto_HasFromOrdinalArray()
    {
        // Arrange
        var type = typeof(CtorTarget_SimpleDto);

        // Act - Check if FromOrdinalArray method exists (inspection reflection)
        var method = type.GetMethod("FromOrdinalArray", BindingFlags.Public | BindingFlags.Static);

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void SimpleDto_HasToOrdinalArray()
    {
        // Arrange
        var type = typeof(CtorTarget_SimpleDto);

        // Act - Check if ToOrdinalArray method exists (inspection reflection)
        var method = type.GetMethod("ToOrdinalArray", BindingFlags.Public | BindingFlags.Instance);

        // Assert
        Assert.NotNull(method);
    }

    [Fact]
    public void ExplicitCtor_ImplementsIOrdinalSerializable()
    {
        // Arrange - CtorTarget_ExplicitCtor has explicit parameterless constructor
        var type = typeof(CtorTarget_ExplicitCtor);

        // Act
        var implementsInterface = type.GetInterfaces()
            .Any(i => i.Name == "IOrdinalSerializable");

        // Assert
        Assert.True(implementsInterface,
            "CtorTarget_ExplicitCtor should implement IOrdinalSerializable because it has a parameterless constructor");
    }

    [Fact]
    public void WithDefaults_ImplementsIOrdinalSerializable()
    {
        // Arrange - CtorTarget_WithDefaults has constructor with all default parameters
        var type = typeof(CtorTarget_WithDefaults);

        // Act
        var implementsInterface = type.GetInterfaces()
            .Any(i => i.Name == "IOrdinalSerializable");

        // Assert
        Assert.True(implementsInterface,
            "CtorTarget_WithDefaults should implement IOrdinalSerializable because all constructor params have defaults");
    }

    // ============================================================================
    // Types that should NOT implement IOrdinalSerializable
    // ============================================================================

    [Fact]
    public void RequiredService_DoesNotImplementIOrdinalSerializable()
    {
        // Arrange - CtorTarget_RequiredService requires IService in constructor
        var type = typeof(CtorTarget_RequiredService);

        // Act
        var implementsInterface = type.GetInterfaces()
            .Any(i => i.Name == "IOrdinalSerializable");

        // Assert
        Assert.False(implementsInterface,
            "CtorTarget_RequiredService should NOT implement IOrdinalSerializable because constructor requires IService");
    }

    [Fact]
    public void RequiredService_DoesNotHaveFromOrdinalArray()
    {
        // Arrange
        var type = typeof(CtorTarget_RequiredService);

        // Act
        var method = type.GetMethod("FromOrdinalArray", BindingFlags.Public | BindingFlags.Static);

        // Assert
        Assert.Null(method);
    }

    [Fact]
    public void Abstract_DoesNotImplementIOrdinalSerializable()
    {
        // Arrange - CtorTarget_Abstract cannot be instantiated
        var type = typeof(CtorTarget_Abstract);

        // Act
        var implementsInterface = type.GetInterfaces()
            .Any(i => i.Name == "IOrdinalSerializable");

        // Assert
        Assert.False(implementsInterface,
            "CtorTarget_Abstract should NOT implement IOrdinalSerializable because abstract types cannot be instantiated");
    }

    [Fact]
    public void NeatooStyle_DoesNotImplementIOrdinalSerializable()
    {
        // Arrange - CtorTarget_NeatooStyle simulates the Neatoo pattern with required service in constructor
        var type = typeof(CtorTarget_NeatooStyle);

        // Act
        var implementsInterface = type.GetInterfaces()
            .Any(i => i.Name == "IOrdinalSerializable");

        // Assert
        Assert.False(implementsInterface,
            "CtorTarget_NeatooStyle should NOT implement IOrdinalSerializable because constructor requires IService (Neatoo pattern)");
    }

    [Fact]
    public void NeatooStyle_DoesNotHaveFromOrdinalArray()
    {
        // Arrange
        var type = typeof(CtorTarget_NeatooStyle);

        // Act
        var method = type.GetMethod("FromOrdinalArray", BindingFlags.Public | BindingFlags.Static);

        // Assert
        Assert.Null(method);
    }

    // ============================================================================
    // Verify factories still work correctly for DI-requiring types
    // ============================================================================

    [Fact]
    public void NeatooStyle_FactoryStillWorks()
    {
        // Arrange
        var provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        var factory = provider.GetService<ICtorTarget_NeatooStyleFactory>();

        // Act & Assert - Factory should work even though ordinal serialization isn't generated
        Assert.NotNull(factory);
        var entity = factory!.Create();
        Assert.NotNull(entity);
    }

    [Fact]
    public void RequiredService_FactoryStillWorks()
    {
        // Arrange
        var provider = new ServerContainerBuilder()
            .WithService<IService, Service>()
            .Build();
        var factory = provider.GetService<ICtorTarget_RequiredServiceFactory>();

        // Act & Assert
        Assert.NotNull(factory);
        var entity = factory!.Create("test");
        Assert.NotNull(entity);
        Assert.Equal("test", entity.Name);
    }
}
