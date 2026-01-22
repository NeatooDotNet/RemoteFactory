using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.UnitTests.Shared;
using RemoteFactory.UnitTests.TestContainers;

namespace RemoteFactory.UnitTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// Authorization class for BugNoCanCreateFetch test.
/// </summary>
public class BugNoCanCreateFetchAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    public bool CanAccess()
    {
        return true;
    }
}

/// <summary>
/// Target class demonstrating the CanCreate generation fix.
/// Bug: In an effort to not have CanInsert, CanCreate was missing.
/// </summary>
[Factory]
[AuthorizeFactory<BugNoCanCreateFetchAuth>]
public class BugNoCanCreateFetchObj : IFactorySaveMeta
{
    public bool IsDeleted => throw new NotImplementedException();
    public bool IsNew => throw new NotImplementedException();

    [Create]
    public void Create([Service] IServerOnlyService service)
    {
        ArgumentNullException.ThrowIfNull(service, nameof(service));
    }

    [Insert]
    public void Insert()
    {
    }
}

#endregion

/// <summary>
/// Regression test for CanCreate method generation.
/// Bug: When authorization covered Read and Write operations, CanCreate was not generated.
/// </summary>
public class BugNoCanCreateFetchTests : IDisposable
{
    private readonly IServiceProvider _provider;
    private readonly IBugNoCanCreateFetchObjFactory _factory;

    public BugNoCanCreateFetchTests()
    {
        _provider = new ServerContainerBuilder()
            .WithSingleton<IServerOnlyService, ServerOnlyService>()
            .ConfigureServices(services => services.AddScoped<BugNoCanCreateFetchAuth>())
            .Build();
        _factory = _provider.GetRequiredService<IBugNoCanCreateFetchObjFactory>();
    }

    public void Dispose()
    {
        (_provider as IDisposable)?.Dispose();
    }

    [Fact]
    public void CanCreate_IsGenerated_WhenAuthorizationCoversReadAndWrite()
    {
        // Bug: CanCreate method was missing when auth covered Read | Write

        var result = _factory.CanCreate();

        Assert.True(result, "CanCreate should be true");
    }

    [Fact]
    public void CanInsert_IsGenerated_WhenAuthorizationCoversWrite()
    {
        var result = _factory.CanInsert();

        Assert.True(result, "CanInsert should be true");
    }
}
