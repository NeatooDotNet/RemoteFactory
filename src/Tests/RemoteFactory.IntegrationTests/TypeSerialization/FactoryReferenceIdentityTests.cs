using Neatoo.RemoteFactory;
using RemoteFactory.IntegrationTests.TestContainers;
using RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

namespace RemoteFactory.IntegrationTests.TypeSerialization;

/// <summary>
/// Reference identity in a [Factory] graph fetched through the client.
/// </summary>
/// <remarks>
/// Named preserves a shared reference and a back reference. Ordinal (the default)
/// duplicates the shared one and throws "possible object cycle" on the back one: the
/// generated ordinal converter writes no $id/$ref (issue #133). The Ordinal tests are
/// written the natural way and skipped as known bugs until #133 is fixed.
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

	[Fact(Skip = "Known bug #133: Ordinal duplicates a shared reference between [Factory] objects.")]
	public async Task Ordinal_SharedReference_IsOneInstanceAfterTheRoundTrip()
	{
		var team = await FetchTeam(SerializationFormat.Ordinal, withBackReference: false);

		Assert.Same(Assert.Single(team.Members), team.Lead);
	}

	[Fact(Skip = "Known bug #133: Ordinal throws 'possible object cycle' on a back reference between [Factory] objects.")]
	public async Task Ordinal_BackReference_PointsAtTheSameRoot()
	{
		var team = await FetchTeam(SerializationFormat.Ordinal, withBackReference: true);

		var member = Assert.Single(team.Members);
		Assert.Same(member, team.Lead);
		Assert.Same(team, member.Team);
	}
}
