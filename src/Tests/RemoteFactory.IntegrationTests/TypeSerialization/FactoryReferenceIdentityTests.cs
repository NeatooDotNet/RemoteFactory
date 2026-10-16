using System.Text.Json;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Reference identity in a [Factory] graph fetched through the client.
/// </summary>
/// <remarks>
/// Both formats preserve a shared reference and a back reference. In the Ordinal format
/// the generated converter writes {"$id":"n","$values":[...]} and {"$ref":"n"} (#133).
/// </remarks>
public class FactoryReferenceIdentityTests
{
	private static async Task<ReferenceTeam> FetchTeam(SerializationFormat format, bool withBackReference)
	{
		var scopes = ClientServerContainers.Scopes(format);
		var factory = scopes.client.GetRequiredService<IReferenceTeamFactory>();
		var team = await factory.Fetch("Alpha", withBackReference, TestContext.Current.CancellationToken);
		Assert.NotNull(team);
		return team;
	}

	[Fact]
	public async Task Named_SharedReference_IsOneInstanceAfterTheRoundTrip()
	{
		var team = await FetchTeam(SerializationFormat.Named, withBackReference: false);

		Assert.Same(Assert.Single(team.Members), team.Lead);
	}

	[Fact]
	public async Task Named_BackReference_PointsAtTheSameRoot()
	{
		var team = await FetchTeam(SerializationFormat.Named, withBackReference: true);

		var member = Assert.Single(team.Members);
		Assert.Same(member, team.Lead);
		Assert.Same(team, member.Team);
	}

	[Fact]
	public async Task Ordinal_SharedReference_IsOneInstanceAfterTheRoundTrip()
	{
		var team = await FetchTeam(SerializationFormat.Ordinal, withBackReference: false);

		Assert.Same(Assert.Single(team.Members), team.Lead);
	}

	[Fact]
	public async Task Ordinal_BackReference_PointsAtTheSameRoot()
	{
		var team = await FetchTeam(SerializationFormat.Ordinal, withBackReference: true);

		var member = Assert.Single(team.Members);
		Assert.Same(member, team.Lead);
		Assert.Same(team, member.Team);
	}

	/// <summary>
	/// The first appearance carries the $id; the repeat is a $ref to it. Lead sorts
	/// before Members, so the member is written in full under Lead.
	/// </summary>
	[Fact]
	public void Ordinal_Wire_RepeatIsARefToTheFirstAppearance()
	{
		var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
		var serializer = scopes.server.GetRequiredService<INeatooJsonSerializer>();
		var member = new ReferenceMember { Name = "Alice" };
		var team = new ReferenceTeam { Name = "Alpha", Members = [member], Lead = member };

		using var json = JsonDocument.Parse(serializer.Serialize(team)!);

		var values = json.RootElement.GetProperty("$values");
		Assert.Equal(["Lead", "Members", "Name"], ReferenceTeam.PropertyNames);
		var lead = values[0];
		var leadId = lead.GetProperty("$id").GetString();
		Assert.Equal("Alice", lead.GetProperty("$values")[0].GetString());
		var listedMember = Assert.Single(values[1].GetProperty("$values").EnumerateArray());
		Assert.Equal(leadId, listedMember.GetProperty("$ref").GetString());
	}

	/// <summary>
	/// A bare array, as 1.10.6 and earlier wrote it, is still read: no identity to restore.
	/// </summary>
	[Fact]
	public void Ordinal_BareArrayFromAnEarlierVersion_IsStillRead()
	{
		var scopes = ClientServerContainers.Scopes(SerializationFormat.Ordinal);
		var serializer = scopes.client.GetRequiredService<INeatooJsonSerializer>();

		var team = serializer.Deserialize<ReferenceTeam>("""[["Alice",null],[["Alice",null]],"Alpha"]""");

		Assert.NotNull(team);
		Assert.Equal("Alpha", team.Name);
		Assert.Equal("Alice", team.Lead?.Name);
		Assert.Equal("Alice", Assert.Single(team.Members).Name);
	}

	private static async Task<InitReferenceTeam> FetchInitTeam(SerializationFormat format, bool withBackReference)
	{
		var scopes = ClientServerContainers.Scopes(format);
		var factory = scopes.client.GetRequiredService<IInitReferenceTeamFactory>();
		var team = await factory.Fetch(withBackReference, TestContext.Current.CancellationToken);
		Assert.NotNull(team);
		return team;
	}

	/// <summary>
	/// A type with an init-only property is built from its properties, after they are
	/// read. A later reference to it, once it is built, still resolves.
	/// </summary>
	[Fact]
	public async Task Ordinal_SharedReferenceToInitOnlyType_IsOneInstanceAfterTheRoundTrip()
	{
		var team = await FetchInitTeam(SerializationFormat.Ordinal, withBackReference: false);

		Assert.Same(Assert.Single(team.Members), team.Lead);
	}

	/// <summary>
	/// A back reference into a type that is still being built cannot resolve; the error
	/// names the type and what to change.
	/// </summary>
	[Fact]
	public async Task Ordinal_BackReferenceToInitOnlyType_ThrowsNamingTheType()
	{
		var ex = await Assert.ThrowsAnyAsync<JsonException>(
			() => FetchInitTeam(SerializationFormat.Ordinal, withBackReference: true));

		Assert.Contains("back reference to InitReferenceTeam, which is still being read", ex.Message, StringComparison.Ordinal);
		Assert.Contains("init-only or required property", ex.Message, StringComparison.Ordinal);
	}

	/// <summary>
	/// Named builds the object first and sets init-only properties afterwards, so the
	/// same back reference resolves there.
	/// </summary>
	[Fact]
	public async Task Named_BackReferenceToInitOnlyType_PointsAtTheSameRoot()
	{
		var team = await FetchInitTeam(SerializationFormat.Named, withBackReference: true);

		var member = Assert.Single(team.Members);
		Assert.Same(member, team.Lead);
		Assert.Same(team, member.Team);
	}
}
