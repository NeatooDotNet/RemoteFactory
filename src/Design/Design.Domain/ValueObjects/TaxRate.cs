// =============================================================================
// DESIGN SOURCE OF TRUTH: A value object whose construction needs a service
// =============================================================================
//
// TaxRate is a record like Money, with one difference: its rate is not something
// the caller knows. It lives in a table only the server reads. That service is the
// whole reason TaxRate carries [Factory] (VisionAlignment R-F). Money and
// Percentage, which need nothing, are plain records with no factory.
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.ValueObjects;

/// <summary>
/// Value object for the sales tax rate of a region, loaded from the server's tax table.
/// </summary>
/// <remarks>
/// DESIGN DECISION: A static [Fetch] that returns null for an unknown region
///
/// A record's properties are set by its constructor, so its Fetch is a static method
/// that returns the record rather than an instance method that fills one in.
/// The region is a key, so the operation is a Fetch (D3), and a region the table
/// does not hold is null, not an exception (R-I).
///
/// [Remote] internal: the client calls it through ITaxRateFactory and it runs on the
/// server, where ITaxTable is registered.
///
/// Once loaded, TaxRate crosses the wire as a parameter or property like any
/// other record. The factory is only how it is obtained.
/// </remarks>
#region skill-value-object-factory
[Factory]
public partial record TaxRate(string Region, Percentage Rate)
{
    #region service-injection-server-only
    [Remote, Fetch]
    internal static async Task<TaxRate?> Fetch(string region, [Service] ITaxTable taxTable)
    {
        var rate = await taxTable.FindRateAsync(region);
        return rate is null ? null : new TaxRate(region, rate);
    }
    #endregion
}
#endregion

/// <summary>
/// The server's table of tax rates by region. Server-only: injected with [Service]
/// into <see cref="TaxRate"/>'s Fetch, never called by the client.
/// </summary>
public interface ITaxTable
{
    Task<Percentage?> FindRateAsync(string region, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory tax table standing in for a database.
/// </summary>
/// <remarks>
/// Named InMemoryTaxTable, not TaxTable, so RegisterMatchingName does not pick it
/// up: that convention runs on the client too, and this table must exist only on
/// the server. It is registered explicitly in ServerServices.
/// </remarks>
public class InMemoryTaxTable : ITaxTable
{
    /// <summary>The regions the table holds, with their rates.</summary>
    public static readonly IReadOnlyDictionary<string, decimal> Rates = new Dictionary<string, decimal>
    {
        ["WA"] = 6.5m,
        ["OR"] = 0m,
        ["CA"] = 7.25m,
    };

    public Task<Percentage?> FindRateAsync(string region, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(Rates.TryGetValue(region, out var rate) ? new Percentage(rate) : null);
    }
}
