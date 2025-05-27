namespace Neatoo.RemoteFactory.AspNetCore.TestLibrary;

[Factory]
public class AspAuthorizeTestObj
{
	public Guid Value { get; set; }

	[Create]
	[AspAuthorize("TestPolicy", Roles = "Test role")]
	public void Create()
	{
		this.Value = Guid.NewGuid();
	}

	[Create]
	[AspAuthorize(Roles = "No auth")]
	public void CreateNoAuth()
	{
		this.Value = Guid.NewGuid();
	}
}
