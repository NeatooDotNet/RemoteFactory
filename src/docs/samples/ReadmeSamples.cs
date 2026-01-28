using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;

namespace Neatoo.Docs.Samples;

// Supporting types for README examples
public interface IUserContext
{
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

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
// Create a new person
// var person = factory.Create();
// person.FirstName = "John";
// person.LastName = "Doe";
// person.Email = "john.doe@example.com";
//
// // Save routes to Insert (IsNew = true)
// var saved = await factory.Save(person);
//
// // Fetch an existing person
// var fetched = await factory.Fetch(saved!.Id);
// // fetched.FirstName is "John"
//
// // Update - Save routes to Update (IsNew = false)
// fetched!.Email = "john.updated@example.com";
// await factory.Save(fetched);
//
// // Delete - set IsDeleted, then Save
// fetched.IsDeleted = true;
// await factory.Save(fetched);
#endregion

#region readme-server-setup
public static class ServerSetup
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register Neatoo ASP.NET Core services
        // services.AddNeatooAspNetCore(typeof(Person).Assembly);

        // Register domain services
        // services.AddScoped<IPersonRepository, PersonRepository>();
    }
}
#endregion

#region readme-client-setup
public static class ClientSetup
{
    public static void ConfigureServices(IServiceCollection services, Uri baseAddress)
    {
        // Register Neatoo RemoteFactory for client
        // services.AddNeatooRemoteFactory(
        //     NeatooFactory.Remote,
        //     typeof(Person).Assembly);

        // Register keyed HttpClient for Neatoo
        // services.AddKeyedScoped(
        //     RemoteFactoryServices.HttpClientKey,
        //     (sp, key) => new HttpClient { BaseAddress = baseAddress });
    }
}
#endregion

#region readme-client-assembly-mode
// In client assembly's AssemblyAttributes.cs:
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]
#endregion

#region readme-full-example
[Factory]
[AuthorizeFactory<IPersonAuthorization>]
public partial class PersonWithAuth : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PersonWithAuth()
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

public interface IPersonAuthorization
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}

public class PersonAuthorization : IPersonAuthorization
{
    private readonly IUserContext _userContext;

    public PersonAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreate() => _userContext.IsAuthenticated;
    public bool CanRead() => _userContext.IsAuthenticated;
    public bool CanWrite() =>
        _userContext.IsInRole("Admin") || _userContext.IsInRole("Manager");
}
#endregion

// Supporting types
public class PersonEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

public interface IPersonRepository
{
    Task<PersonEntity?> GetByIdAsync(Guid id);
    Task AddAsync(PersonEntity entity);
    Task UpdateAsync(PersonEntity entity);
    Task DeleteAsync(Guid id);
    Task SaveChangesAsync();
}
