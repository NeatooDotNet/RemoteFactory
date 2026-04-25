using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.Skill;

// Sample DTO for the interface-factory-auth example.
public class EmployeeRecord
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Department { get; set; } = string.Empty;
}

#region skill-interface-factory-auth-authclass
// Auth class — scopes are Execute or Read on interface factories. CRUD scopes
// (Create/Fetch/Insert/Update/Delete) silently never match interface methods.
public interface IEmployeeQueryAuth
{
    // Parameterless — fires on every interface method call.
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool HasAccess();

    // Parameterized — Guid matched by TYPE. Fires on every interface method
    // whose signature includes a Guid parameter; the generator forwards the
    // value from the call site. Per-entity authorization without needing
    // per-operation attributes.
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    bool CanAccessEmployee(Guid id);

    // String-returning: null/empty = authorized, non-empty = denial message.
    // The string surfaces in NotAuthorizedException.Message.
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    string? CheckReadAccess();
}

public class EmployeeQueryAuth : IEmployeeQueryAuth
{
    private readonly IUserContext _userContext;

    public EmployeeQueryAuth(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public bool HasAccess() => _userContext.IsAuthenticated;

    public bool CanAccessEmployee(Guid id) =>
        _userContext.IsInRole("HRManager") || _userContext.IsInRole("Admin");

    public string? CheckReadAccess() =>
        _userContext.IsInRole("ReadOnly") ? "Tenant is read-only" : null;
}
#endregion

#region skill-interface-factory-auth-factory
// Interface factory — bare interface, NO attributes on methods.
// Placing [Create]/[Fetch]/[Insert]/[Update]/[Delete]/[Execute] on any method
// here is a compile error (NF0106). Fine-grained auth comes from parameter
// matching on the auth class, not operation attributes on the contract.
[Factory]
[AuthorizeFactory<IEmployeeQueryAuth>]
public interface IAuthorizedEmployeeQuery
{
    Task<EmployeeRecord?> GetEmployee(Guid id);
    Task<EmployeeRecord> UpdateEmployee(Guid id, string name);
    Task<IReadOnlyList<EmployeeRecord>> ListByDepartment(Guid id);
}
#endregion

#region skill-interface-factory-auth-impl
// Server-side implementation — plain class, NO [Factory], NO operation
// attributes. Registered on the server only (not the client).
public class AuthorizedEmployeeQuery : IAuthorizedEmployeeQuery
{
    public Task<EmployeeRecord?> GetEmployee(Guid id) =>
        Task.FromResult<EmployeeRecord?>(new EmployeeRecord { Id = id, Name = "Alice", Department = "HR" });

    public Task<EmployeeRecord> UpdateEmployee(Guid id, string name) =>
        Task.FromResult(new EmployeeRecord { Id = id, Name = name, Department = "HR" });

    public Task<IReadOnlyList<EmployeeRecord>> ListByDepartment(Guid id) =>
        Task.FromResult<IReadOnlyList<EmployeeRecord>>([]);
}

// DI registration (server only):
//   builder.Services.AddScoped<IAuthorizedEmployeeQuery, AuthorizedEmployeeQuery>();
//
// The auth class (EmployeeQueryAuth) is auto-registered by the generator via
// services.TryAddTransient<IEmployeeQueryAuth, EmployeeQueryAuth>() in the
// generated FactoryServiceRegistrar — no manual registration needed.
#endregion
