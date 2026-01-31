using EmployeeManagement.Domain.Interfaces;
using Neatoo.RemoteFactory;

namespace EmployeeManagement.Infrastructure.Samples.Interfaces;

#region interfaces-aspauthorize
// Custom IAspAuthorize for testing or non-ASP.NET Core environments

/// <summary>
/// Custom IAspAuthorize implementation for simplified authorization.
/// </summary>
public class CustomAspAuthorize : IAspAuthorize
{
    private readonly IUserContext _userContext;

    public CustomAspAuthorize(IUserContext userContext)
    {
        _userContext = userContext;
    }

    /// <summary>
    /// Performs authorization checks based on AspAuthorizeData requirements.
    /// </summary>
    /// <param name="authorizeData">Collection of authorization requirements.</param>
    /// <param name="forbid">If true, throws AspForbidException on failure.</param>
    /// <returns>Empty string if authorized, error message if not authorized.</returns>
    public Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
    {
        // Check if user is authenticated
        if (!_userContext.IsAuthenticated)
        {
            if (forbid)
                throw new AspForbidException("User is not authenticated.");
            return Task.FromResult<string?>("User is not authenticated.");
        }

        // Iterate through AspAuthorizeData to check Roles requirements
        foreach (var data in authorizeData)
        {
            if (!string.IsNullOrEmpty(data.Roles))
            {
                var requiredRoles = data.Roles.Split(',', StringSplitOptions.RemoveEmptyEntries);
                var hasRequiredRole = requiredRoles.Any(role => _userContext.IsInRole(role.Trim()));

                if (!hasRequiredRole)
                {
                    if (forbid)
                        throw new AspForbidException($"User does not have required role(s): {data.Roles}");
                    return Task.FromResult<string?>($"User does not have required role(s): {data.Roles}");
                }
            }
        }

        // Return empty string on success
        return Task.FromResult<string?>(string.Empty);
    }
}
#endregion
