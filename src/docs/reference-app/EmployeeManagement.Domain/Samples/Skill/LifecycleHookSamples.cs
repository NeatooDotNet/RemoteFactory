using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;
using System.Globalization;

namespace EmployeeManagement.Domain.Samples.Skill;

#region skill-lifecycle-hooks
[Factory]
public partial class SkillEmployeeWithLifecycle : IFactorySaveMeta, IFactoryOnStartAsync, IFactoryOnCompleteAsync
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public SkillEmployeeWithLifecycle()
    {
        Id = Guid.NewGuid();
    }

    public Task FactoryStartAsync(FactoryOperation factoryOperation)
    {
        // Before operation - validation, logging
        return Task.CompletedTask;
    }

    public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        // After operation - cleanup, state reset
        if (factoryOperation == FactoryOperation.Insert)
            IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Insert]
    public async Task Insert([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = new EmployeeEntity
        {
            Id = Id,
            FirstName = FirstName,
            LastName = LastName,
            Email = string.Format(
                CultureInfo.InvariantCulture,
                "{0}.{1}@example.com",
                FirstName.ToLowerInvariant(),
                LastName.ToLowerInvariant()),
            Position = "New Employee",
            SalaryAmount = 0,
            SalaryCurrency = "USD",
            HireDate = DateTime.UtcNow
        };
        await repo.AddAsync(entity, ct);
        await repo.SaveChangesAsync(ct);
    }

    [Remote, Update]
    public async Task Update([Service] IEmployeeRepository repo, CancellationToken ct)
    {
        var entity = await repo.GetByIdAsync(Id, ct);
        if (entity != null)
        {
            entity.FirstName = FirstName;
            entity.LastName = LastName;
            await repo.UpdateAsync(entity, ct);
            await repo.SaveChangesAsync(ct);
        }
    }
}
#endregion
