using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Server.Samples.Authorization;

/// <summary>
/// ASP.NET Core authorization policy configuration for HR domain.
/// </summary>
public static class AuthorizationPolicyConfig
{
    #region authorization-policy-config
    // Configure ASP.NET Core policies for [AspAuthorize] to reference
    public static void ConfigureServices(IServiceCollection services) =>
        services.AddAuthorization(options =>
        {
            options.AddPolicy("RequireHRManager", p => p.RequireRole("HRManager"));
            options.AddPolicy("RequireAuthenticated", p => p.RequireAuthenticatedUser());
        });
    #endregion
}
