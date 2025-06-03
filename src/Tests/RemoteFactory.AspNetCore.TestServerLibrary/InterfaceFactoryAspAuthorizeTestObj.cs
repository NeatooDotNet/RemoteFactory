using Neatoo.RemoteFactory.AspNetCore.TestClientLibrary;

namespace Neatoo.RemoteFactory.AspNetCore.TestServerLibrary;

public class InterfaceAuthorizeTestObjAuth : IInterfaceAuthorizeTestObjAuth
{
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	public bool HasAccess(bool hasAccess)
	{
		return hasAccess;
	}
}

public class InterfaceAuthorizeTestObj : IInterfaceAuthorizeTestObj
{
	public Task<bool> HasAspAccess(bool hasAccess)
	{
		return Task.FromResult(hasAccess);
	}

	public Task<bool> NoAspAccess(bool hasAccess) => throw new NotImplementedException();
}