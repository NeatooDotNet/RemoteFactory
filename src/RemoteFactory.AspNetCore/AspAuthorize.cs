using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Neatoo.RemoteFactory.Internal;
using System.Runtime.CompilerServices;

namespace Neatoo.RemoteFactory.AspNetCore;

public class AspAuthorize : IAspAuthorize
{
	private readonly IAuthorizationPolicyProvider? policyProvider;
	private readonly IPolicyEvaluator? policyEvaluator;
	private readonly IHttpContextAccessor? httpContextAccessor;
	private readonly ILogger<AspAuthorize> logger;

	public AspAuthorize()
	{
		this.logger = NullLogger<AspAuthorize>.Instance;
	}

	public AspAuthorize(IAuthorizationPolicyProvider policyProvider, IPolicyEvaluator policyEvaluator, IHttpContextAccessor httpContextAccessor)
		: this(policyProvider, policyEvaluator, httpContextAccessor, null)
	{
	}

	public AspAuthorize(
		IAuthorizationPolicyProvider policyProvider,
		IPolicyEvaluator policyEvaluator,
		IHttpContextAccessor httpContextAccessor,
		ILogger<AspAuthorize>? logger)
	{
		this.policyProvider = policyProvider;
		this.policyEvaluator = policyEvaluator;
		this.httpContextAccessor = httpContextAccessor;
		this.logger = logger ?? NullLogger<AspAuthorize>.Instance;
	}

	private sealed class AuthorizeData : IAuthorizeData
	{	
		/// <summary>
		/// Gets or sets the policy name that determines access to the resource.
		/// </summary>
		public string? Policy { get; set; }

		/// <summary>
		/// Gets or sets a comma delimited list of roles that are allowed to access the resource.
		/// </summary>
		public string? Roles { get; set; }

		/// <summary>
		/// Gets or sets a comma delimited list of schemes from which user information is constructed.
		/// </summary>
		public string? AuthenticationSchemes { get; set; }
	}

	public async Task<string?> Authorize(IEnumerable<AspAuthorizeData> authorizeData, bool forbid = false)
	{
		if (this.policyEvaluator == null || this.policyProvider == null || this.httpContextAccessor == null)
		{
			throw new InvalidOperationException("No AspCoreNet Authorization registered");
		}

		var correlationId = CorrelationContext.CorrelationId;
		var policyNames = string.Join(", ", authorizeData.Select(a => a.Policy ?? a.Roles ?? "Default"));

		logger.AuthorizationCheck(correlationId, policyNames, "AspAuthorize");

		var context = this.GetHttpContext();

		var policy = await this.GetAuthorizationPolicy(authorizeData.Select(a => new AuthorizeData() { AuthenticationSchemes = a.AuthenticationSchemes, Policy = a.Policy, Roles = a.Roles }));

		var authenticateResult = await this.GetAuthenticateResult(policy, context!);

		if (!authenticateResult.authenticateResult.Succeeded)
		{
			logger.AuthorizationDenied(correlationId, policyNames, "Authentication");
			if (forbid)
			{
				await context.ForbidAsync();
				throw new AspForbidException(authenticateResult.message ?? "Forbidden");
			}

			return authenticateResult.message;
		}

		var authorizeResult = await this.GetPolicyAuthorizationResult(policy, authenticateResult.authenticateResult, context!);

		if (!authorizeResult.authorizationResult.Succeeded)
		{
			logger.AuthorizationDenied(correlationId, policyNames, "Authorization");
			if (forbid)
			{
				await context.ForbidAsync();
				throw new AspForbidException(authorizeResult.message ?? "Not Authorized");
			}
			return authorizeResult.message ?? "Not Authorized";
		}

		logger.AuthorizationGranted(correlationId, policyNames, "AspAuthorize");
		return string.Empty;
	}

	protected virtual HttpContext GetHttpContext()
	{
		var context = this.httpContextAccessor!.HttpContext;
		if (context == null)
		{
			throw new InvalidOperationException("HttpContext is not available. Ensure that AspAuthorize is used within an HTTP request context.");
		}
		return context;
	}

	protected virtual async Task<AuthorizationPolicy> GetAuthorizationPolicy(IEnumerable<IAuthorizeData> authorizeData)
	{
		ArgumentNullException.ThrowIfNull(authorizeData, nameof(authorizeData));
		var policy = await AuthorizationPolicy.CombineAsync(this.policyProvider!, authorizeData);
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

		var authenticateResult = await this.policyEvaluator!.AuthenticateAsync(authorizationPolicy, httpContext);

		return (authenticateResult.Failure?.Message, authenticateResult);
	}

	protected virtual async Task<(string? message, PolicyAuthorizationResult authorizationResult)> GetPolicyAuthorizationResult(AuthorizationPolicy authorizationPolicy, AuthenticateResult authenticateResult, HttpContext httpContext)
	{
		ArgumentNullException.ThrowIfNull(authorizationPolicy, nameof(authorizationPolicy));
		ArgumentNullException.ThrowIfNull(authenticateResult, nameof(authenticateResult));
		ArgumentNullException.ThrowIfNull(httpContext, nameof(httpContext));
		var authorizationResult = await this.policyEvaluator!.AuthorizeAsync(authorizationPolicy, authenticateResult, httpContext, null);
		return (string.Join(", ", authorizationResult.AuthorizationFailure?.FailedRequirements.Select(r => r.ToString()) ?? []), authorizationResult);
	}
}
