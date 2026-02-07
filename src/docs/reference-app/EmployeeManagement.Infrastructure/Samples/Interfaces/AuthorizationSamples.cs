using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples.Interfaces;

// Additional IAspAuthorize example - compiled but no longer extracted as duplicate
// Primary snippet is in InterfacesSamples.cs
public class CustomAspAuthorize : IAspAuthorize
{
    private readonly IUserContext _userContext;

    public CustomAspAuthorize(IUserContext userContext)
    {
        _userContext = userContext;
    }

    public Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
    {
        if (!_userContext.IsAuthenticated)
        {
            if (forbid)
                throw new AspForbidException("User is not authenticated.");
            return Task.FromResult<string?>("User is not authenticated.");
        }

        foreach (var data in authorizeData)
        {
            if (!string.IsNullOrEmpty(data.Roles))
            {
                var requiredRoles = data.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var hasRequiredRole = requiredRoles.Any(role => _userContext.IsInRole(role.Trim()));

                if (!hasRequiredRole)
                {
                    if (forbid)
                        throw new AspForbidException($"Missing role(s): {data.Roles}");
                    return Task.FromResult<string?>($"Missing role(s): {data.Roles}");
                }
            }
        }

        return Task.FromResult<string?>(string.Empty);
    }
}
