using Neatoo.RemoteFactory;

namespace RemoteFactory.IntegrationTests.TestTargets.TypeSerialization;

// ============================================================================
// [Factory] types whose graph shares one instance in two places (Lead and
// Members[0]) and points back at its root (Member.Team). Used to test whether
// reference identity survives a client/server round trip (issue #133).
// ============================================================================

[Factory]
public partial class ReferenceTeam
{
	public string Name { get; set; } = "";
	public List<ReferenceMember> Members { get; set; } = [];
	public ReferenceMember? Lead { get; set; }

	/// <summary>
	/// One member, who is also the lead; <paramref name="withBackReference"/> sets Member.Team.
	/// </summary>
	[Remote, Fetch]
	internal void Fetch(string name, bool withBackReference, [Service] IReferenceMemberFactory memberFactory)
	{
		Name = name;
		var member = memberFactory.Create("Alice");
		if (withBackReference)
			member.Team = this;
		Members.Add(member);
		Lead = member;
	}
}

[Factory]
public partial class ReferenceMember
{
	public string Name { get; set; } = "";
	public ReferenceTeam? Team { get; set; }

	[Create]
	public void Create(string name) => Name = name;
}
