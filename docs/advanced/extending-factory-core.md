---
layout: default
title: "Extending FactoryCore"
description: "Custom IFactoryCore implementations for cross-cutting concerns"
parent: Advanced
nav_order: 5
---

# Extending FactoryCore

RemoteFactory uses `IFactoryCore<T>` to wrap factory method calls, enabling you to add cross-cutting concerns like logging, caching, or custom lifecycle behavior without modifying individual domain models.

## Understanding FactoryCore

### IFactoryCore Interface

```csharp
public interface IFactoryCore<T>
{
    // For Create operations (no existing target)
    T DoFactoryMethodCall(FactoryOperation operation, Func<T> factoryMethodCall);
    Task<T> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<T>> factoryMethodCall);
    Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<T?>> factoryMethodCall);

    // For operations on existing target
    T DoFactoryMethodCall(T target, FactoryOperation operation, Action factoryMethodCall);
    T? DoFactoryMethodCallBool(T target, FactoryOperation operation, Func<bool> factoryMethodCall);
    Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation operation, Func<Task> factoryMethodCall);
    Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall);
}
```

### Default Implementation

The default `FactoryCore<T>` handles lifecycle interfaces:

```csharp
public class FactoryCore<T> : IFactoryCore<T>
{
    public virtual async Task<T> DoFactoryMethodCallAsync(
        T target,
        FactoryOperation operation,
        Func<Task> factoryMethodCall)
    {
        // Call IFactoryOnStart if implemented
        if (target is IFactoryOnStart factoryOnStart)
        {
            factoryOnStart.FactoryStart(operation);
        }

        if (target is IFactoryOnStartAsync factoryOnStartAsync)
        {
            await factoryOnStartAsync.FactoryStartAsync(operation);
        }

        // Execute the factory method
        await factoryMethodCall();

        // Call IFactoryOnComplete if implemented
        if (target is IFactoryOnComplete factoryOnComplete)
        {
            factoryOnComplete.FactoryComplete(operation);
        }

        if (target is IFactoryOnCompleteAsync factoryOnCompleteAsync)
        {
            await factoryOnCompleteAsync.FactoryCompleteAsync(operation);
        }

        return target;
    }
}
```

## Creating Custom FactoryCore

### Logging Example

Add logging to all factory operations:

```csharp
public class LoggingFactoryCore<T> : FactoryCore<T>
{
    private readonly ILogger<T> _logger;

    public LoggingFactoryCore(ILogger<T> logger)
    {
        _logger = logger;
    }

    public override T DoFactoryMethodCall(
        FactoryOperation operation,
        Func<T> factoryMethodCall)
    {
        _logger.LogDebug("Starting {Operation} for {Type}",
            operation, typeof(T).Name);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = base.DoFactoryMethodCall(operation, factoryMethodCall);

            stopwatch.Stop();
            _logger.LogInformation(
                "Completed {Operation} for {Type} in {Duration}ms",
                operation, typeof(T).Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Failed {Operation} for {Type} after {Duration}ms",
                operation, typeof(T).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    public override async Task<T> DoFactoryMethodCallAsync(
        FactoryOperation operation,
        Func<Task<T>> factoryMethodCall)
    {
        _logger.LogDebug("Starting async {Operation} for {Type}",
            operation, typeof(T).Name);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await base.DoFactoryMethodCallAsync(operation, factoryMethodCall);

            stopwatch.Stop();
            _logger.LogInformation(
                "Completed async {Operation} for {Type} in {Duration}ms",
                operation, typeof(T).Name, stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            _logger.LogError(ex,
                "Failed async {Operation} for {Type} after {Duration}ms",
                operation, typeof(T).Name, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }

    // Override other methods similarly...
}
```

### Registration

Register your custom FactoryCore for all types:

```csharp
// Replace default for all types
builder.Services.AddSingleton(typeof(IFactoryCore<>), typeof(LoggingFactoryCore<>));
```

## Type-Specific FactoryCore

Create specialized behavior for specific domain models:

### Audit Trail Example

