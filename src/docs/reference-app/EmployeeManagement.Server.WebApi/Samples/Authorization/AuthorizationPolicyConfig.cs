using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace EmployeeManagement.Server.Samples.Authorization;

#region authorization-policy-config
/// <summary>
/// ASP.NET Core authorization policy configuration for HR domain.
/// </summary>
public static class AuthorizationPolicyConfig
{
    public static void ConfigureServices(IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            // HR Manager policy - only HR managers can access
            options.AddPolicy("RequireHRManager", policy =>
                policy.RequireRole("HRManager"));

            // Payroll policy - payroll staff or HR managers can access
            options.AddPolicy("RequirePayroll", policy =>
                policy.RequireRole("Payroll", "HRManager"));

            // Authenticated policy - any authenticated user can access
            options.AddPolicy("RequireAuthenticated", policy =>
                policy.RequireAuthenticatedUser());
        });
    }
}
#endregion
