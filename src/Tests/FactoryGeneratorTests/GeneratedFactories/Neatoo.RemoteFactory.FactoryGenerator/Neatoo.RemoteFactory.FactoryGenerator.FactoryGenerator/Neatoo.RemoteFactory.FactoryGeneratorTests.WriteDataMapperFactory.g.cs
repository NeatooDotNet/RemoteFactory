#nullable enable
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using static Neatoo.RemoteFactory.FactoryGeneratorTests.WriteTests;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

/*
                    Debugging Messages:
                    Parent class: WriteTests
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute

                    */
namespace Neatoo.RemoteFactory.FactoryGeneratorTests
{
    public interface IWriteDataMapperFactory
    {
        WriteDataMapper? SaveVoid(WriteDataMapper target);
        WriteDataMapper? SaveBool(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTask(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target);
        WriteDataMapper? SaveVoidDep(WriteDataMapper target);
        WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target);
        WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target);
        Task<WriteDataMapper?> SaveTaskBoolFalseDep(WriteDataMapper target);
        WriteDataMapper? SaveVoid(WriteDataMapper target, int? param);
        WriteDataMapper? SaveBool(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTask(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskBoolFalse(WriteDataMapper target, int? param);
        WriteDataMapper? SaveVoidDep(WriteDataMapper target, int? param);
        WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target, int? param);
        WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target, int? param);
        Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target, int? param);
    }

    internal class WriteDataMapperFactory : FactorySaveBase<WriteDataMapper>, IFactorySave<WriteDataMapper>, IWriteDataMapperFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public WriteDataMapperFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public WriteDataMapperFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public WriteDataMapper? LocalInsertVoid(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoid());
        }

        public WriteDataMapper? LocalInsertBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBool());
        }

        public Task<WriteDataMapper?> LocalInsertTask(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTask());
        }

        public Task<WriteDataMapper?> LocalInsertTaskBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBool());
        }

        public WriteDataMapper? LocalInsertVoid1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoid(param));
        }

        public WriteDataMapper? LocalInsertBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBool(param));
        }

        public Task<WriteDataMapper?> LocalInsertTask1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTask(param));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBool(param));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolFalse(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolFalse(param));
        }

        public WriteDataMapper? LocalInsertVoidDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoidDep(service));
        }

        public WriteDataMapper? LocalInsertBoolTrueDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolTrueDep(service));
        }

        public WriteDataMapper? LocalInsertBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolFalseDep(service));
        }

        public Task<WriteDataMapper?> LocalInsertTaskDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskDep(service));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolDep(service));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolFalseDep(service));
        }

        public WriteDataMapper? LocalInsertVoidDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoidDep(param, service));
        }

        public WriteDataMapper? LocalInsertBoolTrueDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolTrueDep(param, service));
        }

        public WriteDataMapper? LocalInsertBoolFalseDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolFalseDep(param, service));
        }

        public Task<WriteDataMapper?> LocalInsertTaskDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskDep(param, service));
        }

        public Task<WriteDataMapper?> LocalInsertTaskBoolDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolDep(param, service));
        }

        public WriteDataMapper? LocalUpdateVoid(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoid());
        }

        public WriteDataMapper? LocalUpdateBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBool());
        }

        public Task<WriteDataMapper?> LocalUpdateTask(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTask());
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBool());
        }

        public WriteDataMapper? LocalUpdateVoid1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoid(param));
        }

        public WriteDataMapper? LocalUpdateBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBool(param));
        }

        public Task<WriteDataMapper?> LocalUpdateTask1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTask(param));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBool(param));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolFalse(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolFalse(param));
        }

        public WriteDataMapper? LocalUpdateVoidDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoidDep(service));
        }

        public WriteDataMapper? LocalUpdateBoolTrueDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolTrueDep(service));
        }

        public WriteDataMapper? LocalUpdateBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolFalseDep(service));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskDep(service));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolDep(service));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolFalseDep(service));
        }

        public WriteDataMapper? LocalUpdateVoidDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoidDep(param, service));
        }

        public WriteDataMapper? LocalUpdateBoolTrueDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolTrueDep(param, service));
        }

        public WriteDataMapper? LocalUpdateBoolFalseDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolFalseDep(param, service));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskDep(param, service));
        }

        public Task<WriteDataMapper?> LocalUpdateTaskBoolDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolDep(param, service));
        }

        public WriteDataMapper? LocalDeleteVoid(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoid());
        }

        public WriteDataMapper? LocalDeleteBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBool());
        }

        public Task<WriteDataMapper?> LocalDeleteTask(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTask());
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBool(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBool());
        }

        public WriteDataMapper? LocalDeleteVoid1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoid(param));
        }

        public WriteDataMapper? LocalDeleteBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBool(param));
        }

        public Task<WriteDataMapper?> LocalDeleteTask1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTask(param));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBool1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBool(param));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolFalse(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolFalse(param));
        }

        public WriteDataMapper? LocalDeleteVoidDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoidDep(service));
        }

        public WriteDataMapper? LocalDeleteBoolTrueDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolTrueDep(service));
        }

        public WriteDataMapper? LocalDeleteBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolFalseDep(service));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskDep(service));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolDep(service));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolFalseDep(WriteDataMapper target)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolFalseDep(service));
        }

        public WriteDataMapper? LocalDeleteVoidDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoidDep(param, service));
        }

        public WriteDataMapper? LocalDeleteBoolTrueDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolTrueDep(param, service));
        }

        public WriteDataMapper? LocalDeleteBoolFalseDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolFalseDep(param, service));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskDep(param, service));
        }

        public Task<WriteDataMapper?> LocalDeleteTaskBoolDep1(WriteDataMapper target, int? param)
        {
            var cTarget = (WriteDataMapper)target ?? throw new Exception("WriteDataMapper must implement WriteDataMapper");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<WriteDataMapper?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolDep(param, service));
        }

        public virtual WriteDataMapper? SaveVoid(WriteDataMapper target)
        {
            return LocalSaveVoid(target);
        }

        async Task<IFactorySaveMeta?> IFactorySave<WriteDataMapper>.Save(WriteDataMapper target)
        {
            return await Task.FromResult((IFactorySaveMeta? )SaveVoid(target));
        }

        public virtual WriteDataMapper? LocalSaveVoid(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteVoid(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoid(target);
            }
            else
            {
                return LocalUpdateVoid(target);
            }
        }

        public virtual WriteDataMapper? SaveBool(WriteDataMapper target)
        {
            return LocalSaveBool(target);
        }

        public virtual WriteDataMapper? LocalSaveBool(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteBool(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertBool(target);
            }
            else
            {
                return LocalUpdateBool(target);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTask(WriteDataMapper target)
        {
            return LocalSaveTask(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTask(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTask(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTask(target);
            }
            else
            {
                return LocalUpdateTask(target);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target)
        {
            return LocalSaveTaskBool(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBool(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskBool(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBool(target);
            }
            else
            {
                return LocalUpdateTaskBool(target);
            }
        }

        public virtual WriteDataMapper? SaveVoidDep(WriteDataMapper target)
        {
            return LocalSaveVoidDep(target);
        }

        public virtual WriteDataMapper? LocalSaveVoidDep(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteVoidDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoidDep(target);
            }
            else
            {
                return LocalUpdateVoidDep(target);
            }
        }

        public virtual WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target)
        {
            return LocalSaveBoolTrueDep(target);
        }

        public virtual WriteDataMapper? LocalSaveBoolTrueDep(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteBoolTrueDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolTrueDep(target);
            }
            else
            {
                return LocalUpdateBoolTrueDep(target);
            }
        }

        public virtual WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target)
        {
            return LocalSaveBoolFalseDep(target);
        }

        public virtual WriteDataMapper? LocalSaveBoolFalseDep(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteBoolFalseDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolFalseDep(target);
            }
            else
            {
                return LocalUpdateBoolFalseDep(target);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target)
        {
            return LocalSaveTaskDep(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskDep(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskDep(target);
            }
            else
            {
                return LocalUpdateTaskDep(target);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target)
        {
            return LocalSaveTaskBoolDep(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolDep(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskBoolDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBoolDep(target);
            }
            else
            {
                return LocalUpdateTaskBoolDep(target);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskBoolFalseDep(WriteDataMapper target)
        {
            return LocalSaveTaskBoolFalseDep(target);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolFalseDep(WriteDataMapper target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskBoolFalseDep(target);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBoolFalseDep(target);
            }
            else
            {
                return LocalUpdateTaskBoolFalseDep(target);
            }
        }

        public virtual WriteDataMapper? SaveVoid(WriteDataMapper target, int? param)
        {
            return LocalSaveVoid1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveVoid1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteVoid1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoid1(target, param);
            }
            else
            {
                return LocalUpdateVoid1(target, param);
            }
        }

        public virtual WriteDataMapper? SaveBool(WriteDataMapper target, int? param)
        {
            return LocalSaveBool1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveBool1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteBool1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertBool1(target, param);
            }
            else
            {
                return LocalUpdateBool1(target, param);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTask(WriteDataMapper target, int? param)
        {
            return LocalSaveTask1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTask1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTask1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTask1(target, param);
            }
            else
            {
                return LocalUpdateTask1(target, param);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskBool(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskBool1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBool1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskBool1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBool1(target, param);
            }
            else
            {
                return LocalUpdateTaskBool1(target, param);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskBoolFalse(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskBoolFalse(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolFalse(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskBoolFalse(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBoolFalse(target, param);
            }
            else
            {
                return LocalUpdateTaskBoolFalse(target, param);
            }
        }

        public virtual WriteDataMapper? SaveVoidDep(WriteDataMapper target, int? param)
        {
            return LocalSaveVoidDep1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveVoidDep1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteVoidDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertVoidDep1(target, param);
            }
            else
            {
                return LocalUpdateVoidDep1(target, param);
            }
        }

        public virtual WriteDataMapper? SaveBoolTrueDep(WriteDataMapper target, int? param)
        {
            return LocalSaveBoolTrueDep1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveBoolTrueDep1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteBoolTrueDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolTrueDep1(target, param);
            }
            else
            {
                return LocalUpdateBoolTrueDep1(target, param);
            }
        }

        public virtual WriteDataMapper? SaveBoolFalseDep(WriteDataMapper target, int? param)
        {
            return LocalSaveBoolFalseDep1(target, param);
        }

        public virtual WriteDataMapper? LocalSaveBoolFalseDep1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return default(WriteDataMapper);
                }

                return LocalDeleteBoolFalseDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertBoolFalseDep1(target, param);
            }
            else
            {
                return LocalUpdateBoolFalseDep1(target, param);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskDep(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskDep1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskDep1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskDep1(target, param);
            }
            else
            {
                return LocalUpdateTaskDep1(target, param);
            }
        }

        public virtual Task<WriteDataMapper?> SaveTaskBoolDep(WriteDataMapper target, int? param)
        {
            return LocalSaveTaskBoolDep1(target, param);
        }

        public virtual Task<WriteDataMapper?> LocalSaveTaskBoolDep1(WriteDataMapper target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(WriteDataMapper));
                }

                return LocalDeleteTaskBoolDep1(target, param);
            }
            else if (target.IsNew)
            {
                return LocalInsertTaskBoolDep1(target, param);
            }
            else
            {
                return LocalUpdateTaskBoolDep1(target, param);
            }
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<WriteDataMapper>();
            services.AddScoped<WriteDataMapperFactory>();
            services.AddScoped<IWriteDataMapperFactory, WriteDataMapperFactory>();
            services.AddScoped<IFactorySave<WriteDataMapper>, WriteDataMapperFactory>();
        }
    }
}