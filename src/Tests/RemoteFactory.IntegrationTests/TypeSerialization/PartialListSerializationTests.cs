using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// A <c>partial</c> <c>[Factory]</c> <c>List&lt;T&gt;</c> carries its items across the wire in both
/// serialization formats (#119).
/// </summary>
public class PartialListSerializationTests
{
    public static IEnumerable<object[]> Formats()
    {
        yield return new object[] { SerializationFormat.Ordinal };
        yield return new object[] { SerializationFormat.Named };
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public async Task Fetch_FromClient_ListArrivesWithItems(SerializationFormat format)
    {
        var scopes = ClientServerContainers.Scopes(format);
        var factory = scopes.client.ServiceProvider.GetRequiredService<IPartialListRootFactory>();

        var root = await factory.Fetch(7);

        Assert.Equal(7, root.Id);
        Assert.Equal(["First", "Second", "Third"], root.Items.Select(i => i.Name));
        Assert.Equal([1, 2, 3], root.Items.Select(i => i.Id));
    }

    [Theory]
    [MemberData(nameof(Formats))]
    public async Task Save_FromClient_ServerReceivesItemsIncludingOneAddedOnClient(SerializationFormat format)
    {
        var scopes = ClientServerContainers.Scopes(format);
        var rootFactory = scopes.client.ServiceProvider.GetRequiredService<IPartialListRootFactory>();
        var itemFactory = scopes.client.ServiceProvider.GetRequiredService<IPartialListItemFactory>();

        var root = await rootFactory.Fetch(7);
        root.Items.Add(itemFactory.Create("Fourth"));

        var saved = await rootFactory.Save(root);

        Assert.NotNull(saved);
        Assert.Equal(["First", "Second", "Third", "Fourth"], saved.NamesSeenByServer);
        Assert.Equal(["First", "Second", "Third", "Fourth"], saved.Items.Select(i => i.Name));
    }
}
