using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.Shared;
using RemoteFactory.IntegrationTests.TestContainers;

namespace RemoteFactory.IntegrationTests.Showcase;

#region Target Class

/// <summary>
/// Interface for ShowcaseSave.
/// </summary>
public interface IShowcaseSave : IFactorySaveMeta
{
}

/// <summary>
/// Target class demonstrating various Save patterns.
/// </summary>
[Factory]
public class ShowcaseSave : IShowcaseSave
{
    [Create]
    public ShowcaseSave() { }

    public bool IsDeleted { get; set; } = false;

    public bool IsNew { get; set; } = true;

    [Insert]
    public void Insert([Service] IService service) { IsNew = false; Assert.NotNull(service); }

    [Update]
    public void Update([Service] IService service) { }

    [Delete]
    public void Delete([Service] IService service) { }

    [Insert]
    public void InsertMatchedByParamType(int a) { IsNew = false; }

    [Update]
    public void UpdateMatchedByParamType(int b) { }

    [Delete]
    public void DeleteMatchedByParamType(int c) { }

    [Insert]
    public void InsertNoDeleteNotNullable() { IsNew = false; }

    [Update]
    public void UpdateNoDeleteNotNullable() { }

    [Insert]
    public Task InsertTask() { IsNew = false; return Task.CompletedTask; }

    [Update]
    public Task UpdateTask() { return Task.CompletedTask; }

    [Delete]
    public Task DeleteTask() { return Task.CompletedTask; }

    [Remote]
    [Insert]
    public void InsertRemote([Service] IServerOnlyService service) { IsNew = false; Assert.NotNull(service); }

    [Remote]
    [Update]
    public void UpdateRemote([Service] IServerOnlyService service) { }
}

#endregion

/// <summary>
/// Integration tests for various Save patterns.
/// </summary>
public class ShowcaseSaveTests
{
    private readonly IServiceScope _clientScope;
    private readonly IShowcaseSaveFactory _factory;

    public ShowcaseSaveTests()
    {
        var (client, server, _) = ClientServerContainers.Scopes();
        _clientScope = client;
        _factory = _clientScope.ServiceProvider.GetRequiredService<IShowcaseSaveFactory>();
    }

    [Fact]
    public void ShowcaseSaveTests_Create()
    {
        var result = _factory.Create();
        Assert.NotNull(result);
        Assert.True(result.IsNew);
    }

    [Fact]
    public void ShowcaseSaveTests_Save()
    {
        var result = _factory.Create();
        var saved = _factory.Save(result);
        Assert.False(saved!.IsNew);
    }

    [Fact]
    public void ShowcaseSaveTests_SaveNoDeleteNotNullable()
    {
        var result = _factory.Create();
        var saved = _factory.SaveNoDeleteNotNullable(result);
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task ShowcaseSaveTests_SaveTask()
    {
        var result = _factory.Create();
        var saved = await _factory.SaveTask(result);
        Assert.False(saved!.IsNew);
    }

    [Fact]
    public void ShowcaseSaveTests_SaveMatchedByParamType()
    {
        var result = _factory.Create();
        var saved = _factory.SaveMatchedByParamType(result, 1)!;
        Assert.False(saved.IsNew);
    }

    [Fact]
    public async Task ShowcaseSaveTests_SaveRemote()
    {
        var result = _factory.Create();
        var saved = await _factory.SaveRemote(result);
        Assert.False(saved.IsNew);
    }
}
