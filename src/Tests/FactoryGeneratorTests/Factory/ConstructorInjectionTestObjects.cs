using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Test objects for validating constructor injection detection.
/// Types that require DI (have constructors with non-service parameters) should NOT
/// generate ordinal serialization code (IOrdinalSerializable, FromOrdinalArray, etc.)
/// because they cannot be instantiated via object initializer syntax.
/// </summary>

// ============================================================================
// Types that SHOULD generate ordinal serialization (can use object initializer)
// ============================================================================

/// <summary>
/// Simple class with parameterless constructor - SHOULD generate ordinal serialization.
/// </summary>
[Factory]
public partial class CtorTestSimpleDto
{
	public string Name { get; set; } = "";
	public int Value { get; set; }

	[Create]
	public static CtorTestSimpleDto Create(string name, int value)
		=> new CtorTestSimpleDto { Name = name, Value = value };
}

/// <summary>
/// Class with explicit parameterless constructor - SHOULD generate ordinal serialization.
/// </summary>
[Factory]
public partial class CtorTestDtoWithExplicitCtor
{
	public string Name { get; set; } = "";
	public int Value { get; set; }

	public CtorTestDtoWithExplicitCtor()
	{
		// Explicit parameterless constructor
	}

	[Create]
	public static CtorTestDtoWithExplicitCtor Create(string name, [Service] IService service)
		=> new CtorTestDtoWithExplicitCtor { Name = name };
}

/// <summary>
/// Class with constructor that has all default parameters - SHOULD generate ordinal serialization.
/// </summary>
[Factory]
public partial class CtorTestDtoWithDefaults
{
	public string Name { get; set; } = "";
	public int Value { get; set; }

	[Create]
	public CtorTestDtoWithDefaults(string name = "default", int value = 0)
	{
		Name = name;
		Value = value;
	}
}

// ============================================================================
// Types that should NOT generate ordinal serialization (require DI)
// ============================================================================

/// <summary>
/// Simulates a Neatoo-style entity that requires injected services.
/// This type should NOT generate ordinal serialization because it cannot
/// be instantiated without DI (the constructor requires IService).
/// </summary>
[Factory]
public partial class CtorTestEntityWithRequiredService
{
	private readonly IService _services;

	public string Name { get; set; } = "";
	public int Value { get; set; }

	// Constructor requires a service parameter - no parameterless constructor
	public CtorTestEntityWithRequiredService(IService services)
	{
		_services = services;
	}

	[Create]
	public static CtorTestEntityWithRequiredService Create(string name, [Service] IService services)
		=> new CtorTestEntityWithRequiredService(services) { Name = name };
}

/// <summary>
/// Abstract base class - should NOT generate ordinal serialization.
/// </summary>
[Factory]
public abstract partial class CtorTestAbstractEntity
{
	public string Id { get; set; } = "";

	[Create]
	public static CtorTestAbstractEntity Create(string id) => throw new NotImplementedException();
}

/// <summary>
/// Simulates Neatoo EntityBase pattern where constructor requires IEntityBaseServices.
/// This is the core pattern that was breaking Neatoo.
/// </summary>
[Factory]
public partial class CtorTestNeatooStyleEntity
{
	private readonly IService _entityServices;

	public Guid Id { get; set; }
	public string? PhoneNumber { get; set; }
	public string? PhoneType { get; set; }

	// Neatoo pattern: constructor requires injected services
	public CtorTestNeatooStyleEntity(IService entityServices)
	{
		_entityServices = entityServices;
	}

	[Create]
	public static CtorTestNeatooStyleEntity Create([Service] IService services)
		=> new CtorTestNeatooStyleEntity(services);

	[Fetch]
	public static CtorTestNeatooStyleEntity Fetch(Guid id, [Service] IService services)
		=> new CtorTestNeatooStyleEntity(services) { Id = id };
}
