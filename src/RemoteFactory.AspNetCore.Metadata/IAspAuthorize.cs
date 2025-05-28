using Microsoft.AspNetCore.Authorization;

namespace Neatoo.RemoteFactory.AspNetCore;
public interface IAspAuthorize
{
	Task<string?> Authorize(IEnumerable<IAuthorizeData> authorizeData);
}