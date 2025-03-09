using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests;

public class ReadTests
{
	[Factory]
	public class ReadObject
	{
		public bool CreateCalled { get; set; }

		[Create]

		public void CreateVoid()
		{
		 this.CreateCalled = true;
		}

		[Create]

		public bool CreateBool()
		{
		 this.CreateCalled = true;
			return true;
		}

		[Create]

		public Task CreateTask()
		{
		 this.CreateCalled = true;
			return Task.CompletedTask;
		}

		[Create]

		public Task<bool> CreateTaskBool()
		{
		 this.CreateCalled = true;
			return Task.FromResult(true);
		}

		[Create]

		public void CreateVoid(int? param)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
		}

		[Create]

		public bool CreateBool(int? param)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Create]

		public Task CreateTask(int? param)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Create]

		public Task<bool> CreateTaskBool(int? param)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Create]

		public Task<bool> CreateTaskBoolFalse(int? param)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Create]

		public void CreateVoidDep([Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.NotNull(service);
		}

		[Create]

		public bool CreateBoolTrueDep([Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Create]

		public bool CreateBoolFalseDep([Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Create]

		public Task CreateTaskDep([Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Create]

		public Task<bool> CreateTaskBoolDep([Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Create]

		public Task<bool> CreateTaskBoolFalseDep([Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Create]

		public void CreateVoidDep(int? param, [Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Create]

		public bool CreateBoolTrueDep(int? param, [Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Create]

		public bool CreateBoolFalseDep(int? param, [Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Create]

		public Task CreateTaskDep(int? param, [Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Create]

		public Task<bool> CreateTaskBoolDep(int? param, [Service] IService service)
		{
		 this.CreateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		public bool FetchCalled { get; set; }

		[Fetch]

		public void FetchVoid()
		{
		 this.FetchCalled = true;
		}

		[Fetch]

		public bool FetchBool()
		{
		 this.FetchCalled = true;
			return true;
		}

		[Fetch]

		public Task FetchTask()
		{
		 this.FetchCalled = true;
			return Task.CompletedTask;
		}

		[Fetch]

		public Task<bool> FetchTaskBool()
		{
		 this.FetchCalled = true;
			return Task.FromResult(true);
		}

		[Fetch]

		public void FetchVoid(int? param)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
		}

		[Fetch]

		public bool FetchBool(int? param)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Fetch]

		public Task FetchTask(int? param)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Fetch]

		public Task<bool> FetchTaskBool(int? param)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Fetch]

		public void FetchVoidDep([Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.NotNull(service);
		}

		[Fetch]

		public bool FetchBoolTrueDep([Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Fetch]

		public bool FetchBoolFalseDep([Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Fetch]

		public Task FetchTaskDep([Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Fetch]

		public Task<bool> FetchTaskBoolDep([Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Fetch]

		public void FetchVoidDep(int? param, [Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Fetch]

		public bool FetchBoolTrueDep(int? param, [Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Fetch]

		public bool FetchBoolFalseDep(int? param, [Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Fetch]

		public Task FetchTaskDep(int? param, [Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Fetch]

		public Task<bool> FetchTaskBoolDep(int? param, [Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Fetch]

		public Task<bool> FetchTaskBoolFalseDep(int? param, [Service] IService service)
		{
		 this.FetchCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(false);
		}
	}

	private IServiceScope clientScope;

	public ReadTests()
	{
		var scopes = ClientServerContainers.Scopes();
	  this.clientScope = scopes.client;
	}

	[Fact]
	public async Task ReadFactoryTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IReadObjectFactory>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.Contains("Create") || m.Name.Contains("Fetch")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			if (method.GetParameters().Any())
			{
				result = method.Invoke(readFactory, new object[] { 1 });
			}
			else
			{
				result = method.Invoke(readFactory, null);
			}

			if (result is Task<ReadObject?> task)
			{
				Assert.Contains("Task", methodName);
				if (method.Name.Contains("False"))
				{
					Assert.Null(await task);
				}
				else
				{
					var r = await task;
					Assert.NotNull(r);
					Assert.True(r!.CreateCalled || r!.FetchCalled);
				}
			}
			else if (result is ReadObject r)
			{
				Assert.DoesNotContain("Task", methodName);
				Assert.DoesNotContain("False", methodName);
				Assert.NotNull(r);
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
