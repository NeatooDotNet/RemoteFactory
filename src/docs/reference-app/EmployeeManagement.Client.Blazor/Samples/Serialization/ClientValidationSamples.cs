using System.ComponentModel.DataAnnotations;

namespace EmployeeManagement.Client.Samples.Serialization;

/// <summary>
/// Client-side validation helper demonstrating DataAnnotations validation.
/// Validation attributes persist across serialization and can be validated on either side.
/// </summary>
public static class ClientValidationHelper
{
    /// <summary>
    /// Validates an object using DataAnnotations attributes.
    /// Returns true if valid, false otherwise with validation results.
    /// </summary>
    public static bool TryValidate<T>(T obj, out List<ValidationResult> results) where T : class
    {
        results = [];
        var context = new ValidationContext(obj);
        return Validator.TryValidateObject(obj, context, results, validateAllProperties: true);
    }

    /// <summary>
    /// Validates an object and throws ValidationException if invalid.
    /// </summary>
    public static void Validate<T>(T obj) where T : class
    {
        var context = new ValidationContext(obj);
        Validator.ValidateObject(obj, context, validateAllProperties: true);
    }
}
