using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.TestTargets.Core;

/// <summary>
/// Simple target for testing synchronous FactoryCore override.
/// </summary>
[Factory]
public class FactoryCoreTarget_Sync
{
    [Create]
    public FactoryCoreTarget_Sync() { }
}

/// <summary>
/// Custom FactoryCore that tracks synchronous method calls.
/// </summary>
public class TrackingFactoryCore_Sync : FactoryCore<FactoryCoreTarget_Sync>
{
    public bool DoFactoryMethodCallCalled { get; set; }

    public override FactoryCoreTarget_Sync DoFactoryMethodCall(FactoryOperation operation, Func<FactoryCoreTarget_Sync> factoryMethodCall)
    {
        DoFactoryMethodCallCalled = true;
        return base.DoFactoryMethodCall(operation, factoryMethodCall);
    }
}

/// <summary>
/// Target for testing async Create/Fetch operations.
/// </summary>
[Factory]
public class AsyncCoreTarget_Read
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }

    [Create]
    public Task CreateAsync()
    {
        CreateCalled = true;
        return Task.CompletedTask;
    }

    [Fetch]
    public Task FetchAsync()
    {
        FetchCalled = true;
        return Task.CompletedTask;
    }

    [Create]
    public Task<bool> CreateBoolAsync()
    {
        CreateCalled = true;
        return Task.FromResult(true);
    }

    [Fetch]
    public Task<bool> FetchBoolAsync()
    {
        FetchCalled = true;
        return Task.FromResult(true);
    }
}

/// <summary>
/// Target for testing async Write operations (Insert, Update, Delete).
/// </summary>
[Factory]
public class AsyncCoreTarget_Write : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }

    [Create]
    public AsyncCoreTarget_Write() { }

    [Insert]
    public Task InsertAsync()
    {
        InsertCalled = true;
        return Task.CompletedTask;
    }

    [Update]
    public Task UpdateAsync()
    {
        UpdateCalled = true;
        return Task.CompletedTask;
    }

    [Delete]
    public Task DeleteAsync()
    {
        DeleteCalled = true;
        return Task.CompletedTask;
    }

    [Insert]
    public Task<bool> InsertBoolAsync()
    {
        InsertCalled = true;
        return Task.FromResult(true);
    }

    [Update]
    public Task<bool> UpdateBoolAsync()
    {
        UpdateCalled = true;
        return Task.FromResult(true);
    }

    [Delete]
    public Task<bool> DeleteBoolAsync()
    {
        DeleteCalled = true;
        return Task.FromResult(true);
    }
}

/// <summary>
/// Custom FactoryCore that tracks async method calls for Read operations.
/// </summary>
public class TrackingAsyncFactoryCore_Read : FactoryCore<AsyncCoreTarget_Read>
{
    public bool DoFactoryMethodCallAsyncCalled { get; private set; }
    public bool DoFactoryMethodCallBoolAsyncCalled { get; private set; }
    public FactoryOperation? LastOperation { get; private set; }
    public int CallCount { get; private set; }

    public override async Task<AsyncCoreTarget_Read> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<AsyncCoreTarget_Read>> factoryMethodCall)
    {
        DoFactoryMethodCallAsyncCalled = true;
        LastOperation = operation;
        CallCount++;
        return await base.DoFactoryMethodCallAsync(operation, factoryMethodCall);
    }

    public override async Task<AsyncCoreTarget_Read?> DoFactoryMethodCallAsyncNullable(FactoryOperation operation, Func<Task<AsyncCoreTarget_Read?>> factoryMethodCall)
    {
        LastOperation = operation;
        CallCount++;
        return await base.DoFactoryMethodCallAsyncNullable(operation, factoryMethodCall);
    }

    public override async Task<AsyncCoreTarget_Read?> DoFactoryMethodCallBoolAsync(AsyncCoreTarget_Read target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
    {
        DoFactoryMethodCallBoolAsyncCalled = true;
        LastOperation = operation;
        CallCount++;
        return await base.DoFactoryMethodCallBoolAsync(target, operation, factoryMethodCall);
    }

    public override async Task<AsyncCoreTarget_Read> DoFactoryMethodCallAsync(AsyncCoreTarget_Read target, FactoryOperation operation, Func<Task> factoryMethodCall)
    {
        DoFactoryMethodCallAsyncCalled = true;
        LastOperation = operation;
        CallCount++;
        return await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
    }
}

/// <summary>
/// Custom FactoryCore that tracks async method calls for Write operations.
/// </summary>
public class TrackingAsyncFactoryCore_Write : FactoryCore<AsyncCoreTarget_Write>
{
    public bool DoFactoryMethodCallAsyncCalled { get; private set; }
    public bool DoFactoryMethodCallBoolAsyncCalled { get; private set; }
    public FactoryOperation? LastOperation { get; private set; }
    public int CallCount { get; private set; }

    public override async Task<AsyncCoreTarget_Write> DoFactoryMethodCallAsync(AsyncCoreTarget_Write target, FactoryOperation operation, Func<Task> factoryMethodCall)
    {
        DoFactoryMethodCallAsyncCalled = true;
        LastOperation = operation;
        CallCount++;
        return await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
    }

    public override async Task<AsyncCoreTarget_Write?> DoFactoryMethodCallBoolAsync(AsyncCoreTarget_Write target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
    {
        DoFactoryMethodCallBoolAsyncCalled = true;
        LastOperation = operation;
        CallCount++;
        return await base.DoFactoryMethodCallBoolAsync(target, operation, factoryMethodCall);
    }
}

/// <summary>
/// Custom FactoryCore that wraps results with before/after behavior.
/// </summary>
public class WrappingAsyncFactoryCore_Read : FactoryCore<AsyncCoreTarget_Read>
{
    public bool BeforeCallExecuted { get; private set; }
    public bool AfterCallExecuted { get; private set; }
    public AsyncCoreTarget_Read? LastResult { get; private set; }

    public override async Task<AsyncCoreTarget_Read> DoFactoryMethodCallAsync(FactoryOperation operation, Func<Task<AsyncCoreTarget_Read>> factoryMethodCall)
    {
        BeforeCallExecuted = true;
        var result = await base.DoFactoryMethodCallAsync(operation, factoryMethodCall);
        AfterCallExecuted = true;
        LastResult = result;
        return result;
    }

    public override async Task<AsyncCoreTarget_Read?> DoFactoryMethodCallBoolAsync(AsyncCoreTarget_Read target, FactoryOperation operation, Func<Task<bool>> factoryMethodCall)
    {
        BeforeCallExecuted = true;
        var result = await base.DoFactoryMethodCallBoolAsync(target, operation, factoryMethodCall);
        AfterCallExecuted = true;
        LastResult = result;
        return result;
    }

    public override async Task<AsyncCoreTarget_Read> DoFactoryMethodCallAsync(AsyncCoreTarget_Read target, FactoryOperation operation, Func<Task> factoryMethodCall)
    {
        BeforeCallExecuted = true;
        var result = await base.DoFactoryMethodCallAsync(target, operation, factoryMethodCall);
        AfterCallExecuted = true;
        LastResult = result;
        return result;
    }
}
