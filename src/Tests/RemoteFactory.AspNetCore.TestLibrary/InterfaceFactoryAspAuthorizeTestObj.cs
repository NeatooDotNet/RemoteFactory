namespace Neatoo.RemoteFactory.AspNetCore.TestClientLibrary;

public interface IInterfaceAuthorizeTestObjAuth
{
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	bool HasAccess(bool hasAccess);
}

[Factory]
[AuthorizeFactory<IInterfaceAuthorizeTestObjAuth>]
public interface IInterfaceAuthorizeTestObj
{
	[AspAuthorize("TestPolicy", Roles = "Test role")]
	public Task<bool> HasAspAccess(bool hasAccess);

	[AspAuthorize(Roles = "Not Authorized")]
	public Task<bool> NoAspAccess(bool hasAccess);
}