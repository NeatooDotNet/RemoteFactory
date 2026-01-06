/// <summary>
/// Code samples for docs/concepts/factory-operations.md - Write operations (Insert, Update, Delete)
/// </summary>

using Neatoo.RemoteFactory;

namespace RemoteFactory.Samples.DomainModel.FactoryOperations.WriteExamples;

#region docs:concepts/factory-operations:insert-example
[Factory]
public class PersonInsertExample : IFactorySaveMeta
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonContext context)
    {
        var entity = new PersonEntity();
        entity.FirstName = FirstName;
        entity.LastName = LastName;
        context.Persons.Add(entity);
        await context.SaveChangesAsync();

        Id = entity.Id;
        IsNew = false;
    }
}
#endregion

#region docs:concepts/factory-operations:update-example
[Factory]
public class PersonUpdateExample : IFactorySaveMeta
{
    public int Id { get; private set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Remote]
    [Update]
    public async Task Update([Service] IPersonContext context)
    {
        var entity = await context.Persons.FindAsync(Id)
            ?? throw new InvalidOperationException("Person not found");

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        await context.SaveChangesAsync();
    }
}
#endregion

#region docs:concepts/factory-operations:using-save
public class UsingSaveExample
{
    public async Task DemoSave(IPersonModelFactory factory)
    {
        // Create and save new
        var person = factory.Create()!;
        person.FirstName = "John";
        await factory.Save(person);  // Calls Insert

        // Modify and save existing
        person.FirstName = "Jane";
        await factory.Save(person);  // Calls Update

        // Delete
        person.IsDeleted = true;
        await factory.Save(person);  // Calls Delete
    }
}
#endregion

#region docs:concepts/factory-operations:save-vs-trysave
public class SaveVsTrySaveExample
{
    public async Task DemoSaveWithException(IPersonModelFactory factory, IPersonModel person)
    {
        // Save throws on authorization failure
        try
        {
            var result = await factory.Save(person);
        }
        catch (NotAuthorizedException)
        {
            // Handle authorization failure
        }
    }

    public async Task DemoTrySave(IPersonModelFactory factory, IPersonModel person)
    {
        // TrySave returns result
        var result = await factory.TrySave(person);
        if (result.HasAccess)
        {
            var savedPerson = result.Result;
        }
        else
        {
            var message = result.Message;
        }
    }
}
#endregion

// Supporting types
public interface IPersonContext
{
    Microsoft.EntityFrameworkCore.DbSet<PersonEntity> Persons { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public class PersonEntity
{
    public int Id { get; set; }
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
}

public interface IPersonModel : IFactorySaveMeta
{
    string? FirstName { get; set; }
    new bool IsDeleted { get; set; }
}

public interface IPersonModelFactory
{
    IPersonModel? Create();
    Task<IPersonModel?> Save(IPersonModel person);
    Task<Authorized<IPersonModel>> TrySave(IPersonModel person);
}
