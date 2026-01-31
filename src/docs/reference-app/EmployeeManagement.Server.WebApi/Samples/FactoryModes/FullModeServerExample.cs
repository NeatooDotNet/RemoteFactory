using EmployeeManagement.Domain.Aggregates;
using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;

namespace EmployeeManagement.Server.Samples.FactoryModes;

#region modes-full-example
/// <summary>
/// Employee aggregate with full CRUD operations for server deployment.
/// </summary>
[Factory]
public partial class EmployeeFullMode : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Employee with generated ID.
    /// </summary>
    [Create]
    public EmployeeFullMode()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        DepartmentId = entity.DepartmentId;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Inserts a new Employee.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            DepartmentId = DepartmentId
        };
        await repository.AddAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Updates an existing Employee.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            DepartmentId = DepartmentId
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
}

/// <summary>
/// Server setup with Full mode and Server runtime.
/// </summary>
public static class FullModeServerSetup
{
    public static void Configure(IServiceCollection services)
    {
        var domainAssembly = typeof(Employee).Assembly;

        // AddNeatooAspNetCore uses Server mode - handles incoming HTTP requests
        services.AddNeatooAspNetCore(
            new NeatooSerializationOptions { Format = SerializationFormat.Ordinal },
            domainAssembly);

        // Register factory types
        services.RegisterMatchingName(domainAssembly);

        // Register server-side repositories
        services.AddScoped<IEmployeeRepository, InMemoryEmployeeRepository>();
    }
}
#endregion
