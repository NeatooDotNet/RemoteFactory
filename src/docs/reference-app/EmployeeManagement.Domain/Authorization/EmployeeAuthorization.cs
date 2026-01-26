using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Domain.Authorization;

#region authorization-class
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

    #region authorization-create
    /// <summary>
    /// Only HR and Managers can create new employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create)]
    public bool CanCreate()
    {
        return _userContext.IsAuthenticated &&
               (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
    }
    #endregion

    #region authorization-read
    /// <summary>
    /// All authenticated users can read employee data.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Read)]
    public bool CanRead()
    {
        return _userContext.IsAuthenticated;
    }
    #endregion

    #region authorization-write
    /// <summary>
    /// Only HR and Managers can modify employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Write)]
    public bool CanWrite()
    {
        return _userContext.IsAuthenticated &&
               (_userContext.IsInRole("HR") || _userContext.IsInRole("Manager"));
    }
    #endregion

    #region authorization-execute
    /// <summary>
    /// Only HR can delete employees.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Execute)]
    public bool CanExecute()
    {
        return _userContext.IsAuthenticated && _userContext.IsInRole("HR");
    }
    #endregion
}
#endregion
