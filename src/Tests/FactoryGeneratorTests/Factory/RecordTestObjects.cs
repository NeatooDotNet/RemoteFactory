using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;

/// <summary>
/// Test record definitions for validating record support in RemoteFactory.
/// These records cover various scenarios:
/// - Positional records with [Create] on type
/// - Records with explicit constructors
/// - Records with [Fetch] operations
/// - Records with service injection
/// - Records with remote operations
/// - Sealed records
/// - Records with default values
/// - Records with additional properties
/// </summary>

// ============================================================================
// Simple positional record with [Create] on type
// ============================================================================

/// <summary>
/// Simple positional record using [Create] on the type declaration.
/// The generator should use the primary constructor for factory creation.
/// </summary>
[Factory]
[Create]
public partial record SimpleRecord(string Name, int Value);

// ============================================================================
// Record with service injection in primary constructor
// ============================================================================

/// <summary>
/// Record with service dependency injected through primary constructor.
/// The [Service] attribute should work on positional parameters.
/// </summary>
[Factory]
[Create]
public partial record RecordWithService(string Name, [Service] IService Service);

// ============================================================================
// Record with Fetch operations
// ============================================================================

/// <summary>
/// Record with static Fetch methods.
/// Tests that [Fetch] operations work correctly with records.
/// </summary>
[Factory]
[Create]
public partial record FetchableRecord(string Id, string Data)
{
    [Fetch]
    public static FetchableRecord FetchById(string id)
        => new FetchableRecord(id, $"Fetched-{id}");

    [Fetch]
    public static Task<FetchableRecord> FetchByIdAsync(string id)
        => Task.FromResult(new FetchableRecord(id, $"AsyncFetched-{id}"));

    // Note: Nullable return types on static fetch methods currently cause CS8603 warnings
    // in the generated code. This is a known limitation that could be addressed in a
    // future generator update. For testing purposes, we use non-nullable returns.
    //
    // [Fetch]
    // public static FetchableRecord? FetchByIdNullable(string id)
    //     => id == "null" ? null : new FetchableRecord(id, $"Nullable-{id}");
}

// ============================================================================
// Record with explicit constructor (not positional)
// ============================================================================

/// <summary>
/// Record with explicit constructor and [Create] on the constructor.
/// This is an alternative to [Create] on the type.
/// </summary>
[Factory]
public partial record ExplicitConstructorRecord
{
    public string Name { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }

    [Create]
    public ExplicitConstructorRecord(string name)
    {
        Name = name;
        CreatedAt = DateTime.UtcNow;
    }
}

// ============================================================================
// Sealed record
// ============================================================================

/// <summary>
/// Sealed record to verify factory generation works with sealed records.
/// </summary>
[Factory]
[Create]
public sealed partial record SealedRecord(string Value);

// ============================================================================
// Record with default parameter values
// ============================================================================

/// <summary>
/// Record with default parameter values in primary constructor.
/// Tests that default values are correctly handled.
/// </summary>
[Factory]
[Create]
public partial record RecordWithDefaults(string Name = "default", int Value = 42);

// ============================================================================
// Record with additional init properties beyond primary constructor
// ============================================================================

/// <summary>
/// Record with extra init properties beyond the primary constructor.
/// The factory should create the record using the primary constructor.
/// </summary>
[Factory]
[Create]
public partial record RecordWithExtraProps(string Name)
{
    public string ComputedProp => $"Hello, {Name}";
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
}

// ============================================================================
// Remote record operations
// ============================================================================

/// <summary>
/// Record with remote fetch operations.
/// Tests that [Remote] [Fetch] works correctly with records through serialization.
/// </summary>
[Factory]
[Create]
public partial record RemoteRecord(string Name)
{
    [Fetch]
    [Remote]
    public static RemoteRecord FetchRemote(string name)
        => new RemoteRecord($"Remote-{name}");

    [Fetch]
    [Remote]
    public static Task<RemoteRecord> FetchRemoteAsync(string name)
        => Task.FromResult(new RemoteRecord($"RemoteAsync-{name}"));
}

// ============================================================================
// Record with service in fetch method
// ============================================================================

