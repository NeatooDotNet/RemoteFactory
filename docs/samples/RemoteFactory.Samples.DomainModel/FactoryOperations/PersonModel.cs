/// <summary>
/// Code samples for docs/getting-started/quick-start.md and docs/concepts/factory-operations.md
///
/// Snippets in this file:
/// - docs:getting-started/quick-start:person-model-full
/// - docs:concepts/factory-operations:create-constructor
/// - docs:concepts/factory-operations:fetch-method
/// - docs:concepts/factory-operations:combined-save
/// - docs:concepts/factory-operations:delete-method
/// </summary>

using Neatoo.RemoteFactory;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations;

#region docs:getting-started/quick-start:person-model-full
[Factory]
public class PersonModel : IPersonModel
{
    #region docs:concepts/factory-operations:create-constructor
    [Create]
    public PersonModel()
    {
        IsNew = true;
    }
    #endregion

    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? Email { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    #region docs:concepts/factory-operations:fetch-method
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
    #endregion

    #region docs:concepts/factory-operations:combined-save
    [Remote]
    [Insert]
    [Update]
    public async Task Save([Service] IPersonContext context)
    {
        PersonEntity entity;

        if (IsNew)
        {
            entity = new PersonEntity();
            context.Persons.Add(entity);
        }
        else
        {
            entity = await context.Persons.FindAsync(Id)
                ?? throw new InvalidOperationException("Person not found");
        }

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.Email = Email;
        await context.SaveChangesAsync();

        Id = entity.Id;
        IsNew = false;
    }
    #endregion

    #region docs:concepts/factory-operations:delete-method
    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(Id);
        if (entity != null)
        {
            context.Persons.Remove(entity);
            await context.SaveChangesAsync();
        }
    }
    #endregion
}
#endregion
