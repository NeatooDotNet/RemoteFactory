using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Validation with Data Annotations
// ============================================================================

#region save-validation
/// <summary>
/// Employee aggregate with validation attributes for Save operations.
/// </summary>
[Factory]
public partial class ValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "First name is required")]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    public string LastName { get; set; } = "";

    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string? Email { get; set; }

    [Range(0, double.MaxValue, ErrorMessage = "Salary must be non-negative")]
    public decimal Salary { get; set; }

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Insert persists the employee after validation passes.
    /// </summary>
    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Validation happens before this method via SaveWithValidation helper
        IsNew = false;
        return Task.CompletedTask;
    }
}

/// <summary>
/// Helper class for validating entities before save.
/// </summary>
public static class ValidationHelper
{
    /// <summary>
    /// Validates an entity and saves if valid.
    /// Returns null with validation errors if invalid.
    /// </summary>
    public static async Task<(T? Result, ICollection<ValidationResult>? Errors)> SaveWithValidation<T>(
        IFactorySave<T> factory,
        T entity)
        where T : class, IFactorySaveMeta
    {
        var validationResults = new List<ValidationResult>();
        var validationContext = new ValidationContext(entity);

        if (!Validator.TryValidateObject(entity, validationContext, validationResults, validateAllProperties: true))
        {
            return (null, validationResults);
        }

        var result = await factory.Save(entity);
        return (result as T, null);
    }
}
#endregion

// ============================================================================
// Server-Side Validation with Exceptions
// ============================================================================

#region save-validation-throw
/// <summary>
/// Employee aggregate with server-side validation in Insert method.
/// </summary>
[Factory]
public partial class ServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public ServerValidatedEmployee()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Insert with server-side validation.
    /// Throws ValidationException if validation fails.
    /// </summary>
    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        // Validate FirstName
        if (string.IsNullOrWhiteSpace(FirstName))
        {
            throw new ValidationException("First name is required");
        }

        // Validate LastName
        if (string.IsNullOrWhiteSpace(LastName))
        {
            throw new ValidationException("Last name is required");
        }

        // Validate Email format if provided
        if (!string.IsNullOrEmpty(Email) && !Email.Contains('@'))
        {
            throw new ValidationException("Invalid email format");
        }

        // All validations passed - persist entity
        IsNew = false;
        return Task.CompletedTask;
    }
}

// Usage pattern:
// try
// {
//     await factory.Save(employee);
// }
// catch (ValidationException ex)
// {
//     // Handle validation failure
//     Console.WriteLine($"Validation failed: {ex.Message}");
// }
#endregion
