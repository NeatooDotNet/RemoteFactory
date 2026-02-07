using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Validation with Data Annotations
// ============================================================================

#region save-validation
// Use DataAnnotations for validation; call Validator.TryValidateObject before Save
[Factory]
public partial class SaveValidatedEmployee : IFactorySaveMeta
{
    [Required] public string FirstName { get; set; } = "";
    [Required] public string LastName { get; set; } = "";
    [EmailAddress] public string? Email { get; set; }
    [Range(0, double.MaxValue)] public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }
    /* ... */
}
#endregion

// Full implementation
public partial class SaveValidatedEmployee
{
    public Guid Id { get; private set; }

    [Create]
    public SaveValidatedEmployee() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }
}

public static class ValidationHelper
{
    public static async Task<(T? Result, ICollection<ValidationResult>? Errors)> SaveWithValidation<T>(
        IFactorySave<T> factory, T entity)
        where T : class, IFactorySaveMeta
    {
        var results = new List<ValidationResult>();
        if (!Validator.TryValidateObject(entity, new ValidationContext(entity), results, true))
            return (null, results);
        var result = await factory.Save(entity);
        return (result as T, null);
    }
}

// ============================================================================
// Server-Side Validation with Exceptions
// ============================================================================

// Full implementation for SaveServerValidatedEmployee
[Factory]
public partial class SaveServerValidatedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SaveServerValidatedEmployee() { Id = Guid.NewGuid(); }

    #region save-validation-throw
    // Throw ValidationException in Insert/Update for server-side validation
    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(FirstName)) throw new ValidationException("First name required");
        if (string.IsNullOrWhiteSpace(LastName)) throw new ValidationException("Last name required");
        IsNew = false;
        return Task.CompletedTask;
    }
    #endregion
}
