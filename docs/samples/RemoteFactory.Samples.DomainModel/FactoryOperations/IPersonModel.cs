/// <summary>
/// Code samples for docs/getting-started/quick-start.md and docs/concepts/factory-operations.md
///
/// Snippets in this file:
/// - docs:getting-started/quick-start:person-interface
/// </summary>

using Neatoo.RemoteFactory;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations;

#region docs:getting-started/quick-start:person-interface
public interface IPersonModel : IFactorySaveMeta
{
    int Id { get; }
    string? FirstName { get; set; }
    string? LastName { get; set; }
    string? Email { get; set; }
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}
#endregion