```csharp
public class AuditFactoryCore : FactoryCore<IAuditableModel>
{
    private readonly ICurrentUser _user;
    private readonly IAuditContext _auditContext;

    public AuditFactoryCore(ICurrentUser user, IAuditContext auditContext)
    {
        _user = user;
        _auditContext = auditContext;
    }

    public override async Task<IAuditableModel> DoFactoryMethodCallAsync(
        IAuditableModel target,
        FactoryOperation operation,
        Func<Task> factoryMethodCall)
    {
        // Capture before state for auditing
        var beforeState = CaptureState(target);

        // Execute the operation
        var result = await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);

        // Capture after state and create audit entry
        var afterState = CaptureState(target);

        await _auditContext.AuditEntries.AddAsync(new AuditEntry
        {
            EntityType = typeof(IAuditableModel).Name,
            EntityId = target.Id.ToString(),
            Operation = operation.ToString(),
            UserId = _user.Id,
            Timestamp = DateTime.UtcNow,
            BeforeState = beforeState,
            AfterState = afterState
        });

        await _auditContext.SaveChangesAsync();

        return result;
    }

    private string CaptureState(IAuditableModel target)
    {
        return JsonSerializer.Serialize(target);
    }
}

// Register for specific type
builder.Services.AddScoped<IFactoryCore<IAuditableModel>, AuditFactoryCore>();
```

## Use Cases

### Caching

```csharp
public class CachingFactoryCore<T> : FactoryCore<T> where T : ICacheable
{
    private readonly IDistributedCache _cache;
    private readonly ILogger<T> _logger;

    public override async Task<T?> DoFactoryMethodCallBoolAsync(
        T target,
        FactoryOperation operation,
        Func<Task<bool>> factoryMethodCall)
    {
        // For Fetch operations, check cache first
        if (operation == FactoryOperation.Fetch)
        {
            var cacheKey = target.GetCacheKey();
            var cached = await _cache.GetAsync(cacheKey);

            if (cached != null)
            {
                _logger.LogDebug("Cache hit for {Key}", cacheKey);
                return JsonSerializer.Deserialize<T>(cached);
            }
        }

        var result = await base.DoFactoryMethodCallBoolAsync(target, operation, factoryMethodCall);

        // Cache the result after successful Fetch
        if (result != null && operation == FactoryOperation.Fetch)
        {
            var cacheKey = result.GetCacheKey();
            var json = JsonSerializer.Serialize(result);
            await _cache.SetAsync(cacheKey, Encoding.UTF8.GetBytes(json),
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10)
                });
        }

        // Invalidate cache on write operations
        if (operation is FactoryOperation.Update or FactoryOperation.Delete)
        {
            await _cache.RemoveAsync(target.GetCacheKey());
        }

        return result;
    }
}

public interface ICacheable
{
    string GetCacheKey();
}
```

### Retry Logic

```csharp
public class RetryFactoryCore<T> : FactoryCore<T>
{
    private readonly ILogger<T> _logger;
    private readonly int _maxRetries = 3;

    public override async Task<T> DoFactoryMethodCallAsync(
        T target,
        FactoryOperation operation,
        Func<Task> factoryMethodCall)
    {
        var attempts = 0;

        while (true)
        {
            try
            {
                attempts++;
                return await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
            }
            catch (DbUpdateConcurrencyException) when (attempts < _maxRetries)
            {
                _logger.LogWarning(
                    "Concurrency conflict on {Operation} for {Type}, attempt {Attempt}",
                    operation, typeof(T).Name, attempts);

                await Task.Delay(100 * attempts);  // Exponential backoff
            }
            catch (TimeoutException) when (attempts < _maxRetries)
            {
                _logger.LogWarning(
                    "Timeout on {Operation} for {Type}, attempt {Attempt}",
                    operation, typeof(T).Name, attempts);

                await Task.Delay(100 * attempts);
            }
        }
    }
}
```

### Metrics Collection

```csharp
public class MetricsFactoryCore<T> : FactoryCore<T>
{
    private readonly IMetricsCollector _metrics;

    public override async Task<T> DoFactoryMethodCallAsync(
        FactoryOperation operation,
        Func<Task<T>> factoryMethodCall)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await base.DoFactoryMethodCallAsync(operation, factoryMethodCall);

            stopwatch.Stop();
            _metrics.RecordDuration(
                "factory_operation_duration_ms",
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, string>
                {
                    ["type"] = typeof(T).Name,
                    ["operation"] = operation.ToString(),
                    ["status"] = "success"
                });

            return result;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            _metrics.RecordDuration(
                "factory_operation_duration_ms",
                stopwatch.ElapsedMilliseconds,
                new Dictionary<string, string>
                {
                    ["type"] = typeof(T).Name,
                    ["operation"] = operation.ToString(),
                    ["status"] = "error"
                });

            _metrics.IncrementCounter(
                "factory_operation_errors",
                new Dictionary<string, string>
                {
                    ["type"] = typeof(T).Name,
                    ["operation"] = operation.ToString()
                });

            throw;
        }
    }
}
```

### Transaction Scope

