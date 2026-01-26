using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Serialization;

#region serialization-ordinal-versioning
/// <summary>
/// Demonstrates ordinal serialization versioning considerations.
/// Properties are serialized in alphabetical order.
/// </summary>
[Factory]
public partial class EmployeeWithVersioning
{
    // Properties serialized in alphabetical order: Active, Age, Email, FirstName, HireDate, LastName
    // Adding a new property (e.g., "Department") inserts at position 0 (alphabetically before "Email")
    // This shifts existing positions - requires rebuilding both client and server

    public bool Active { get; set; } = true;      // [0]
    public int Age { get; set; }                  // [1]
    // Adding Department here would be [2], shifting Email, FirstName, HireDate, LastName
    public string Email { get; set; } = "";       // [2]
    public string FirstName { get; set; } = "";   // [3]
    public DateTime HireDate { get; set; }        // [4]
    public string LastName { get; set; } = "";    // [5]

    // Best practice: When adding properties, rebuild both client and server
    // to ensure ordinal positions match.

    [Create]
    public EmployeeWithVersioning() { }
}
#endregion

#region serialization-custom-ordinal
/// <summary>
/// Money value object implementing IOrdinalSerializable.
/// Use when you need custom ordinal serialization for non-factory types.
/// </summary>
public class MoneyOrdinal : IOrdinalSerializable
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyOrdinal(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Returns properties in alphabetical order for ordinal serialization.
    /// Order: Amount, Currency
    /// </summary>
    public object?[] ToOrdinalArray()
    {
        // Alphabetical order: Amount, Currency
        return [Amount, Currency];
    }
}
#endregion

#region serialization-references
/// <summary>
/// Demonstrates circular reference handling.
/// Parent-child bidirectional references are preserved.
/// </summary>
[Factory]
public partial class TeamWithMembers
{
    public Guid Id { get; private set; }
    public string TeamName { get; set; } = "";
    public List<TeamMember> Members { get; set; } = [];

    [Create]
    public TeamWithMembers()
    {
        Id = Guid.NewGuid();
    }

    public void AddMember(string name)
    {
        var member = new TeamMember(name, this);
        Members.Add(member);
    }
}

[Factory]
public partial class TeamMember
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// Bidirectional reference to parent Team.
    /// RemoteFactory preserves object identity via $ref pointers.
    /// </summary>
    public TeamWithMembers Team { get; set; } = null!;

    [Create]
    public TeamMember(string name, TeamWithMembers team)
    {
        Id = Guid.NewGuid();
        Name = name;
        Team = team;
    }
}
#endregion

#region serialization-interface
/// <summary>
/// Interface properties serialize as their concrete type with $type discriminator.
/// </summary>
[Factory]
public partial class EmployeeWithContact
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    /// <summary>
    /// Interface property holds concrete EmailContact or PhoneContact.
    /// Serialized with $type discriminator for correct deserialization.
    /// </summary>
    public IContactInfo? PrimaryContact { get; set; }

    [Create]
    public EmployeeWithContact()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Contact information interface.
/// </summary>
public interface IContactInfo
{
    string Type { get; }
    string Value { get; }
}

/// <summary>
/// Email contact implementation.
/// </summary>
[Factory]
public partial class EmailContact : IContactInfo
{
    public string Type => "Email";
    public string Value { get; }

    [Create]
    public EmailContact(string email)
    {
        Value = email;
    }
}

/// <summary>
/// Phone contact implementation.
/// </summary>
[Factory]
public partial class PhoneContact : IContactInfo
{
    public string Type => "Phone";
    public string Value { get; }
    public string Extension { get; }

    [Create]
    public PhoneContact(string phone, string extension = "")
    {
        Value = phone;
        Extension = extension;
    }
}
#endregion

#region serialization-collections
/// <summary>
/// Demonstrates collection serialization patterns.
/// </summary>
[Factory]
public partial class OrganizationData
{
    public Guid Id { get; private set; }

    /// <summary>
    /// List collections serialized element-by-element.
    /// </summary>
    public List<string> EmployeeNames { get; set; } = [];

    /// <summary>
    /// Dictionary with Guid keys and string values.
    /// </summary>
    public Dictionary<Guid, string> DepartmentNames { get; set; } = [];

    /// <summary>
    /// Nested collections supported.
    /// </summary>
    public List<List<string>> TeamHierarchy { get; set; } = [];

    /// <summary>
    /// Array collections.
    /// </summary>
    public string[] ActiveProjects { get; set; } = [];

    [Create]
    public OrganizationData()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

#region serialization-polymorphism
/// <summary>
/// Base employee type for polymorphic serialization.
/// </summary>
[Factory]
public abstract partial class EmployeeTypeBase
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";
    public abstract string EmploymentType { get; }
}

