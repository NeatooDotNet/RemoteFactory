using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Policy;
using Microsoft.AspNetCore.Http;

namespace RemoteFactory.AspNetCore;

public class AspAuthorize
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

	public async Task Authorize()
	{

		var context = this.httpContextAccessor.HttpContext;

		IEnumerable<IAuthorizeData> authorizeData = [ new AuthorizeAttribute("TestPolicy") ];
		
		var policy = await AuthorizationPolicy.CombineAsync(this.policyProvider, authorizeData);

		var authenticateResult = await this.policyEvaluator.AuthenticateAsync(policy!, context!);

		var authorizeResult = await this.policyEvaluator.AuthorizeAsync(policy!, authenticateResult, context!, null);

	}
}
