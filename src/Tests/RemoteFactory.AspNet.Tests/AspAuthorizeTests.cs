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
		var result = await this.factory.Create(true, TestContext.Current.CancellationToken);

		Assert.NotNull(result);
	}

	[Fact]
	public async Task AspAuthorize_CreateNoAspAuth()
	{
		var noAuthResult = await this.factory.CreateNoAspAuth(true, TestContext.Current.CancellationToken);

		Assert.Null(noAuthResult);
	}

	[Fact]
	public async Task AspAuthorize_CreateNoNeatooAuth()
	{
		var noAuthResult = await this.factory.CreateNoAspAuth(false, TestContext.Current.CancellationToken);

		Assert.Null(noAuthResult);
	}

	[Fact]
	public async Task AspAuthorize_CanCreate()
	{
		var result = await this.factory.CanCreate(true, TestContext.Current.CancellationToken);

		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanCreateMultiple()
	{
		var result = await this.factory.CanCreateMultiple(true, TestContext.Current.CancellationToken);

		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanCreateNoAspAuth()
	{
		var noAuthResult = await this.factory.CanCreateNoAspAuth(true, TestContext.Current.CancellationToken);

		Assert.False(noAuthResult.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanCreateNoNeatooAuth()
	{
		var noAuthResult = await this.factory.CanCreateNoAspAuth(false, TestContext.Current.CancellationToken);

		Assert.False(noAuthResult.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_Save()
	{
		var obj = await this.factory.Create(true, TestContext.Current.CancellationToken);

		obj = await this.factory.Save(obj!, true, TestContext.Current.CancellationToken);

		Assert.NotNull(obj);
	}

	[Fact]
	public async Task AspAuthorize_TrySave()
	{
		var obj = await this.factory.Create(true, TestContext.Current.CancellationToken);

		var result = await this.factory.TrySave(obj!, true, TestContext.Current.CancellationToken);

		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_SaveNoAspAuth()
	{
		var obj = await this.factory.Create(true, TestContext.Current.CancellationToken);

		await Assert.ThrowsAsync<NotAuthorizedException>(() => this.factory.SaveNoAspAuth(obj!, true, TestContext.Current.CancellationToken));
	}

	[Fact]
	public async Task AspAuthorize_TrySaveNoAspAuth()
	{
		var obj = await this.factory.Create(true, TestContext.Current.CancellationToken);

		var result = await this.factory.TrySaveNoAspAuth(obj!, true, TestContext.Current.CancellationToken);

		Assert.False(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanInsert()
	{
		var result = await this.factory.CanInsert(true, TestContext.Current.CancellationToken);
		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanInsert_NoNeatooAuth()
	{
		var result = await this.factory.CanInsert(false, TestContext.Current.CancellationToken);
		Assert.False(result.HasAccess);
	}


	[Fact]
	public async Task AspAuthorize_CanInsertNoAspAuth()
	{
		var result = await this.factory.CanInsertNoAspAuth(true, TestContext.Current.CancellationToken);

		Assert.False(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanSave()
	{
		var result = await this.factory.CanSave(true, TestContext.Current.CancellationToken);
		Assert.True(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanSave_NoNeatooAuth()
	{
		var result = await this.factory.CanSave(false, TestContext.Current.CancellationToken);
		Assert.False(result.HasAccess);
	}

	[Fact]
	public async Task AspAuthorize_CanSaveNoAspAuth()
	{
		var result = await this.factory.CanSaveNoAspAuth(true, TestContext.Current.CancellationToken);

		Assert.False(result.HasAccess);
	}
}
