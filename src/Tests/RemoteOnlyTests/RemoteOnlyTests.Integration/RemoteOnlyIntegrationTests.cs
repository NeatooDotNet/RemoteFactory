extern alias ClientAssembly;
extern alias ServerAssembly;

using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.Internal;
using Xunit;

// Type aliases for clarity
using ClientTestAggregateFactory = ClientAssembly::RemoteOnlyTests.Domain.TestAggregateFactory;
using ClientITestAggregateFactory = ClientAssembly::RemoteOnlyTests.Domain.ITestAggregateFactory;
using ClientITestChildFactory = ClientAssembly::RemoteOnlyTests.Domain.ITestChildFactory;
using ClientITestChildListFactory = ClientAssembly::RemoteOnlyTests.Domain.ITestChildListFactory;
using ClientITestAggregate = ClientAssembly::RemoteOnlyTests.Domain.ITestAggregate;
using ClientITestChildList = ClientAssembly::RemoteOnlyTests.Domain.ITestChildList;

using ServerTestAggregateFactory = ServerAssembly::RemoteOnlyTests.Domain.TestAggregateFactory;
using ServerITestAggregateFactory = ServerAssembly::RemoteOnlyTests.Domain.ITestAggregateFactory;

namespace RemoteOnlyTests.Integration;

/// <summary>
/// Integration tests that verify FactoryMode.RemoteOnly works correctly.
/// Uses separate client (RemoteOnly) and server (Full) assemblies.
/// </summary>
public class RemoteOnlyIntegrationTests
{
    // ========================================================================
    // Assembly Verification Tests
    // Verify the client and server assemblies are compiled with correct modes
    // ========================================================================

    [Fact]
    public void ClientFactory_HasNoLocalCreateMethod()
    {
        // RemoteOnly mode should NOT generate LocalCreate
        var clientFactoryType = typeof(ClientTestAggregateFactory);
        var localCreate = clientFactoryType.GetMethod("LocalCreate");

        Assert.Null(localCreate);
    }

    [Fact]
    public void ClientFactory_HasRemoteCreateMethod()
    {
        // RemoteOnly mode should generate RemoteCreate
        var clientFactoryType = typeof(ClientTestAggregateFactory);
        var remoteCreate = clientFactoryType.GetMethod("RemoteCreate");

        Assert.NotNull(remoteCreate);
    }

    [Fact]
    public void ClientFactory_HasSingleConstructor()
    {
        // RemoteOnly mode generates only the remote constructor
        var clientFactoryType = typeof(ClientTestAggregateFactory);
        var constructors = clientFactoryType.GetConstructors();

        Assert.Single(constructors);
        // The constructor should require IMakeRemoteDelegateRequest
        Assert.Contains(constructors[0].GetParameters(),
            p => p.ParameterType == typeof(IMakeRemoteDelegateRequest));
    }

    [Fact]
    public void ServerFactory_HasLocalCreateMethod()
    {
        // Full mode SHOULD generate LocalCreate
        var serverFactoryType = typeof(ServerTestAggregateFactory);
        var localCreate = serverFactoryType.GetMethod("LocalCreate");

        Assert.NotNull(localCreate);
    }

    [Fact]
    public void ServerFactory_HasTwoConstructors()
    {
        // Full mode generates both local and remote constructors
        var serverFactoryType = typeof(ServerTestAggregateFactory);
        var constructors = serverFactoryType.GetConstructors();

        Assert.Equal(2, constructors.Length);
    }

    // ========================================================================
    // Create Operation Tests
    // ========================================================================

    [Fact]
    public async Task Create_ClientToServer_InitializesAggregate()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();

        var aggregate = await factory.Create();

