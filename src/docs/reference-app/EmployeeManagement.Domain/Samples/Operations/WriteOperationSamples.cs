using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Operations;

#region operations-insert
/// <summary>
/// Employee aggregate demonstrating Insert operation.
/// </summary>
[Factory]
public partial class EmployeeInsertDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    /// <summary>
    /// Creates a new Employee with generated ID and employee number.
    /// </summary>
    [Create]
    public EmployeeInsertDemo()
    {
        Id = Guid.NewGuid();
        EmployeeNumber = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{Id.ToString()[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Inserts a new Employee into the repository.
    /// </summary>
    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repository)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            SalaryAmount = Salary,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };

        await repository.AddAsync(entity);
        await repository.SaveChangesAsync();
        IsNew = false;
    }
}
#endregion

#region operations-update
/// <summary>
/// Employee aggregate demonstrating Update operation.
/// </summary>
[Factory]
public partial class EmployeeUpdateDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }
    public string Department { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeUpdateDemo()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            return false;

        Id = entity.Id;
        EmployeeNumber = $"EMP-{entity.Id.ToString()[..8].ToUpperInvariant()}";
        FirstName = entity.FirstName;
        LastName = entity.LastName;
        Salary = entity.SalaryAmount;
        Department = entity.Position;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Updates an existing Employee in the repository.
    /// </summary>
    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(Id);
        if (entity == null)
            throw new InvalidOperationException($"Employee with ID {Id} not found.");

        // Update mutable properties
        entity.FirstName = FirstName;
        entity.LastName = LastName;
        entity.SalaryAmount = Salary;
        entity.Position = Department;

        await repository.UpdateAsync(entity);
        await repository.SaveChangesAsync();
    }
}
#endregion

#region operations-delete
/// <summary>
/// Employee aggregate demonstrating Delete operation.
/// </summary>
[Factory]
public partial class EmployeeDeleteDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeDeleteDemo()
    {
        Id = Guid.NewGuid();
    }

    /// <summary>
    /// Fetches an existing Employee by ID.
    /// </summary>
    [Remote, Fetch]
    public async Task<bool> Fetch(Guid employeeId, [Service] IEmployeeRepository repository)
    {
        var entity = await repository.GetByIdAsync(employeeId);
        if (entity == null)
            return false;

        Id = entity.Id;
        IsNew = false;
        return true;
    }

    /// <summary>
    /// Deletes the Employee from the repository.
    /// </summary>
    [Remote, Delete]
    public async Task Delete([Service] IEmployeeRepository repository)
    {
        await repository.DeleteAsync(Id);
        await repository.SaveChangesAsync();
    }
}
#endregion

#region operations-insert-update
/// <summary>
/// Employee aggregate demonstrating combined Insert/Update (upsert) pattern.
/// </summary>
[Factory]
public partial class EmployeeUpsertDemo : IFactorySaveMeta
{
    public Guid Id { get; private set; }
    public string EmployeeNumber { get; private set; } = "";
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public decimal Salary { get; set; }
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public EmployeeUpsertDemo()
    {
        Id = Guid.NewGuid();
        EmployeeNumber = $"EMP-{DateTime.UtcNow:yyyyMMdd}-{Id.ToString()[..8].ToUpperInvariant()}";
    }

    /// <summary>
    /// Upserts the Employee - inserts if new, updates if existing.
    /// </summary>
    [Remote, Insert, Update]
    public async Task Upsert([Service] IEmployeeRepository repository)
    {
        var existing = await repository.GetByIdAsync(Id);

        if (existing == null)
        {
            // Insert new entity
            var entity = new EmployeeEntity
            {
                Id = Id,
                FirstName = FirstName,
                LastName = LastName,
                SalaryAmount = Salary,
                SalaryCurrency = "USD",
                HireDate = DateTime.UtcNow
            };
            await repository.AddAsync(entity);
        }
        else
        {
            // Update existing entity
            existing.FirstName = FirstName;
            existing.LastName = LastName;
            existing.SalaryAmount = Salary;
            await repository.UpdateAsync(existing);
        }

        await repository.SaveChangesAsync();
        IsNew = false;
    }
}
#endregion
