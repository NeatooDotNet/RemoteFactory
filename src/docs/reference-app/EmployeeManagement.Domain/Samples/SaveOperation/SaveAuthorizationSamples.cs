using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Samples.SaveOperation;

// ============================================================================
// Granular Authorization for Save Operations
// ============================================================================

#region save-authorization
// Granular authorization: CanCreate for Create, CanWrite for Insert/Update/Delete
public interface IEmployeeWriteAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)] bool CanCreate();  // Any authenticated
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)] bool CanWrite();    // HR or Admin only
}

[Factory]
[AuthorizeFactory<IEmployeeWriteAuth>]  // Factory checks auth before routing
public partial class AuthorizedEmployeeWrite : IFactorySaveMeta { /* ... */ }
#endregion

// Full implementation
public class EmployeeWriteAuth : IEmployeeWriteAuth
{
    private readonly IUserContext _userContext;
    public EmployeeWriteAuth(IUserContext userContext) => _userContext = userContext;
    public bool CanCreate() => _userContext.IsAuthenticated;
    public bool CanWrite() => _userContext.IsInRole("HR") || _userContext.IsInRole("Admin");
}

public partial class AuthorizedEmployeeWrite
{
    public Guid Id { get; private set; }
    public string FirstName { get; set; } = "";
    public string LastName { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedEmployeeWrite() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert([Service] IEmployeeRepository repository, CancellationToken ct)
    {
        IsNew = false;
        return Task.CompletedTask;
    }

    [Remote, Update]
    public Task Update([Service] IEmployeeRepository repository, CancellationToken ct) => Task.CompletedTask;

    [Remote, Delete]
    public Task Delete([Service] IEmployeeRepository repository, CancellationToken ct) => Task.CompletedTask;
}

// ============================================================================
// Combined Authorization for All Write Operations
// ============================================================================

#region save-authorization-combined
// Single auth check for all writes: Write = Insert | Update | Delete
public interface ICombinedWriteAuth
{
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)] bool CanWrite();  // Covers all writes
}

[Factory]
[AuthorizeFactory<ICombinedWriteAuth>]  // Single check for Insert, Update, Delete
public partial class AuthorizedDepartmentWrite : IFactorySaveMeta { /* ... */ }
#endregion

// Full implementation
public class CombinedWriteAuth : ICombinedWriteAuth
{
    private readonly IUserContext _userContext;
    public CombinedWriteAuth(IUserContext userContext) => _userContext = userContext;
    public bool CanWrite() => _userContext.IsInRole("Editor") || _userContext.IsInRole("Admin");
}

public partial class AuthorizedDepartmentWrite
{
    public Guid Id { get; private set; }
    public string Name { get; set; } = "";
    public bool IsNew { get; private set; } = true;
    public bool IsDeleted { get; set; }

    [Create]
    public AuthorizedDepartmentWrite() { Id = Guid.NewGuid(); }

    [Remote, Insert]
    public Task Insert(CancellationToken ct) { IsNew = false; return Task.CompletedTask; }

    [Remote, Update]
    public Task Update(CancellationToken ct) => Task.CompletedTask;

    [Remote, Delete]
    public Task Delete(CancellationToken ct) => Task.CompletedTask;
}
