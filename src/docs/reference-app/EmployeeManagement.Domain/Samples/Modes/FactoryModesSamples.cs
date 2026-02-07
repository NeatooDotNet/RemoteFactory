using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Modes;

// Supporting class for modes-local-remote-methods snippet
[Factory]
public partial class EmployeeLocalRemote : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeLocalRemote() => Id = Guid.NewGuid();

    #region modes-local-remote-methods
    // No [Remote]: runs locally on client, no serialization
    [Fetch]
    public void FetchLocal(string data) => FirstName = data;

    // [Remote]: serializes and executes on server
    [Remote, Fetch]
    public async Task<bool> FetchFromServer(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        return true;
    }
    #endregion

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
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

// Supporting class for logical mode testing (full class not shown in docs)
[Factory]
public partial class EmployeeLogicalMode : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeLogicalMode() => Id = Guid.NewGuid();

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
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
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
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
    public async Task Update([Service] IEmployeeRepository repository, CancellationToken ct)
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
    public async Task Delete([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        await repository.DeleteAsync(Id, ct);
        await repository.SaveChangesAsync(ct);
    }
}

// Minimal complete setup examples as comments (Program.cs patterns)

#region modes-full-example
// Server: Full mode (default) + AddNeatooAspNetCore
// services.AddNeatooAspNetCore(options, domainAssembly);
// services.RegisterMatchingName(domainAssembly);
// app.UseNeatoo();  // /api/neatoo endpoint
#endregion

#region modes-remoteonly-example
// Client: [assembly: FactoryMode(RemoteOnly)] + Remote runtime
// services.AddNeatooRemoteFactory(NeatooFactory.Remote, options, domainAssembly);
// services.AddKeyedScoped(RemoteFactoryServices.HttpClientKey, (sp, key) =>
//     new HttpClient { BaseAddress = new Uri(serverUrl) });
#endregion

#region modes-logical-example
// Single-tier: Full mode + Logical runtime (no HTTP)
// services.AddNeatooRemoteFactory(NeatooFactory.Logical, options, domainAssembly);
// services.RegisterMatchingName(domainAssembly);
// services.AddScoped<IEmployeeRepository, EmployeeRepository>();
#endregion
