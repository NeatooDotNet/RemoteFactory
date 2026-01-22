using System.ComponentModel.DataAnnotations;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

/// <summary>
/// Test domain objects for validation serialization round-trip tests.
/// </summary>

/// <summary>
/// Simple serializable validation error.
/// Wraps ValidationResult properties for JSON serialization.
/// </summary>
public class ValidationError
{
    public string? ErrorMessage { get; set; }
    public List<string>? MemberNames { get; set; }

    public ValidationError() { }

    public ValidationError(ValidationResult result)
    {
        ErrorMessage = result.ErrorMessage;
        MemberNames = result.MemberNames?.ToList();
    }
}

/// <summary>
/// Entity with DataAnnotations validation attributes.
/// ValidationErrors property holds serializable validation results.
/// </summary>
[Factory]
public class ValidatedEntity : IFactorySaveMeta
{
    public Guid Id { get; set; }

    [Required(ErrorMessage = "Name is required")]
    public string? Name { get; set; }

    [Range(1, 100, ErrorMessage = "Value must be between 1 and 100")]
    public int Value { get; set; }

    /// <summary>
    /// Validation errors populated by server or client.
    /// Serializes to/from JSON for round-trip validation state transfer.
    /// </summary>
    public List<ValidationError>? ValidationErrors { get; set; }

    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; } = true;

    [Remote, Create]
    public void Create()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetch with optional validation error triggering.
    /// When triggerValidationError is true, validates the entity and populates ValidationErrors.
    /// </summary>
    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, bool triggerValidationError)
    {
        Id = id;
        // Name and Value left as default (null and 0) to trigger validation errors

        if (triggerValidationError)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, new ValidationContext(this), results, validateAllProperties: true);
            ValidationErrors = results.Select(r => new ValidationError(r)).ToList();
        }

        IsNew = false;
        return Task.FromResult(true);
    }

    /// <summary>
    /// Fetch with pre-set valid values and optional validation.
    /// </summary>
    [Remote, Fetch]
    public Task<bool> FetchValid(Guid id, string name, int value, bool runValidation)
    {
        Id = id;
        Name = name;
        Value = value;

        if (runValidation)
        {
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(this, new ValidationContext(this), results, validateAllProperties: true);
            ValidationErrors = results.Count > 0 ? results.Select(r => new ValidationError(r)).ToList() : null;
        }

        IsNew = false;
        return Task.FromResult(true);
    }

    [Remote, Insert]
    public Task Insert()
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update()
    {
        return Task.CompletedTask;
    }
}
