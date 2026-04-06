using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.Authorization;

namespace RemoteFactory.IntegrationTests.Authorization;

// ============================================================================
// Feature #1: Type-based parameter matching on class factories
// ============================================================================

public class ClassFactoryAuthParamTests : IDisposable
{
    private readonly IAuthParamClassTargetFactory _factory;

    public ClassFactoryAuthParamTests()
    {
        ClassAuthWithParams.AllowWrite = true;

        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services =>
            {
                services.AddScoped<ClassAuthWithParams>();
            },
            configureServer: services =>
            {
                services.AddScoped<ClassAuthWithParams>();
            });

        _factory = client.ServiceProvider.GetRequiredService<IAuthParamClassTargetFactory>();
    }

    public void Dispose()
    {
        ClassAuthWithParams.AllowWrite = true;
    }

    #region Fetch with parameterized auth (type-matched Guid)

    [Fact]
    public async Task Fetch_GoodGuid_AuthPasses_ReturnsResult()
    {
        var id = Guid.NewGuid();
        var result = await _factory.Fetch(id);
        Assert.NotNull(result);
        Assert.Equal(id, result!.Id);
    }

    [Fact]
    public async Task Fetch_DenyFetchGuid_AuthFails_ReturnsNull()
    {
        var result = await _factory.Fetch(ClassAuthWithParams.DenyFetchGuid);
        Assert.Null(result);
    }

    #endregion

    #region Create with parameterless auth only

    [Fact]
    public void Create_ParameterlessAuth_Passes()
    {
        var result = _factory.Create();
        Assert.NotNull(result);
    }

    #endregion

    #region Save with parameterless write auth

    [Fact]
    public async Task Save_Insert_WriteAllowed_Passes()
    {
        var obj = _factory.Create()!;
        Assert.True(obj.IsNew);
        var result = await _factory.Save(obj);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Save_Insert_WriteDenied_Throws()
    {
        ClassAuthWithParams.AllowWrite = false;
        var obj = _factory.Create()!;
        await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.Save(obj));
    }

    [Fact]
    public async Task Save_Update_WriteAllowed_Passes()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        var result = await _factory.Save(obj);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Save_Update_WriteDenied_Throws()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        ClassAuthWithParams.AllowWrite = false;
        await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.Save(obj));
    }

    [Fact]
    public async Task Save_Delete_WriteAllowed_Passes()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        obj!.IsDeleted = true;
        var result = await _factory.Save(obj);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Save_Delete_WriteDenied_Throws()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        obj!.IsDeleted = true;
        ClassAuthWithParams.AllowWrite = false;
        await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.Save(obj));
    }

    #endregion

    #region CanFetch with parameterized auth (type-matched Guid)

    [Fact]
    public void CanFetch_GoodGuid_ReturnsTrue()
    {
        var result = _factory.CanFetch(Guid.NewGuid());
        Assert.True(result.HasAccess);
    }

    [Fact]
    public void CanFetch_DenyFetchGuid_ReturnsFalse()
    {
        var result = _factory.CanFetch(ClassAuthWithParams.DenyFetchGuid);
        Assert.False(result.HasAccess);
    }

    #endregion

    #region CanCreate (parameterless only)

    [Fact]
    public void CanCreate_Parameterless_ReturnsTrue()
    {
        var result = _factory.CanCreate();
        Assert.True(result.HasAccess);
    }

    #endregion

    #region CanSave with write auth

    [Fact]
    public void CanSave_WriteAllowed_ReturnsTrue()
    {
        var result = _factory.CanSave();
        Assert.True(result.HasAccess);
    }

    [Fact]
    public void CanSave_WriteDenied_ReturnsFalse()
    {
        ClassAuthWithParams.AllowWrite = false;
        var result = _factory.CanSave();
        Assert.False(result.HasAccess);
    }

    #endregion

    #region TrySave with write auth

    [Fact]
    public async Task TrySave_WriteDenied_ReturnsUnauthorized()
    {
        ClassAuthWithParams.AllowWrite = false;
        var obj = _factory.Create()!;
        var result = await _factory.TrySave(obj);
        Assert.Null(result.Result);
        Assert.False(result.HasAccess);
    }

    #endregion
}

