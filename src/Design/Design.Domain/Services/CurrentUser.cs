// =============================================================================
// DESIGN SOURCE OF TRUTH: The Signed-In User, on Both Tiers
// =============================================================================
//
// Authorization classes decide from the signed-in user. ICurrentUser is that
// user as the domain sees it: whether someone is signed in, and their roles.
//
// DESIGN DECISION: Registered on both tiers
//
// An [AuthorizeFactory<T>] class runs wherever its operation runs: a local
// Create and a Can* check on the client, a [Remote] Fetch or Save on the
// server. So both containers register an ICurrentUser:
// - the server builds it from HttpContext.User, which UseAuthentication sets
//   (Design.Server's ServerServices);
// - a Blazor client builds it from its AuthenticationStateProvider. Design's
//   client signs nobody in, so it registers an anonymous one.
//
// DESIGN DECISION: The implementation's name does not match the interface
//
// RegisterMatchingName maps IFoo to Foo. A class named CurrentUser would be
// registered by convention with a ClaimsPrincipal DI cannot build, and the
// server would fail its start-up validation. ClaimsUser is registered
// explicitly, with the principal it reads.
//
// =============================================================================

using System.Security.Claims;

namespace Design.Domain.Services;

/// <summary>
/// The signed-in user, as authorization classes read it.
/// </summary>
public interface ICurrentUser
{
    bool IsAuthenticated { get; }
    bool IsInRole(string role);
}

/// <summary>
/// An <see cref="ICurrentUser"/> read from a claims principal.
/// </summary>
public sealed class ClaimsUser(ClaimsPrincipal principal) : ICurrentUser
{
    /// <summary>Nobody signed in.</summary>
    public static ClaimsUser Anonymous { get; } = new(new ClaimsPrincipal(new ClaimsIdentity()));

    public bool IsAuthenticated => principal.Identity?.IsAuthenticated ?? false;

    public bool IsInRole(string role) => IsAuthenticated && principal.IsInRole(role);
}
