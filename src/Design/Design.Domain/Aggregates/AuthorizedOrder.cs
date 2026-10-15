// =============================================================================
// DESIGN SOURCE OF TRUTH: [AuthorizeFactory<T>] Custom Domain Authorization
// =============================================================================
//
// This file demonstrates custom domain authorization using [AuthorizeFactory<T>]
// on a CLASS FACTORY aggregate root: a local Create, and [Remote] internal
// reads and writes.
//
// DESIGN PATTERN: Internal class with public interface + AuthorizeFactory
//
// Combines two patterns:
// 1. Internal class + public interface (like Order.cs) for IL trimming
// 2. [AuthorizeFactory<T>] for domain-specific authorization
//
// The generator produces Can* methods on the factory interface:
//   - CanCreate(), CanFetch() for read operations
//   - CanInsert(), CanUpdate(), CanDelete() for the writes
//   - CanSave() aggregating all write auth checks
//
// DESIGN DECISION: Separate from SecureOrder
//
// SecureOrder demonstrates [AspAuthorize] (ASP.NET Core policy-based auth).
// AuthorizedOrder demonstrates [AuthorizeFactory<T>] (custom domain auth).
// Keeping them separate avoids muddying the demonstration of each pattern.
//
// DESIGN DECISION: Create is local; the reads and writes are [Remote] internal
//
// A root's Create is local (VisionAlignment D14): the client builds the new
// order, and the auth class runs the Create rule on the client, reading the
// client's ICurrentUser. Fetch and the writes are [Remote] internal: they run
// on the server, where the same rules read the server's ICurrentUser, and
// their bodies trim from client assemblies.
//
// GENERATOR BEHAVIOR: Can* method visibility derived from auth methods
//
// Can* method guard behavior derives from the AUTH CLASS METHODS, not
// the factory method. Because IAuthorizedOrderAuth has all public methods
// (no [Remote], not internal), the generated Can* methods have NO
// IsServerRuntime guard and run on the client. If auth methods needed
// server-only services, they would use [Remote] internal on the auth
// interface, which would add the guard and route Can* to the server.
// See ShowcaseAuthRemoteTests.cs for the [Remote] auth method pattern.
//
// GENERATOR BEHAVIOR: CanSave aggregation
//
// CanSave aggregates the distinct auth methods from Insert, Update, and Delete.
// With our auth interface:
// - Insert auth methods: HasAccess() (Write scope) + CanCreate() (Create | Insert scope)
// - Update auth methods: HasAccess() (Write scope matches Update)
// - Delete auth methods: HasAccess() (Write scope matches Delete) + CanDelete() (Delete scope)
// - CanSave distinct set: {HasAccess(), CanCreate(), CanDelete()}
//
// CanCreate() covers Insert as well as Create: the local Create checks it on
// the client, and the server re-checks it when the new order is saved.
// CanFetch() is NOT part of CanSave; its Fetch scope matches no write.
//
// GENERATOR BEHAVIOR: Auth failure behavior
//
// - Create/Fetch return null when authorization fails
// - Save throws NotAuthorizedException when authorization fails
// - TrySave returns the refusal as Authorized<T> with HasAccess=false, without throwing
//
// =============================================================================

using Neatoo.RemoteFactory;

namespace Design.Domain.Aggregates;

/// <summary>
/// Public interface for AuthorizedOrder aggregate root.
/// Exposed to callers outside the assembly (e.g., Blazor client).
/// </summary>
public interface IAuthorizedOrder : IFactorySaveMeta
{
    int Id { get; set; }
    string CustomerName { get; set; }
    decimal Total { get; set; }
    new bool IsNew { get; set; }
    new bool IsDeleted { get; set; }
}

/// <summary>
/// Aggregate root demonstrating [AuthorizeFactory] with a local Create and [Remote] internal reads and writes.
/// </summary>
/// <remarks>
/// DESIGN DECISION: Internal class with public interface
///
/// Same pattern as Order.cs. The class is internal so the generator uses
/// IAuthorizedOrder in factory interface signatures. This enables IL trimming
/// of the concrete class on Blazor WASM clients.
///
/// DESIGN DECISION: IFactorySaveMeta for Save routing + TrySave
///
/// IFactorySaveMeta enables the generated Save() method that routes to
/// Insert/Update/Delete based on IsNew and IsDeleted. Combined with
/// [AuthorizeFactory], this also generates TrySave() which catches
/// NotAuthorizedException and returns Authorized&lt;IAuthorizedOrder&gt;.
/// </remarks>
#region authorization-apply
[Factory]
[AuthorizeFactory<IAuthorizedOrderAuth>]
internal partial class AuthorizedOrder : IAuthorizedOrder, IFactorySaveMeta, IFactoryOnCompleteAsync
#endregion
{
    // -------------------------------------------------------------------------
    // Properties
    // -------------------------------------------------------------------------

    public int Id { get; set; }
    public string CustomerName { get; set; } = string.Empty;
    public decimal Total { get; set; }

    // -------------------------------------------------------------------------
    // IFactorySaveMeta Implementation
    // -------------------------------------------------------------------------

    public bool IsNew { get; set; } = true;
    public bool IsDeleted { get; set; }

    // -------------------------------------------------------------------------
    // Lifecycle Hook: Reset IsNew after Insert
    //
    // DESIGN DECISION: Use IFactoryOnCompleteAsync to reset IsNew
    //
    // Same pattern as Order.cs. After a successful Insert, IsNew becomes false
    // so subsequent Save calls route to Update instead of Insert.
    // -------------------------------------------------------------------------

    public Task FactoryCompleteAsync(FactoryOperation factoryOperation)
    {
        if (factoryOperation == FactoryOperation.Insert)
        {
            IsNew = false;
        }
        return Task.CompletedTask;
    }

    // -------------------------------------------------------------------------
    // Factory Operations
    //
    // Create is public and local (D14). Fetch and the writes are [Remote] internal:
    // - [Remote] causes promotion to public on the factory interface
    // - internal visibility enables IL trimming of method bodies on client
    // - Can* method behavior derives from auth methods, not these factory methods
    // -------------------------------------------------------------------------

    [Create]
    public void Create(string customerName)
    {
        CustomerName = customerName;
        IsNew = true;
    }

    [Remote, Fetch]
    internal void Fetch(int id)
    {
        // Simulate loading from database
        Id = id;
        CustomerName = $"Customer_{id}";
        Total = 100.00m;
        IsNew = false;
    }

    [Remote, Insert]
    internal Task Insert()
    {
        // Simulate database insert with ID assignment
        Id = Random.Shared.Next(1000, 9999);
        return Task.CompletedTask;
    }

    [Remote, Update]
    internal Task Update()
    {
        // Simulate database update
        return Task.CompletedTask;
    }

    [Remote, Delete]
    internal Task Delete()
    {
        // Simulate database delete
        return Task.CompletedTask;
    }
}
