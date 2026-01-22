using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;

namespace RemoteFactory.UnitTests.TestTargets.Parameters;

#region Create/Fetch with CancellationToken

/// <summary>
/// Test target for [Create] and [Fetch] with CancellationToken parameter.
/// CancellationToken is a common async pattern that should be properly supported.
/// </summary>
[Factory]
public partial class CancellableReadTarget
{
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }
    public bool CancellationWasChecked { get; set; }

    [Create]
    public CancellableReadTarget() { }

    [Create]
    public async Task CreateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        CreateCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Fetch]
    public async Task FetchAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        FetchCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Fetch]
    public async Task<bool> FetchBoolAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        FetchCalled = true;
        await Task.Delay(10, cancellationToken);
        return true;
    }
}

#endregion

#region Write Operations with CancellationToken

/// <summary>
/// Test target for Write operations with CancellationToken parameter.
/// </summary>
[Factory]
public partial class CancellableWriteTarget : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool DeleteCalled { get; set; }
    public bool CancellationWasChecked { get; set; }

    [Create]
    public CancellableWriteTarget() { }

    [Insert]
    public async Task InsertAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        InsertCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Update]
    public async Task UpdateAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        UpdateCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Delete]
    public async Task DeleteAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        DeleteCalled = true;
        await Task.Delay(10, cancellationToken);
    }
}

#endregion

#region Mixed Parameters with CancellationToken

/// <summary>
/// Test target with business params, CancellationToken, and Service params.
/// Tests that CancellationToken is correctly excluded from serialized parameters.
/// </summary>
[Factory]
public partial class MixedParamCancellableTarget
{
    public int? BusinessParam { get; set; }
    public bool ServiceWasInjected { get; set; }
    public bool CancellationWasChecked { get; set; }
    public bool CreateCalled { get; set; }
    public bool FetchCalled { get; set; }

    [Create]
    public MixedParamCancellableTarget() { }

    [Create]
    public async Task CreateAsync(int param, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        BusinessParam = param;
        CancellationWasChecked = true;
        CreateCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Fetch]
    public async Task FetchAsync(
        int param,
        CancellationToken cancellationToken,
        [Service] IService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        cancellationToken.ThrowIfCancellationRequested();
        BusinessParam = param;
        ServiceWasInjected = true;
        CancellationWasChecked = true;
        FetchCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Create]
    public async Task CreateWithServiceAsync(
        [Service] IService service,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(service);
        cancellationToken.ThrowIfCancellationRequested();
        ServiceWasInjected = true;
        CancellationWasChecked = true;
        CreateCalled = true;
        await Task.Delay(10, cancellationToken);
    }

    [Fetch]
    public async Task FetchComplexAsync(
        int intParam,
        string stringParam,
        CancellationToken cancellationToken,
        [Service] IService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        cancellationToken.ThrowIfCancellationRequested();
        BusinessParam = intParam;
        ServiceWasInjected = true;
        CancellationWasChecked = true;
        FetchCalled = true;
        await Task.Delay(10, cancellationToken);
    }
}

#endregion

#region CancellationToken with Bool Return

/// <summary>
/// Test target for CancellationToken with bool return type.
/// </summary>
[Factory]
public partial class CancellableBoolTarget
{
    public bool CreateCalled { get; set; }
    public bool ShouldSucceed { get; set; } = true;
    public bool CancellationWasChecked { get; set; }

    [Create]
    public CancellableBoolTarget() { }

    [Create]
    public async Task<bool> CreateBoolAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        CreateCalled = true;
        await Task.Delay(10, cancellationToken);
        return ShouldSucceed;
    }

    [Fetch]
    public async Task<bool> FetchBoolAsync(bool shouldSucceed, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        ShouldSucceed = shouldSucceed;
        CancellationWasChecked = true;
        await Task.Delay(10, cancellationToken);
        return shouldSucceed;
    }
}

#endregion

#region Write Operations with CancellationToken and Bool Return

/// <summary>
/// Test target for Write operations with CancellationToken and bool return.
/// </summary>
[Factory]
public partial class CancellableWriteBoolTarget : IFactorySaveMeta
{
    public bool IsDeleted { get; set; }
    public bool IsNew { get; set; }

    public bool InsertCalled { get; set; }
    public bool UpdateCalled { get; set; }
    public bool CancellationWasChecked { get; set; }
    public bool ShouldSucceed { get; set; } = true;

    [Create]
    public CancellableWriteBoolTarget() { }

    [Insert]
    public async Task<bool> InsertBoolAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        InsertCalled = true;
        await Task.Delay(10, cancellationToken);
        return ShouldSucceed;
    }

    [Update]
    public async Task<bool> UpdateBoolAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CancellationWasChecked = true;
        UpdateCalled = true;
        await Task.Delay(10, cancellationToken);
        return ShouldSucceed;
    }
}

#endregion

#region Default CancellationToken Value

/// <summary>
/// Test target with default CancellationToken parameter value.
/// </summary>
[Factory]
public partial class DefaultCancellableTarget
{
    public bool CreateCalled { get; set; }

    [Create]
    public DefaultCancellableTarget() { }

    [Create]
    public async Task CreateAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        CreateCalled = true;
        await Task.Delay(10, cancellationToken);
    }
}

#endregion
