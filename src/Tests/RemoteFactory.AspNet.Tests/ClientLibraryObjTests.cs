using Neatoo.RemoteFactory.AspNetCore.TestServerLibrary;
using RemoteFactory.AspNetCore.Tests;

namespace Neatoo.RemoteFactory.AspNetCore.Tests;

public class ClientLibraryObjTests : IClassFixture<ContainerFixture>
{
   private readonly IClientLibraryObjClientFactory factory;

   public ClientLibraryObjTests(ContainerFixture fixture)
	{
		this.factory = fixture.CreateScope.ServiceProvider.GetRequiredService<IClientLibraryObjClientFactory>();
	}

	[Fact]
	public void ServerFactoryTests_Create()
	{
		var result = this.factory.Create("TestName");
	}
}
