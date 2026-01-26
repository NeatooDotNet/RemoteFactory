using EmployeeManagement.Domain.Interfaces;
using EmployeeManagement.Domain.Samples.Authorization;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Attributes;

#region attributes-factory
/// <summary>
/// [Factory] marks a class for factory generation.
/// Generates IEmployeeFactory interface and EmployeeFactory implementation.
/// </summary>
[Factory]
public partial class SimpleEmployee
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";

    [Create]
    public SimpleEmployee()
    {
        Id = Guid.NewGuid();
    }
}
#endregion

#region attributes-suppressfactory
/// <summary>
/// Base class with factory generation.
/// </summary>
[Factory]
public partial class BaseEmployeeEntity
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";

    [Create]
    public BaseEmployeeEntity()
    {
        Id = Guid.NewGuid();
    }
}

/// <summary>
/// [SuppressFactory] prevents factory generation for derived class.
/// Use when base class has [Factory] but derived should not.
/// </summary>
[SuppressFactory]
public partial class InternalEmployeeEntity : BaseEmployeeEntity
{
    public string InternalCode { get; set; } = "";

    // No factory generated for this class
    // Must be created via base factory or manually
}
#endregion

#region attributes-create
/// <summary>
/// [Create] marks constructors and static methods for instance creation.
/// </summary>
[Factory]
public partial class EmployeeWithCreate
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; private set; } = "";
    public decimal InitialSalary { get; private set; }

    private EmployeeWithCreate() { }

    /// <summary>
    /// Parameterless constructor [Create].
    /// </summary>
    [Create]
    public EmployeeWithCreate(string firstName)
    {
        Id = Guid.NewGuid();
        FirstName = firstName;
    }

    /// <summary>
    /// Static factory method [Create] for parameterized creation.
    /// </summary>
    [Create]
    public static EmployeeWithCreate Create(
        string employeeNumber,
        string firstName,
        decimal initialSalary)
    {
        return new EmployeeWithCreate
        {
            Id = Guid.NewGuid(),
            EmployeeNumber = employeeNumber,
            FirstName = firstName,
            InitialSalary = initialSalary
        };
    }
}
#endregion

#region attributes-fetch
/// <summary>
/// [Fetch] marks methods that load data into existing instances.
/// </summary>
[Factory]
public partial class EmployeeWithFetch : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithFetch() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Fetch] method loads employee by ID.
    /// Returns bool - false means not found (factory returns null).
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
        IsNew = false;
        return true;
    }
}
#endregion

#region attributes-insert
/// <summary>
/// [Insert] marks methods that persist new entities.
/// </summary>
[Factory]
public partial class EmployeeWithInsert : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithInsert() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Insert] method persists a new entity.
    /// </summary>
    [Remote, Insert]
    public async Task Insert(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
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

#region attributes-update
/// <summary>
/// [Update] marks methods that persist changes to existing entities.
/// </summary>
[Factory]
public partial class EmployeeWithUpdate : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithUpdate() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// [Update] method persists changes to an existing entity.
    /// </summary>
    [Remote, Update]
    public async Task Update(
        [Service] IEmployeeRepository repository,
        CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repository.UpdateAsync(entity, ct);
        await repository.SaveChangesAsync(ct);
    }
}
#endregion

#region attributes-delete
/// <summary>
/// [Delete] marks methods that remove entities.
/// </summary>
[Factory]
public partial class EmployeeWithDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithDelete() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// [Delete] method removes the entity from persistence.
    /// </summary>
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

#region attributes-execute
/// <summary>
/// [Execute] marks methods for business operations (commands).
/// </summary>
[Factory]
public static partial class EmployeePromotion
{
    /// <summary>
    /// [Execute] method performs a business operation.
    /// Underscore prefix is removed in generated delegate name.
    /// </summary>
    [Remote, Execute]
    private static async Task<bool> _PromoteEmployee(
        Guid employeeId,
        string newPosition,
        decimal salaryIncrease,
        [Service] IEmployeeRepository repository,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var employee = await repository.GetByIdAsync(employeeId, ct);
        if (employee == null) return false;

        var oldPosition = employee.Position;
        employee.Position = newPosition;
        employee.SalaryAmount += salaryIncrease;

        await repository.UpdateAsync(employee, ct);
        await repository.SaveChangesAsync(ct);

        await auditLog.LogAsync("Promotion", employeeId, "Employee",
            $"Promoted from {oldPosition} to {newPosition}", ct);

        return true;
    }
}
#endregion

