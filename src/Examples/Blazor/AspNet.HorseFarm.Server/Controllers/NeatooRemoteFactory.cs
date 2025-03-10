using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neatoo.RemoteFactory.Internal;

namespace AspNet.HorseFarm.Server.Controllers;

 [Route("api/remotefactory")]
 [ApiController]
 public class NeatooRemoteFactory : ControllerBase
 {
   private readonly HandleRemoteDelegateRequest handleRemoteDelegateRequest;

   public NeatooRemoteFactory(HandleRemoteDelegateRequest handleRemoteDelegateRequest)
	{
	  this.handleRemoteDelegateRequest = handleRemoteDelegateRequest;
   }

	[HttpPost]
	public Task<RemoteResponseDto> Post(RemoteDelegateRequestDto requestDto)
	{
		return this.handleRemoteDelegateRequest(requestDto);
	}
}
