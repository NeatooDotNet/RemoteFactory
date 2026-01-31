// =============================================================================
// DESIGN SOURCE OF TRUTH: Value Objects
// =============================================================================
//
// Value objects don't need [Factory] because they don't have factory operations.
//
// DESIGN DECISION: Value objects don't use [Factory]
//
// Reasons:
// 1. No Create/Fetch/Save lifecycle - they're constructed directly
// 2. No remote operations - they're serialized as part of entities
// 3. Immutability means no state changes to track
//
// DID NOT DO THIS: Add [Factory] to value objects
//
// This would generate unnecessary factory infrastructure for types that
// are simply constructed and serialized as data.
//
// =============================================================================

namespace Design.Domain.ValueObjects;

/// <summary>
/// Value object representing monetary amounts with currency.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Records are ideal for value objects
///
/// Reasons:
/// 1. Immutable by default
/// 2. Value equality built-in
/// 3. Concise syntax
/// 4. Serialization-friendly
///
/// GENERATOR BEHAVIOR: Records with no [Factory] attribute are serialized
/// as regular data types. The parent entity's serializer handles them.
/// </remarks>
public record Money(decimal Amount, string Currency = "USD")
{
    /// <summary>
    /// Zero amount in USD.
    /// </summary>
    public static readonly Money Zero = new(0);

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
public record Percentage(decimal Value)
{
    public static readonly Percentage Zero = new(0);
    public static readonly Percentage Full = new(100);

    public Money Of(Money money)
    {
        return money.Multiply(Value / 100m);
    }
}
