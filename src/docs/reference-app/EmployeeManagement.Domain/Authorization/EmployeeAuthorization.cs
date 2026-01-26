using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Authorization;

/// <summary>
/// Authorization rules for Employee operations.
/// </summary>
public class EmployeeAuthorization
{
    private readonly IUserContext _userContext;

    public EmployeeAuthorization(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Only HR and Managers can create new employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public bool CanCreate()
    {
        return _userContext.IsAuthenticated &&
               (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
    }

    /// <summary>
    /// All authenticated users can read employee data.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanRead()
    {
        return _userContext.IsAuthenticated;
    }

    /// <summary>
    /// Only HR and Managers can modify employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite()
    {
        return _userContext.IsAuthenticated &&
               (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
    }

    /// <summary>
    /// Only HR can delete employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanExecute()
    {
        return _userContext.IsAuthenticated && _userContext.IsInRole("HR");
    }
}
