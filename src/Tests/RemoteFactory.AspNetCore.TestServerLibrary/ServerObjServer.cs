using Neatoo.RemoteFactory.AspNetCore.TestClientLibrary;

namespace Neatoo.RemoteFactory.AspNetCore.TestServerLibrary;


[Factory]
internal sealed class ServerFactoryObj : RemoteFactory.AspNetCore.TestClientLibrary.ServerFactoryObj, IServerFactoryObj
{

	[Create]
	[Remote]
   public void Create(string name)
	{
		this.Name = name;
	}

	public bool? SaveCalled { get; set; } 

	[Insert]
	[Remote]
	public void Insert() {
		this.SaveCalled = true;
	}

	[Update]
	[Remote]	
	public void Update()
	{
		this.SaveCalled = true;
	}
}
