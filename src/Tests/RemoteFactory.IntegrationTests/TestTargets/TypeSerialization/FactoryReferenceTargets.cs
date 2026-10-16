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

// ============================================================================
// The same graph with an init-only property on each type. The Ordinal converter
// builds such a type from its properties, so a shared reference to it resolves
// but a back reference into it, read while it is still being built, fails (#133).
// ============================================================================

[Factory]
public partial class InitReferenceTeam
{
	public string Code { get; init; } = "T";
	public List<InitReferenceMember> Members { get; set; } = [];
	public InitReferenceMember? Lead { get; set; }

	/// <summary>
	/// One member, who is also the lead; <paramref name="withBackReference"/> sets Member.Team.
	/// </summary>
	[Remote, Fetch]
	internal void Fetch(bool withBackReference, [Service] IInitReferenceMemberFactory memberFactory)
	{
		var member = memberFactory.Create();
		if (withBackReference)
			member.Team = this;
		Members.Add(member);
		Lead = member;
	}
}

[Factory]
public partial class InitReferenceMember
{
	public string Code { get; init; } = "M";
	public InitReferenceTeam? Team { get; set; }

	[Create]
	public void Create() { }
}
