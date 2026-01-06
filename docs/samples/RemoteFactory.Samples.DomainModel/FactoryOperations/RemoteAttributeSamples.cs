/// <summary>
/// Code samples for docs/concepts/factory-operations.md - Remote attribute and operation matching
/// </summary>

using Neatoo.RemoteFactory;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations.RemoteExamples;

#region docs:concepts/factory-operations:remote-attribute
[Factory]
public class PersonRemoteExample
{
    // Executes locally (no [Remote])
    [Create]
    public PersonRemoteExample() { }

    // Executes on server
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(int id, [Service] IPersonContext context)
    {
        // Server-side code with database access
        var entity = await context.Persons.FindAsync(id);
        return entity != null;
    }
}
#endregion

#region docs:concepts/factory-operations:operation-matching
[Factory]
public class OrderModelWithMatching : IFactorySaveMeta
{
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // Default save operations
    [Remote]
    [Insert]
    public void Insert([Service] IOrderContext context) { }

    [Remote]
    [Update]
    public void Update([Service] IOrderContext context) { }

    [Remote]
    [Delete]
    public void Delete([Service] IOrderContext context) { }

    // Operations with extra parameter
    [Remote]
    [Insert]
    public void InsertWithAudit(string auditReason, [Service] IOrderContext context) { }

    [Remote]
    [Update]
    public void UpdateWithAudit(string auditReason, [Service] IOrderContext context) { }

    [Remote]
    [Delete]
    public void DeleteWithAudit(string auditReason, [Service] IOrderContext context) { }
}
#endregion

// Note: The generated Save methods interface is shown in the docs as:
// public interface IOrderModelWithMatchingFactory
// {
//     Task<IOrderModel?> Save(IOrderModel target);
//     Task<IOrderModel?> SaveWithAudit(IOrderModel target, string auditReason);
// }

// Supporting types
public interface IPersonContext
{
    Microsoft.EntityFrameworkCore.DbSet<PersonEntity> Persons { get; }
}

public class PersonEntity
{
    public int Id { get; set; }
}

public interface IOrderContext
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
