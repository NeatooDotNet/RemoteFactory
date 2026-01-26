using EmployeeManagement.Domain.Interfaces;
using System.Collections.Concurrent;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of IEmployeeRepository for demonstration.
/// </summary>
public class InMemoryEmployeeRepository : IEmployeeRepository
{
    private static readonly ConcurrentDictionary<Guid, EmployeeEntity> Employees = new();

    public Task<EmployeeEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(Employees.GetValueOrDefault(id));
    }

    public Task<List<EmployeeEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Employees.Values.ToList());
    }

    public Task<List<EmployeeEntity>> GetByDepartmentIdAsync(Guid departmentId, CancellationToken ct = default)
    {
        var result = Employees.Values
            .Where(e => e.DepartmentId == departmentId)
            .ToList();
        return Task.FromResult(result);
    }

    public Task AddAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        Employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(EmployeeEntity entity, CancellationToken ct = default)
    {
        Employees[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        Employees.TryRemove(id, out _);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        // No-op for in-memory storage
        return Task.CompletedTask;
    }

    /// <summary>
    /// Clears all data. Useful for testing.
    /// </summary>
    public static void Clear()
    {
        Employees.Clear();
    }
}
