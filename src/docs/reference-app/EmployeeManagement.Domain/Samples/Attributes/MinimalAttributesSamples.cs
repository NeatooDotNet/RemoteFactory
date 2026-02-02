using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.Samples.Authorization;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes.Minimal;

// Minimal samples for attributes-reference.md documentation
// Each snippet is 2-5 lines focused on essential API usage

#region attributes-factory
[Factory]  // Enables factory generation
public partial class MinimalEmployee
{
    [Create]
    public MinimalEmployee() { }
}
#endregion

#region attributes-suppressfactory
[Factory]
public partial class BaseEntity { }

[SuppressFactory]  // Prevents factory generation on derived class
public partial class InternalEntity : BaseEntity { }
#endregion

#region attributes-create
[Factory]
public partial class EmployeeCreate
{
    [Create]  // Constructor-based creation
    public EmployeeCreate(string name) { Name = name; }

    [Create]  // Static factory method - different signature
    public static EmployeeCreate Create(string name, decimal salary) => new(name) { Salary = salary };

    public string Name { get; }
    public decimal Salary { get; private set; }
}
#endregion

#region attributes-fetch
[Factory]
public partial class EmployeeFetch
{
    [Remote, Fetch]  // Returns bool: false = not found (factory returns null)
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);
}
#endregion

#region attributes-insert
[Factory]
public partial class EmployeeInsert : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Insert]  // Persists new entity
    public Task Insert([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
#endregion

#region attributes-update
[Factory]
public partial class EmployeeUpdate : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Update]  // Persists changes to existing entity
    public Task Update([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
#endregion

#region attributes-delete
[Factory]
public partial class EmployeeDelete : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Delete]  // Removes entity from persistence
    public Task Delete([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
#endregion

#region attributes-execute
[Factory]
public static partial class PromoteCommand
{
    [Remote, Execute]  // Business operation - underscore prefix removed in delegate name
    private static Task<bool> _Execute(Guid employeeId, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);
}
#endregion

#region attributes-event
[Factory]
public partial class EmployeeEventsMinimal
{
    [Event]  // Fire-and-forget - CancellationToken must be last parameter
    public Task NotifyManager(Guid employeeId, [Service] IEmailService email, CancellationToken ct)
        => email.SendAsync("mgr@co.com", "Update", $"Employee {employeeId}", ct);
}
#endregion

#region attributes-remote
[Factory]
public partial class EmployeeRemote
{
    [Create]  // No [Remote] - executes locally without network call
    public EmployeeRemote() { }

    [Remote, Fetch]  // [Remote] - serializes request, sends via HTTP, deserializes response
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct) => Task.FromResult(true);
}
#endregion

#region attributes-service
[Factory]
public partial class EmployeeWithService
{
    [Remote, Fetch]
    public Task<bool> Fetch(
        Guid employeeId,                          // Value parameter: serialized to server
        [Service] IEmployeeRepository repository, // [Service]: resolved from DI container
        CancellationToken ct) => Task.FromResult(true);
}
#endregion

#region attributes-authorizefactory-generic
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Class-level authorization
public partial class AuthEmployee { }
#endregion

#region attributes-authorizefactory-interface
public interface IMinimalDocAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]   // Maps to Fetch operations
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]  // Maps to Insert, Update, Delete
    bool CanWrite();
}
#endregion

#region attributes-authorizefactory-method
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Class-level: checked first
public partial class MethodAuthEmployee : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Delete]
    [AspAuthorize(Roles = "HRManager")]  // Method-level: additional check after class-level
    public Task Delete([Service] IEmployeeRepository repo, CancellationToken ct) => Task.CompletedTask;
}
#endregion

#region attributes-aspauthorize
[Factory]
public partial class PolicyEmployee : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Fetch]
    [AspAuthorize("RequireEmployee")]  // Policy-based authorization
    public Task<bool> FetchWithPolicy(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(true);

    [Remote, Insert]
    [AspAuthorize(Roles = "HR,Manager")]  // Role-based authorization
    public Task InsertWithRoles([Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.CompletedTask;
}
#endregion

#region attributes-multiple-operations
[Factory]
public partial class UpsertSetting : IFactorySaveMeta
{
    public bool IsNew { get; private set; }
    public bool IsDeleted { get; set; }

    [Remote, Insert, Update]  // Both operations point to same method
    public Task Upsert(CancellationToken ct) => Task.CompletedTask;
}
#endregion

#region attributes-remote-operation
[Factory]
public partial class RemoteOps : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Remote, Fetch]   // Server-side data loading
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct) => Task.FromResult(true);

    [Remote, Insert]  // Server-side persistence
    public Task Insert([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;
}
#endregion

#region attributes-authorization-operation
public interface IOpAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]  // Combined flags
    bool CanCreateAndRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}
#endregion

#region attributes-inheritance
[Factory]   // Inherited: Yes
public partial class BaseWithFactory
{
    [Create]    // Inherited: No
    public BaseWithFactory() { }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct) => Task.FromResult(true);
}

public partial class DerivedEntity : BaseWithFactory
{
    // Inherits [Factory] and [Remote] from base
    // Does NOT inherit [Create] - must redeclare
    [Create]
    public DerivedEntity() : base() { }
}
#endregion

#region attributes-pattern-crud
[Factory]
public partial class CrudEntity : IFactorySaveMeta
{
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public CrudEntity() { }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct)
        => Task.FromResult(true);

    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;

    [Remote, Update]
    public Task Update([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;

    [Remote, Delete]
    public Task Delete([Service] IEmployeeRepository r, CancellationToken ct) => Task.CompletedTask;
}
#endregion

#region attributes-pattern-readonly
[Factory]
public partial class ReadOnlyEntity
{
    [Create]
    public ReadOnlyEntity() { }

    [Remote, Fetch]
    public Task<bool> Fetch(Guid id, [Service] IEmployeeRepository r, CancellationToken ct)
        => Task.FromResult(true);
    // No Insert, Update, Delete - read-only projection
}
#endregion

#region attributes-pattern-command
[Factory]
public static partial class TransferCommand
{
    [Remote, Execute]  // Static class with [Execute] for command pattern
    private static Task<TransferCommandResult> _Execute(
        Guid employeeId, Guid newDeptId, [Service] IEmployeeRepository repo, CancellationToken ct)
        => Task.FromResult(new TransferCommandResult(true, "Transferred"));
}
public record TransferCommandResult(bool Success, string Message);
#endregion

#region attributes-pattern-event
[Factory]
public partial class LifecycleEvents
{
    [Event]  // Fire-and-forget domain events
    public Task OnEmployeeHired(Guid id, string email, [Service] IEmailService svc, CancellationToken ct)
        => svc.SendAsync(email, "Welcome!", $"ID: {id}", ct);

    [Event]
    public Task OnEmployeePromoted(Guid id, string title, [Service] IEmailService svc, CancellationToken ct)
        => svc.SendAsync("hr@co.com", "Promotion", $"{id} to {title}", ct);
}
#endregion
