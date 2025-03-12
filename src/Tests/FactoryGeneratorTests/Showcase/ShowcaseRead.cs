using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Showcase;

public interface IShowcaseRead
{
	List<int>? InitProperty { get; init; }
	List<int> IntList { get; set; }
}

[Factory]
internal class ShowcaseRead : IShowcaseRead
{
	public ShowcaseRead([Service] IService service)
	{
		this.IntList = default!;
		Assert.NotNull(service);
	}

	[Create]
	public ShowcaseRead(List<int> intList, [Service] IService service)
	{
		// For non-nullable properties
		Assert.NotNull(service);
		this.IntList = intList;
	}

	public List<int> IntList { get; set; }
	public List<int>? InitProperty { get; init; }

	[Create]
	public void CreateVoid(List<int> intList) { this.IntList = intList; }

	[Create]
	public bool CreateBool(List<int> intList) { return false; }

	[Create]
	public Task CreateTask(List<int> intList) { this.IntList = intList; return Task.CompletedTask; }

	[Create]
	public Task<bool> CreateTaskBool(List<int> intList) { this.IntList = intList; return Task.FromResult(false); }

	[Create]
	public Task CreateService(List<int> intList, [Service] IService service)
	{
		this.IntList = intList; Assert.NotNull(service); return Task.CompletedTask;
	}

	[Create]
	public static async Task<IShowcaseRead> CreateStatic(List<int> intList, [Service] IService service)
	{
		await Task.CompletedTask;

		// For async construction, init properties
		Assert.NotNull(service);
		var showcaseCreate = new ShowcaseRead(service)
		{
			IntList = intList,
			InitProperty = intList
		};
		return showcaseCreate;
	}

	[Remote]
	[Create]
	public void CreateRemote(List<int> intList, [Service] IServerOnlyService service)
	{
		this.IntList = intList;
		Assert.NotNull(service);
	}

	[Create]
	public Task CreateRemoteClientFail(List<int> intList, [Service] IServerOnlyService service)
	{
		// Fails - Verifying that this cannot be called on the client	
		Assert.Fail(); return Task.CompletedTask;
	}

	[Fetch]
	public void FetchVoid(int id) {  }
}

public class ShowcaseReadTests : FactoryTestBase<IShowcaseReadFactory>
{
	[Fact]
	public void ShowcaseRead_Constructor()
	{
		var intList = new List<int> { 1, 2, 3 };
		var createConstructor = this.factory.Create(intList);
		Assert.Equal(intList, createConstructor.IntList);
	}

	[Fact]
	public void ShowcaseRead_CreateVoid()
	{
		var intList = new List<int> { 1, 2, 3 };
		var createVoid = this.factory.CreateVoid(intList);
		Assert.Equal(intList, createVoid.IntList);
	}

	[Fact]
	public void ShowcaseRead_CreateBool()
	{
		var intList = new List<int> { 1, 2, 3 };
		var createBool = this.factory.CreateBool(intList);
		Assert.Null(createBool);
	}

	[Fact]
	public async Task ShowcaseRead_CreateTask()
	{
		var intList = new List<int> { 1, 2, 3 };
		var createTask = await this.factory.CreateTask(intList);
		Assert.Equal(intList, createTask.IntList);
	}

	[Fact]
	public async Task ShowcaseRead_CreateTaskBool()
	{
		var intList = new List<int> { 1, 2, 3 };
		var createTaskBool = await this.factory.CreateTaskBool(intList);
		Assert.Null(createTaskBool);
	}

	[Fact]
	public async Task ShowcaseRead_CreateService()
	{
		var intList = new List<int> { 1, 2, 3 };
		var createService = await this.factory.CreateService(intList);
		Assert.Equal(intList, createService.IntList);
	}

	[Fact]
	public async Task ShowcaseRead_CreateStatic()
	{
		var intList = new List<int> { 1, 2, 3 };
		var createStatic = await this.factory.CreateStatic(intList);
		Assert.Equal(intList, createStatic.IntList);
		Assert.Equal(intList, createStatic.InitProperty);
	}

	[Fact]
	public async Task ShowcaseRead_CreateRemoteOnlyClientFail()
	{
		var intList = new List<int> { 1, 2, 3 };
		await Assert.ThrowsAsync<InvalidOperationException>(() => this.factory.CreateRemoteClientFail(intList));
	}

	[Fact]
	public async Task ShowcaseRead_CreateRemote()
	{
		var intList = new List<int> { 1, 2, 3 };
		var result = await this.factory.CreateRemote(intList);
		Assert.Equal(intList, result.IntList);
	}
}
