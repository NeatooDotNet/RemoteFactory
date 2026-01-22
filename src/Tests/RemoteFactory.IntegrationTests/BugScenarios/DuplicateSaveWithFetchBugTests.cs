using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.BugScenarios;

#region Test Target Classes

/// <summary>
/// Second service interface to match the Neatoo Person pattern.
/// </summary>
public interface ISecondTestService { }

/// <summary>
/// Second service implementation.
/// </summary>
public class SecondTestService : ISecondTestService { }

/// <summary>
/// Authorization interface matching the Neatoo Person example pattern.
/// </summary>
public interface IDuplicateSaveWithFetchAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
    bool HasAccess();

    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    bool HasCreate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
    bool HasFetch();

    [AuthorizeFactory(AuthorizeFactoryOperation.Insert)]
    bool HasInsert();

    [AuthorizeFactory(AuthorizeFactoryOperation.Update)]
    bool HasUpdate();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool HasDelete();
}

/// <summary>
/// Authorization implementation that allows all operations.
/// </summary>
public class DuplicateSaveWithFetchAuth : IDuplicateSaveWithFetchAuth
{
    public bool HasAccess() => true;
    public bool HasCreate() => true;
    public bool HasFetch() => true;
    public bool HasInsert() => true;
    public bool HasUpdate() => true;
    public bool HasDelete() => true;
}

/// <summary>
/// Interface for the bug reproduction class.
/// </summary>
public interface IDuplicateSaveWithFetchBug : IFactorySaveMeta
{
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}

/// <summary>
/// Entity type returned by Insert/Update (like PersonEntity in real scenario).
/// </summary>
public class TestEntityForBug
{
    public int Id { get; set; }
    public string? Name { get; set; }
}

/// <summary>
/// Bug reproduction: EXACT match of Neatoo Person scenario that triggers CS0111.
///
/// Evidence file shows the bug triggers when:
/// 1. Fetch, Insert, Update all have TWO services + CancellationToken
/// 2. Delete has only ONE service + CancellationToken (different service count!)
/// 3. Insert/Update return Task&lt;Entity?&gt;, Delete returns Task (void)
/// 4. Authorization via [AuthorizeFactory&lt;T&gt;]
///
/// Expected bug: Two SaveFactoryMethods created with same Name but different UniqueName,
/// causing duplicate Save/TrySave in interface (CS0111).
/// </summary>
[Factory]
[AuthorizeFactory<IDuplicateSaveWithFetchAuth>]
internal partial class DuplicateSaveWithFetchBug : IDuplicateSaveWithFetchBug
{
    [Create]
    public DuplicateSaveWithFetchBug([Service] ISecondTestService secondService)
    {
    }

    public bool IsDeleted { get; set; } = false;
    public bool IsNew { get; set; } = true;

    // Fetch with TWO services + CancellationToken (EXACT match of Person.Fetch)
    [Remote]
    [Fetch]
    public async Task<bool> Fetch(
        [Service] IServerOnlyService db,
        [Service] ISecondTestService secondService,
        CancellationToken cancellationToken)
    {
        IsNew = false;
        await Task.CompletedTask;
        return true;
    }

    // Insert returns Task<TestEntityForBug?> with TWO services (EXACT match of Person.Insert)
    [Remote]
    [Insert]
    public async Task<TestEntityForBug?> Insert(
        [Service] IServerOnlyService db,
        [Service] ISecondTestService secondService,
        CancellationToken cancellationToken)
    {
        IsNew = false;
        await Task.CompletedTask;
        return new TestEntityForBug { Id = 1, Name = "Test" };
    }

    // Update returns Task<TestEntityForBug?> with TWO services (EXACT match of Person.Update)
    [Remote]
    [Update]
    public async Task<TestEntityForBug?> Update(
        [Service] IServerOnlyService db,
        [Service] ISecondTestService secondService,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
        return new TestEntityForBug { Id = 1, Name = "Updated" };
    }

    // Delete returns Task (void) with ONE service (EXACT match of Person.Delete)
    // NOTE: Delete has FEWER services than Insert/Update!
    [Remote]
    [Delete]
    public async Task Delete(
        [Service] IServerOnlyService db,
        CancellationToken cancellationToken)
    {
        await Task.CompletedTask;
    }
}

#endregion

/// <summary>
/// Tests for the duplicate Save with Fetch bug.
/// If this test compiles, the bug is fixed (CS0111 would prevent compilation).
/// </summary>
public class DuplicateSaveWithFetchBugTests
{
    private readonly IServiceScope _clientScope;
    private readonly IServiceScope _serverScope;
    private readonly IDuplicateSaveWithFetchBugFactory _factory;

    public DuplicateSaveWithFetchBugTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes(
            configureClient: services =>
            {
                services.AddScoped<ISecondTestService, SecondTestService>();
                services.AddScoped<IDuplicateSaveWithFetchAuth, DuplicateSaveWithFetchAuth>();
            },
            configureServer: services =>
            {
                services.AddScoped<ISecondTestService, SecondTestService>();
                services.AddScoped<IDuplicateSaveWithFetchAuth, DuplicateSaveWithFetchAuth>();
            });

        _clientScope = client;
        _serverScope = server;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IDuplicateSaveWithFetchBugFactory>();
    }

    /// <summary>
    /// If this test compiles, the bug is fixed.
    /// If CS0111 errors occur during build, the bug is present.
    /// </summary>
    [Fact]
    public async Task Save_WithCancellationToken_NoDuplicates()
    {
        var obj = _factory.Create()!;
        Assert.True(obj.IsNew);

        // This line would fail to compile with CS0111 if duplicates exist
        var saved = await _factory.Save(obj, CancellationToken.None);
        Assert.NotNull(saved);
    }

    [Fact]
    public async Task TrySave_WithCancellationToken_NoDuplicates()
    {
        var obj = _factory.Create()!;

        // This line would fail to compile with CS0111 if duplicates exist
        var result = await _factory.TrySave(obj, CancellationToken.None);
        Assert.True(result.HasAccess);
    }

    [Fact]
    public async Task Fetch_WithCancellationToken_Works()
    {
        // Verify Fetch also works with CancellationToken
        var fetched = await _factory.Fetch(CancellationToken.None);
        // May be null if fetch returns false
    }
}
