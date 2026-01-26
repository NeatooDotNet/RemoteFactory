using System.ComponentModel.DataAnnotations;
using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Save;

#region save-ifactorysavemeta
/// <summary>
/// Employee entity implementing IFactorySaveMeta for state tracking.
/// IsNew and IsDeleted determine which save operation to execute.
/// </summary>
[Factory]
public partial class EmployeeSaveState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";

    /// <summary>
    /// True for new entities not yet persisted.
    /// Set to true in constructor, false after Fetch or Insert.
    /// </summary>
    public bool IsNew { get; private set; } = true;

    /// <summary>
    /// True for entities marked for deletion.
    /// Set by application code before calling Save().
    /// </summary>
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeSaveState()
    {
        Id = Guid.NewGuid();
        // IsNew defaults to true for new entities
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        IsNew = false; // Fetched entities are not new
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false; // No longer new after insert
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
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

#region save-write-operations
/// <summary>
/// Complete Insert, Update, and Delete operations with repository patterns.
/// </summary>
[Factory]
public partial class EmployeeWriteOps : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Position { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWriteOps() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Position = entity.Position;
        Salary = entity.SalaryAmount;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Insert: Persists a new entity. Sets IsNew = false after success.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    /// <summary>
    /// Update: Persists changes to an existing entity.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = $"{FirstName.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = Position,
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    /// <summary>
    /// Delete: Removes the entity from persistence.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region save-state-new
/// <summary>
/// Demonstrates Create initializing IsNew = true state.
/// </summary>
[Factory]
public partial class EmployeeNewState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Create sets IsNew = true for new entities.
    /// Workflow: Create() -> modify -> Save() -> Insert
    /// </summary>
    [Create]
    public EmployeeNewState()
    {
        Id = Guid.NewGuid();
        // IsNew is true by default - new entity
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region save-state-fetch
/// <summary>
/// Demonstrates Fetch setting IsNew = false for existing entities.
/// </summary>
[Factory]
public partial class EmployeeFetchState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeFetchState() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Fetch sets IsNew = false after loading.
    /// Workflow: Fetch() -> modify -> Save() -> Update
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false; // Existing entity - not new
        return true;
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region save-state-delete
/// <summary>
/// Demonstrates deletion workflow with IsDeleted = true.
/// </summary>
[Factory]
public partial class EmployeeDeleteState : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeDeleteState() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Mark for deletion and save.
    /// Workflow: Fetch() -> entity.IsDeleted = true -> Save() -> Delete
    /// </summary>
    public void MarkDeleted()
    {
        IsDeleted = true;
    }

    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region save-complete-example
/// <summary>
/// Complete Department aggregate with all CRUD operations and IFactorySaveMeta.
/// </summary>
[Factory]
public partial class DepartmentSample : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public string Code { get; set; } = "";
    public Guid? ManagerId { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public DepartmentSample()
    {
        Id = Guid.NewGuid();
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        Name = entity.Name;
        Code = entity.Code;
        ManagerId = entity.ManagerId;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var entity = new DepartmentEntity { Id = Id, Name = Name, Code = Code, ManagerId = ManagerId };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }

    [Remote, Update]
    public async Task Update([Service] IDepartmentRepository repo, CancellationToken ct)
    {
        var entity = new DepartmentEntity { Id = Id, Name = Name, Code = Code, ManagerId = ManagerId };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Delete]
    public async Task Delete([Service] IDepartmentRepository repo, CancellationToken ct)
    {
        await repo.DeleteAsync(Id, ct);
        await repo.SaveChangesAsync(ct);
    }
}
#endregion

#region save-partial-methods
/// <summary>
/// Immutable audit log entry with only Insert (create-once pattern).
/// No Update or Delete operations defined.
/// </summary>
[Factory]
public partial class AuditLogEntry : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public DateTime Timestamp { get; private set; }
    public string Action { get; private set; } = "";
    public string EntityType { get; private set; } = "";
    public Guid EntityId { get; private set; }
    public string Details { get; private set; } = "";

    // IFactorySaveMeta - Insert only
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }
    // Note: Setting IsDeleted = true throws NotImplementedException
    // because Delete operation is not defined

    [Create]
    public static AuditLogEntry Create(string action, string entityType, Guid entityId, string details)
    {
        return new AuditLogEntry
        {
            Id = Guid.NewGuid(),
            Timestamp = DateTime.UtcNow,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details
        };
    }

    /// <summary>
    /// Only Insert is defined - audit logs are immutable.
    /// Save() routes new entities to Insert.
    /// Update and Delete are not supported.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IAuditLogService auditLog, CancellationToken ct)
    {
        await auditLog.LogAsync(Action, EntityId, EntityType, Details, ct);
        IsNew = false;
    }
}
#endregion

#region save-no-delete
/// <summary>
/// Entity with Insert and Update but no Delete support.
/// </summary>
[Factory]
public partial class EmployeeNoDelete : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;

    // Note: If IsDeleted is set to true and Save() is called,
    // it throws NotImplementedException because Delete is not defined
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeNoDelete() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false;
        return true;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
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
            Id = Id, FirstName = Name, LastName = "",
            Email = $"{Name.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "Updated",
            SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.UpdateAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    // No [Delete] operation - Save() throws if IsDeleted = true
}
#endregion

