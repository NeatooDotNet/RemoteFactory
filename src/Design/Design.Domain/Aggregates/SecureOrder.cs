// =============================================================================
// DESIGN SOURCE OF TRUTH: ASP.NET Core Policy-Based Authorization
// =============================================================================
//
// This file demonstrates using [AspAuthorize] for ASP.NET Core policy-based
// authorization on factory methods. Use this when you want to leverage
// ASP.NET Core's authorization infrastructure (policies, roles, claims).
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Order with ASP.NET Core policy-based authorization.
/// </summary>
/// <remarks>
/// DESIGN DECISION: [AspAuthorize] for ASP.NET Core integration
///
/// Use [AspAuthorize] when:
/// - You're using ASP.NET Core Identity
/// - Authorization is role or policy-based
/// - You want to leverage existing policy infrastructure
///
/// Use [AuthorizeFactory] (custom auth class) when:
/// - Authorization logic is domain-specific
/// - You need testable auth without HTTP context
/// - Different operations have different complex rules
///
/// DID NOT DO THIS: Apply [AspAuthorize] at class level for all operations
///
/// Reasons:
/// 1. Different operations often need different authorization
/// 2. Class-level auth can be combined with method-level for granularity
/// 3. Some operations (like Create for a new draft) may not need auth
/// </remarks>
[Factory]
public partial class SecureOrder : IFactorySaveMeta
{
    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }
    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // -------------------------------------------------------------------------
    // Create - No authorization required
    //
    // DESIGN DECISION: Create operations often don't need authorization
    //
    // Creating a new, unsaved entity doesn't access data. Authorization
    // is checked when the entity is saved (Insert).
    // -------------------------------------------------------------------------

    [Remote, Create]
    public void Create(string customerName)
    {
        CustomerName = customerName;
        IsNew = true;
    }

    // -------------------------------------------------------------------------
    // Fetch - Requires authentication
    //
    // GENERATOR BEHAVIOR: The factory checks the "RequireAuthenticated" policy
    // before executing the Fetch operation. If authorization fails, the
    // factory returns null.
    // -------------------------------------------------------------------------

    /// <summary>
    /// Fetches an order - requires authenticated user.
    /// </summary>
    /// <remarks>
    /// DESIGN DECISION: Policy-based authorization with [AspAuthorize]
    ///
    /// The "RequireAuthenticated" policy is defined in Program.cs:
    /// <code>
    /// options.AddPolicy("RequireAuthenticated", policy =>
    ///     policy.RequireAuthenticatedUser());
    /// </code>
    ///
    /// COMMON MISTAKE: Forgetting to register the policy
    ///
    /// If the policy isn't registered in Program.cs, authorization will fail
    /// with a runtime error. Always verify policies are configured.
    /// </remarks>
    [Remote, Fetch]
    [AspAuthorize("RequireAuthenticated")]
    public void Fetch(int id)
    {
        Id = id;
        CustomerName = $"Customer_{id}";
        Total = 100.00m;
        IsNew = false;
    }

    // -------------------------------------------------------------------------
    // Insert - Requires Admin or Manager role
    //
    // DESIGN DECISION: Role-based authorization with Roles property
    //
    // Use the Roles property instead of a policy when authorization is
    // simply "user must have one of these roles". Policies are better
    // for complex requirements (claims, custom logic).
    // -------------------------------------------------------------------------

    /// <summary>
    /// Inserts a new order - requires Admin or Manager role.
    /// </summary>
    [Remote, Insert]
    [AspAuthorize(Roles = "Admin,Manager")]
    public Task Insert()
    {
        Id = Random.Shared.Next(1000, 9999);
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Update - Requires both policy AND role
    //
    // DESIGN DECISION: Multiple [AspAuthorize] attributes for defense in depth
    //
    // When multiple [AspAuthorize] attributes are applied, ALL must pass.
    // This enables layered authorization:
    // 1. First check: User must be authenticated
    // 2. Second check: User must have the Manager role
    //
    // DID NOT DO THIS: OR logic between attributes
    //
    // Reasons:
    // 1. AND logic is safer (more restrictive)
    // 2. OR logic can be achieved within a single policy
    // 3. Matches ASP.NET Core's [Authorize] behavior
    // -------------------------------------------------------------------------

    /// <summary>
    /// Updates an order - requires authentication AND Manager role.
    /// </summary>
    [Remote, Update]
    [AspAuthorize("RequireAuthenticated")]
    [AspAuthorize(Roles = "Manager")]
    public Task Update()
    {
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Delete - Requires Admin role only
    // -------------------------------------------------------------------------

    /// <summary>
    /// Deletes an order - requires Admin role.
    /// </summary>
    [Remote, Delete]
    [AspAuthorize(Roles = "Admin")]
    public Task Delete()
    {
        return Task.CompletedTask;
    }
}
