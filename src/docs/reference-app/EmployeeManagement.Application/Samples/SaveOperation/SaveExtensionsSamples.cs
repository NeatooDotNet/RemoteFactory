using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Save Utility Methods
// ============================================================================

#region save-extensions
/// <summary>
/// Utility methods for common save patterns.
/// </summary>
public static class SaveUtilities
{
    /// <summary>
    /// Saves with explicit cancellation check before save.
    /// </summary>
    public static async Task<T?> SaveWithCancellation<T>(
        IFactorySave<T> factory,
        T entity,
        CancellationToken cancellationToken)
        where T : class, IFactorySaveMeta
    {
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();

        var result = await factory.Save(entity, cancellationToken);
        return result as T;
    }

    /// <summary>
    /// Saves a batch of entities, checking cancellation between each save.
    /// Returns list of successfully saved entities.
    /// </summary>
    public static async Task<List<T>> SaveBatch<T>(
        IFactorySave<T> factory,
        IEnumerable<T> entities,
        CancellationToken cancellationToken)
        where T : class, IFactorySaveMeta
    {
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentNullException.ThrowIfNull(entities);

        var saved = new List<T>();

        foreach (var entity in entities)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var result = await factory.Save(entity, cancellationToken);
            if (result is T typedResult)
            {
                saved.Add(typedResult);
            }
        }

        return saved;
    }

    // Extension method pattern (alternative syntax):
    // public static async Task<T?> SaveWithCancel<T>(
    //     this IFactorySave<T> factory,
    //     T entity,
    //     CancellationToken ct)
    //     where T : class, IFactorySaveMeta
    // {
    //     ct.ThrowIfCancellationRequested();
    //     return await factory.Save(entity, ct) as T;
    // }
}
#endregion
