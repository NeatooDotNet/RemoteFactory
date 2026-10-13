// =============================================================================
// DESIGN SOURCE OF TRUTH: Value Objects
// =============================================================================
//
// A value object is a plain record (VisionAlignment R-F). It crosses the wire as
// a parameter or a property with no factory, and anyone may construct it with
// `new`. Its constructor throws on an invalid value: a value object has no rules
// to report, only values it refuses to be.
//
// A value object takes [Factory] only when constructing it needs a service (a
// database, an API call) or authorization. TaxRate (TaxRate.cs) is that case:
// its rate is read from a server-only table.
//
// DID NOT DO THIS: [Factory] on every value object, built through its factory
//
// WRONG (a factory with nothing to do):
// [Factory]
// public partial record Money(decimal Amount, string Currency)
// {
//     [Create] public static Money Create(decimal amount, string currency) => new(amount, currency);
// }
// public void Fetch(OrderLineRow row, [Service] IMoneyFactory moneyFactory) {
//     UnitPrice = moneyFactory.Create(row.UnitPrice, "USD");
// }
//
// RIGHT:
// public void Fetch(OrderLineRow row) {
//     UnitPrice = new Money(row.UnitPrice);
// }
//
// The factory added an interface, a DI registration and an injected parameter at
// every construction site, and bought nothing a constructor does not do.
//
// =============================================================================

namespace Design.Domain.ValueObjects;

/// <summary>
/// Value object for a monetary amount in one currency.
/// </summary>
/// <remarks>
/// The constructor refuses a currency that is not a three-letter uppercase code.
/// The amount has no invariant here (a refund line may be negative).
/// </remarks>
public record Money(decimal Amount, string Currency = "USD")
{
    /// <summary>
    /// Zero amount in USD.
    /// </summary>
    public static readonly Money Zero = new(0);

    /// <summary>
    /// The currency code, validated when the record is constructed.
    /// </summary>
    /// <remarks>
    /// Get-only on purpose: <c>with { Currency = ... }</c> would bypass the check,
    /// so it does not compile. <c>with { Amount = ... }</c> keeps the validated code.
    /// </remarks>
    public string Currency { get; } = IsCurrencyCode(Currency)
        ? Currency
        : throw new ArgumentException($"'{Currency}' is not a three-letter uppercase currency code.", nameof(Currency));

    private static bool IsCurrencyCode(string? code) =>
        code is { Length: 3 } && code.All(c => c is >= 'A' and <= 'Z');

    // -------------------------------------------------------------------------
    // DESIGN DECISION: Value objects can have behavior
    //
    // Value objects aren't just data containers - they can (and should)
    // encapsulate domain logic related to their values.
    // -------------------------------------------------------------------------

    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot add {other.Currency} to {Currency}");
        return this with { Amount = Amount + other.Amount };
    }

    public Money Subtract(Money other)
    {
        if (Currency != other.Currency)
            throw new InvalidOperationException($"Cannot subtract {other.Currency} from {Currency}");
        return this with { Amount = Amount - other.Amount };
    }

    public Money Multiply(decimal factor)
    {
        return this with { Amount = Amount * factor };
    }

    // -------------------------------------------------------------------------
    // COMMON MISTAKE: Making value objects mutable
    //
    // WRONG:
    // public class Money {
    //     public decimal Amount { get; set; }  // <-- Mutable!
    //     public void SetAmount(decimal a) { Amount = a; }
    // }
    //
    // RIGHT (use records or readonly properties):
    // public record Money(decimal Amount, string Currency);
    //
    // Mutating a value object violates the DDD pattern and can cause
    // subtle bugs when the same value is shared across entities.
    // -------------------------------------------------------------------------
}

/// <summary>
/// Value object for a percentage from 0 to 100.
/// </summary>
public record Percentage(decimal Value)
{
    public static readonly Percentage Zero = new(0);
    public static readonly Percentage Full = new(100);

    /// <summary>
    /// The percentage, validated when the record is constructed.
    /// </summary>
    public decimal Value { get; } = Value is >= 0m and <= 100m
        ? Value
        : throw new ArgumentOutOfRangeException(nameof(Value), Value, "A percentage is between 0 and 100.");

    public Money Of(Money money)
    {
        return money.Multiply(Value / 100m);
    }
}
