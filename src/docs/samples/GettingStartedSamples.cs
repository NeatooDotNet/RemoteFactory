using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Samples.Infrastructure;
using System.ComponentModel.DataAnnotations;

namespace Neatoo.RemoteFactory.Samples;

/// <summary>
/// Code samples for docs/getting-started.md documentation.
/// </summary>
public partial class GettingStartedSamples
{
    #region getting-started-person-model
    public interface IPersonModel : IFactorySaveMeta
    {
        Guid Id { get; }

        [Required(ErrorMessage = "First name is required")]
        string FirstName { get; set; }

        [Required(ErrorMessage = "Last name is required")]
        string LastName { get; set; }

        [EmailAddress(ErrorMessage = "Invalid email format")]
        string? Email { get; set; }

        string? Phone { get; set; }
        DateTime Created { get; }
        DateTime Modified { get; }

        // Override IsDeleted to make it settable for deletion support
        new bool IsDeleted { get; set; }
    }

    [Factory]
    public partial class PersonModel : IPersonModel
    {
        public Guid Id { get; private set; }
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? Phone { get; set; }
        public DateTime Created { get; private set; }
        public DateTime Modified { get; private set; }
        public bool IsNew { get; set; } = true;
        public bool IsDeleted { get; set; }

        [Create]
        public PersonModel()
        {
            Id = Guid.NewGuid();
            Created = DateTime.UtcNow;
            Modified = DateTime.UtcNow;
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
            Phone = entity.Phone;
            Created = entity.Created;
            Modified = entity.Modified;
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
                Phone = Phone,
                Created = Created,
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
            entity.Phone = Phone;
            entity.Modified = DateTime.UtcNow;
            Modified = entity.Modified;

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

    #region getting-started-usage
    public async Task UsePersonFactory(IPersonModelFactory factory)
    {
        // Create new person
        var person = factory.Create();
        person.FirstName = "Jane";
        person.LastName = "Smith";
        person.Email = "jane.smith@example.com";
        person.Phone = "555-0123";

        // Save (Insert) - routes to Insert method when IsNew=true
        var saved = await factory.Save(person);

        // Fetch existing person by ID
        var fetched = await factory.Fetch(saved!.Id);

        // Update - routes to Update method when IsNew=false
        fetched!.Email = "jane.updated@example.com";
        var updated = await factory.Save(fetched);

        // Delete - routes to Delete method when IsDeleted=true
        updated!.IsDeleted = true;
        await factory.Save(updated);
    }
    #endregion

    [Fact]
    public async Task PersonModel_FullWorkflow()
    {
        var scopes = SampleTestContainers.Scopes();
        var factory = scopes.local.GetRequiredService<IPersonModelFactory>();

        // Create new person
        var person = factory.Create();
        person.FirstName = "Jane";
        person.LastName = "Smith";
        person.Email = "jane.smith@example.com";
        person.Phone = "555-0123";

        // Save (Insert) - routes to Insert method when IsNew=true
        var saved = await factory.Save(person);
        Assert.NotNull(saved);
        Assert.False(saved.IsNew);

        // Fetch existing person by ID
        var fetched = await factory.Fetch(saved.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Jane", fetched.FirstName);
        Assert.Equal("Smith", fetched.LastName);

        // Update - routes to Update method when IsNew=false
        fetched.Email = "jane.updated@example.com";
        var updated = await factory.Save(fetched);
        Assert.NotNull(updated);

        // Delete - routes to Delete method when IsDeleted=true
        updated.IsDeleted = true;
        await factory.Save(updated);
    }

    [Fact]
    public void PersonModel_Compiles()
    {
        var person = new PersonModel();
        Assert.NotNull(person);
        Assert.True(person.IsNew);
        Assert.NotEqual(Guid.Empty, person.Id);
    }
}

#region getting-started-client-mode
// In your client project's AssemblyAttributes.cs file:
// using Neatoo.RemoteFactory;
// [assembly: FactoryMode(FactoryMode.RemoteOnly)]

// This tells the source generator to only generate remote stubs
// (no local implementation code) for this assembly
#endregion

#region getting-started-server-program
// Server Program.cs
public static class GettingStartedServerProgram
{
    public static void ConfigureServer(IServiceCollection services)
    {
        // Add Neatoo ASP.NET Core with custom serialization
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal // Compact format (default)
        };

        services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(GettingStartedSamples.PersonModel).Assembly);

        // Register domain services
        services.AddScoped<IPersonRepository, PersonRepository>();

        // Add CORS for Blazor client
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins("https://localhost:5001")
                      .AllowAnyHeader()
                      .AllowAnyMethod();
            });
        });
    }

    public static void ConfigureApp(Microsoft.AspNetCore.Builder.WebApplication app)
    {
        app.UseCors();
        app.UseNeatoo(); // Maps /api/neatoo endpoint
    }
}
#endregion

#region getting-started-client-program
// Client Program.cs (Blazor WASM)
public static class GettingStartedClientProgram
{
    public static void ConfigureClient(IServiceCollection services, string serverUrl)
    {
        // Register Neatoo RemoteFactory for remote mode
        services.AddNeatooRemoteFactory(
            NeatooFactory.Remote,
            typeof(GettingStartedSamples.PersonModel).Assembly);

        // Register keyed HttpClient for Neatoo API calls
        services.AddKeyedScoped(
            RemoteFactoryServices.HttpClientKey,
            (sp, key) => new HttpClient
            {
                BaseAddress = new Uri(serverUrl)
            });
    }
}
#endregion

#region getting-started-serialization-config
public static class SerializationConfigExample
{
    public static void ConfigureWithNamedFormat(IServiceCollection services)
    {
        // Use Named format for easier debugging (larger payloads)
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Named
        };

        services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(GettingStartedSamples.PersonModel).Assembly);
    }

    public static void ConfigureWithOrdinalFormat(IServiceCollection services)
    {
        // Use Ordinal format for production (compact payloads)
        var serializationOptions = new NeatooSerializationOptions
        {
            Format = SerializationFormat.Ordinal // Default
        };

        services.AddNeatooAspNetCore(
            serializationOptions,
            typeof(GettingStartedSamples.PersonModel).Assembly);
    }
}
#endregion
