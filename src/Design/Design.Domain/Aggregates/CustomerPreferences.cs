// =============================================================================
// DESIGN SOURCE OF TRUTH: [Create] Constructor and an [Insert, Update] Upsert
// =============================================================================
//
// A customer's delivery preferences: one row per customer, keyed by name. The
// client creates it with defaults or fetches the stored one, edits it, and
// saves; the server writes the row whether or not one exists yet.
//
// DESIGN DECISION: A [Create] constructor, because creating it does nothing
// but set defaults
//
// A constructor marked [Create] is the factory's Create. Be aware it is also
// the constructor every other path uses to build the object: a Fetch, and a
// deserialization on either tier, both run it before they fill the properties.
// So it holds only defaults the next step may overwrite. Anything with a side
// effect (a lookup, a counter, an event) belongs in a [Create] method instead
// (VisionAlignment R-J). It takes no arguments: a type whose only constructor
// requires arguments loses its ordinal converter, and DI could not build it to
// fetch.
//
// DESIGN DECISION: One [Insert, Update] method, because the store's write is
// the same either way
//
// GENERATOR BEHAVIOR: the factory's save method is named "Save" plus the
// method name, less the group's highest-ranked operation (Delete, then Update,
// then Insert) when the name starts with it. Here that is Update, and "Upsert"
// does not start with it, so the method is SaveUpsert. A method named Insert
// would give SaveInsert. With no [Delete], marking the object
// deleted makes the save throw.
//
// =============================================================================

using System.Collections.Concurrent;
using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Public interface for a customer's delivery preferences.
/// </summary>
public interface ICustomerPreferences : IFactorySaveMeta
{
    string CustomerName { get; set; }
    string DeliveryWindow { get; set; }
    bool NotifyBySms { get; set; }
    DateTimeOffset? LastSavedAt { get; set; }
}

/// <summary>
/// A customer's delivery preferences.
/// </summary>
#region attributes-factory
[Factory]
internal partial class CustomerPreferences : ICustomerPreferences, IFactoryOnStartAsync
#endregion
{
    #region operations-create-constructor
    [Create]
    public CustomerPreferences()
    {
        DeliveryWindow = "Any";
    }
    #endregion

    public string CustomerName { get; set; } = string.Empty;
    public string DeliveryWindow { get; set; }
    public bool NotifyBySms { get; set; }
    public DateTimeOffset? LastSavedAt { get; set; }

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Loads a customer's stored preferences; false when there are none.
    /// </summary>
    [Remote, Fetch]
    internal async Task<bool> Fetch(string customerName, [Service] ICustomerPreferencesStore store)
    {
        var row = await store.FindAsync(customerName);
        if (row == null)
            return false;

        CustomerName = row.CustomerName;
        DeliveryWindow = row.DeliveryWindow;
        NotifyBySms = row.NotifyBySms;
        LastSavedAt = row.LastSavedAt;
        IsNew = false;
        return true;
    }

    #region operations-insert-update
    [Remote, Insert, Update]
    internal async Task Upsert([Service] ICustomerPreferencesStore store)
    {
        await store.WriteAsync(new CustomerPreferencesRow(CustomerName, DeliveryWindow, NotifyBySms, LastSavedAt));
        IsNew = false;
    }
    #endregion

    /// <summary>
    /// Stamps the save time before the write.
    /// </summary>
    /// <remarks>
    /// GENERATOR BEHAVIOR: the start hook runs before a Task-returning operation:
    /// here Fetch and Upsert (as Insert or Update). It does not run for the
    /// [Create] constructor. It prepares state; it is not a check (R-E).
    /// </remarks>
    #region operations-lifecycle-onstart
    public Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation is FactoryOperation.Insert or FactoryOperation.Update)
        {
            LastSavedAt = DateTimeOffset.UtcNow;
        }
        return Task.CompletedTask;
    }
    #endregion
}

/// <summary>
/// One customer's stored preferences.
/// </summary>
public record CustomerPreferencesRow(string CustomerName, string DeliveryWindow, bool NotifyBySms, DateTimeOffset? LastSavedAt);

/// <summary>
/// The preferences store. Server-only.
/// </summary>
public interface ICustomerPreferencesStore
{
    Task<CustomerPreferencesRow?> FindAsync(string customerName, CancellationToken cancellationToken = default);

    /// <summary>Writes the customer's row, replacing any stored one.</summary>
    Task WriteAsync(CustomerPreferencesRow row, CancellationToken cancellationToken = default);
}

/// <summary>
/// In-memory store standing in for a database.
/// </summary>
public class InMemoryCustomerPreferencesStore : ICustomerPreferencesStore
{
    private readonly ConcurrentDictionary<string, CustomerPreferencesRow> rows = new(StringComparer.Ordinal);

    /// <summary>How many rows the store holds.</summary>
    public int Count => rows.Count;

    public Task<CustomerPreferencesRow?> FindAsync(string customerName, CancellationToken cancellationToken = default)
        => Task.FromResult(rows.TryGetValue(customerName, out var row) ? row : null);

    public Task WriteAsync(CustomerPreferencesRow row, CancellationToken cancellationToken = default)
    {
        rows[row.CustomerName] = row;
        return Task.CompletedTask;
    }
}
