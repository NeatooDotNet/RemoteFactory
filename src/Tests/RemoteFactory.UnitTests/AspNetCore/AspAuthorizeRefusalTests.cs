using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore;
using Neatoo.RemoteFactory.Internal;

namespace RemoteFactory.UnitTests.AspNetCore;

/// <summary>
/// AspAuthorize without forbid, the path class factories take: every refusal is a non-empty
/// message, because the generated factory converts the result to <see cref="Authorized"/>, which
/// reads null or empty as allowed.
/// </summary>
/// <remarks>
/// The policy pipeline is ASP.NET Core's own (AddAuthorization, PolicyEvaluator); only the
/// HttpContext is built by hand.
/// </remarks>
public class AspAuthorizeRefusalTests
{
    private const string AdminPolicy = "Admin";
    private const string FailingPolicy = "Failing";

    private sealed class FailingRequirement : IAuthorizationRequirement;

    /// <summary>Refuses through context.Fail(), naming no failed requirement.</summary>
    private sealed class FailingHandler : AuthorizationHandler<FailingRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, FailingRequirement requirement)
        {
            context.Fail();
            return Task.CompletedTask;
        }
    }

    private static AspAuthorize Build(ClaimsPrincipal user)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddAuthorization(options =>
        {
            options.AddPolicy(AdminPolicy, p => p.RequireRole("Admin"));
            options.AddPolicy(FailingPolicy, p => p.AddRequirements(new FailingRequirement()));
        });
        services.AddSingleton<IAuthorizationHandler, FailingHandler>();
        var provider = services.BuildServiceProvider();

        var accessor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { RequestServices = provider, User = user } };
        return new AspAuthorize(
            provider.GetRequiredService<IAuthorizationPolicyProvider>(),
            provider.GetRequiredService<IPolicyEvaluator>(),
            accessor,
            new CorrelationContextImpl());
    }

    private static ClaimsPrincipal SignedIn(params string[] roles) =>
        new(new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "user"), .. roles.Select(r => new Claim(ClaimTypes.Role, r))],
            authenticationType: "Test"));

    private static ClaimsPrincipal Anonymous() => new(new ClaimsIdentity());

    [Fact]
    public async Task AnonymousCaller_IsRefused()
    {
        var sut = Build(Anonymous());

        Authorized authorized = await sut.Authorize([new AspAuthorizeData(AdminPolicy)], false);

        Assert.False(authorized.HasAccess);
        Assert.False(string.IsNullOrEmpty(authorized.Message));
    }

    [Fact]
    public async Task PolicyFailingWithoutANamedRequirement_IsRefused()
    {
        var sut = Build(SignedIn("Admin"));

        Authorized authorized = await sut.Authorize([new AspAuthorizeData(FailingPolicy)], false);

        Assert.False(authorized.HasAccess);
        Assert.False(string.IsNullOrEmpty(authorized.Message));
    }

    [Fact]
    public async Task SignedInWithoutTheRole_IsRefused()
    {
        var sut = Build(SignedIn("User"));

        Authorized authorized = await sut.Authorize([new AspAuthorizeData(AdminPolicy)], false);

        Assert.False(authorized.HasAccess);
    }

    [Fact]
    public async Task SignedInWithTheRole_IsAllowed()
    {
        var sut = Build(SignedIn("Admin"));

        Authorized authorized = await sut.Authorize([new AspAuthorizeData(AdminPolicy)], false);

        Assert.True(authorized.HasAccess);
    }
}
