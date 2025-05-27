using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Showcase;

public interface IShowcaseAuthorize
{
	[AuthorizeFactory(AuthorizeFactoryOperation.Read | AuthorizeFactoryOperation.Write)]
	public bool AnyAccess();

	[AuthorizeFactory(AuthorizeFactoryOperation.Read)]
	public bool CanRead();

	[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
	public bool CanCreate();

	[AuthorizeFactory(AuthorizeFactoryOperation.Fetch)]
	public bool CanFetch();

	[AuthorizeFactory(AuthorizeFactoryOperation.Delete)]
	public bool CanDelete();
}

internal class ShowcaseAuthorize : IShowcaseAuthorize
{
	public ShowcaseAuthorize([Service] IService service) { Assert.NotNull(service); }
	public bool AnyAccess() { return true; }
	public bool CanRead() { return true; }
	public bool CanCreate() { return true; }
	public bool CanFetch() { return false; }
	public bool CanDelete() { return false; }
}

public interface IShowcaseAuthObj : IFactorySaveMeta
{
	new bool IsDeleted { get; set; }
	new bool IsNew { get; set; }
}

[Factory]
[AuthorizeFactory<IShowcaseAuthorize>]
internal class ShowcaseAuthObj : IShowcaseAuthObj
{
	[Fetch]
	[Create]
	public ShowcaseAuthObj([Service] IService service) { Assert.NotNull(service); }

	public bool IsDeleted { get; set; } = false;

	public bool IsNew { get; set; } = false;

	[Insert]
	public void Insert([Service] IService service) { this.IsNew = false; Assert.NotNull(service); }

	[Update]
	public void Update([Service] IService service) { }

	[Delete]
	public void Delete([Service] IService service) { }
}

public class ShowcaseAuthTests : FactoryTestBase<IShowcaseAuthObjFactory>
{
	[Fact]
	public void ShowcaseAuth_CanCreate()
	{
		Assert.True(this.factory.CanCreate());
	}

	[Fact]
	public void ShowcaseAuth_Create()
	{
		var create = this.factory.Create();
		Assert.NotNull(create);
	}

	[Fact]
	public void ShowcaseAuth_CanFetch()
	{
		Assert.False(this.factory.CanFetch());
	}

	[Fact]
	public void ShowcaseAuth_Fetch()
	{
		var fetch = this.factory.Fetch();
		Assert.Null(fetch);
	}

	[Fact]
	public void ShowcaseAuth_CanDelete()
	{
		Assert.False(this.factory.CanDelete());
	}

	[Fact]
	public void ShowcaseAuth_CanSave()
	{
		// False because CanDelete is false
		// Even though an Insert is allowed
		// But success cannot be guaranteed	
		Assert.False(this.factory.CanSave());
	}

	[Fact]
	public void ShowcaseAuth_Save()
	{
		var create = this.factory.Create()!;
		var result = this.factory.Save(create);
		Assert.NotNull(result);
		Assert.False(result!.IsNew);
	}


	[Fact]
	public void ShowcaseAuth_Save_Exception_CannotDelete()
	{
		var create = this.factory.Create()!;
		create.IsDeleted = true;
		Assert.Throws<NotAuthorizedException>(() => this.factory.Save(create));
	}

	[Fact]
	public void ShowcaseAuth_TrySave_Null_CannotDelete()
	{
		var create = this.factory.Create()!;
		create.IsDeleted = true;
		var result = this.factory.TrySave(create);
		Assert.Null(result.Result);
		Assert.False(result.HasAccess);
	}

	[Fact]
	public void ShowcaseAuth_TrySave_Success()
	{
		// Success becase Insert and general Write is allowed
		var create = this.factory.Create()!;
		create.IsNew = true;
		var result = this.factory.TrySave(create);
		Assert.NotNull(result.Result);
		Assert.True(result.HasAccess);
		Assert.False(result.Result!.IsNew);
	}
}
