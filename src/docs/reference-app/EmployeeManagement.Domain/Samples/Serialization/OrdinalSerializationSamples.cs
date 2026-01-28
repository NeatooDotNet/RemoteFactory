using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Serialization;

#region serialization-ordinal-generated
/// <summary>
/// Simple [Factory] entity demonstrating alphabetical property ordering for ordinal serialization.
/// The generator produces IOrdinalSerializable, IOrdinalConverterProvider, and IOrdinalSerializationMetadata
/// implementations automatically.
/// </summary>
[Factory]
public partial class EmployeeRecord
{
    // Properties are serialized in alphabetical order:
    // Index 0: Department
    // Index 1: Email
    // Index 2: HireDate
    // Index 3: Name

    public string Department { get; set; } = "";  // Ordinal index 0
    public string Email { get; set; } = "";       // Ordinal index 1
    public DateTime HireDate { get; set; }        // Ordinal index 2
    public string Name { get; set; } = "";        // Ordinal index 3

    [Create]
    public EmployeeRecord() { }
}
// Ordinal JSON: ["Engineering", "john@example.com", "2024-01-15T00:00:00Z", "John Doe"]
// Named JSON:   {"Department":"Engineering","Email":"john@example.com",
//                "HireDate":"2024-01-15T00:00:00Z","Name":"John Doe"}
#endregion

#region serialization-ordinal-versioning
// Versioning rules for ordinal serialization:
// 1. ADD new properties - they will be appended based on alphabetical position
// 2. NEVER remove existing properties - breaks deserialization
// 3. NEVER rename properties - changes ordinal indices
// 4. NEVER change property types - causes type mismatch errors

/// <summary>
/// Demonstrates safe versioning with ordinal serialization.
/// New properties are added alphabetically; existing indices remain stable.
/// </summary>
[Factory]
public partial class VersionedEmployee
{
    // Version 1 properties (stable indices)
    public string Department { get; set; } = "";  // Index 0 - original property
    public string Name { get; set; } = "";        // Index 1 - original property

    // Version 2 property (inserted alphabetically between Department and Name)
    public string? Email { get; set; }            // Index 1 - shifts Name to index 2

    // Version 3 property (appended after Name alphabetically)
    public string? Title { get; set; }            // Index 3 - comes after Name

    [Create]
    public VersionedEmployee() { }
}
// After all versions:
// Index 0: Department
// Index 1: Email (added in v2)
// Index 2: Name (shifted from index 1)
// Index 3: Title (added in v3)
#endregion
