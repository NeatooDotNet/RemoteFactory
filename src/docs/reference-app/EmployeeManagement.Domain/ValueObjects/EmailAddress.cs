using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.ValueObjects;

/// <summary>
/// Email address value object with validation.
/// </summary>
[Factory]
public partial class EmailAddress
{
    public string Value { get; }

    [Create]
    public EmailAddress(string value)
    {
        if (!IsValid(value))
            throw new ArgumentException("Invalid email format", nameof(value));
        Value = value;
    }

    private static bool IsValid(string value) =>
        !string.IsNullOrEmpty(value) && value.Contains('@', StringComparison.Ordinal) && value.Contains('.', StringComparison.Ordinal);

    public override string ToString() => Value;

    public override bool Equals(object? obj) =>
        obj is EmailAddress other && Value == other.Value;

    public override int GetHashCode() => Value.GetHashCode(StringComparison.Ordinal);
}
