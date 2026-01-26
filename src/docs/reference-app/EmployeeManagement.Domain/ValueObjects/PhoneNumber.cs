using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.ValueObjects;

/// <summary>
/// Phone number value object.
/// </summary>
[Factory]
public partial class PhoneNumber
{
    public string CountryCode { get; }
    public string Number { get; }

    [Create]
    public PhoneNumber(string countryCode, string number)
    {
        CountryCode = countryCode ?? throw new ArgumentNullException(nameof(countryCode));
        Number = number ?? throw new ArgumentNullException(nameof(number));
    }

    public override string ToString() => $"{CountryCode} {Number}";

    public override bool Equals(object? obj) =>
        obj is PhoneNumber other && CountryCode == other.CountryCode && Number == other.Number;

    public override int GetHashCode() => HashCode.Combine(CountryCode, Number);
}
