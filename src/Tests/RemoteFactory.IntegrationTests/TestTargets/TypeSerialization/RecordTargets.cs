using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

/// <summary>
/// Test record definitions for validating record serialization round-trips.
/// These records cover various scenarios for client-server serialization testing.
/// </summary>

// ============================================================================
// Simple positional record with [Create] on type
// ============================================================================

/// <summary>
/// Simple positional record using [Create] on the type declaration.
/// </summary>
[Factory]
[Create]
public partial record SimpleRecord(string Name, int Value);

// ============================================================================
// Record with service injection in primary constructor
// ============================================================================

/// <summary>
/// Record with service dependency injected through primary constructor.
/// </summary>
[Factory]
[Create]
public partial record RecordWithService(string Name, [Service] IService Service);

// ============================================================================
// Remote record operations
// ============================================================================

/// <summary>
/// Record with remote fetch operations for testing serialization round-trips.
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
/// </summary>
[Factory]
[Create]
public partial record OuterRecord(string Name, InnerRecord Inner);

// ============================================================================
// Record with collection property
// ============================================================================

/// <summary>
/// Record with a collection in primary constructor.
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
/// Regression test for CS8639: typeof cannot be used on nullable reference types.
/// </summary>
[Factory]
[Create]
public partial record RecordWithComplexNullableGenerics(
    string Name,
    Dictionary<string, int>? Metadata,
    List<string?>? NullableItems);

// ============================================================================
// Record with nullable value types (CS1525 regression test)
// ============================================================================

/// <summary>
/// Record with nullable value types in primary constructor.
/// Regression test for CS1525: Invalid expression term.
/// </summary>
[Factory]
[Create]
public partial record RecordWithNullableValueTypes(
    string Name,
    int? NullableInt,
    DateTime? NullableDateTime,
    Guid? NullableGuid,
    decimal? NullableDecimal);

// ============================================================================
// Record with default parameter values
// ============================================================================

/// <summary>
/// Record with default parameter values in primary constructor.
/// </summary>
[Factory]
[Create]
public partial record RecordWithDefaults(string Name = "default", int Value = 42);

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