#region save-validation
/// <summary>
/// Employee with data annotation validation attributes.
/// </summary>
[Factory]
public partial class EmployeeValidated : IFactorySaveMeta
{
    public Guid Id { get; private set; }

    [Required(ErrorMessage = "First name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string FirstName { get; set; } = "";

    [Required(ErrorMessage = "Last name is required")]
    [StringLength(100, MinimumLength = 1)]
    public string LastName { get; set; } = "";

    [Required(ErrorMessage = "Email is required")]
    [EmailAddress(ErrorMessage = "Invalid email format")]
    public string Email { get; set; } = "";

    [Range(0, 10000000, ErrorMessage = "Salary must be between 0 and 10,000,000")]
    public decimal Salary { get; set; }

    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeValidated() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = LastName,
            Email = Email, DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region save-validation-throw
/// <summary>
/// Employee with server-side validation in Insert method.
/// </summary>
[Factory]
public partial class EmployeeServerValidated : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; set; } = "";
    public string FirstName { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeServerValidated() { Id = Guid.NewGuid(); }

    /// <summary>
    /// Server-side validation before persisting.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        // Validate employee number format
        if (string.IsNullOrEmpty(EmployeeNumber) || !EmployeeNumber.StartsWith('E'))
        {
            throw new ArgumentException(
                "Employee number must start with 'E'",
                nameof(EmployeeNumber));
        }

        // Validate salary is reasonable
        if (Salary < 0)
        {
            throw new ArgumentException(
                "Salary cannot be negative",
                nameof(Salary));
        }

        if (Salary > 10_000_000)
        {
            throw new ArgumentException(
                "Salary exceeds maximum allowed value",
                nameof(Salary));
        }

        var entity = new EmployeeEntity
        {
            Id = Id, FirstName = FirstName, LastName = "",
            Email = $"{EmployeeNumber.ToLowerInvariant()}@example.com",
            DepartmentId = Guid.Empty, Position = "New",
            SalaryAmount = Salary, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
        IsNew = false;
    }
}
#endregion

#region save-optimistic-concurrency
/// <summary>
/// Demonstrates exception handling for optimistic concurrency.
/// RemoteFactory properly serializes exceptions across the client-server boundary.
/// </summary>
[Factory]
public partial class EmployeeWithConcurrency : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public int Version { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeWithConcurrency() { Id = Guid.NewGuid(); }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(id, ct);
        if (entity == null) return false;
        Id = entity.Id;
        Name = entity.FirstName;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Update with concurrency handling.
    /// Exceptions are serialized and propagate to clients correctly.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        try
        {
            var entity = new EmployeeEntity
            {
                Id = Id, FirstName = Name, LastName = "",
                Email = $"{Name.ToLowerInvariant()}@example.com",
                DepartmentId = Guid.Empty, Position = "Updated",
                SalaryAmount = 0, SalaryCurrency = "USD", HireDate = DateTime.UtcNow
            };
            await repo.UpdateAsync(entity, ct);
            await repo.SaveChangesAsync(ct);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("concurrency"))
        {
            // Re-throw as a domain exception that clients understand
            throw new ConcurrencyException(
                "The employee was modified by another user. Please refresh and try again.",
                ex);
        }
    }
}

/// <summary>
/// Domain exception for concurrency conflicts.
/// </summary>
public class ConcurrencyException : Exception
{
    public ConcurrencyException(string message, Exception? inner = null)
        : base(message, inner) { }
}
#endregion
