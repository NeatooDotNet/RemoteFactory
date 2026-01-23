using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Factory;


	public class MixedWriteTests
	{

		[Factory]
		public class MixedWriteObject : IFactorySaveMeta
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
			public Task<bool> UpdateBool()
			{
				this.UpdateCalled = true;
				return Task.FromResult(true);
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
			public void UpdateBool(int? param)
			{
				this.UpdateCalled = true;
				Assert.Equal(1, param);
			}

			[Update]
			public void UpdateTask(int? param)
			{
				this.UpdateCalled = true;
				Assert.Equal(1, param);
			}

			[Update]
			public void UpdateTaskBool(int? param)
			{
				this.UpdateCalled = true;
				Assert.Equal(1, param);
			}

			[Update]
			public void UpdateTaskBoolFalse(int? param)
			{
				this.UpdateCalled = true;
				Assert.Equal(1, param);
			}

			[Update]
			public void UpdateVoidDep([Service] IService service)
			{
				this.UpdateCalled = true;
				Assert.NotNull(service);
			}

			[Remote]
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
			public void UpdateTaskBoolDep([Service] IService service)
			{
				this.UpdateCalled = true;
				Assert.NotNull(service);
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

			[Remote]
			[Update]
			public bool UpdateBoolTrueDep(int? param, [Service] IService service)
			{
				this.UpdateCalled = true;
				Assert.Equal(1, param);
				Assert.NotNull(service);
				return true;
			}

			[Update]
			public Task<bool> UpdateBoolFalseDep(int? param, [Service] IService service)
			{
				this.UpdateCalled = true;
				Assert.Equal(1, param);
				Assert.NotNull(service);
				return Task.FromResult(false);
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

			[Remote]
			[Delete]
			public bool DeleteBool(int? param)
			{
				this.DeleteCalled = true;
				Assert.Equal(1, param);
				return true;
			}

			[Delete]
			public void DeleteTask(int? param)
			{
				this.DeleteCalled = true;
				Assert.Equal(1, param);
			}

			[Delete]
			public Task DeleteTaskBool(int? param)
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

			[Remote]
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
			public bool DeleteTaskBoolDep([Service] IService service)
			{
				this.DeleteCalled = true;
				Assert.NotNull(service);
				return true;
			}

			[Delete]
			public void DeleteTaskBoolFalseDep([Service] IService service)
			{
				this.DeleteCalled = true;
				Assert.NotNull(service);
			}

			[Delete]
			public Task<bool> DeleteVoidDep(int? param, [Service] IService service)
			{
				this.DeleteCalled = true;
				Assert.Equal(1, param);
				Assert.NotNull(service);
				return Task.FromResult(true);
			}

			[Delete]
			public void DeleteBoolTrueDep(int? param, [Service] IService service)
			{
				this.DeleteCalled = true;
				Assert.Equal(1, param);
				Assert.NotNull(service);
			}

			[Remote]
			[Delete]
			public bool DeleteBoolFalseDep(int? param, [Service] IService service)
			{
				this.DeleteCalled = true;
				Assert.Equal(1, param);
				Assert.NotNull(service);
				return false;
			}

			[Remote]
			[Delete]
			public Task DeleteTaskDep(int? param, [Service] IService service)
			{
				this.DeleteCalled = true;
				Assert.Equal(1, param);
				Assert.NotNull(service);
				return Task.CompletedTask;
			}

			[Remote]
			[Delete]
			public void DeleteTaskBoolDep(int? param, [Service] IService service)
			{
				this.DeleteCalled = true;
				Assert.Equal(1, param);
				Assert.NotNull(service);
			}

		}


		// DEPRECATED: Reflection-based tests removed
		// Write operation tests are now in RemoteFactory.IntegrationTests.Combinations.*BehaviorTests
	}


