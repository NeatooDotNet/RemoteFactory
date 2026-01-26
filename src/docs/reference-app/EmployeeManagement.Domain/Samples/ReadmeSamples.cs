using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Readme;

// README.md code samples
// These provide concise examples for the project README
// Note: Classes use [SuppressFactory] to avoid generating actual factories

#region readme-domain-model
public interface IPerson : IFactorySaveMeta
{
    Guid Id { get; }
    string FirstName { get; set; }
    string LastName { get; set; }
    string? Email { get; set; }
    new bool IsDeleted { get; set; }
}

[Factory]
[SuppressFactory] // Suppress factory generation for documentation sample
public partial class Person : IPerson
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public Person()
    {
        Id = Guid.NewGuid();
    }

    [Remote]
    [Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        IsNew = false;
        return true;
    }

    [Remote]
    [Insert]
    public async Task Insert([Service] IPersonRepository repository)
    {
        var entity = new PersonEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            Created = DateTime.UtcNow,
            Modified = DateTime.UtcNow
        };
        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        IsNew = false;
    }

    [Remote]
    [Update]
    public async Task Update([Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id)
            ?? throw new InvalidOperationException($"Person {Id} not found");

        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.Email = Email;
        entity.Modified = DateTime.UtcNow;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }

    [Remote]
    [Delete]
    public async Task Delete([Service] IPersonRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}
#endregion

#region readme-client-usage
public static class ClientUsageExample
{
    // IPersonFactory is auto-generated from Person class
    public static async Task BasicOperations(IPersonFactory factory)
    {
        // Create a new person
        var person = factory.Create();
        person.FirstName = "John";
        person.LastName = "Doe";
        person.Email = "john.doe@example.com";

        // Save (routes to Insert because IsNew = true)
        await factory.Save(person);

        // Fetch existing
        var existing = await factory.Fetch(person.Id);

        // Update
        existing!.Email = "john.updated@example.com";
        await factory.Save(existing);  // Routes to Update

        // Delete
        existing.IsDeleted = true;
        await factory.Save(existing);  // Routes to Delete
    }
}
#endregion

#region readme-client-assembly-mode
// In client assembly's AssemblyAttributes.cs:
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
#endregion

#region readme-server-setup
// Server setup (Program.cs):
// services.AddNeatooAspNetCore(typeof(Person).Assembly);
// app.UseNeatoo();
#endregion

#region readme-client-setup
// Client setup (Program.cs):
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Remote,
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     typeof(Person).Assembly);
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri("https://api.example.com/") });
#endregion

#region readme-full-example
[Factory]
[SuppressFactory] // Suppress factory generation for documentation sample
[AuthorizeFactory<IPersonAuthorization>]
public partial class PersonWithAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PersonWithAuth() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(id);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IPersonRepository repository)
    {
        await repository.AddAsync(new PersonEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = "", Created = DateTime.UtcNow, Modified = DateTime.UtcNow
        });
        await repository.SaveChangesAsync();
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IPersonRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id)
            ?? throw new InvalidOperationException();
        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.Modified = DateTime.UtcNow;
        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }

    [Remote, Delete]
    public async Task Delete([Service] IPersonRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}

public interface IPersonAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}
#endregion

// Supporting interfaces and types for README samples
public interface IPersonRepository
{
    Task<PersonEntity?> GetByIdAsync(Guid id);
    Task AddAsync(PersonEntity entity);
    Task UpdateAsync(PersonEntity entity);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}

public class PersonEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string? Email { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

// Factory interface shape (auto-generated by source generator)
public interface IPersonFactory
{
    Person Create();
    Task<Person?> Fetch(Guid id, CancellationToken ct = default);
    Task<Person> Save(Person entity, CancellationToken ct = default);
}