        Assert.NotNull(aggregate);
        Assert.NotEqual(Guid.Empty, aggregate.Id);
        Assert.True(aggregate.IsNew);
    }

    [Fact]
    public async Task Create_InitializesChildListOnServer()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();

        var aggregate = await factory.Create();

        // The child list was created by the SERVER's ITestChildListFactory
        Assert.NotNull(aggregate.Children);
        Assert.IsAssignableFrom<ClientITestChildList>(aggregate.Children);
        Assert.Empty(aggregate.Children); // Starts empty
    }

    // ========================================================================
    // Fetch Operation Tests
    // ========================================================================

    [Fact]
    public async Task Fetch_ClientToServer_LoadsAggregate()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();
        var testId = Guid.NewGuid();

        var aggregate = await factory.Fetch(testId);

        Assert.NotNull(aggregate);
        Assert.Equal(testId, aggregate.Id);
        Assert.False(aggregate.IsNew);
    }

    [Fact]
    public async Task Fetch_LoadsChildrenFromServer()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();

        var aggregate = await factory.Fetch(Guid.NewGuid());

        Assert.NotNull(aggregate.Children);
        Assert.Equal(3, aggregate.Children.Count); // TestDataStore returns 3 children
    }

    [Fact]
    public async Task Fetch_ChildPropertiesPreservedThroughSerialization()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();

        var aggregate = await factory.Fetch(Guid.NewGuid());

        Assert.All(aggregate.Children, child =>
        {
            Assert.NotEqual(Guid.Empty, child.Id);
            Assert.NotNull(child.Name);
            Assert.True(child.Value > 0);
        });
    }

    // ========================================================================
    // Save Operation Tests - Insert
    // ========================================================================

    [Fact]
    public async Task Save_NewAggregate_CallsInsert()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();
        var aggregate = await factory.Create();
        aggregate.Name = "TestInsert";

        var saved = await factory.Save(aggregate);

        Assert.NotNull(saved);
        Assert.False(saved.IsNew); // Insert sets IsNew = false
        Assert.Equal("TestInsert", saved.Name);
    }

    // ========================================================================
    // Save Operation Tests - Update
    // ========================================================================

    [Fact]
    public async Task Save_ExistingAggregate_CallsUpdate()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        aggregate.Name = "UpdatedName";

        var saved = await factory.Save(aggregate);

        Assert.NotNull(saved);
        Assert.Equal("UpdatedName", saved.Name);
    }

    // ========================================================================
    // Save Operation Tests - Delete
    // ========================================================================

    [Fact]
    public async Task Save_DeletedAggregate_CallsDelete()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();
        var aggregate = await factory.Fetch(Guid.NewGuid());
        aggregate.IsDeleted = true;

        var saved = await factory.Save(aggregate);

        Assert.NotNull(saved);
        Assert.True(saved.IsDeleted);
    }

    // ========================================================================
    // Local Create Tests - Child entities without [Remote]
    // ========================================================================

    [Fact]
    public void LocalCreate_Child_WorksOnClientContainer()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestChildFactory>();

        // Local [Create] (no [Remote]) should work on client
        var child = factory.Create();

        Assert.NotNull(child);
        Assert.NotEqual(Guid.Empty, child.Id);
    }

    [Fact]
    public void LocalCreate_ChildList_WorksOnClientContainer()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestChildListFactory>();

        // Local [Create] (no [Remote]) should work on client
        var list = factory.Create();

        Assert.NotNull(list);
        Assert.Empty(list);
    }

    // ========================================================================
    // Child Modification Round-Trip Tests
    // ========================================================================

    [Fact]
    public async Task AddChild_OnClient_PreservedThroughRoundTrip()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var aggregateFactory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();
        var childFactory = client.ServiceProvider.GetRequiredService<ClientITestChildFactory>();

        // Create aggregate on server, returned to client
        var aggregate = await aggregateFactory.Create();

        // Add child on client
        var newChild = childFactory.Create();
        newChild.Name = "AddedOnClient";
        newChild.Value = 100m;
        aggregate.Children.Add(newChild);

        // Save - goes to server and back
        var saved = await aggregateFactory.Save(aggregate);

        Assert.Single(saved!.Children);
        Assert.Contains(saved.Children, c => c.Name == "AddedOnClient" && c.Value == 100m);
    }

    [Fact]
    public async Task ModifyChild_OnClient_PreservedThroughRoundTrip()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();

        // Fetch aggregate with children
        var aggregate = await factory.Fetch(Guid.NewGuid());
        var firstChildId = aggregate.Children[0].Id;
        aggregate.Children[0].Name = "ModifiedOnClient";
        aggregate.Children[0].Value = 999m;

        // Save and verify modification preserved
        var saved = await factory.Save(aggregate);

        var modifiedChild = saved!.Children.First(c => c.Id == firstChildId);
        Assert.Equal("ModifiedOnClient", modifiedChild.Name);
        Assert.Equal(999m, modifiedChild.Value);
    }

    [Fact]
    public async Task RemoveChild_OnClient_PreservedThroughRoundTrip()
    {
        var (client, _) = RemoteOnlyContainers.Scopes();
        var factory = client.ServiceProvider.GetRequiredService<ClientITestAggregateFactory>();

        // Fetch aggregate with 3 children
        var aggregate = await factory.Fetch(Guid.NewGuid());
        Assert.Equal(3, aggregate.Children.Count);

        var removedId = aggregate.Children[0].Id;
        aggregate.Children.RemoveAt(0);

        // Save and verify removal
        var saved = await factory.Save(aggregate);

        Assert.Equal(2, saved!.Children.Count);
        Assert.DoesNotContain(saved.Children, c => c.Id == removedId);
    }

    // ========================================================================
    // Interface Consistency Tests
    // ========================================================================

    [Fact]
    public void FactoryInterface_IdenticalBetweenClientAndServer()
    {
        // Both assemblies should generate the same interface
        var clientInterface = typeof(ClientITestAggregateFactory);
        var serverInterface = typeof(ServerITestAggregateFactory);

        // Get method names (excluding object methods)
        var clientMethods = clientInterface.GetMethods()
            .Where(m => m.DeclaringType == clientInterface)
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToList();

        var serverMethods = serverInterface.GetMethods()
            .Where(m => m.DeclaringType == serverInterface)
            .Select(m => m.Name)
            .OrderBy(n => n)
            .ToList();

        Assert.Equal(serverMethods, clientMethods);
    }
}
