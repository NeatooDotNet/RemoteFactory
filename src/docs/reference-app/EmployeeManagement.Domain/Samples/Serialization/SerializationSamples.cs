using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Serialization;

// Ordinal versioning: Properties serialized in alphabetical order.
// Adding a property shifts positions - rebuild client and server together.
[Factory]
public partial class EmployeeWithVersioning
{
    #region serialization-ordinal-versioning
    // Properties serialized alphabetically: Active[0], Age[1], Email[2], FirstName[3]...
    // Adding "Department" inserts at [2], shifting everything after.
    public bool Active { get; set; } = true;
    public int Age { get; set; }
    public string Email { get; set; } = "";
    public string FirstName { get; set; } = "";
    #endregion

    public DateTime HireDate { get; set; }
    public string LastName { get; set; } = "";

    [Create]
    public EmployeeWithVersioning() { }
}

// Custom ordinal serialization for non-[Factory] types.
public class MoneyOrdinal : IOrdinalSerializable
{
    public decimal Amount { get; }
    public string Currency { get; }

    public MoneyOrdinal(decimal amount, string currency)
    {
        Amount = amount;
        Currency = currency;
    }

    #region serialization-custom-ordinal
    // Implement IOrdinalSerializable: return properties in alphabetical order.
    public object?[] ToOrdinalArray() => [Amount, Currency];
    #endregion
}

// Circular references: Parent-child bidirectional refs preserved via $ref pointers.
[Factory]
public partial class TeamWithMembers
{
    public Guid Id { get; private set; }
    public string TeamName { get; set; } = "";
    public List<TeamMember> Members { get; set; } = [];

    [Create]
    public TeamWithMembers() { Id = Guid.NewGuid(); }

    #region serialization-references
    // Bidirectional reference - serializes as {"$ref": "1"} to preserve identity.
    public void AddMember(string name) => Members.Add(new TeamMember(name, this));
    #endregion
}

[Factory]
public partial class TeamMember
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public TeamWithMembers Team { get; set; } = null!;  // Back-reference to parent

    [Create]
    public TeamMember(string name, TeamWithMembers team)
    {
        Id = Guid.NewGuid();
        Name = name;
        Team = team;
    }
}

// Interface serialization: Concrete type serialized with $type discriminator.
public interface IContactInfo { string Type { get; } string Value { get; } }

[Factory]
public partial class EmployeeWithContact
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    #region serialization-interface
    // Interface property serializes with $type: "EmailContact" or "PhoneContact"
    public IContactInfo? PrimaryContact { get; set; }
    #endregion

    [Create]
    public EmployeeWithContact() { Id = Guid.NewGuid(); }
}

[Factory]
public partial class EmailContact : IContactInfo
{
    public string Type => "Email";
    public string Value { get; }
    [Create] public EmailContact(string email) { Value = email; }
}

[Factory]
public partial class PhoneContact : IContactInfo
{
    public string Type => "Phone";
    public string Value { get; }
    public string Extension { get; }
    [Create] public PhoneContact(string phone, string extension = "") { Value = phone; Extension = extension; }
}

// Collection serialization: Lists, Dictionaries, arrays, and nested collections.
[Factory]
public partial class OrganizationData
{
    public Guid Id { get; private set; }

    #region serialization-collections
    public List<string> EmployeeNames { get; set; } = [];              // List<T>
    public Dictionary<Guid, string> DepartmentNames { get; set; } = []; // Dictionary
    public List<List<string>> TeamHierarchy { get; set; } = [];        // Nested
    public string[] ActiveProjects { get; set; } = [];                 // Array
    #endregion

    [Create]
    public OrganizationData() { Id = Guid.NewGuid(); }
}

// Polymorphism: Base type collections serialize with $type discriminator.
[Factory]
public abstract partial class EmployeeTypeBase
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";
    public abstract string EmploymentType { get; }
}

[Factory]
public partial class FullTimeEmployee : EmployeeTypeBase
{
    public override string EmploymentType => "FullTime";
    public decimal AnnualSalary { get; set; }
    [Create] public FullTimeEmployee() { Id = Guid.NewGuid(); }
}

[Factory]
public partial class ContractEmployee : EmployeeTypeBase
{
    public override string EmploymentType => "Contract";
    public decimal HourlyRate { get; set; }
    [Create] public ContractEmployee() { Id = Guid.NewGuid(); }
}

[Factory]
public partial class Workforce
{
    public Guid Id { get; private set; }

    #region serialization-polymorphism
    // Mixed types: each serialized with $type discriminator for correct deserialization.
    public List<EmployeeTypeBase> Employees { get; set; } = [];
    #endregion

    [Create]
    public Workforce() { Id = Guid.NewGuid(); }
}

// Validation: Attributes preserved but not enforced during serialization.
[Factory]
public partial class ValidatedEmployee
{
    public Guid Id { get; private set; }

    #region serialization-validation
    // Validation attributes are serialized with the type, validated server-side.
    [Required] [StringLength(100)] public string FirstName { get; set; } = "";
    [Required] [EmailAddress] public string Email { get; set; } = "";
    [Range(0, 10000000)] public decimal Salary { get; set; }
    #endregion

    [Required] [StringLength(100)] public string LastName { get; set; } = "";

    [Create]
    public ValidatedEmployee() { Id = Guid.NewGuid(); }
}

// Server-side validation after deserialization.
[Factory]
public partial class ServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [Required] [EmailAddress] public string Email { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerValidatedEmployee() { Id = Guid.NewGuid(); }

    #region serialization-validation-server
    // Validate after deserialization using DataAnnotations.
    [Remote, Insert]
    public Task Insert(CancellationToken ct)
    {
        var context = new ValidationContext(this);
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(this, context, results, validateAllProperties: true))
            throw new ValidationException($"Validation failed: {string.Join("; ", results.Select(r => r.ErrorMessage))}");
        IsNew = false;
        return Task.CompletedTask;
    }
    #endregion
}

// Custom JsonConverter for non-[Factory] types (e.g., third-party types).
#region serialization-custom-converter
[JsonConverter(typeof(MoneyJsonConverter))]
public class MoneyValue(decimal amount, string currency)
{
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;
}

public class MoneyJsonConverter : JsonConverter<MoneyValue>
{
    public override MoneyValue Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        using var doc = JsonDocument.ParseValue(ref reader);
        return new MoneyValue(doc.RootElement.GetProperty("amount").GetDecimal(),
                              doc.RootElement.GetProperty("currency").GetString() ?? "USD");
    }

    public override void Write(Utf8JsonWriter writer, MoneyValue value, JsonSerializerOptions options)
    {
        writer.WriteStartObject();
        writer.WriteNumber("amount", value.Amount);
        writer.WriteString("currency", value.Currency);
        writer.WriteEndObject();
    }
}
#endregion

// Configuration snippets - single source of truth (removed duplicates from other files)
