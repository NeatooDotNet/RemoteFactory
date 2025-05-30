using Neatoo.RemoteFactory.AspNetCore.TestLibrary;

namespace RemoteFactory.AspNetCore.Tests;

public class InterfaceAspAuthorizeTests : IClassFixture<ContainerFixture>
{
	private readonly IInterfaceAuthorizeTestObjFactory interfaceObj;

	public InterfaceAspAuthorizeTests(ContainerFixture container)
	{
		this.interfaceObj = container.CreateScope.ServiceProvider.GetRequiredService<IInterfaceAuthorizeTestObjFactory>();
	}

	[Fact]
	public async Task InterfaceAspAuthorize_HasAspAccess()
	{
		var result = await this.interfaceObj.HasAspAccess(true);
	}

	[Fact]
	public async Task InterfaceAspAuthorize_HasAspAccess_NoNeatooAccess()
	{
		await Assert.ThrowsAsync<HttpRequestException>(() => this.interfaceObj.NoAspAccess(false));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_NoAspAccess()
	{
		await Assert.ThrowsAsync<HttpRequestException>(() => this.interfaceObj.NoAspAccess(true));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_CanHasAspAccess()
	{
		Assert.True(await this.interfaceObj.CanHasAspAccess(true));
	}

	[Fact]
	public async Task InterfaceAspAuthorize_CanNotAuthorized()
	{
		Assert.False(await this.interfaceObj.CanNoAspAccess(true));
	}
}
