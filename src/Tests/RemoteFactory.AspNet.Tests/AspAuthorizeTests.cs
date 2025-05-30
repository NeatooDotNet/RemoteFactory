using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.AspNetCore.TestLibrary;

namespace RemoteFactory.AspNetCore.Tests;

public class AspAuthorizeTests : IClassFixture<ContainerFixture>
{
	private readonly IAspAuthorizeTestObjFactory factory;

	public AspAuthorizeTests(ContainerFixture container)
	{
		this.factory = container.CreateScope.ServiceProvider.GetRequiredService<IAspAuthorizeTestObjFactory>();
	}

	[Fact]
	public async Task AspAuthorize_Create()
	{
		var result = await this.factory.Create(true);

		Assert.NotNull(result);
	}

	[Fact]
	public async Task AspAuthorize_CreateNoAspAuth()
	{
		var noAuthResult = await this.factory.CreateNoAspAuth(true);

		Assert.Null(noAuthResult);
	}

	[Fact]
	public async Task AspAuthorize_CreateNoNeatooAuth()
	{
		var noAuthResult = await this.factory.CreateNoAspAuth(false);

		Assert.Null(noAuthResult);
	}

	[Fact]
	public async Task AspAuthorize_CanCreate()
	{
		var result = await this.factory.CanCreate(true);

		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanCreateMultiple()
	{
		var result = await this.factory.CanCreateMultiple(true);

		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanCreateNoAspAuth()
	{
		var noAuthResult = await this.factory.CanCreateNoAspAuth(true);

		Assert.False(noAuthResult.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanCreateNoNeatooAuth()
	{
		var noAuthResult = await this.factory.CanCreateNoAspAuth(false);

		Assert.False(noAuthResult.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_Save()
	{
		var obj = await this.factory.Create(true);
		
		obj = await this.factory.Save(obj!, true);

		Assert.NotNull(obj);
	}

	[Fact]
	public async Task AspAuthorize_TrySave()
	{
		var obj = await this.factory.Create(true);

		var result = await this.factory.TrySave(obj!, true);

		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_SaveNoAspAuth()
	{
		var obj = await this.factory.Create(true);

		await Assert.ThrowsAsync<NotAuthorizedException>(() => this.factory.SaveNoAspAuth(obj!, true));
	}

	[Fact]
	public async Task AspAuthorize_TrySaveNoAspAuth()
	{
		var obj = await this.factory.Create(true);

		var result = await this.factory.TrySaveNoAspAuth(obj!, true);

		Assert.False(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanInsert()
	{
		var result = await this.factory.CanInsert(true);
		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanInsert_NoNeatooAuth()
	{
		var result = await this.factory.CanInsert(false);
		Assert.False(result.HasAccess);
	}


	[Fact]
	public async Task AspAuthorize_CanInsertNoAspAuth()
	{
		var result = await this.factory.CanInsertNoAspAuth(true);

		Assert.False(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanSave()
	{
		var result = await this.factory.CanSave(true);
		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanSave_NoNeatooAuth()
	{
		var result = await this.factory.CanSave(false);
		Assert.False(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanSaveNoAspAuth()
	{
		var result = await this.factory.CanSaveNoAspAuth(true);

		Assert.False(result.HasAccess);
	}
}
