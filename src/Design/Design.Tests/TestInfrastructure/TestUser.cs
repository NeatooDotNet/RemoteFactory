using Design.Domain.Aggregates;
using Design.Domain.Services;

namespace Design.Tests.TestInfrastructure;

/// <summary>
/// The signed-in user in the in-process containers: signed in, with every order
/// role, until a test takes one away.
/// </summary>
public sealed class TestUser : ICurrentUser
{
    public bool IsAuthenticated { get; set; } = true;

    public HashSet<string> Roles { get; } = [OrderRoles.Sales, OrderRoles.Support, OrderRoles.Manager];

    public bool IsInRole(string role) => IsAuthenticated && Roles.Contains(role);
}
