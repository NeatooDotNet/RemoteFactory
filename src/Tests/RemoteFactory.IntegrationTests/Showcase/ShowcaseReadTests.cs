using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.Showcase;

#region Target Class

/// <summary>
/// Interface for ShowcaseRead.
/// </summary>
public interface IShowcaseRead
{
    List<int>? InitProperty { get; init; }
    List<int> IntList { get; set; }
}

/// <summary>
/// Target class demonstrating various Create patterns.
/// </summary>
[Factory]
internal class ShowcaseRead : IShowcaseRead
{
    public ShowcaseRead([Service] IService service)
    {
        IntList = default!;
        Assert.NotNull(service);
    }

    [Create]
    public ShowcaseRead(List<int> intList, [Service] IService service)
    {
        // For non-nullable properties
        Assert.NotNull(service);
        IntList = intList;
    }

    public List<int> IntList { get; set; }
    public List<int>? InitProperty { get; init; }

    [Create]
    public void CreateVoid(List<int> intList) { IntList = intList; }

    [Create]
    public bool CreateBool(List<int> intList) { return false; }

    [Create]
    public Task CreateTask(List<int> intList) { IntList = intList; return Task.CompletedTask; }

    [Create]
    public Task<bool> CreateTaskBool(List<int> intList) { IntList = intList; return Task.FromResult(false); }

    [Create]
    public Task CreateService(List<int> intList, [Service] IService service)
    {
        IntList = intList; Assert.NotNull(service); return Task.CompletedTask;
    }

    [Create]
    public static async Task<IShowcaseRead> CreateStatic(List<int> intList, [Service] IService service)
    {
        await Task.CompletedTask;

        // For async construction, init properties
        Assert.NotNull(service);
        var showcaseCreate = new ShowcaseRead(service)
        {
            IntList = intList,
            InitProperty = intList
        };
        return showcaseCreate;
    }

    [Remote]
    [Create]
    public void CreateRemote(List<int> intList, [Service] IServerOnlyService service)
    {
        IntList = intList;
        Assert.NotNull(service);
    }

    [Create]
    public Task CreateRemoteClientFail(List<int> intList, [Service] IServerOnlyService service)
    {
        // Fails - Verifying that this cannot be called on the client
        Assert.Fail(); return Task.CompletedTask;
    }

    [Fetch]
    public void FetchVoid(int id) { }
}

#endregion

/// <summary>
/// Integration tests for various Create patterns.
/// </summary>
public class ShowcaseReadTests
{
    private readonly IServiceScope _clientScope;
    private readonly IShowcaseReadFactory _factory;

    public ShowcaseReadTests()
    {
        var (server, client, _) = ClientServerContainers.Scopes();
        _clientScope = client;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IShowcaseReadFactory>();
    }

    [Fact]
    public void ShowcaseRead_Constructor()
    {
        var intList = new List<int> { 1, 2, 3 };
        var createConstructor = _factory.Create(intList);
        Assert.Equal(intList, createConstructor.IntList);
    }

    [Fact]
    public void ShowcaseRead_CreateVoid()
    {
        var intList = new List<int> { 1, 2, 3 };
        var createVoid = _factory.CreateVoid(intList);
        Assert.Equal(intList, createVoid.IntList);
    }

    [Fact]
    public void ShowcaseRead_CreateBool()
    {
        var intList = new List<int> { 1, 2, 3 };
        var createBool = _factory.CreateBool(intList);
        Assert.Null(createBool);
    }

    [Fact]
    public async Task ShowcaseRead_CreateTask()
    {
        var intList = new List<int> { 1, 2, 3 };
        var createTask = await _factory.CreateTask(intList);
        Assert.Equal(intList, createTask.IntList);
    }

    [Fact]
    public async Task ShowcaseRead_CreateTaskBool()
    {
        var intList = new List<int> { 1, 2, 3 };
        var createTaskBool = await _factory.CreateTaskBool(intList);
        Assert.Null(createTaskBool);
    }

    [Fact]
    public async Task ShowcaseRead_CreateService()
    {
        var intList = new List<int> { 1, 2, 3 };
        var createService = await _factory.CreateService(intList);
        Assert.Equal(intList, createService.IntList);
    }

    [Fact]
    public async Task ShowcaseRead_CreateStatic()
    {
        var intList = new List<int> { 1, 2, 3 };
        var createStatic = await _factory.CreateStatic(intList);
        Assert.Equal(intList, createStatic.IntList);
        Assert.Equal(intList, createStatic.InitProperty);
    }

    [Fact]
    public async Task ShowcaseRead_CreateRemoteOnlyClientFail()
    {
        var intList = new List<int> { 1, 2, 3 };
        await Assert.ThrowsAsync<InvalidOperationException>(() => _factory.CreateRemoteClientFail(intList));
    }

    [Fact]
    public async Task ShowcaseRead_CreateRemote()
    {
        var intList = new List<int> { 1, 2, 3 };
        var result = await _factory.CreateRemote(intList);
        Assert.Equal(intList, result.IntList);
    }
}
