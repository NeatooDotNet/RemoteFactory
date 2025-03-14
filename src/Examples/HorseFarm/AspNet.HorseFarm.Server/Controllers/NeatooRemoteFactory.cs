using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Neatoo.RemoteFactory;

namespace AspNet.HorseFarm.Server.Controllers;

 [Route("api/neatoo")]
 [ApiController]
 public class NeatooRemoteFactory : ControllerBase
 {
   private readonly HandleRemoteDelegateRequest handleRemoteDelegateRequest;

   public NeatooRemoteFactory(HandleRemoteDelegateRequest handleRemoteDelegateRequest)
	{
	  this.handleRemoteDelegateRequest = handleRemoteDelegateRequest;
   }

	[HttpPost]
	public Task<RemoteResponseDto> Post(RemoteRequestDto requestDto)
	{
		return this.handleRemoteDelegateRequest(requestDto);
	}
}
