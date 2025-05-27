using Microsoft.AspNetCore.Authorization;

namespace Neatoo.RemoteFactory.AspNetCore;
public interface IAspAuthorize
{
	Task<bool> Authorize(IEnumerable<IAuthorizeData> authorizeData);
}