using Neatoo.RemoteFactory.AspNetCore.TestServerLibrary;
using RemoteFactory.AspNetCore.Tests;

namespace Neatoo.RemoteFactory.AspNetCore.Tests;

public class ServerFactoryTests : IClassFixture<ContainerFixture>
{
   private readonly IServerFactoryObjClientFactory factory;

   public ServerFactoryTests(ContainerFixture fixture)
	{
		this.factory = fixture.CreateScope.ServiceProvider.GetRequiredService<IServerFactoryObjClientFactory>();
	}

	[Fact]
	public void ServerFactoryTests_Create()
	{
		var result = this.factory.Create("TestName");
	}
}
