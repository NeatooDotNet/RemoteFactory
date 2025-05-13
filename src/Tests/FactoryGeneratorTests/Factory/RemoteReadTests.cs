using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;


public class RemoteReadTests
{

	[Factory]
	public class RemoteReadDataMapper
	{
		public bool CreateCalled { get; set; }

		[Create]
		[Remote]
		public void CreateVoid()
		{
			this.CreateCalled = true;
		}

		[Create]
		[Remote]
		public bool CreateBool()
		{
			this.CreateCalled = true;
			return true;
		}

		[Create]
		[Remote]
		public Task CreateTask()
		{
			this.CreateCalled = true;
			return Task.CompletedTask;
		}

		[Create]
		[Remote]
		public Task<bool> CreateTaskBool()
		{
			this.CreateCalled = true;
			return Task.FromResult(true);
		}

		[Create]
		[Remote]
		public void CreateVoid(int? param)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
		}

		[Create]
		[Remote]
		public bool CreateBool(int? param)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Create]
		[Remote]
		public Task CreateTask(int? param)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Create]
		[Remote]
		public Task<bool> CreateTaskBool(int? param)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Create]
		[Remote]
		public Task<bool> CreateTaskBoolFalse(int? param)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Create]
		[Remote]
		public void CreateVoidDep([Service] IService service)
		{
			this.CreateCalled = true;
			Assert.NotNull(service);
		}

		[Create]
		[Remote]
		public bool CreateBoolTrueDep([Service] IService service)
		{
			this.CreateCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Create]
		[Remote]
		public bool CreateBoolFalseDep([Service] IService service)
		{
			this.CreateCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Create]
		[Remote]
		public Task CreateTaskDep([Service] IService service)
		{
			this.CreateCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Create]
		[Remote]
		public Task<bool> CreateTaskBoolDep([Service] IService service)
		{
			this.CreateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Create]
		[Remote]
		public Task<bool> CreateTaskBoolFalseDep([Service] IService service)
		{
			this.CreateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Create]
		[Remote]
		public void CreateVoidDep(int? param, [Service] IService service)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Create]
		[Remote]
		public bool CreateBoolTrueDep(int? param, [Service] IService service)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Create]
		[Remote]
		public bool CreateBoolFalseDep(int? param, [Service] IService service)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Create]
		[Remote]
		public Task CreateTaskDep(int? param, [Service] IService service)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Create]
		[Remote]
		public Task<bool> CreateTaskBoolDep(int? param, [Service] IService service)
		{
			this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		public bool FetchCalled { get; set; }

		[Fetch]
		[Remote]
		public void FetchVoid()
		{
			this.FetchCalled = true;
		}

		[Fetch]
		[Remote]
		public bool FetchBool()
		{
			this.FetchCalled = true;
			return true;
		}

		[Fetch]
		[Remote]
		public Task FetchTask()
		{
			this.FetchCalled = true;
			return Task.CompletedTask;
		}

		[Fetch]
		[Remote]
		public Task<bool> FetchTaskBool()
		{
			this.FetchCalled = true;
			return Task.FromResult(true);
		}

		[Fetch]
		[Remote]
		public void FetchVoid(int? param)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
		}

		[Fetch]
		[Remote]
		public bool FetchBool(int? param)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Fetch]
		[Remote]
		public Task FetchTask(int? param)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Fetch]
		[Remote]
		public Task<bool> FetchTaskBool(int? param)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Fetch]
		[Remote]
		public void FetchVoidDep([Service] IService service)
		{
			this.FetchCalled = true;
			Assert.NotNull(service);
		}

		[Fetch]
		[Remote]
		public bool FetchBoolTrueDep([Service] IService service)
		{
			this.FetchCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Fetch]
		[Remote]
		public bool FetchBoolFalseDep([Service] IService service)
		{
			this.FetchCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Fetch]
		[Remote]
		public Task FetchTaskDep([Service] IService service)
		{
			this.FetchCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Fetch]
		[Remote]
		public Task<bool> FetchTaskBoolDep([Service] IService service)
		{
			this.FetchCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Fetch]
		[Remote]
		public void FetchVoidDep(int? param, [Service] IService service)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Fetch]
		[Remote]
		public bool FetchBoolTrueDep(int? param, [Service] IService service)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Fetch]
		[Remote]
		public bool FetchBoolFalseDep(int? param, [Service] IService service)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Fetch]
		[Remote]
		public Task FetchTaskDep(int? param, [Service] IService service)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Fetch]
		[Remote]
		public Task<bool> FetchTaskBoolDep(int? param, [Service] IService service)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Fetch]
		[Remote]
		public Task<bool> FetchTaskBoolFalseDep(int? param, [Service] IService service)
		{
			this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(false);
		}
	}


	private IServiceScope clientScope;
	private IServiceScope localScope;

	public RemoteReadTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
		this.localScope = scopes.local;
	}

	[Fact]
	public Task RemoteReadFactoryTest_Client()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();
		return this.RemoteRead(readFactory);
	}

	[Fact]
	public Task RemoteReadFactoryTest_Local()
	{
		var readFactory = this.localScope.ServiceProvider.GetRequiredService<IRemoteReadDataMapperFactory>();
		return this.RemoteRead(readFactory);
	}

	private async Task RemoteRead(IRemoteReadDataMapperFactory readFactory)
	{
		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Create") || m.Name.StartsWith("Fetch")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			if (method.GetParameters().Length != 0)
			{
				result = method.Invoke(readFactory, [1]);
			}
			else
			{
				result = method.Invoke(readFactory, null);
			}

			if (result is Task<RemoteReadDataMapper?> taskBool)
			{
				if (method.Name.Contains("False"))
				{
					Assert.Null(await taskBool);
				}
				else
				{
					var remoteRead = await taskBool;
					Assert.NotNull(result);
					Assert.True(remoteRead!.CreateCalled || remoteRead!.FetchCalled);
				}
			}
			else
			{
				Assert.DoesNotContain("Task", methodName);
				Assert.Contains("Bool", methodName);
				Assert.Contains("False", methodName);
				Assert.Null(result);
			}

		}
	}
}