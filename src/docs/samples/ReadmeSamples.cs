using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Samples.Infrastructure;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for README.md documentation.
/// </summary>
public partial class ReadmeSamples
{
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
    // [Fact]
    public async Task PersonFactory_ClientUsage()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.client.GetRequiredService<IPersonFactory>();

        // Create a new person
        var person = factory.Create();
        person.FirstName = "John";
        person.LastName = "Doe";
        person.Email = "john.doe@example.com";

        // Save (routes to Insert since IsNew = true)
        var saved = await factory.Save(person);

        // Fetch an existing person
        var fetched = await factory.Fetch(saved!.Id);
        Assert.NotNull(fetched);
        Assert.Equal("John", fetched.FirstName);

        // Update
        fetched.Email = "john.updated@example.com";
        await factory.Save(fetched);

        // Delete
        fetched.IsDeleted = true;
        await factory.Save(fetched);
    }
    #endregion

    // [Fact]
    public void ReadmeDomainModel_Compiles()
    {
        // Verify the domain model compiles correctly
        var person = new Person();
        Assert.NotNull(person);
        Assert.True(person.IsNew);
    }
}

#region readme-client-assembly-mode
// In client assembly's AssemblyAttributes.cs:
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]
#endregion

#region readme-server-setup
// Server Program.cs
public static class ServerSetupExample
{
    public static void ConfigureServices(IServiceCollection services)
    {
        // Register Neatoo ASP.NET Core services
        services.AddNeatooAspNetCore(typeof(ReadmeSamples.Person).Assembly);

        // Register domain services
        services.AddScoped<IPersonRepository, PersonRepository>();
    }
}
#endregion

#region readme-client-setup
// Client Program.cs (Blazor WASM)
public static class ClientSetupExample
{
    public static void ConfigureServices(IServiceCollection services, Uri baseAddress)
    {
        // Register Neatoo RemoteFactory for client mode
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(ReadmeSamples.Person).Assembly);

        // Register keyed HttpClient for Neatoo
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient { BaseAddress = baseAddress });
    }
}
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

public partial class PersonAuthorization : IPersonAuthorization
{
    private readonly IUserContext _userContext;

    public PersonAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool CanCreate() => _userContext.IsAuthenticated;
    public bool CanRead() => _userContext.IsAuthenticated;
    public bool CanWrite() => _userContext.IsInRole("Admin") || _userContext.IsInRole("Manager");
}
#endregion
