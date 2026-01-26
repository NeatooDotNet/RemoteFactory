using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Authorization;

/// <summary>
/// Authorization rules for Department operations.
/// </summary>
public class DepartmentAuthorization
{
    private readonly IUserContext _userContext;

    public DepartmentAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Only Admin can create departments.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public bool CanCreate()
    {
        return _userContext.IsAuthenticated && _userContext.IsInRole("Admin");
    }

    /// <summary>
    /// All authenticated users can read departments.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanRead()
    {
        return _userContext.IsAuthenticated;
    }

    /// <summary>
    /// Only Admin and Managers can modify departments.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite()
    {
        return _userContext.IsAuthenticated &&
               (_userContext.IsInRole("Admin") || _userContext.IsInRole("Manager"));
    }
}
