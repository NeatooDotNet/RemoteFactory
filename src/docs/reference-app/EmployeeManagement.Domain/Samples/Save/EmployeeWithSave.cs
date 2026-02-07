using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Save;

// Full working implementation for Save operation tests
// Lifecycle hook snippets are now in LifecycleHooksSamples.cs
[Factory]
public partial class EmployeeWithSave : IFactorySaveMeta, IFactoryOnStart, IFactoryOnComplete, IFactoryOnCancelled
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public string Email { get; set; } = "";
    public Guid DepartmentId { get; set; }
    public string Position { get; set; } = "";
    public decimal Salary { get; set; }
    public int Version { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    public bool OnStartCalled { get; private set; }
    public bool OnCompleteCalled { get; private set; }
    public bool OnCancelledCalled { get; private set; }
    public FactoryOperation? LastOperation { get; private set; }

    [Create]
    public EmployeeWithSave()
    {
        Id = Guid.NewGuid();
        Version = 1;
    }

    [Remote, Fetch]
    public async Task<bool> Fetch(Guid id, [Service] IEmployeeRepository repository, CancellationToken ct)
    {
        var entity = await repository.GetByIdAsync(id, ct);
        if (entity == null) return false;

        Id = entity.Id;
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Email = entity.Email;
        DepartmentId = entity.DepartmentId;
        Position = entity.Position;
        Salary = entity.SalaryAmount;
        IsNew = false;
        return true;
    }

    public void FactoryStart(FactoryOperation factoryOperation)
    {
        OnStartCalled = true;
        LastOperation = factoryOperation;

        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            if (string.IsNullOrWhiteSpace(FirstName))
                throw new InvalidOperationException("FirstName is required");
            if (string.IsNullOrWhiteSpace(LastName))
                throw new InvalidOperationException("LastName is required");
        }
    }

    public void FactoryComplete(FactoryOperation factoryOperation)
    {
        OnCompleteCalled = true;
        LastOperation = factoryOperation;

        if (factoryOperation == FactoryOperation.Insert ||
            factoryOperation == FactoryOperation.Update)
        {
            Version++;
        }
    }

    public void FactoryCancelled(FactoryOperation factoryOperation)
    {
        OnCancelledCalled = true;
        LastOperation = factoryOperation;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            Position = Position,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
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
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = Email,
            DepartmentId = DepartmentId,
            Position = Position,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
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
