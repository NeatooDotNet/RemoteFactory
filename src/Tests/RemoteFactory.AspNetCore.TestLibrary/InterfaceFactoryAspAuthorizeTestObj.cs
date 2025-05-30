namespace Neatoo.RemoteFactory.AspNetCore.TestLibrary;

public class InterfaceAuthorizeTestObjAuth
{
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	public bool HasAccess(bool hasAccess)
	{
		return hasAccess;
	}
}

[Factory]
[AuthorizeFactory<InterfaceAuthorizeTestObjAuth>]
public interface IInterfaceAuthorizeTestObj
{
	[AspAuthorize("TestPolicy", Roles = "Test role")]
	public Task<bool> HasAspAccess(bool hasAccess);

	[AspAuthorize(Roles = "Not Authorized")]
	public Task<bool> NoAspAccess(bool hasAccess);
}


public class InterfaceAuthorizeTestObj : IInterfaceAuthorizeTestObj
{
	public Task<bool> HasAspAccess(bool hasAccess)
	{
		return Task.FromResult(hasAccess);
	}

	public Task<bool> NoAspAccess(bool hasAccess) => throw new NotImplementedException();
}