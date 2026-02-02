using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.SaveOperation;

// ============================================================================
// Save Utility Methods
// ============================================================================

public static class SaveUtilities
{
    #region save-extensions
    // Batch save utility: saves multiple entities with cancellation support
    public static async Task<List<T>> SaveBatch<T>(IFactorySave<T> factory, IEnumerable<T> entities, CancellationToken ct)
        where T : class, IFactorySaveMeta
    {
        var saved = new List<T>();
        foreach (var entity in entities)
        {
            ct.ThrowIfCancellationRequested();
            if (await factory.Save(entity, ct) is T result) saved.Add(result);
        }
        return saved;
    }
    #endregion

    public static async Task<T?> SaveWithCancellation<T>(
        IFactorySave<T> factory, T entity, CancellationToken cancellationToken)
        where T : class, IFactorySaveMeta
    {
        ArgumentNullException.ThrowIfNull(factory);
        cancellationToken.ThrowIfCancellationRequested();
        var result = await factory.Save(entity, cancellationToken);
        return result as T;
    }
}
