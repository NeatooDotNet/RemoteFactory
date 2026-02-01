// =============================================================================
// DESIGN SOURCE OF TRUTH: Value Objects
// =============================================================================
//
// Value objects use [Factory] for self-hydration. The parent passes raw data
// (e.g., from an EF entity) and the value object is responsible for constructing
// itself from that data.
//
// DESIGN DECISION: Value objects use [Factory] for self-hydration
//
// Reasons:
// 1. Encapsulation - parent doesn't need to know value object's internal structure
// 2. Consistency - all domain objects use factory pattern
// 3. If structure changes, only the value object's factory method changes
//
// DID NOT DO THIS: Have parent construct value objects directly
//
// WRONG (parent knows too much about structure):
// public void Fetch(EmployeeEntity entity) {
//     Salary = new Money(entity.SalaryAmount, entity.SalaryCurrency);
// }
//
// RIGHT (value object hydrates itself):
// public void Fetch(EmployeeEntity entity, [Service] IMoneyFactory moneyFactory) {
//     Salary = moneyFactory.Create(entity.SalaryAmount, entity.SalaryCurrency);
// }
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary amounts with currency.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Records with [Factory] for value object encapsulation
///
/// For records, the "hydration" happens via the Create factory method, which
/// wraps the primary constructor. This provides encapsulation while keeping
/// the immutability benefits of records.
///
/// GENERATOR BEHAVIOR: The generator creates IMoneyFactory with a Create
/// method matching the factory method signature.
/// </remarks>
[Factory]
public partial record Money(decimal Amount, string Currency = "USD")
{
    /// <summary>
    /// Zero amount in USD.
    /// </summary>
    public static readonly Money Zero = new(0);

    /// <summary>
    /// Factory method for creating Money instances.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Value objects use [Create] for factory construction
    ///
    /// The parent injects IMoneyFactory and calls Create with raw data.
    /// The value object encapsulates how to construct itself from that data.
    ///
    /// This becomes important when:
    /// 1. Construction involves validation
    /// 2. Multiple data sources map to the same value object
    /// 3. The internal structure changes (only this method needs updating)
    /// </remarks>
    [Create]
    public static Money Create(decimal amount, string currency = "USD")
    {
        return new Money(amount, currency);
    }

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
/// Value object representing a percentage.
/// </summary>
[Factory]
public partial record Percentage(decimal Value)
{
    public static readonly Percentage Zero = new(0);
    public static readonly Percentage Full = new(100);

    /// <summary>
    /// Factory method for creating Percentage instances.
    /// </summary>
    [Create]
    public static Percentage Create(decimal value)
    {
        return new Percentage(value);
    }

    public Money Of(Money money)
    {
        return money.Multiply(Value / 100m);
    }
}
