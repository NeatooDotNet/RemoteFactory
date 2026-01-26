using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Modes;

#region modes-local-remote-methods
/// <summary>
/// Demonstrates local vs remote method execution based on [Remote] attribute.
/// </summary>
[Factory]
public partial class EmployeeLocalRemote : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Local execution - no [Remote] attribute.
    /// Runs on client, no network call, no serialization.
    /// </summary>
    [Create]
    public EmployeeLocalRemote()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Remote execution - [Remote] attribute present.
    /// Serialized and sent to server where repository exists.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Remote execution for persistence operations.
    /// Repository only available on server.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region modes-logical-testing
/// <summary>
/// Employee for Logical mode testing - all operations run locally.
/// </summary>
[Factory]
public partial class EmployeeLogicalMode : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    // Logical mode: All methods execute locally in the same process
    // [Remote] attribute is honored but no serialization occurs
    // Ideal for unit testing domain logic without mocking HTTP

    [Create]
    public EmployeeLogicalMode()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// In Logical mode, runs locally with local DI resolution.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid id,
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion

// Factory Mode Configuration Examples
// These would appear in Program.cs or startup configuration files
// See the Server.WebApi and Client.Blazor projects for actual usage

/* Configuration Examples (static code - not compilable as samples):

#region modes-full-config
// Full mode: Both local methods and remote stubs generated
// Use in shared domain assemblies
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Full,  // Generate both local and remote code
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
#endregion

#region modes-logical-config
// Logical mode: Everything runs locally, no HTTP
// Use for testing
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Logical,  // All methods local, no serialization
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
#endregion

#region modes-remote-config
// Remote mode: Client-side HTTP stubs only
// Use in Blazor WASM clients
builder.Services.AddNeatooRemoteFactory(
    NeatooFactory.Remote,  // Generate HTTP stubs for remote calls
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);

// Required: Register HttpClient for remote calls
builder.Services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
    new HttpClient { BaseAddress = new Uri("https://api.example.com/") });
#endregion

#region modes-server-config
// Server mode: Handle remote requests via ASP.NET Core
// Use in server applications
builder.Services.AddNeatooAspNetCore(
    new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
    typeof(Employee).Assembly);
#endregion

*/

#region modes-full-example
// Complete server setup: Full mode (compile) + Server runtime.
// Use this configuration for ASP.NET Core server applications.
//
// In Program.cs:
//
// var domainAssembly = typeof(Employee).Assembly;
//
// // Register RemoteFactory with ASP.NET Core integration
// builder.Services.AddNeatooAspNetCore(
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// // Register interface -> implementation mappings
// builder.Services.RegisterMatchingName(domainAssembly);
//
// // Configure middleware
// app.UseAuthentication();
// app.UseAuthorization();
// app.UseNeatoo(); // Add /api/neatoo endpoint
#endregion

#region modes-remoteonly-example
// Complete client setup: RemoteOnly mode (compile) + Remote runtime.
// Use this configuration for Blazor WASM or other HTTP clients.
//
// In AssemblyAttributes.cs:
// [assembly: FactoryMode(FactoryModeOption.RemoteOnly)]
//
// In Program.cs:
// var domainAssembly = typeof(Employee).Assembly;
//
// // Register RemoteFactory in Remote mode
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Remote,
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// // Required: Register keyed HttpClient for remote calls
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri("https://api.example.com/") });
#endregion

#region modes-logical-example
// Complete single-tier setup: Full mode (compile) + Logical runtime.
// Use this configuration for console apps, tests, or single-tier apps.
//
// In Program.cs:
// var domainAssembly = typeof(Employee).Assembly;
//
// // Register RemoteFactory in Logical mode
// services.AddNeatooRemoteFactory(
//     NeatooFactory.Logical,
//     new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
//     domainAssembly);
//
// // Register interface -> implementation mappings
// services.RegisterMatchingName(domainAssembly);
//
// // Register infrastructure services directly
// services.AddDbContext<AppDbContext>(...);
// services.AddScoped<IEmployeeRepository, EmployeeRepository>();
#endregion