/// <summary>
/// Full-time employee type.
/// </summary>
[Factory]
public partial class FullTimeEmployee : EmployeeTypeBase
{
    public override string EmploymentType => "FullTime";
    public decimal AnnualSalary { get; set; }
    public int VacationDays { get; set; }

    [Create]
    public FullTimeEmployee()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Contract employee type.
/// </summary>
[Factory]
public partial class ContractEmployee : EmployeeTypeBase
{
    public override string EmploymentType => "Contract";
    public decimal HourlyRate { get; set; }
    public DateTime ContractEndDate { get; set; }

    [Create]
    public ContractEmployee()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Container for polymorphic employee collection.
/// $type discriminator identifies concrete types during deserialization.
/// </summary>
[Factory]
public partial class Workforce
{
    public Guid Id { get; private set; }

    /// <summary>
    /// Collection holds mixed FullTimeEmployee and ContractEmployee instances.
    /// Each serialized with $type discriminator.
    /// </summary>
    public List<EmployeeTypeBase> Employees { get; set; } = [];

    [Create]
    public Workforce()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

#region serialization-validation
/// <summary>
/// Validation attributes on serializable types.
/// Attributes are preserved but not enforced during serialization.
/// </summary>
[Factory]
public partial class ValidatedEmployee
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Range(0, 10000000, ErrorMessage = "Salary must be between 0 and 10,000,000")]
    public decimal Salary { get; set; }

    [Create]
    public ValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

#region serialization-validation-server
/// <summary>
/// Server-side validation after deserialization using Validator.
/// </summary>
[Factory]
public partial class ServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }

    [Required]
    [StringLength(100)]
    public string FirstName { get; set; } = "";

    [Required]
    [StringLength(100)]
    public string LastName { get; set; } = "";

    [Required]
    [EmailAddress]
    public string Email { get; set; } = "";

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerValidatedEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Validate after deserialization using DataAnnotations.
    /// </summary>
    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        // Validate using DataAnnotations
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();

        if (!Validator.TryValidateObject(this, context, results, validateAllProperties: true))
        {
            var errors = string.Join("; ", results.Select(r => r.ErrorMessage));
            throw new ValidationException($"Validation failed: {errors}");
        }

        IsNew = false;
        return Task.CompletedTask;
    }
}
#endregion

#region serialization-custom-converter
/// <summary>
/// Custom JsonConverter for types that cannot use [Factory].
/// Use for third-party types or special serialization logic.
/// </summary>
public class MoneyJsonConverter : JsonConverter<MoneyValue>
{
    public override MoneyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        // Expect object format: { "amount": 100.00, "currency": "USD" }
        using var doc = JsonDocument.ParseValue(ref reader);
        var root = doc.RootElement;

        var amount = root.GetProperty("amount").GetDecimal();
        var currency = root.GetProperty("currency").GetString() ?? "USD";

        return new MoneyValue(amount, currency);
    }

    public override void Write(Utf8JsonWriter writer, MoneyValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}

/// <summary>
/// Value object with custom JSON converter.
/// Not a [Factory] type - uses custom converter instead.
/// </summary>
[JsonConverter(typeof(MoneyJsonConverter))]
public class MoneyValue
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyValue(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }
}
#endregion

#region serialization-config
// Serialization format configuration during DI registration.
//
// Ordinal format (default) - compact array-based serialization:
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Logical,
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// Named format - human-readable JSON with property names:
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Logical,
//     new NeatooSerializationOptions { Format = SerializationFormat.Named },
//     domainAssembly);
#endregion

#region serialization-debug-named
// Switching to Named format for debugging serialization issues.
//
// if (builder.Environment.IsDevelopment())
// {
//     // Named format for human-readable JSON in dev tools
//     builder.Services.AddNeatooAspNetCore(
//         new NeatooSerializationOptions { Format = SerializationFormat.Named },
//         domainAssembly);
// }
// else
// {
//     // Ordinal format for compact production payloads
//     builder.Services.AddNeatooAspNetCore(
//         new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//         domainAssembly);
// }
#endregion

#region serialization-json-options
// NeatooSerializationOptions configuration.
// RemoteFactory manages JsonSerializerOptions internally.
//
// var options = new NeatooSerializationOptions
// {
//     // Format: Choose Ordinal (default, compact) or Named (readable)
//     Format = SerializationFormat.Ordinal
// };
//
// Note: RemoteFactory manages JsonSerializerOptions internally
// For custom type serialization, implement:
// - IOrdinalSerializable for [Factory] types
// - IOrdinalConverterProvider<T> for non-factory types
// - JsonConverter<T> for Named format only
#endregion
