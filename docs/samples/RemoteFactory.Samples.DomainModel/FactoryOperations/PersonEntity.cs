/// <summary>
/// Code samples for docs/getting-started/quick-start.md
///
/// Snippets in this file:
/// - docs:getting-started/quick-start:person-entity
/// - docs:getting-started/quick-start:person-context
/// </summary>

using Microsoft.EntityFrameworkCore;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations;

#region docs:getting-started/quick-start:person-entity
public class PersonEntity
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}
#endregion

#region docs:getting-started/quick-start:person-context
public interface IPersonContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
#endregion
