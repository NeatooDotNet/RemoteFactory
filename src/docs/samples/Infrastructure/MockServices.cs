using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Neatoo.RemoteFactory.Samples.Infrastructure;

/// <summary>
/// Mock repository interface for documentation samples.
/// </summary>
public interface IPersonRepository
{
    Task<PersonEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<PersonEntity>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(PersonEntity entity, CancellationToken ct = default);
    Task UpdateAsync(PersonEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Mock repository implementation for documentation samples.
/// </summary>
public class PersonRepository : IPersonRepository
{
    private readonly Dictionary<Guid, PersonEntity> _entities = new();

    public Task<PersonEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _entities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task<List<PersonEntity>> GetAllAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_entities.Values.ToList());
    }

    public Task AddAsync(PersonEntity entity, CancellationToken ct = default)
    {
        _entities[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(PersonEntity entity, CancellationToken ct = default)
    {
        _entities[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _entities.Remove(id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Persistence entity for Person domain model.
/// </summary>
public class PersonEntity
{
    public Guid Id { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
    public byte[]? RowVersion { get; set; }
}

/// <summary>
/// Mock order repository for documentation samples.
/// </summary>
public interface IOrderRepository
{
    Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task AddAsync(OrderEntity entity, CancellationToken ct = default);
    Task UpdateAsync(OrderEntity entity, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}

/// <summary>
/// Mock order repository implementation.
/// </summary>
public class OrderRepository : IOrderRepository
{
    private readonly Dictionary<Guid, OrderEntity> _entities = new();

    public Task<OrderEntity?> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        _entities.TryGetValue(id, out var entity);
        return Task.FromResult(entity);
    }

    public Task AddAsync(OrderEntity entity, CancellationToken ct = default)
    {
        _entities[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task UpdateAsync(OrderEntity entity, CancellationToken ct = default)
    {
        _entities[entity.Id] = entity;
        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        _entities.Remove(id);
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken ct = default)
    {
        return Task.CompletedTask;
    }
}

/// <summary>
/// Persistence entity for Order domain model.
/// </summary>
public class OrderEntity
{
    public Guid Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public Guid CustomerId { get; set; }
    public decimal Total { get; set; }
    public string Status { get; set; } = "Pending";
    public DateTime Created { get; set; }
    public DateTime Modified { get; set; }
}

/// <summary>
/// User context interface for authorization samples.
/// </summary>
public interface IUserContext
{
    Guid UserId { get; }
    string Username { get; }
    string[] Roles { get; }
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

/// <summary>
/// Mock user context implementation.
/// </summary>
public class MockUserContext : IUserContext
{
    public Guid UserId { get; set; } = Guid.NewGuid();
    public string Username { get; set; } = "testuser";
    public string[] Roles { get; set; } = ["User"];
    public bool IsAuthenticated { get; set; } = true;

    public bool IsInRole(string role) => Roles.Contains(role, StringComparer.OrdinalIgnoreCase);
}

/// <summary>
/// Mock email service for event samples.
/// </summary>
public interface IEmailService
{
    Task SendAsync(string to, string subject, string body, CancellationToken ct = default);
}

/// <summary>
/// Mock email service implementation.
/// </summary>
public class MockEmailService : IEmailService
{
    public List<(string To, string Subject, string Body)> SentEmails { get; } = new();

    public Task SendAsync(string to, string subject, string body, CancellationToken ct = default)
    {
        SentEmails.Add((to, subject, body));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Mock audit log service for event samples.
/// </summary>
public interface IAuditLogService
{
    Task LogAsync(string action, Guid entityId, string entityType, string details, CancellationToken ct = default);
}

/// <summary>
/// Mock audit log service implementation.
/// </summary>
public class MockAuditLogService : IAuditLogService
{
    public List<(string Action, Guid EntityId, string EntityType, string Details)> Logs { get; } = new();

    public Task LogAsync(string action, Guid entityId, string entityType, string details, CancellationToken ct = default)
    {
        Logs.Add((action, entityId, entityType, details));
        return Task.CompletedTask;
    }
}

/// <summary>
/// Test implementation of IHostApplicationLifetime.
/// </summary>
public sealed class TestHostApplicationLifetime : IHostApplicationLifetime
{
    private readonly CancellationTokenSource _startedSource = new();
    private readonly CancellationTokenSource _stoppingSource = new();
    private readonly CancellationTokenSource _stoppedSource = new();

    public CancellationToken ApplicationStarted => _startedSource.Token;
    public CancellationToken ApplicationStopping => _stoppingSource.Token;
    public CancellationToken ApplicationStopped => _stoppedSource.Token;

    public void StopApplication() => _stoppingSource.Cancel();
}

/// <summary>
/// Null logger factory for testing.
/// </summary>
public class NullLoggerFactory : ILoggerFactory
{
    public void AddProvider(ILoggerProvider provider) { }
    public ILogger CreateLogger(string categoryName) => NullLogger.Instance;
    public void Dispose() { }
}

/// <summary>
/// Null logger for testing.
/// </summary>
public class NullLogger : ILogger
{
    public static readonly NullLogger Instance = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(LogLevel logLevel) => false;
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
}
