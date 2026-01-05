using System.Reflection;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Tests that verify the constructor injection detection correctly determines
/// which types should/should not have ordinal serialization generated.
///
/// Types that require DI (have constructors with non-service, non-default parameters)
/// should NOT implement IOrdinalSerializable because they cannot be instantiated
/// via object initializer syntax during deserialization.
/// </summary>
public class ConstructorInjectionTests
{
	// ============================================================================
	// Types that SHOULD implement IOrdinalSerializable
	// ============================================================================

	[Fact]
	public void CtorTestSimpleDto_ImplementsIOrdinalSerializable()
	{
		// Arrange - CtorTestSimpleDto has parameterless constructor
		var type = typeof(CtorTestSimpleDto);

		// Act
		var implementsInterface = type.GetInterfaces()
			.Any(i => i.Name == "IOrdinalSerializable");

		// Assert
		Assert.True(implementsInterface,
			"CtorTestSimpleDto should implement IOrdinalSerializable because it has a parameterless constructor");
	}

	[Fact]
	public void CtorTestSimpleDto_HasFromOrdinalArray()
	{
		// Arrange
		var type = typeof(CtorTestSimpleDto);

		// Act
		var method = type.GetMethod("FromOrdinalArray", BindingFlags.Public | BindingFlags.Static);

		// Assert
		Assert.NotNull(method);
	}

	[Fact]
	public void CtorTestSimpleDto_HasToOrdinalArray()
	{
		// Arrange
		var type = typeof(CtorTestSimpleDto);

		// Act
		var method = type.GetMethod("ToOrdinalArray", BindingFlags.Public | BindingFlags.Instance);

		// Assert
		Assert.NotNull(method);
	}

	[Fact]
	public void CtorTestDtoWithExplicitCtor_ImplementsIOrdinalSerializable()
	{
		// Arrange - CtorTestDtoWithExplicitCtor has explicit parameterless constructor
		var type = typeof(CtorTestDtoWithExplicitCtor);

		// Act
		var implementsInterface = type.GetInterfaces()
			.Any(i => i.Name == "IOrdinalSerializable");

		// Assert
		Assert.True(implementsInterface,
			"CtorTestDtoWithExplicitCtor should implement IOrdinalSerializable because it has a parameterless constructor");
	}

	[Fact]
	public void CtorTestDtoWithDefaults_ImplementsIOrdinalSerializable()
	{
		// Arrange - CtorTestDtoWithDefaults has constructor with all default parameters
		var type = typeof(CtorTestDtoWithDefaults);

		// Act
		var implementsInterface = type.GetInterfaces()
			.Any(i => i.Name == "IOrdinalSerializable");

		// Assert
		Assert.True(implementsInterface,
			"CtorTestDtoWithDefaults should implement IOrdinalSerializable because all constructor params have defaults");
	}

	// ============================================================================
	// Types that should NOT implement IOrdinalSerializable
	// ============================================================================

	[Fact]
	public void CtorTestEntityWithRequiredService_DoesNotImplementIOrdinalSerializable()
	{
		// Arrange - CtorTestEntityWithRequiredService requires IService in constructor
		var type = typeof(CtorTestEntityWithRequiredService);

		// Act
		var implementsInterface = type.GetInterfaces()
			.Any(i => i.Name == "IOrdinalSerializable");

		// Assert
		Assert.False(implementsInterface,
			"CtorTestEntityWithRequiredService should NOT implement IOrdinalSerializable because constructor requires IService");
	}

	[Fact]
	public void CtorTestEntityWithRequiredService_DoesNotHaveFromOrdinalArray()
	{
		// Arrange
		var type = typeof(CtorTestEntityWithRequiredService);

		// Act
		var method = type.GetMethod("FromOrdinalArray", BindingFlags.Public | BindingFlags.Static);

		// Assert
		Assert.Null(method);
	}

	[Fact]
	public void CtorTestAbstractEntity_DoesNotImplementIOrdinalSerializable()
	{
		// Arrange - CtorTestAbstractEntity cannot be instantiated
		var type = typeof(CtorTestAbstractEntity);

		// Act
		var implementsInterface = type.GetInterfaces()
			.Any(i => i.Name == "IOrdinalSerializable");

		// Assert
		Assert.False(implementsInterface,
			"CtorTestAbstractEntity should NOT implement IOrdinalSerializable because abstract types cannot be instantiated");
	}

	[Fact]
	public void CtorTestNeatooStyleEntity_DoesNotImplementIOrdinalSerializable()
	{
		// Arrange - CtorTestNeatooStyleEntity simulates the Neatoo pattern with required service in constructor
		var type = typeof(CtorTestNeatooStyleEntity);

		// Act
		var implementsInterface = type.GetInterfaces()
			.Any(i => i.Name == "IOrdinalSerializable");

		// Assert
		Assert.False(implementsInterface,
			"CtorTestNeatooStyleEntity should NOT implement IOrdinalSerializable because constructor requires IService (Neatoo pattern)");
	}

	[Fact]
	public void CtorTestNeatooStyleEntity_DoesNotHaveFromOrdinalArray()
	{
		// Arrange
		var type = typeof(CtorTestNeatooStyleEntity);

		// Act
		var method = type.GetMethod("FromOrdinalArray", BindingFlags.Public | BindingFlags.Static);

		// Assert
		Assert.Null(method);
	}

	// ============================================================================
	// Verify factories still work correctly for DI-requiring types
	// ============================================================================

	[Fact]
	public void CtorTestNeatooStyleEntity_FactoryStillWorks()
	{
		// Arrange
		var scopes = ClientServerContainers.Scopes();
		var factory = scopes.local.ServiceProvider.GetService(typeof(ICtorTestNeatooStyleEntityFactory)) as ICtorTestNeatooStyleEntityFactory;

		// Act & Assert - Factory should work even though ordinal serialization isn't generated
		Assert.NotNull(factory);
		var entity = factory.Create();
		Assert.NotNull(entity);
	}

	[Fact]
	public void CtorTestEntityWithRequiredService_FactoryStillWorks()
	{
		// Arrange
		var scopes = ClientServerContainers.Scopes();
		var factory = scopes.local.ServiceProvider.GetService(typeof(ICtorTestEntityWithRequiredServiceFactory)) as ICtorTestEntityWithRequiredServiceFactory;

		// Act & Assert
		Assert.NotNull(factory);
		var entity = factory.Create("test");
		Assert.NotNull(entity);
		Assert.Equal("test", entity.Name);
	}
}