#region attributes-event
/// <summary>
/// [Event] marks methods for fire-and-forget domain events.
/// CancellationToken is required as the last parameter.
/// </summary>
[Factory]
public partial class EmployeeEvents
{
    /// <summary>
    /// [Event] method runs fire-and-forget.
    /// CancellationToken must be the last parameter.
    /// </summary>
    [Event]
    public async Task NotifyManager(
        Guid employeeId,
        string message,
        [Service] IEmailService emailService,
        CancellationToken ct)
    {
        await emailService.SendAsync(
            "manager@company.com",
            "Employee Update",
            $"Employee {employeeId}: {message}",
            ct);
    }
}
#endregion

#region attributes-remote
/// <summary>
/// [Remote] marks methods that execute on the server.
/// </summary>
[Factory]
public partial class EmployeeRemoteExecution : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Local execution - no [Remote].
    /// Runs on client without network call.
    /// </summary>
    [Create]
    public EmployeeRemoteExecution()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// [Remote, Fetch] - executes on server.
    /// Request serialized, sent via HTTP, response deserialized.
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
        IsNew = false;
        return true;
    }
}
#endregion

#region attributes-service
/// <summary>
/// [Service] marks parameters for dependency injection.
/// </summary>
[Factory]
public partial class EmployeeServiceParams : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServiceParams() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Mix of value parameters (serialized) and [Service] parameters (injected).
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,                          // Value: serialized to server
        [Service] IEmployeeRepository repository, // Service: resolved from server DI
        [Service] IAuditLogService auditLog,      // Service: resolved from server DI
        CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;

        await auditLog.LogAsync("Fetch", employeeId, "Employee", "Loaded", ct);
        return true;
    }
}
#endregion

#region attributes-authorizefactory-generic
/// <summary>
/// [AuthorizeFactory<T>] applies custom authorization to the factory.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class AuthorizedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedEmployee() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }
}
#endregion

#region attributes-authorizefactory-interface
/// <summary>
/// [AuthorizeFactory] on interface methods defines authorization checks.
/// </summary>
public interface IDocumentAuthorization
{
    /// <summary>
    /// Check for Create operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    /// <summary>
    /// Check for Read operations (Fetch).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    /// <summary>
    /// Check for Write operations (Insert, Update, Delete).
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    bool CanWrite();
}
#endregion

