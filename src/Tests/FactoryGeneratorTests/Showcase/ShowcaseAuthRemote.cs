using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Showcase;

public interface IAuthRemote
{
	[Remote]
	[AuthorizeFactory(AuthorizeFactoryOperation.Create)]
	bool Create();
}

internal class AuthServerOnly : IAuthRemote
{
	public AuthServerOnly([Service] IServerOnlyService service)
	{
		Assert.NotNull(service);
	}

	public bool Create()
	{
		return true;
	}
}

public interface IShowcaseAuthRemote
{
	List<int> IntList { get; set; }
}

[Factory]
[AuthorizeFactory<IAuthRemote>]
internal class ShowcaseAuthRemote : IShowcaseAuthRemote
{
	public ShowcaseAuthRemote()
	{
		this.IntList = default!;
	}

	public List<int> IntList { get; set; }

	[Create]
	public void Create(List<int> intList)
	{
		this.IntList = intList;
	}
}

public class ShowcaseAuthRemoteTests : FactoryTestBase<IShowcaseAuthRemoteFactory>
{
	[Fact]
	public async Task ShowcaseAuthRemoteTest_Create()
	{
		var intList = new List<int> { 1, 2, 3 };
		var result = await this.factory.Create(intList);
		Assert.NotNull(result);
		Assert.Equal(intList, result.IntList);
	}

	[Fact]
	public async Task ShowcaseAuthRemoteTest_CanCreate()
	{
		Assert.True(await this.factory.CanCreate());
	}
}