// ============================================================================
// Feature #2: Target parameter in auth methods (write operations)
// ============================================================================

public class AuthTargetParamTests
{
    private readonly IAuthTargetParamObjFactory _factory;

    public AuthTargetParamTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services =>
            {
                services.AddScoped<AuthWithTargetParam>();
            },
            configureServer: services =>
            {
                services.AddScoped<AuthWithTargetParam>();
            });

        _factory = client.ServiceProvider.GetRequiredService<IAuthTargetParamObjFactory>();
    }

    #region Read operations (target auth does NOT apply)

    [Fact]
    public void Create_Succeeds_TargetAuthDoesNotApply()
    {
        var result = _factory.Create();
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Fetch_Succeeds_TargetAuthDoesNotApply()
    {
        var result = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(result);
    }

    #endregion

    #region Write operations - auth receives target and allows

    [Fact]
    public async Task Save_Insert_ActiveStatus_AuthPasses()
    {
        var obj = _factory.Create()!;
        obj.Status = "Active";
        Assert.True(obj.IsNew);
        var result = await _factory.Save(obj);
        Assert.NotNull(result);
        Assert.False(result!.IsNew);
    }

    [Fact]
    public async Task Save_Update_ActiveStatus_AuthPasses()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        obj!.Status = "Active";
        var result = await _factory.Save(obj);
        Assert.NotNull(result);
    }

    [Fact]
    public async Task Save_Delete_ActiveStatus_AuthPasses()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        obj!.Status = "Active";
        obj.IsDeleted = true;
        var result = await _factory.Save(obj);
        Assert.NotNull(result);
    }

    #endregion

    #region Write operations - auth receives target and denies

    [Fact]
    public async Task Save_Insert_LockedStatus_AuthDenies()
    {
        var obj = _factory.Create()!;
        obj.Status = "Locked";
        await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.Save(obj));
    }

    [Fact]
    public async Task Save_Update_LockedStatus_AuthDenies()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        obj!.Status = "Locked";
        await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.Save(obj));
    }

    [Fact]
    public async Task Save_Delete_LockedStatus_AuthDenies()
    {
        var obj = await _factory.Fetch(Guid.NewGuid());
        Assert.NotNull(obj);
        obj!.Status = "Locked";
        obj.IsDeleted = true;
        await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.Save(obj));
    }

    #endregion

    #region TrySave with target auth

    [Fact]
    public async Task TrySave_LockedStatus_ReturnsUnauthorized()
    {
        var obj = _factory.Create()!;
        obj.Status = "Locked";
        var result = await _factory.TrySave(obj);
        Assert.Null(result.Result);
        Assert.False(result.HasAccess);
    }

    [Fact]
    public async Task TrySave_ActiveStatus_ReturnsResult()
    {
        var obj = _factory.Create()!;
        obj.Status = "Active";
        var result = await _factory.TrySave(obj);
        Assert.NotNull(result.Result);
        Assert.True(result.HasAccess);
    }

    #endregion

    #region CanCreate/CanFetch generated (Read auth is parameterless)

    [Fact]
    public void CanCreate_Generated_ReturnsTrue()
    {
        var result = _factory.CanCreate();
        Assert.True(result.HasAccess);
    }

    [Fact]
    public void CanFetch_Generated_ReturnsTrue()
    {
        var result = _factory.CanFetch();
        Assert.True(result.HasAccess);
    }

    #endregion

    #region CanInsert/CanUpdate/CanDelete/CanSave NOT generated (suppressed by target auth)

    [Fact]
    public void CanInsert_NotGenerated_OnInterface()
    {
        // CanInsert/CanUpdate/CanDelete/CanSave should NOT appear on the factory interface
        // because the Write auth method has a target parameter.
        // Verify by checking the interface does NOT have these methods.
        var interfaceType = typeof(IAuthTargetParamObjFactory);
        Assert.Null(interfaceType.GetMethod("CanInsert"));
        Assert.Null(interfaceType.GetMethod("CanUpdate"));
        Assert.Null(interfaceType.GetMethod("CanDelete"));
        Assert.Null(interfaceType.GetMethod("CanSave"));
    }

    #endregion
}
