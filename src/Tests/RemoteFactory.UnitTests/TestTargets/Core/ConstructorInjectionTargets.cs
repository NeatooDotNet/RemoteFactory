using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Core;

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
public partial class CtorTarget_SimpleDto
{
    public string Name { get; set; } = "";
    public int Value { get; set; }

    [Create]
    public static CtorTarget_SimpleDto Create(string name, int value)
        => new CtorTarget_SimpleDto { Name = name, Value = value };
}

/// <summary>
/// Class with explicit parameterless constructor - SHOULD generate ordinal serialization.
/// </summary>
[Factory]
public partial class CtorTarget_ExplicitCtor
{
    public string Name { get; set; } = "";
    public int Value { get; set; }

    public CtorTarget_ExplicitCtor()
    {
        // Explicit parameterless constructor
    }

    [Create]
    public static CtorTarget_ExplicitCtor Create(string name, [Service] IService service)
        => new CtorTarget_ExplicitCtor { Name = name };
}

/// <summary>
/// Class with constructor that has all default parameters - SHOULD generate ordinal serialization.
/// </summary>
[Factory]
public partial class CtorTarget_WithDefaults
{
    public string Name { get; set; } = "";
    public int Value { get; set; }

    [Create]
    public CtorTarget_WithDefaults(string name = "default", int value = 0)
    {
        Name = name;
        Value = value;
    }
}

// ============================================================================
// Types that should NOT generate ordinal serialization (require DI)
// ============================================================================

/// <summary>
/// Simulates an entity that requires injected services.
/// This type should NOT generate ordinal serialization because it cannot
/// be instantiated without DI (the constructor requires IService).
/// </summary>
[Factory]
public partial class CtorTarget_RequiredService
{
    private readonly IService _services;

    public string Name { get; set; } = "";
    public int Value { get; set; }

    // Constructor requires a service parameter - no parameterless constructor
    public CtorTarget_RequiredService(IService services)
    {
        _services = services;
    }

    [Create]
    public static CtorTarget_RequiredService Create(string name, [Service] IService services)
        => new CtorTarget_RequiredService(services) { Name = name };
}

/// <summary>
/// Abstract base class - should NOT generate ordinal serialization.
/// </summary>
[Factory]
public abstract partial class CtorTarget_Abstract
{
    public string Id { get; set; } = "";

    [Create]
    public static CtorTarget_Abstract Create(string id) => throw new NotImplementedException();
}

/// <summary>
/// Simulates Neatoo EntityBase pattern where constructor requires IEntityBaseServices.
/// This is the core pattern that was breaking Neatoo.
/// </summary>
[Factory]
public partial class CtorTarget_NeatooStyle
{
    private readonly IService _entityServices;

    public Guid Id { get; set; }
    public string? PhoneNumber { get; set; }
    public string? PhoneType { get; set; }

    // Neatoo pattern: constructor requires injected services
    public CtorTarget_NeatooStyle(IService entityServices)
    {
        _entityServices = entityServices;
    }

    [Create]
    public static CtorTarget_NeatooStyle Create([Service] IService services)
        => new CtorTarget_NeatooStyle(services);

    [Fetch]
    public static CtorTarget_NeatooStyle Fetch(Guid id, [Service] IService services)
        => new CtorTarget_NeatooStyle(services) { Id = id };
}
