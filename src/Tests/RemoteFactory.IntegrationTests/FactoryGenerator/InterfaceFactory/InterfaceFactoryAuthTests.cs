using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.FactoryGenerator.InterfaceFactory;

/// <summary>
/// Tests for interface factories with [AuthorizeFactory] authorization.
/// This addresses GAP-004 from the test plan: Interface Factory with Authorization - only ASP auth tested.
///
/// Tests verify:
/// - [AuthorizeFactory&lt;T&gt;] on interfaces with sync bool authorization
/// - [AuthorizeFactory&lt;T&gt;] on interfaces with sync string authorization
/// - Auth pass/fail scenarios
/// - CanX method generation for interface operations
/// - Remote interface authorization
/// </summary>

#region Authorization Classes

/// <summary>
/// Authorization class for interface factory with synchronous bool and string return types.
/// </summary>
public class InterfaceAuth
{
    // Execute authorization - applies to all interface methods
    // Parameterless - always called
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanExecuteBool()
    {
        return true;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public string? CanExecuteString()
    {
        return string.Empty; // Empty string means authorized
    }

    // Parameterized - called when operation has matching parameter
    // id == specific GUID causes bool failure, another causes string failure
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanExecuteBoolFail(Guid id)
    {
        return id != Guid.Parse("00000000-0000-0000-0000-000000000010");
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public string? CanExecuteStringFail(Guid id)
    {
        return id == Guid.Parse("00000000-0000-0000-0000-000000000020") ? "ExecuteDenied" : string.Empty;
    }

    // Read authorization - can be used together with Execute
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanReadBool()
    {
        return true;
    }
}

/// <summary>
/// Authorization class that always denies for testing auth failures.
/// </summary>
public class InterfaceAuthDeny
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanExecuteBool()
    {
        return false;
    }

    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public string? CanExecuteString()
    {
        return "AccessDenied";
    }
}

#endregion

#region Interface Definitions

/// <summary>
/// Interface with [Factory] and [AuthorizeFactory&lt;T&gt;] using sync authorization.
/// Methods have various signatures to test different scenarios.
/// </summary>
[Factory]
[AuthorizeFactory<InterfaceAuth>]
public interface IAuthorizedService
{
    Task<string> GetData(Guid id);
    Task<int> ProcessData(Guid id, string data);
    Task<bool> CheckStatus(Guid id);
    Task<List<string>> GetItems(Guid id, int count);
}

/// <summary>
/// Interface with [Factory] and [AuthorizeFactory&lt;T&gt;] that always denies.
/// Used to test authorization failure scenarios.
/// </summary>
[Factory]
[AuthorizeFactory<InterfaceAuthDeny>]
public interface IDeniedService
{
    Task<string> GetDeniedData(Guid id);
}

#endregion

#region Interface Implementations (Server-side only)

/// <summary>
/// Implementation of IAuthorizedService - only exists on server side.
/// </summary>
public class AuthorizedService : IAuthorizedService
{
    private readonly IServerOnlyService serverOnlyService;

    public AuthorizedService(IServerOnlyService serverOnlyService)
    {
        this.serverOnlyService = serverOnlyService;
    }

    public Task<string> GetData(Guid id)
    {
        Assert.NotNull(serverOnlyService);
        return Task.FromResult($"Data for {id}");
    }

    public Task<int> ProcessData(Guid id, string data)
    {
        Assert.NotNull(serverOnlyService);
        return Task.FromResult(data.Length);
    }

    public Task<bool> CheckStatus(Guid id)
    {
        Assert.NotNull(serverOnlyService);
        return Task.FromResult(true);
    }

    public Task<List<string>> GetItems(Guid id, int count)
    {
        Assert.NotNull(serverOnlyService);
        return Task.FromResult(Enumerable.Range(0, count).Select(i => $"Item{i}").ToList());
    }
}

/// <summary>
/// Implementation of IDeniedService - only exists on server side.
/// </summary>
public class DeniedService : IDeniedService
{
    public Task<string> GetDeniedData(Guid id)
    {
        // This should never be called because auth always fails
        throw new InvalidOperationException("This should never be called - auth should deny");
    }
}

#endregion

#region Test Class

public class InterfaceFactoryAuthTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IAuthorizedServiceFactory _factory;
    private readonly IDeniedServiceFactory _deniedServiceFactory;

    public InterfaceFactoryAuthTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services =>
            {
                services.AddScoped<InterfaceAuth>();
                services.AddScoped<InterfaceAuthDeny>();
            },
            configureServer: services =>
            {
                services.AddScoped<IAuthorizedService, AuthorizedService>();
                services.AddScoped<IDeniedService, DeniedService>();
                services.AddScoped<InterfaceAuth>();
                services.AddScoped<InterfaceAuthDeny>();
            });

        _clientScope = client;
        _serverScope = server;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IAuthorizedServiceFactory>();
        _deniedServiceFactory = _clientScope.ServiceProvider.GetRequiredService<IDeniedServiceFactory>();
    }

    #region Sync Authorization - Pass Scenarios

    [Fact]
    public async Task InterfaceAuth_GetData_AuthorizationPasses()
    {
        var id = Guid.NewGuid();

        var result = await _factory.GetData(id);

        Assert.Equal($"Data for {id}", result);
    }

    [Fact]
    public async Task InterfaceAuth_ProcessData_AuthorizationPasses()
    {
        var id = Guid.NewGuid();

        var result = await _factory.ProcessData(id, "test data");

        Assert.Equal(9, result); // "test data".Length
    }

    [Fact]
    public async Task InterfaceAuth_CheckStatus_AuthorizationPasses()
    {
        var id = Guid.NewGuid();

        var result = await _factory.CheckStatus(id);

        Assert.True(result);
    }

    [Fact]
    public async Task InterfaceAuth_GetItems_AuthorizationPasses()
    {
        var id = Guid.NewGuid();

        var result = await _factory.GetItems(id, 3);

        Assert.NotNull(result);
        Assert.Equal(3, result.Count);
        Assert.Contains("Item0", result);
        Assert.Contains("Item1", result);
        Assert.Contains("Item2", result);
    }

    #endregion

    #region Sync Authorization - Fail Scenarios (Bool)

    [Fact]
    public async Task InterfaceAuth_GetData_AuthorizationFailsBool_ThrowsException()
    {
        // Use specific GUID that triggers bool auth failure
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");

        await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await _factory.GetData(id);
        });
    }

    [Fact]
    public async Task InterfaceAuth_ProcessData_AuthorizationFailsBool_ThrowsException()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");

        await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await _factory.ProcessData(id, "data");
        });
    }

    [Fact]
    public async Task InterfaceAuth_CheckStatus_AuthorizationFailsBool_ThrowsException()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");

        await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await _factory.CheckStatus(id);
        });
    }

    #endregion

    #region Sync Authorization - Fail Scenarios (String)

    [Fact]
    public async Task InterfaceAuth_GetData_AuthorizationFailsString_ThrowsExceptionWithMessage()
    {
        // Use specific GUID that triggers string auth failure
        var id = Guid.Parse("00000000-0000-0000-0000-000000000020");

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await _factory.GetData(id);
        });

        Assert.Contains("ExecuteDenied", ex.Message);
    }

    [Fact]
    public async Task InterfaceAuth_ProcessData_AuthorizationFailsString_ThrowsExceptionWithMessage()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000020");

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await _factory.ProcessData(id, "data");
        });

        Assert.Contains("ExecuteDenied", ex.Message);
    }

    #endregion

    #region Always Denied Service Tests

    [Fact]
    public async Task InterfaceAuthDeny_GetDeniedData_AuthorizationAlwaysFails()
    {
        var id = Guid.NewGuid();

        await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await _deniedServiceFactory.GetDeniedData(id);
        });
    }

    [Fact]
    public async Task InterfaceAuthDeny_GetDeniedData_FailsWithMessage()
    {
        var id = Guid.NewGuid();

        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await _deniedServiceFactory.GetDeniedData(id);
        });

        // Authorization may fail on either bool (no message) or string (message), but should fail
        Assert.NotNull(ex);
    }

    #endregion

    #region CanX Method Generation Tests

    [Fact]
    public void InterfaceAuth_CanGetData_ReturnsTrue_WhenAuthorized()
    {
        var id = Guid.NewGuid();

        var canAccess = _factory.CanGetData(id);

        Assert.True(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuth_CanGetData_ReturnsFalse_WhenBoolAuthFails()
    {
        // Use specific GUID that triggers bool auth failure
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");

        var canAccess = _factory.CanGetData(id);

        Assert.False(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuth_CanGetData_ReturnsFalse_WhenStringAuthFails()
    {
        // Use specific GUID that triggers string auth failure
        var id = Guid.Parse("00000000-0000-0000-0000-000000000020");

        var canAccess = _factory.CanGetData(id);

        Assert.False(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuth_CanProcessData_ReturnsTrue_WhenAuthorized()
    {
        var id = Guid.NewGuid();

        // CanProcessData only takes the Guid id (authorization parameter)
        var canAccess = _factory.CanProcessData(id);

        Assert.True(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuth_CanProcessData_ReturnsFalse_WhenBoolAuthFails()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");

        var canAccess = _factory.CanProcessData(id);

        Assert.False(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuth_CanCheckStatus_ReturnsTrue_WhenAuthorized()
    {
        var id = Guid.NewGuid();

        var canAccess = _factory.CanCheckStatus(id);

        Assert.True(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuth_CanCheckStatus_ReturnsFalse_WhenBoolAuthFails()
    {
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");

        var canAccess = _factory.CanCheckStatus(id);

        Assert.False(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuth_CanGetItems_ReturnsTrue_WhenAuthorized()
    {
        var id = Guid.NewGuid();

        // CanGetItems only takes the Guid id (authorization parameter)
        var canAccess = _factory.CanGetItems(id);

        Assert.True(canAccess.HasAccess);
    }

    [Fact]
    public void InterfaceAuthDeny_CanGetDeniedData_ReturnsFalse_Always()
    {
        // CanGetDeniedData takes no parameters because InterfaceAuthDeny has no parameterized auth methods
        var canAccess = _deniedServiceFactory.CanGetDeniedData();

        Assert.False(canAccess.HasAccess);
    }

    #endregion

    #region Direct Interface Method Tests (via registered interface)

    [Fact]
    public async Task InterfaceAuth_DirectInterface_GetData_AuthorizationPasses()
    {
        var authorizedService = _clientScope.ServiceProvider.GetRequiredService<IAuthorizedService>();
        var id = Guid.NewGuid();

        var result = await authorizedService.GetData(id);

        Assert.Equal($"Data for {id}", result);
    }

    [Fact]
    public async Task InterfaceAuth_DirectInterface_ProcessData_AuthorizationPasses()
    {
        var authorizedService = _clientScope.ServiceProvider.GetRequiredService<IAuthorizedService>();
        var id = Guid.NewGuid();

        var result = await authorizedService.ProcessData(id, "hello");

        Assert.Equal(5, result);
    }

    [Fact]
    public async Task InterfaceAuth_DirectInterface_GetData_AuthorizationFails()
    {
        var authorizedService = _clientScope.ServiceProvider.GetRequiredService<IAuthorizedService>();
        var id = Guid.Parse("00000000-0000-0000-0000-000000000010");

        await Assert.ThrowsAsync<NotAuthorizedException>(async () =>
        {
            await authorizedService.GetData(id);
        });
    }

    #endregion

    #region Multiple Method Calls

    [Fact]
    public async Task InterfaceAuth_MultipleMethods_AllAuthorize()
    {
        var id = Guid.NewGuid();

        // All methods should authorize successfully
        var data = await _factory.GetData(id);
        var processed = await _factory.ProcessData(id, "test");
        var status = await _factory.CheckStatus(id);
        var items = await _factory.GetItems(id, 2);

        Assert.NotNull(data);
        Assert.Equal(4, processed);
        Assert.True(status);
        Assert.Equal(2, items.Count);
    }

    [Fact]
    public async Task InterfaceAuth_DifferentIdScenarios_EachAuthorizedIndependently()
    {
        var goodId = Guid.NewGuid();
        var badBoolId = Guid.Parse("00000000-0000-0000-0000-000000000010");
        var badStringId = Guid.Parse("00000000-0000-0000-0000-000000000020");

        // Good ID should succeed
        var result1 = await _factory.GetData(goodId);
        Assert.NotNull(result1);

        // Bad bool ID should fail
        await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.GetData(badBoolId));

        // Bad string ID should fail with message
        var ex = await Assert.ThrowsAsync<NotAuthorizedException>(() => _factory.GetData(badStringId));
        Assert.Contains("ExecuteDenied", ex.Message);

        // Good ID should still succeed after failures
        var result2 = await _factory.GetData(goodId);
        Assert.NotNull(result2);
    }

    #endregion
}

#endregion
