using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

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

   [Authorize("TestPolicy")]
   public async Task<bool> Authorize(IEnumerable<IAuthorizeData> authorizeData)
   {
	  var authorized = false;

	  var context = this.httpContextAccessor.HttpContext;

	  var policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeData);

	  var authenticateResult = await this.policyEvaluator.AuthenticateAsync(policy!, context!);

	  var authorizeResult = await this.policyEvaluator.AuthorizeAsync(policy!, authenticateResult, context!, null);

	  if (authenticateResult.Succeeded && authorizeResult.Succeeded)
	  {
		 authorized = true;
	  }

	  return authorized;
   }
}
