using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Application.Samples.Attributes;

#region attributes-authorization-operation
/// <summary>
/// Authorization interface with combined operation flags.
/// </summary>
public interface IEmployeeOperationAuth
{
    /// <summary>
    /// Combined flags apply same check to multiple operations.
    /// </summary>
    [AuthorizeFactory(AuthorizeFactoryOperation.Create | AuthorizeFactoryOperation.Fetch)]
    bool CanCreateAndRead();

    [AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
    bool CanDelete();
}

/// <summary>
/// Implementation with operation-level authorization.
/// </summary>
public class EmployeeOperationAuthImpl : IEmployeeOperationAuth
{
    private readonly IUserContext _userContext;

    public EmployeeOperationAuthImpl(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Create and Fetch require only authentication.
    /// </summary>
    public bool CanCreateAndRead()
    {
        return _userContext.IsAuthenticated;
    }

    /// <summary>
    /// Delete requires Administrator role.
    /// </summary>
    public bool CanDelete()
    {
        return _userContext.IsInRole("Administrator");
    }
}
#endregion