/// <summary>
/// Record with service injection in fetch method.
/// </summary>
[Factory]
[Create]
public partial record RecordWithServiceFetch(string Id, string Data)
{
    [Fetch]
    public static RecordWithServiceFetch FetchWithService(string id, [Service] IService service)
    {
        if (service == null)
            throw new ArgumentNullException(nameof(service));
        return new RecordWithServiceFetch(id, $"ServiceFetched-{id}");
    }
}

// ============================================================================
// Nested record
// ============================================================================

/// <summary>
/// Inner record for nested record tests.
/// </summary>
[Factory]
[Create]
public partial record InnerRecord(string InnerValue);

/// <summary>
/// Outer record containing an inner record.
/// Tests that records containing records work correctly.
/// </summary>
[Factory]
[Create]
public partial record OuterRecord(string Name, InnerRecord Inner);

// ============================================================================
// Record with collection property
// ============================================================================

/// <summary>
/// Record with a collection in primary constructor.
/// Tests serialization of collection properties.
/// </summary>
[Factory]
[Create]
public partial record RecordWithCollection(string Name, List<string> Items);

// ============================================================================
// Record with nullable property
// ============================================================================

/// <summary>
/// Record with nullable property in primary constructor.
/// </summary>
[Factory]
[Create]
public partial record RecordWithNullable(string Name, string? Description);

// ============================================================================
// Record with nullable collection (CS8639 regression test)
// ============================================================================

/// <summary>
/// Record with nullable collection property.
/// Tests that typeof() in PropertyTypes array handles nullable reference types.
/// Regression test for CS8639: typeof cannot be used on nullable reference types.
/// </summary>
[Factory]
[Create]
public partial record RecordWithNullableCollection(string Name, List<string>? Items);

// ============================================================================
// Record with complex nullable generics (CS8639 regression test)
// ============================================================================

/// <summary>
/// Record with complex nullable generic types.
/// Tests edge cases with nested generics and mixed nullability.
/// Regression test for CS8639: typeof cannot be used on nullable reference types.
/// </summary>
[Factory]
[Create]
public partial record RecordWithComplexNullableGenerics(
    string Name,
    Dictionary<string, int>? Metadata,
    List<string?>? NullableItems);

// ============================================================================
// Record for value equality tests
// ============================================================================

/// <summary>
/// Record for testing value-based equality after serialization.
/// </summary>
[Factory]
[Create]
public partial record EqualityTestRecord(string Name, int Value, DateTime CreatedAt);

// ============================================================================
// Complex record with multiple data types
// ============================================================================

/// <summary>
/// Complex record with multiple property types for comprehensive testing.
/// </summary>
[Factory]
[Create]
public partial record ComplexRecord(
    string StringProp,
    int IntProp,
    long LongProp,
    double DoubleProp,
    decimal DecimalProp,
    bool BoolProp,
    DateTime DateTimeProp,
    Guid GuidProp);

// ============================================================================
// Known Limitations - Documented for future work
// ============================================================================

// LIMITATION 1: Record Inheritance
// The generator's ordinal serialization helpers (PropertyNames, PropertyTypes,
// ToOrdinalArray, FromOrdinalArray, CreateOrdinalConverter) are generated as
// instance members that hide the base class members without using the 'new'
// keyword. This causes CS0108 warnings/errors.
//
// Example that doesn't work:
//   [Factory]
//   [Create]
//   public partial record BaseRecord(string Name, int Value);
//
//   [Factory]
//   [Create]
//   public partial record DerivedRecord(string Name, int Value, string Extra)
//       : BaseRecord(Name, Value);
//
// Workaround: Don't use [Factory] on both base and derived records.
// Track this in a future generator enhancement.

// LIMITATION 2: C# 11 Required Members
// The generator doesn't handle C# 11 'required' members. When generating
// the factory's Create method, it calls the constructor but doesn't set
// required properties in an object initializer, causing CS9035.
//
// Example that doesn't work:
//   [Factory]
//   public partial record RecordWithRequired
//   {
//       public required string Name { get; init; }
//       public required int Value { get; init; }
//
//       [Create]
//       public RecordWithRequired() { }
//   }
//
// Workaround: Use positional records or don't use 'required' members.
// Track this in a future generator enhancement.