```csharp
public class TransactionalFactoryCore<T> : FactoryCore<T>
{
    private readonly IDbContextFactory<AppDbContext> _contextFactory;

    public override async Task<T> DoFactoryMethodCallAsync(
        T target,
        FactoryOperation operation,
        Func<Task> factoryMethodCall)
    {
        // Wrap write operations in transaction
        if (operation is FactoryOperation.Insert or
            FactoryOperation.Update or
            FactoryOperation.Delete)
        {
            await using var context = await _contextFactory.CreateDbContextAsync();
            await using var transaction = await context.Database.BeginTransactionAsync();

            try
            {
                var result = await base.DoFactoryMethodCallAsync(
                    target, operation, factoryMethodCall);

                await transaction.CommitAsync();
                return result;
            }
            catch
            {
                await transaction.RollbackAsync();
                throw;
            }
        }

        return await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
    }
}
```

## Combining Multiple Concerns

Use decorator pattern for multiple behaviors:

```csharp
public class CompositeFactoryCore<T> : IFactoryCore<T>
{
    private readonly IFactoryCore<T> _inner;
    private readonly ILogger<T> _logger;
    private readonly IMetricsCollector _metrics;

    public CompositeFactoryCore(
        IFactoryCore<T> inner,
        ILogger<T> logger,
        IMetricsCollector metrics)
    {
        _inner = inner;
        _logger = logger;
        _metrics = metrics;
    }

    public async Task<T> DoFactoryMethodCallAsync(
        FactoryOperation operation,
        Func<Task<T>> factoryMethodCall)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogDebug("Starting {Operation} for {Type}", operation, typeof(T).Name);

        try
        {
            var result = await _inner.DoFactoryMethodCallAsync(operation, factoryMethodCall);

            stopwatch.Stop();
            _logger.LogInformation("Completed {Operation} in {Duration}ms",
                operation, stopwatch.ElapsedMilliseconds);
            _metrics.RecordDuration("factory_duration", stopwatch.ElapsedMilliseconds);

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed {Operation}", operation);
            _metrics.IncrementCounter("factory_errors");
            throw;
        }
    }

    // Implement other interface methods...
}

// Registration with decorator
builder.Services.AddScoped<FactoryCore<T>>();  // Base implementation
builder.Services.AddScoped<IFactoryCore<T>>(sp =>
{
    var inner = sp.GetRequiredService<FactoryCore<T>>();
    var logger = sp.GetRequiredService<ILogger<T>>();
    var metrics = sp.GetRequiredService<IMetricsCollector>();
    return new CompositeFactoryCore<T>(inner, logger, metrics);
});
```

## Best Practices

### Keep It Lightweight

FactoryCore is called for every operation. Keep implementations fast:

```csharp
// Good: Quick synchronous checks
public override T DoFactoryMethodCall(FactoryOperation operation, Func<T> call)
{
    _logger.LogDebug("Operation: {Op}", operation);  // Fast
    return base.DoFactoryMethodCall(operation, call);
}

// Avoid: Blocking I/O
public override T DoFactoryMethodCall(FactoryOperation operation, Func<T> call)
{
    _httpClient.GetAsync("...").Wait();  // Blocks!
    return base.DoFactoryMethodCall(operation, call);
}
```

### Use Scoped Registration

For stateful cores, use scoped registration:

```csharp
// Scoped: New instance per request
builder.Services.AddScoped(typeof(IFactoryCore<>), typeof(MyFactoryCore<>));

// Singleton only if stateless
builder.Services.AddSingleton(typeof(IFactoryCore<>), typeof(StatelessFactoryCore<>));
```

### Handle All Method Overloads

Ensure you override all relevant methods:

```csharp
public class CompleteFactoryCore<T> : FactoryCore<T>
{
    // Create operations
    public override T DoFactoryMethodCall(FactoryOperation op, Func<T> call) { ... }
    public override Task<T> DoFactoryMethodCallAsync(FactoryOperation op, Func<Task<T>> call) { ... }
    public override Task<T?> DoFactoryMethodCallAsyncNullable(FactoryOperation op, Func<Task<T?>> call) { ... }

    // Operations on existing target
    public override T DoFactoryMethodCall(T target, FactoryOperation op, Action call) { ... }
    public override T? DoFactoryMethodCallBool(T target, FactoryOperation op, Func<bool> call) { ... }
    public override Task<T> DoFactoryMethodCallAsync(T target, FactoryOperation op, Func<Task> call) { ... }
    public override Task<T?> DoFactoryMethodCallBoolAsync(T target, FactoryOperation op, Func<Task<bool>> call) { ... }
}
```

## Next Steps

- **[Factory Lifecycle](factory-lifecycle.md)**: IFactoryOnStart and IFactoryOnComplete
- **[Factory Pattern](../concepts/factory-pattern.md)**: How factories work
- **[Generated Code](../reference/generated-code.md)**: Understanding generated factory structure
