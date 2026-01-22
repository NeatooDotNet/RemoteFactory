using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.Showcase;

#region Authorization Interface and Implementation

/// <summary>
/// Authorization interface for ShowcaseAuthObj.
/// Demonstrates various authorization scenarios.
/// </summary>
public interface IShowcaseAuthorize
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool AnyAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    bool CanRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool CanCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool CanFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

/// <summary>
/// Implementation that allows Create and general Read/Write, but denies Fetch and Delete.
/// </summary>
internal class ShowcaseAuthorize : IShowcaseAuthorize
{
    public ShowcaseAuthorize([Service] IService service) { Assert.NotNull(service); }
    public bool AnyAccess() { return true; }
    public bool CanRead() { return true; }
    public bool CanCreate() { return true; }
    public bool CanFetch() { return false; }
    public bool CanDelete() { return false; }
}

#endregion

#region Target Class

/// <summary>
/// Interface for ShowcaseAuthObj.
/// </summary>
public interface IShowcaseAuthObj : IFactorySaveMeta
{
    new bool IsDeleted { get; set; }
    new bool IsNew { get; set; }
}

/// <summary>
/// Target class demonstrating authorization scenarios.
/// </summary>
[Factory]
[AuthorizeFactory<IShowcaseAuthorize>]
internal class ShowcaseAuthObj : IShowcaseAuthObj
{
    [Fetch]
    [Create]
    public ShowcaseAuthObj([Service] IService service) { Assert.NotNull(service); }

    public bool IsDeleted { get; set; } = false;

    public bool IsNew { get; set; } = false;

    [Insert]
    public void Insert([Service] IService service) { IsNew = false; Assert.NotNull(service); }

    [Update]
    public void Update([Service] IService service) { }

    [Delete]
    public void Delete([Service] IService service) { }
}

#endregion

/// <summary>
/// Integration tests for authorization scenarios.
/// </summary>
public class ShowcaseAuthTests
{
    private readonly IServiceScope _clientScope;
    private readonly IShowcaseAuthObjFactory _factory;

    public ShowcaseAuthTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services =>
            {
                services.AddScoped<IShowcaseAuthorize, ShowcaseAuthorize>();
            },
            configureServer: services =>
            {
                services.AddScoped<IShowcaseAuthorize, ShowcaseAuthorize>();
            });

        _clientScope = client;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IShowcaseAuthObjFactory>();
    }

    [Fact]
    public void ShowcaseAuth_CanCreate()
    {
        Assert.True(_factory.CanCreate());
    }

    [Fact]
    public void ShowcaseAuth_Create()
    {
        var create = _factory.Create();
        Assert.NotNull(create);
    }

    [Fact]
    public void ShowcaseAuth_CanFetch()
    {
        Assert.False(_factory.CanFetch());
    }

    [Fact]
    public void ShowcaseAuth_Fetch()
    {
        var fetch = _factory.Fetch();
        Assert.Null(fetch);
    }

    [Fact]
    public void ShowcaseAuth_CanDelete()
    {
        Assert.False(_factory.CanDelete());
    }

    [Fact]
    public void ShowcaseAuth_CanSave()
    {
        // False because CanDelete is false
        // Even though an Insert is allowed
        // But success cannot be guaranteed
        Assert.False(_factory.CanSave());
    }

    [Fact]
    public void ShowcaseAuth_Save()
    {
        var create = _factory.Create()!;
        var result = _factory.Save(create);
        Assert.NotNull(result);
        Assert.False(result!.IsNew);
    }


    [Fact]
    public void ShowcaseAuth_Save_Exception_CannotDelete()
    {
        var create = _factory.Create()!;
        create.IsDeleted = true;
        Assert.Throws<NotAuthorizedException>(() => _factory.Save(create));
    }

    [Fact]
    public void ShowcaseAuth_TrySave_Null_CannotDelete()
    {
        var create = _factory.Create()!;
        create.IsDeleted = true;
        var result = _factory.TrySave(create);
        Assert.Null(result.Result);
        Assert.False(result.HasAccess);
    }

    [Fact]
    public void ShowcaseAuth_TrySave_Success()
    {
        // Success because Insert and general Write is allowed
        var create = _factory.Create()!;
        create.IsNew = true;
        var result = _factory.TrySave(create);
        Assert.NotNull(result.Result);
        Assert.True(result.HasAccess);
        Assert.False(result.Result!.IsNew);
    }
}
