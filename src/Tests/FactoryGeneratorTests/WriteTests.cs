using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests;

public class WriteTests
{

	[Factory]
	public class WriteDataMapper : IFactorySaveMeta
	{
		public bool IsDeleted { get; set; }

		public bool IsNew { get; set; }

		public bool InsertCalled { get; set; }
		[Insert]
		public void InsertVoid()
		{
			this.InsertCalled = true;
		}

		[Insert]
		public bool InsertBool()
		{
			this.InsertCalled = true;
			return true;
		}

		[Insert]
		public Task InsertTask()
		{
			this.InsertCalled = true;
			return Task.CompletedTask;
		}

		[Insert]
		public Task<bool> InsertTaskBool()
		{
			this.InsertCalled = true;
			return Task.FromResult(true);
		}

		[Insert]
		public void InsertVoid(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
		}

		[Insert]
		public bool InsertBool(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Insert]
		public Task InsertTask(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Insert]
		public Task<bool> InsertTaskBool(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Insert]
		public Task<bool> InsertTaskBoolFalse(int? param)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Insert]
		public void InsertVoidDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
		}

		[Insert]
		public bool InsertBoolTrueDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Insert]
		public bool InsertBoolFalseDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Insert]
		public Task InsertTaskDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Insert]
		public Task<bool> InsertTaskBoolDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Insert]
		public Task<bool> InsertTaskBoolFalseDep([Service] IService service)
		{
			this.InsertCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Insert]
		public void InsertVoidDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Insert]
		public bool InsertBoolTrueDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Insert]
		public bool InsertBoolFalseDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Insert]
		public Task InsertTaskDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Insert]
		public Task<bool> InsertTaskBoolDep(int? param, [Service] IService service)
		{
			this.InsertCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}



		public bool UpdateCalled { get; set; }
		[Update]
		public void UpdateVoid()
		{
			this.UpdateCalled = true;
		}

		[Update]
		public bool UpdateBool()
		{
			this.UpdateCalled = true;
			return true;
		}

		[Update]
		public Task UpdateTask()
		{
			this.UpdateCalled = true;
			return Task.CompletedTask;
		}

		[Update]
		public Task<bool> UpdateTaskBool()
		{
			this.UpdateCalled = true;
			return Task.FromResult(true);
		}

		[Update]
		public void UpdateVoid(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
		}

		[Update]
		public bool UpdateBool(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Update]
		public Task UpdateTask(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Update]
		public Task<bool> UpdateTaskBool(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Update]
		public Task<bool> UpdateTaskBoolFalse(int? param)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Update]
		public void UpdateVoidDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
		}

		[Update]
		public bool UpdateBoolTrueDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Update]
		public bool UpdateBoolFalseDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Update]
		public Task UpdateTaskDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Update]
		public Task<bool> UpdateTaskBoolDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Update]
		public Task<bool> UpdateTaskBoolFalseDep([Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Update]
		public void UpdateVoidDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Update]
		public bool UpdateBoolTrueDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Update]
		public bool UpdateBoolFalseDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Update]
		public Task UpdateTaskDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Update]
		public Task<bool> UpdateTaskBoolDep(int? param, [Service] IService service)
		{
			this.UpdateCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		public bool DeleteCalled { get; set; }
		[Delete]
		public void DeleteVoid()
		{
			this.DeleteCalled = true;
		}

		[Delete]
		public bool DeleteBool()
		{
			this.DeleteCalled = true;
			return true;
		}

		[Delete]
		public Task DeleteTask()
		{
			this.DeleteCalled = true;
			return Task.CompletedTask;
		}

		[Delete]
		public Task<bool> DeleteTaskBool()
		{
			this.DeleteCalled = true;
			return Task.FromResult(true);
		}

		[Delete]
		public void DeleteVoid(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
		}

		[Delete]
		public bool DeleteBool(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return true;
		}

		[Delete]
		public Task DeleteTask(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return Task.CompletedTask;
		}

		[Delete]
		public Task<bool> DeleteTaskBool(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(true);
		}

		[Delete]
		public Task<bool> DeleteTaskBoolFalse(int? param)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			return Task.FromResult(false);
		}

		[Delete]
		public void DeleteVoidDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
		}

		[Delete]
		public bool DeleteBoolTrueDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return true;
		}

		[Delete]
		public bool DeleteBoolFalseDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return false;
		}

		[Delete]
		public Task DeleteTaskDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Delete]
		public Task<bool> DeleteTaskBoolDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(true);
		}

		[Delete]
		public Task<bool> DeleteTaskBoolFalseDep([Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.NotNull(service);
			return Task.FromResult(false);
		}

		[Delete]
		public void DeleteVoidDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
		}

		[Delete]
		public bool DeleteBoolTrueDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return true;
		}

		[Delete]
		public bool DeleteBoolFalseDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return false;
		}

		[Delete]
		public Task DeleteTaskDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.CompletedTask;
		}

		[Delete]
		public Task<bool> DeleteTaskBoolDep(int? param, [Service] IService service)
		{
			this.DeleteCalled = true;
			Assert.Equal(1, param);
			Assert.NotNull(service);
			return Task.FromResult(true);
		}
	}

	private IServiceScope clientScope;

	public WriteTests()
	{
		var scopes = ClientServerContainers.Scopes();
		this.clientScope = scopes.client;
	}

	[Fact]
	public async Task WriteDataMapperTest()
	{
		var readFactory = this.clientScope.ServiceProvider.GetRequiredService<IWriteDataMapperFactory>();

		var methods = readFactory.GetType().GetMethods().Where(m => m.Name.StartsWith("Save")).ToList();

		foreach (var method in methods)
		{
			object? result;
			var methodName = method.Name;

			async Task<WriteDataMapper?> doSave(WriteDataMapper writeDataMapper)
			{
				if (method.GetParameters().Count() == 2)
				{
					result = method.Invoke(readFactory, [writeDataMapper, 1]);
				}
				else
				{
					result = method.Invoke(readFactory, [writeDataMapper]);
				}

				if (result is Task<WriteDataMapper?> taskBool)
				{
					Assert.Contains("Task", methodName);
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
				else if (result is WriteDataMapper r)
				{
					Assert.DoesNotContain("Task", methodName);
					Assert.DoesNotContain("False", methodName);
					Assert.NotNull(r);
					return r;
				}
				else
				{
					Assert.DoesNotContain("Task", methodName);
					Assert.Contains("Bool", methodName);
					Assert.Contains("False", methodName);
					Assert.Null(result);
					return null;
				}
			}

			var writeDataMapperToSave = new WriteDataMapper();
			var writeDataMapper = await doSave(writeDataMapperToSave);
			Assert.True(writeDataMapper?.UpdateCalled ?? true);

			writeDataMapperToSave = new WriteDataMapper() { IsNew = true };
			writeDataMapper = await doSave(writeDataMapperToSave);
			Assert.True(writeDataMapper?.InsertCalled ?? true);

			writeDataMapperToSave = new WriteDataMapper() { IsDeleted = true };
			writeDataMapper = await doSave(writeDataMapperToSave);
			Assert.True(writeDataMapper?.DeleteCalled ?? true);

		}
	}
}
