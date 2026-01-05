---
layout: default
title: Cancellation
parent: Concepts
nav_order: 6
---

# Cancellation Support

RemoteFactory provides comprehensive CancellationToken support across the entire client-server pipeline. When a client cancels a request, the server detects the disconnection and stops processing.

## How It Works

Cancellation flows through the HTTP connection, not through serialization:

```
CLIENT                                          SERVER
┌─────────────────────────────────────┐         ┌─────────────────────────────────────┐
│ factory.FetchAsync(id, ct)          │         │                                     │
│   │                                 │         │                                     │
│   ▼                                 │         │                                     │
│ HttpClient.SendAsync(request, ct) ──┼── TCP ──┼──► HttpContext.RequestAborted       │
│   │                                 │         │         │                           │
│   │  ct.Cancel() ───────────────────┼─► X ────┼─► fires │                           │
│   │                                 │  close  │         ▼                           │
│   ▼                                 │  conn   │    Server stops processing          │
│ OperationCanceledException          │         │                                     │
└─────────────────────────────────────┘         └─────────────────────────────────────┘
```

1. Client's CancellationToken is passed to `HttpClient.SendAsync`
2. When cancelled, HttpClient closes the TCP connection
3. Server's `HttpContext.RequestAborted` fires automatically
4. Server propagates cancellation to the executing factory method

## Client Usage

### Basic Cancellation

```csharp
using var cts = new CancellationTokenSource();

// Cancel after timeout
cts.CancelAfter(TimeSpan.FromSeconds(30));

try
{
    var person = await personFactory.FetchAsync(id, cts.Token);
}
catch (OperationCanceledException)
{
    // Request timed out or was cancelled
}
```

### Cancel Previous Request

Common pattern for search-as-you-type scenarios:

```csharp
private CancellationTokenSource? _searchCts;

async Task OnSearchTextChanged(string searchText)
{
    // Cancel any in-flight request
    _searchCts?.Cancel();
    _searchCts = new CancellationTokenSource();

    try
    {
        Results = await searchFactory.SearchAsync(searchText, _searchCts.Token);
    }
    catch (OperationCanceledException)
    {
        // Previous search cancelled - this is expected
    }
}
```

### Component Disposal

Cancel requests when a Blazor component is disposed:

```csharp
@implements IDisposable

@code {
    private CancellationTokenSource _cts = new();

    protected override async Task OnInitializedAsync()
    {
        Data = await factory.FetchAsync(Id, _cts.Token);
    }

    public void Dispose()
    {
        _cts.Cancel();
        _cts.Dispose();
    }
}
```

## Server-Side Cancellation Sources

The server combines multiple cancellation sources into a single token:

| Source | When It Fires |
|--------|---------------|
| `HttpContext.RequestAborted` | Client disconnects, browser closes, network failure |
| `IHostApplicationLifetime.ApplicationStopping` | Server graceful shutdown (SIGTERM, app pool recycle) |

Both sources trigger the same CancellationToken passed to your factory methods.

## Using CancellationToken in Factory Methods

### Pass Token to Async Operations

```csharp
[Remote]
[Fetch]
public async Task<bool> FetchAsync(
    Guid id,
    CancellationToken cancellationToken,
    [Service] IDbContext db)
{
    // EF Core respects cancellation and rolls back if cancelled
    var entity = await db.Orders
        .Include(o => o.Lines)
        .FirstOrDefaultAsync(o => o.Id == id, cancellationToken);

    if (entity == null) return false;

    // Load properties...
    return true;
}
```

### Check Cancellation at Safe Points

For long-running operations, check cancellation periodically:

```csharp
[Remote]
[Execute]
public async Task ProcessBatchAsync(
    IEnumerable<Guid> ids,
    CancellationToken cancellationToken,
    [Service] IDbContext db)
{
    foreach (var id in ids)
    {
        // Check before each expensive operation
        cancellationToken.ThrowIfCancellationRequested();

        var entity = await db.Items.FindAsync(new object[] { id }, cancellationToken);
        await ProcessItemAsync(entity, cancellationToken);
    }
}
```

### Mixed Parameters

CancellationToken can appear anywhere in the parameter list:

