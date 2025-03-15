using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;



public class RemoteWriteTests
{

	[Factory]
	public class RemoteWriteObject : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }

		public bool IsNew { get; set; }

		public bool InsertCalled { get; set; }
		[Insert]
		[Remote]
		public void InsertVoid()
		{
			this.InsertCalled = true;
		}

		[Insert]
		[Remote]
		public bool InsertBool()
		{
			this.InsertCalled = true;
			return true;
		}

		[Insert]
		[Remote]
		public Task InsertTask()
		{
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Insert]
		[Remote]
		public Task<bool> InsertTaskBool()
		{
			this.InsertCalled = true;
			return Task.FromResult(true);
		}

		[Insert]
		[Remote]
		public void InsertVoid(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
		}

		[Insert]
		[Remote]
		public bool InsertBool(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Insert]
		[Remote]
		public Task InsertTask(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Insert]
		[Remote]
		public Task<bool> InsertTaskBool(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Insert]
		[Remote]
		public Task<bool> InsertTaskBoolFalse(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Insert]
		[Remote]
		public void InsertVoidDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
		}

		[Insert]
		[Remote]
		public bool InsertBoolTrueDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Insert]
		[Remote]
		public bool InsertBoolFalseDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Insert]
		[Remote]
		public Task InsertTaskDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Insert]
		[Remote]
		public Task<bool> InsertTaskBoolDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Insert]
		[Remote]
		public Task<bool> InsertTaskBoolFalseDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Insert]
		[Remote]
		public void InsertVoidDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Insert]
		[Remote]
		public bool InsertBoolTrueDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Insert]
		[Remote]
		public bool InsertBoolFalseDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Insert]
		[Remote]
		public Task InsertTaskDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Insert]
		[Remote]
		public Task<bool> InsertTaskBoolDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}



		public bool UpdateCalled { get; set; }
		[Update]
		[Remote]
		public void UpdateVoid()
		{
			this.UpdateCalled = true;
		}

		[Update]
		[Remote]
		public bool UpdateBool()
		{
			this.UpdateCalled = true;
			return true;
		}

		[Update]
		[Remote]
		public Task UpdateTask()
		{
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public Task<bool> UpdateTaskBool()
		{
			this.UpdateCalled = true;
			return Task.FromResult(true);
		}

		[Update]
		[Remote]
		public void UpdateVoid(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
		}

		[Update]
		[Remote]
		public bool UpdateBool(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Update]
		[Remote]
		public Task UpdateTask(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public Task<bool> UpdateTaskBool(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Update]
		[Remote]
		public Task<bool> UpdateTaskBoolFalse(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Update]
		[Remote]
		public void UpdateVoidDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
		}

		[Update]
		[Remote]
		public bool UpdateBoolTrueDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Update]
		[Remote]
		public bool UpdateBoolFalseDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Update]
		[Remote]
		public Task UpdateTaskDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public Task<bool> UpdateTaskBoolDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Update]
		[Remote]
		public Task<bool> UpdateTaskBoolFalseDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Update]
		[Remote]
		public void UpdateVoidDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Update]
		[Remote]
		public bool UpdateBoolTrueDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Update]
		[Remote]
		public bool UpdateBoolFalseDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Update]
		[Remote]
		public Task UpdateTaskDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Update]
		[Remote]
		public Task<bool> UpdateTaskBoolDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		public bool DeleteCalled { get; set; }
		[Delete]
		[Remote]
		public void DeleteVoid()
		{
			this.DeleteCalled = true;
		}

		[Delete]
		[Remote]
		public bool DeleteBool()
		{
			this.DeleteCalled = true;
			return true;
		}

		[Delete]
		[Remote]
		public Task DeleteTask()
		{
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public Task<bool> DeleteTaskBool()
		{
			this.DeleteCalled = true;
			return Task.FromResult(true);
		}

		[Delete]
		[Remote]
		public void DeleteVoid(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
		}

		[Delete]
		[Remote]
		public bool DeleteBool(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Delete]
		[Remote]
		public Task DeleteTask(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public Task<bool> DeleteTaskBool(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Delete]
		[Remote]
		public Task<bool> DeleteTaskBoolFalse(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Delete]
		[Remote]
		public void DeleteVoidDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
		}

		[Delete]
		[Remote]
		public bool DeleteBoolTrueDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Delete]
		[Remote]
		public bool DeleteBoolFalseDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Delete]
		[Remote]
		public Task DeleteTaskDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public Task<bool> DeleteTaskBoolDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Delete]
		[Remote]
		public Task<bool> DeleteTaskBoolFalseDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Delete]
		[Remote]
		public void DeleteVoidDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Delete]
		[Remote]
		public bool DeleteBoolTrueDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Delete]
		[Remote]
		public bool DeleteBoolFalseDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Delete]
		[Remote]
		public Task DeleteTaskDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Delete]
		[Remote]
		public Task<bool> DeleteTaskBoolDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}
	}

	private IServiceScope clientScope;

	public RemoteWriteTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
	}

	[Fact]
	public async Task RemoteWriteDataMapperTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IRemoteWriteObjectFactory>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Save")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			async Task<RemoteWriteObject?> doSave(RemoteWriteObject remoteWrite)
			{
				if (method.GetParameters().Count() == 2)
				{
					result = method.Invoke(readFactory, [remoteWrite, 1]);
				}
				else
				{
					result = method.Invoke(readFactory, [remoteWrite]);
				}

				if (result is Task<RemoteWriteObject?> taskBool)
				{
					if (method.Name.Contains("False"))
					{
						Assert.Null(await taskBool);
					}
					else
					{
						Assert.NotNull(await taskBool);
					}
					return taskBool.Result;
				}
				else
				{
					Assert.Contains("Bool", methodName);
					Assert.Contains("False", methodName);
					Assert.Null(result);
					return null;
				}
			}

			var writeDataMapperToSave = new RemoteWriteObject();
			var writeDataMapper = await doSave(writeDataMapperToSave);
			Assert.True(writeDataMapper?.UpdateCalled ?? true);

			writeDataMapperToSave = new RemoteWriteObject() { IsNew = true };
			writeDataMapper = await doSave(writeDataMapperToSave);
			Assert.True(writeDataMapper?.InsertCalled ?? true);

			writeDataMapperToSave = new RemoteWriteObject() { IsDeleted = true };
			writeDataMapper = await doSave(writeDataMapperToSave);
			Assert.True(writeDataMapper?.DeleteCalled ?? true);

		}
	}
}


