using EmployeeManagement.Domain.Interfaces;
using System.Collections.Concurrent;

namespace EmployeeManagement.Infrastructure.Repositories;

/// <summary>
/// In-memory implementation of IDepartmentRepository for demonstration.
/// </summary>
public class InMemoryDepartmentRepository : IDepartmentRepository
{
    private static readonly ConcurrentDictionary<Guid, DepartmentEntity> Departments = new();

    public Task<DepartmentEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        return Task.FromResult(Departments.GetValueOrDefault(id));
    }

    public Task<List<DepartmentEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(Departments.Values.ToList());
    }

    public Task AddAsync(DepartmentEntity entity, CancellationToken ct = default)
    {
        Departments[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(DepartmentEntity entity, CancellationToken ct = default)
    {
        Departments[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        Departments.TryRemove(id, out _);
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
        Departments.Clear();
    }
}
