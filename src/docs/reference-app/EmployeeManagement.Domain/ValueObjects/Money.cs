using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.ValueObjects;

/// <summary>
/// Money value object representing an amount with currency.
/// </summary>
[Factory]
public partial class Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    [Create]
    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Amount = amount;
        Currency = currency;
    }

    public override string ToString() => $"{Amount:N2} {Currency}";

    public override bool Equals(object? obj) =>
        obj is Money other && Amount == other.Amount && Currency == other.Currency;

    public override int GetHashCode() => HashCode.Combine(Amount, Currency);

    /// <summary>
    /// Adds two Money instances with the same currency.
    /// </summary>
    public static Money Add(Money a, Money b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Currency != b.Currency)
            throw new InvalidOperationException("Cannot add money with different currencies");
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator +(Money a, Money b) => Add(a, b);
}