```csharp
[Remote]
[Fetch]
public async Task<bool> FetchAsync(
    Guid id,                              // Business parameter
    CancellationToken cancellationToken,  // Cancellation token
    [Service] IDbContext db,              // Injected service
    [Service] ILogger<Order> logger)      // Another service
{
    // ...
}
```

## Lifecycle Hooks

### IFactoryOnCancelled

Implement `IFactoryOnCancelled` or `IFactoryOnCancelledAsync` for cleanup when operations are cancelled:

```csharp
[Factory]
public class FileProcessor : EntityBase<FileProcessor>, IFactoryOnCancelled
{
    private string? _tempFilePath;

    [Remote]
    [Execute]
    public async Task ProcessAsync(
        Stream input,
        CancellationToken cancellationToken,
        [Service] IFileService files)
    {
        _tempFilePath = await files.CreateTempFileAsync(input, cancellationToken);
        await ProcessTempFileAsync(_tempFilePath, cancellationToken);
    }

    public void FactoryCancelled(FactoryOperation operation)
    {
        // Clean up temp file if operation was cancelled
        if (_tempFilePath != null && File.Exists(_tempFilePath))
        {
            File.Delete(_tempFilePath);
        }
    }
}
```

### Async Cleanup

```csharp
public class Order : EntityBase<Order>, IFactoryOnCancelledAsync
{
    public async Task FactoryCancelledAsync(FactoryOperation operation)
    {
        await _lockService.ReleaseLockAsync(Id);
    }
}
```

## Transaction Handling

CancellationToken integrates with EF Core transactions:

```csharp
[Remote]
[Insert]
public async Task InsertAsync(
    CancellationToken cancellationToken,
    [Service] IDbContext db)
{
    // If cancellation occurs during SaveChangesAsync,
    // EF Core automatically rolls back the transaction
    await db.SaveChangesAsync(cancellationToken);
}
```

For multiple operations, use explicit transactions:

```csharp
[Remote]
[Execute]
public async Task TransferAsync(
    Guid fromId,
    Guid toId,
    decimal amount,
    CancellationToken cancellationToken,
    [Service] IDbContext db)
{
    await using var transaction = await db.Database
        .BeginTransactionAsync(cancellationToken);

    try
    {
        var from = await db.Accounts.FindAsync(new object[] { fromId }, cancellationToken);
        var to = await db.Accounts.FindAsync(new object[] { toId }, cancellationToken);

        from.Balance -= amount;
        to.Balance += amount;

        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    catch (OperationCanceledException)
    {
        // Transaction automatically rolls back
        throw;
    }
}
```

## Error Handling

### Client-Side

```csharp
try
{
    var result = await factory.FetchAsync(id, cancellationToken);
}
catch (OperationCanceledException)
{
    // Request was cancelled (timeout, user action, or component disposal)
    // Usually safe to ignore or show "Request cancelled" message
}
catch (HttpRequestException ex)
{
    // Network error (may also occur on cancellation)
    ShowError("Network error: " + ex.Message);
}
```

### Server-Side

The server logs cancellation events:

```
[INF] Remote request cancelled: FetchOrderDelegate (CorrelationId: abc123)
```

## Best Practices

1. **Always pass CancellationToken to async operations**
   - EF Core queries and saves
   - HttpClient calls
   - File I/O operations
   - Any async method that accepts CancellationToken

2. **Check cancellation at safe points**
   - Before expensive operations
   - Inside loops processing many items
   - After completing a logical unit of work

3. **Use transactions for atomicity**
   - Cancellation doesn't auto-rollback multiple SaveChanges calls
   - Wrap related operations in explicit transactions

4. **Handle OperationCanceledException gracefully**
   - Don't log as error (it's expected behavior)
   - Clean up resources in IFactoryOnCancelled

5. **Don't suppress cancellation**
   - Let OperationCanceledException propagate
   - The framework handles cleanup appropriately

## What Cannot Be Cancelled

Some operations complete regardless of cancellation:

- Response already sent to client
- Database commits that completed before cancellation
- External API calls that don't support cancellation
- Synchronous operations (no async await points)

Design your operations with cancellation boundaries in mind.
