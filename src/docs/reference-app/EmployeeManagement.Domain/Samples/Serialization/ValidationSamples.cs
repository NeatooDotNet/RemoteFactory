using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Serialization;

#region serialization-validation
/// <summary>
/// Demonstrates validation attributes that persist across serialization.
/// Attributes are not serialized but remain on the type for validation.
/// </summary>
[Factory]
public partial class ValidatedEmployee
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "Employee name is required")]
    [StringLength(100, MinimumLength = 2, ErrorMessage = "Name must be between 2 and 100 characters")]
    public string Name { get; set; } = "";

    [EmailAddress(ErrorMessage = "Invalid email address format")]
    public string? Email { get; set; }

    [Range(30000, 500000, ErrorMessage = "Salary must be between $30,000 and $500,000")]
    public decimal Salary { get; set; }

    [Create]
    public ValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// Helper class demonstrating client-side validation using DataAnnotations.
/// </summary>
public static class EmployeeValidator
{
    public static bool TryValidate(ValidatedEmployee employee, out List<ValidationResult> results)
    {
        results = [];
        var context = new ValidationContext(employee);
        return Validator.TryValidateObject(employee, context, results, validateAllProperties: true);
    }
}
#endregion

#region serialization-validation-server
/// <summary>
/// Demonstrates server-side validation in [Remote, Insert] method.
/// Implements IFactorySaveMeta for tracking new/deleted state.
/// </summary>
[Factory]
public partial class ServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Server-side validation before persistence
        if (string.IsNullOrWhiteSpace(Name))
        {
            throw new ValidationException("Employee name cannot be empty");
        }

        if (string.IsNullOrWhiteSpace(Email) || !Email.Contains('@'))
        {
            throw new ValidationException("Invalid email address format");
        }

        // Map to entity and persist
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = Name,
            LastName = "",
            Email = Email
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);

        // Mark as no longer new after successful insert
        IsNew = false;
    }
}
#endregion
