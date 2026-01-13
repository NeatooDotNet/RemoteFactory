namespace Neatoo.RemoteFactory;

/// <summary>
/// Factory contract for types that support Save operation routing.
/// Generated factories implement this interface to provide the Save method
/// that routes to Insert, Update, or Delete based on the entity's state.
/// </summary>
/// <typeparam name="T">The factory-enabled type that implements <see cref="IFactorySaveMeta"/>.</typeparam>
public interface IFactorySave<T>
    where T : IFactorySaveMeta
{
    /// <summary>
    /// Saves the entity by routing to the appropriate operation based on state:
    /// Insert (IsNew), Update (!IsNew and !IsDeleted), or Delete (IsDeleted).
    /// </summary>
    /// <param name="entity">The entity to save.</param>
    /// <param name="cancellationToken">Optional cancellation token to cancel the operation.</param>
    /// <returns>The saved entity, or null if the operation was not authorized or the entity was not found.</returns>
    Task<IFactorySaveMeta?> Save(T entity, CancellationToken cancellationToken = default);
}
