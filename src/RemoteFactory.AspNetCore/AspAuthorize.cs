using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using System.Runtime.CompilerServices;

namespace Neatoo.RemoteFactory.AspNetCore;

public class AspAuthorize : IAspAuthorize
{
	private readonly IAuthorizationPolicyProvider policyProvider;
	private readonly IPolicyEvaluator policyEvaluator;
	private readonly IHttpContextAccessor httpContextAccessor;

	public AspAuthorize(IAuthorizationPolicyProvider policyProvider, IPolicyEvaluator policyEvaluator, IHttpContextAccessor httpContextAccessor)
	{
		this.policyProvider = policyProvider;
		this.policyEvaluator = policyEvaluator;
		this.httpContextAccessor = httpContextAccessor;
	}

	public async Task<string?> Authorize(IEnumerable<IAuthorizeData> authorizeData)
	{
		var context = this.GetHttpContext();

		var policy = await this.GetAuthorizationPolicy(authorizeData);

		var authenticateResult = await this.GetAuthenticateResult(policy, context!);

		if (authenticateResult.message != null)
		{
			return authenticateResult.message;
		}

		var authorizeResult = await this.GetPolicyAuthorizationResult(policy, authenticateResult.authenticateResult, context!);

		return authorizeResult.message;
	}

	protected virtual HttpContext GetHttpContext()
	{
		var context = this.httpContextAccessor.HttpContext;
		if (context == null)
		{
			throw new InvalidOperationException("HttpContext is not available. Ensure that AspAuthorize is used within an HTTP request context.");
		}
		return context;
	}

	protected virtual async Task<AuthorizationPolicy> GetAuthorizationPolicy(IEnumerable<IAuthorizeData> authorizeData)
	{
		ArgumentNullException.ThrowIfNull(authorizeData, nameof(authorizeData));
		var policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeData);
		if (policy == null)
		{
			throw new InvalidOperationException("Authorization policy not found.");
		}
		return policy;
	}

	protected virtual async Task<(string? message, AuthenticateResult authenticateResult)> GetAuthenticateResult(AuthorizationPolicy authorizationPolicy, HttpContext httpContext)
	{
		ArgumentNullException.ThrowIfNull(authorizationPolicy, nameof(authorizationPolicy));
		ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));

		var authenticateResult = await this.policyEvaluator.AuthenticateAsync(authorizationPolicy, httpContext);

		return (authenticateResult.Failure?.Message, authenticateResult);
	}

	protected virtual async Task<(string? message, PolicyAuthorizationResult authorizationResult)> GetPolicyAuthorizationResult(AuthorizationPolicy authorizationPolicy, AuthenticateResult authenticateResult, HttpContext httpContext)
	{
		ArgumentNullException.ThrowIfNull(authorizationPolicy, nameof(authorizationPolicy));
		ArgumentNullException.ThrowIfNull(authenticateResult, nameof(authenticateResult));
		ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));
		var authorizationResult = await this.policyEvaluator.AuthorizeAsync(authorizationPolicy, authenticateResult, httpContext, null);
		return (string.Join(", ", authorizationResult.AuthorizationFailure?.FailedRequirements.Select(r => r.ToString()) ?? []), authorizationResult);
	}
}
