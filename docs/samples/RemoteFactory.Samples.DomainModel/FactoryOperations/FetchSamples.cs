/// <summary>
/// Code samples for docs/concepts/factory-operations.md - Fetch operations
/// </summary>

using Neatoo.RemoteFactory;
using Microsoft.EntityFrameworkCore;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations.FetchExamples;

#region docs:concepts/factory-operations:multiple-fetch-methods
[Factory]
public class PersonWithMultipleFetch
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; } = true;

    [Create]
    public PersonWithMultipleFetch() { }

    // Simple fetch by ID
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false;
        return true;
    }

    // Fetch with multiple parameters
    [Remote]
    [Fetch]
    public async Task<bool> FetchByEmail(string email, [Service] IPersonContext context)
    {
        var entity = await context.Persons
            .FirstOrDefaultAsync(p => p.Email == email);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false;
        return true;
    }
}
#endregion

// Note: The generated factory interface is shown in the docs as:
// public interface IPersonWithMultipleFetchFactory
// {
//     IPersonModel? Create();
//     Task<IPersonWithMultipleFetch?> Fetch(int id);
//     Task<IPersonWithMultipleFetch?> FetchByEmail(string email);
// }

// Supporting types
public interface IPersonContext
{
    DbSet<PersonEntity> Persons { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PersonEntity
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
}
