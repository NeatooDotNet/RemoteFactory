namespace Neatoo.RemoteFactory.AspNetCore.TestLibrary;

public class AspAuthorizeTestObjAuth
{
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	public bool HasAccess(bool hasAccess)
	{
		return hasAccess;
	}
}

[Factory]
[AuthorizeFactory<AspAuthorizeTestObjAuth>]
public class AspAuthorizeTestObj :  IFactorySaveMeta
{
	public Guid Value { get; set; }

   public bool IsDeleted { get; set; }

	public bool IsNew { get; set; } = true;

   [Create]
	[AspAuthorize("TestPolicy", Roles = "Test role")]
	public void Create(bool hasAccess)
	{
		this.Value = Guid.NewGuid();
	}

	[Create]
	[AspAuthorize(Roles = "No auth")]
	public void CreateNoAspAuth(bool hasAccess)
	{
		this.Value = Guid.NewGuid();
	}

	[Create]
	[AspAuthorize("TestPolicy")]
	[AspAuthorize(Roles = "Test role 2")]
	public void CreateMultiple(bool hasAccess)
	{
		this.Value = Guid.NewGuid();
	}

	[Insert]
	[AspAuthorize("TestPolicy", Roles = "Test role")]
	public void Insert(bool hasAccess)
	{
		// Insert logic here
	}

	[Insert]
	[AspAuthorize("TestPolicy", Roles = "No Auth")]
	public void InsertNoAspAuth(bool hasAccess)
	{
		// Insert logic here
	}

}