#region attributes-authorizefactory-method
/// <summary>
/// Method-level [AspAuthorize] adds ADDITIONAL authorization on top of class-level auth.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]  // Class-level: runs first
public partial class EmployeeWithMethodAuth2 : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithMethodAuth2() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Delete requires BOTH:
    /// 1. Class-level [AuthorizeFactory<IEmployeeAuthorization>] CanWrite check
    /// 2. Method-level [AspAuthorize] HRManager role check
    /// Both must pass for operation to succeed.
    /// </summary>
    [Remote, Delete]
    [AspAuthorize(Roles = "HRManager")]  // Method-level: runs after class-level
    public async Task Delete(
        [Service] IEmployeeRepository repo,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        await auditLog.LogAsync("Terminate", Id, "Employee", "Deleted", ct);
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region attributes-aspauthorize
/// <summary>
/// [AspAuthorize] applies ASP.NET Core authorization policies.
/// </summary>
[Factory]
public partial class PolicyProtectedEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public PolicyProtectedEmployee() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Policy-based authorization via constructor parameter.
    /// </summary>
    [Remote, Fetch]
    [AspAuthorize("RequireEmployee")]  // Policy name via constructor
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Role-based authorization via Roles property.
    /// </summary>
    [Remote, Insert]
    [AspAuthorize(Roles = "HR,Manager")]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region attributes-multiple-operations
/// <summary>
/// Multiple operation attributes on one method (upsert pattern).
/// </summary>
[Factory]
public partial class SettingSample : IFactorySaveMeta
{
    public string Key { get; private set; } = "";
    public string Value { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SettingSample(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Both [Insert] and [Update] point to same method.
    /// Generated factory has both Insert() and Update() methods.
    /// </summary>
    [Remote, Insert, Update]
    public Task Upsert(CancellationToken ct)
    {
        // Handle both insert and update cases
        IsNew = false;
        return Task.CompletedTask;
    }
}
#endregion

#region attributes-remote-operation
/// <summary>
/// [Remote] combined with operation attributes.
/// </summary>
[Factory]
public partial class EmployeeRemoteOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeRemoteOps() { Id = Guid.NewGuid(); }

    /// <summary>
    /// [Remote, Fetch] - server-side data loading.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// [Remote, Insert] - server-side persistence.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region attributes-authorization-operation
/// <summary>
/// Authorization combined with operation attributes.
/// </summary>
[Factory]
[AuthorizeFactory<IEmployeeAuthorization>]
public partial class EmployeeAuthOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeAuthOps() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Authorization checked before Fetch executes.
    /// IEmployeeAuthorization.CanRead() must return true.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Authorization checked before Insert executes.
    /// IEmployeeAuthorization.CanWrite() must return true.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region attributes-inheritance
/// <summary>
/// Demonstrates attribute inheritance behavior.
/// </summary>
[Factory]   // Inherited: Yes
public partial class BaseEntityWithFactory
{
    public Guid Id { get; protected set; }
    public string Name { get; set; } = "";

    [Create]    // Inherited: No - must be redeclared
    public BaseEntityWithFactory()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]  // [Remote] Inherited: Yes
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        return true;
    }
}

/// <summary>
/// Derived class inherits [Factory] and [Remote] but not [Create].
/// </summary>
public partial class DerivedWithInheritedFactory : BaseEntityWithFactory
{
    public string DerivedProperty { get; set; } = "";

    // Inherits: [Factory] from base
    // Inherits: [Remote] from base.Fetch()
    // Does NOT inherit: [Create] - must redeclare if needed

    [Create]  // Must redeclare for this class to have a Create
    public DerivedWithInheritedFactory() : base()
    {
        DerivedProperty = "Default";
    }
}
#endregion

#region attributes-pattern-crud
/// <summary>
/// Complete CRUD entity pattern with all operations.
/// </summary>
[Factory]
public partial class CrudEmployee : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public string Position { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public CrudEmployee()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        Position = entity.Position;
        Salary = entity.SalaryAmount;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = Email, DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = Email, DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region attributes-pattern-readonly
/// <summary>
/// Read-only entity pattern with only Create and Fetch.
/// </summary>
[Factory]
public partial class EmployeeReport
{
    public Guid Id { get; private set; }
    public string EmployeeName { get; private set; } = "";
    public string Department { get; private set; } = "";
    public decimal TotalSalary { get; private set; }

    [Create]
    public EmployeeReport()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Read-only: Only Fetch operation defined.
    /// No Insert, Update, or Delete.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(
        Guid employeeId,
        [Service] IEmployeeRepository repo,
        CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(employeeId, ct);
        if (entity == null) return false;

        Id = entity.Id;
        EmployeeName = $"{entity.FirstName} {entity.LastName}";
        Department = entity.DepartmentId.ToString();
        TotalSalary = entity.SalaryAmount;
        return true;
    }

    // No Insert, Update, Delete - this is a read-only projection
}
#endregion

#region attributes-pattern-command
/// <summary>
/// Command handler pattern using static class with [Execute].
/// </summary>
[Factory]
public static partial class TransferEmployeeCmd
{
    /// <summary>
    /// Command pattern: static class with [Execute] method.
    /// Underscore prefix removed in generated delegate.
    /// </summary>
    [Remote, Execute]
    private static async Task<CommandResult> _Execute(
        Guid employeeId,
        Guid newDepartmentId,
        string reason,
        [Service] IEmployeeRepository employeeRepo,
        [Service] IDepartmentRepository deptRepo,
        [Service] IAuditLogService auditLog,
        CancellationToken ct)
    {
        var employee = await employeeRepo.GetByIdAsync(employeeId, ct);
        if (employee == null)
            return new CommandResult(false, "Employee not found");

        var newDept = await deptRepo.GetByIdAsync(newDepartmentId, ct);
        if (newDept == null)
            return new CommandResult(false, "Department not found");

        var oldDeptId = employee.DepartmentId;
        employee.DepartmentId = newDepartmentId;

        await employeeRepo.UpdateAsync(employee, ct);
        await employeeRepo.SaveChangesAsync(ct);

        await auditLog.LogAsync("Transfer", employeeId, "Employee",
            $"Transferred from {oldDeptId} to {newDepartmentId}. Reason: {reason}", ct);

        return new CommandResult(true, $"Transferred to {newDept.Name}");
    }
}

public record CommandResult(bool Success, string Message);
#endregion
