#nullable enable
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using static Neatoo.RemoteFactory.FactoryGeneratorTests.RemoteWriteTests;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Xunit;

/*
                    Debugging Messages:
                    Parent class: RemoteWriteTests
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute

                    */
namespace Neatoo.RemoteFactory.FactoryGeneratorTests
{
    public interface IRemoteWriteObjectFactory
    {
        Task<RemoteWriteObject?> SaveVoid(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveBool(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveTask(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveTaskBool(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveVoidDep(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveBoolTrueDep(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveBoolFalseDep(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveTaskDep(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveTaskBoolDep(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveTaskBoolFalseDep(RemoteWriteObject target);
        Task<RemoteWriteObject?> SaveVoid(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveBool(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveTask(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveTaskBool(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveTaskBoolFalse(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveVoidDep(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveBoolTrueDep(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveBoolFalseDep(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveTaskDep(RemoteWriteObject target, int? param);
        Task<RemoteWriteObject?> SaveTaskBoolDep(RemoteWriteObject target, int? param);
    }

    internal class RemoteWriteObjectFactory : FactorySaveBase<RemoteWriteObject>, IFactorySave<RemoteWriteObject>, IRemoteWriteObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        public delegate Task<RemoteWriteObject?> SaveVoidDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveBoolDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveTaskDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveTaskBoolDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveVoidDepDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveBoolTrueDepDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveBoolFalseDepDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveTaskDepDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveTaskBoolDepDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveTaskBoolFalseDepDelegate(RemoteWriteObject target);
        public delegate Task<RemoteWriteObject?> SaveVoid1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveBool1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveTask1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveTaskBool1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveTaskBoolFalseDelegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveVoidDep1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveBoolTrueDep1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveBoolFalseDep1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveTaskDep1Delegate(RemoteWriteObject target, int? param);
        public delegate Task<RemoteWriteObject?> SaveTaskBoolDep1Delegate(RemoteWriteObject target, int? param);
        // Delegate Properties to provide Local or Remote fork in execution
        public SaveVoidDelegate SaveVoidProperty { get; }
        public SaveBoolDelegate SaveBoolProperty { get; }
        public SaveTaskDelegate SaveTaskProperty { get; }
        public SaveTaskBoolDelegate SaveTaskBoolProperty { get; }
        public SaveVoidDepDelegate SaveVoidDepProperty { get; }
        public SaveBoolTrueDepDelegate SaveBoolTrueDepProperty { get; }
        public SaveBoolFalseDepDelegate SaveBoolFalseDepProperty { get; }
        public SaveTaskDepDelegate SaveTaskDepProperty { get; }
        public SaveTaskBoolDepDelegate SaveTaskBoolDepProperty { get; }
        public SaveTaskBoolFalseDepDelegate SaveTaskBoolFalseDepProperty { get; }
        public SaveVoid1Delegate SaveVoid1Property { get; }
        public SaveBool1Delegate SaveBool1Property { get; }
        public SaveTask1Delegate SaveTask1Property { get; }
        public SaveTaskBool1Delegate SaveTaskBool1Property { get; }
        public SaveTaskBoolFalseDelegate SaveTaskBoolFalseProperty { get; }
        public SaveVoidDep1Delegate SaveVoidDep1Property { get; }
        public SaveBoolTrueDep1Delegate SaveBoolTrueDep1Property { get; }
        public SaveBoolFalseDep1Delegate SaveBoolFalseDep1Property { get; }
        public SaveTaskDep1Delegate SaveTaskDep1Property { get; }
        public SaveTaskBoolDep1Delegate SaveTaskBoolDep1Property { get; }

        public RemoteWriteObjectFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            SaveVoidProperty = LocalSaveVoid;
            SaveBoolProperty = LocalSaveBool;
            SaveTaskProperty = LocalSaveTask;
            SaveTaskBoolProperty = LocalSaveTaskBool;
            SaveVoidDepProperty = LocalSaveVoidDep;
            SaveBoolTrueDepProperty = LocalSaveBoolTrueDep;
            SaveBoolFalseDepProperty = LocalSaveBoolFalseDep;
            SaveTaskDepProperty = LocalSaveTaskDep;
            SaveTaskBoolDepProperty = LocalSaveTaskBoolDep;
            SaveTaskBoolFalseDepProperty = LocalSaveTaskBoolFalseDep;
            SaveVoid1Property = LocalSaveVoid1;
            SaveBool1Property = LocalSaveBool1;
            SaveTask1Property = LocalSaveTask1;
            SaveTaskBool1Property = LocalSaveTaskBool1;
            SaveTaskBoolFalseProperty = LocalSaveTaskBoolFalse;
            SaveVoidDep1Property = LocalSaveVoidDep1;
            SaveBoolTrueDep1Property = LocalSaveBoolTrueDep1;
            SaveBoolFalseDep1Property = LocalSaveBoolFalseDep1;
            SaveTaskDep1Property = LocalSaveTaskDep1;
            SaveTaskBoolDep1Property = LocalSaveTaskBoolDep1;
        }

        public RemoteWriteObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            SaveVoidProperty = RemoteSaveVoid;
            SaveBoolProperty = RemoteSaveBool;
            SaveTaskProperty = RemoteSaveTask;
            SaveTaskBoolProperty = RemoteSaveTaskBool;
            SaveVoidDepProperty = RemoteSaveVoidDep;
            SaveBoolTrueDepProperty = RemoteSaveBoolTrueDep;
            SaveBoolFalseDepProperty = RemoteSaveBoolFalseDep;
            SaveTaskDepProperty = RemoteSaveTaskDep;
            SaveTaskBoolDepProperty = RemoteSaveTaskBoolDep;
            SaveTaskBoolFalseDepProperty = RemoteSaveTaskBoolFalseDep;
            SaveVoid1Property = RemoteSaveVoid1;
            SaveBool1Property = RemoteSaveBool1;
            SaveTask1Property = RemoteSaveTask1;
            SaveTaskBool1Property = RemoteSaveTaskBool1;
            SaveTaskBoolFalseProperty = RemoteSaveTaskBoolFalse;
            SaveVoidDep1Property = RemoteSaveVoidDep1;
            SaveBoolTrueDep1Property = RemoteSaveBoolTrueDep1;
            SaveBoolFalseDep1Property = RemoteSaveBoolFalseDep1;
            SaveTaskDep1Property = RemoteSaveTaskDep1;
            SaveTaskBoolDep1Property = RemoteSaveTaskBoolDep1;
        }

        public Task<RemoteWriteObject?> LocalInsertVoid(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoid()));
        }

        public Task<RemoteWriteObject?> LocalInsertBool(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBool()));
        }

        public Task<RemoteWriteObject?> LocalInsertTask(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTask());
        }

        public Task<RemoteWriteObject?> LocalInsertTaskBool(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBool());
        }

        public Task<RemoteWriteObject?> LocalInsertVoid1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoid(param)));
        }

        public Task<RemoteWriteObject?> LocalInsertBool1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBool(param)));
        }

        public Task<RemoteWriteObject?> LocalInsertTask1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTask(param));
        }

        public Task<RemoteWriteObject?> LocalInsertTaskBool1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBool(param));
        }

        public Task<RemoteWriteObject?> LocalInsertTaskBoolFalse(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolFalse(param));
        }

        public Task<RemoteWriteObject?> LocalInsertVoidDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoidDep(service)));
        }

        public Task<RemoteWriteObject?> LocalInsertBoolTrueDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolTrueDep(service)));
        }

        public Task<RemoteWriteObject?> LocalInsertBoolFalseDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolFalseDep(service)));
        }

        public Task<RemoteWriteObject?> LocalInsertTaskDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskDep(service));
        }

        public Task<RemoteWriteObject?> LocalInsertTaskBoolDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolDep(service));
        }

        public Task<RemoteWriteObject?> LocalInsertTaskBoolFalseDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolFalseDep(service));
        }

        public Task<RemoteWriteObject?> LocalInsertVoidDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertVoidDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalInsertBoolTrueDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolTrueDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalInsertBoolFalseDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertBoolFalseDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalInsertTaskDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskDep(param, service));
        }

        public Task<RemoteWriteObject?> LocalInsertTaskBoolDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertTaskBoolDep(param, service));
        }

        public Task<RemoteWriteObject?> LocalUpdateVoid(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoid()));
        }

        public Task<RemoteWriteObject?> LocalUpdateBool(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBool()));
        }

        public Task<RemoteWriteObject?> LocalUpdateTask(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTask());
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskBool(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBool());
        }

        public Task<RemoteWriteObject?> LocalUpdateVoid1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoid(param)));
        }

        public Task<RemoteWriteObject?> LocalUpdateBool1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBool(param)));
        }

        public Task<RemoteWriteObject?> LocalUpdateTask1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTask(param));
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskBool1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBool(param));
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskBoolFalse(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolFalse(param));
        }

        public Task<RemoteWriteObject?> LocalUpdateVoidDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoidDep(service)));
        }

        public Task<RemoteWriteObject?> LocalUpdateBoolTrueDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolTrueDep(service)));
        }

        public Task<RemoteWriteObject?> LocalUpdateBoolFalseDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolFalseDep(service)));
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskDep(service));
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskBoolDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolDep(service));
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskBoolFalseDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolFalseDep(service));
        }

        public Task<RemoteWriteObject?> LocalUpdateVoidDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateVoidDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalUpdateBoolTrueDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolTrueDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalUpdateBoolFalseDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateBoolFalseDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskDep(param, service));
        }

        public Task<RemoteWriteObject?> LocalUpdateTaskBoolDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateTaskBoolDep(param, service));
        }

        public Task<RemoteWriteObject?> LocalDeleteVoid(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoid()));
        }

        public Task<RemoteWriteObject?> LocalDeleteBool(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBool()));
        }

        public Task<RemoteWriteObject?> LocalDeleteTask(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTask());
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskBool(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBool());
        }

        public Task<RemoteWriteObject?> LocalDeleteVoid1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoid(param)));
        }

        public Task<RemoteWriteObject?> LocalDeleteBool1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBool(param)));
        }

        public Task<RemoteWriteObject?> LocalDeleteTask1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTask(param));
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskBool1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBool(param));
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskBoolFalse(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolFalse(param));
        }

        public Task<RemoteWriteObject?> LocalDeleteVoidDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoidDep(service)));
        }

        public Task<RemoteWriteObject?> LocalDeleteBoolTrueDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolTrueDep(service)));
        }

        public Task<RemoteWriteObject?> LocalDeleteBoolFalseDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolFalseDep(service)));
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskDep(service));
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskBoolDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolDep(service));
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskBoolFalseDep(RemoteWriteObject target)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolFalseDep(service));
        }

        public Task<RemoteWriteObject?> LocalDeleteVoidDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCall<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteVoidDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalDeleteBoolTrueDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolTrueDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalDeleteBoolFalseDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(DoFactoryMethodCallBool<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteBoolFalseDep(param, service)));
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskDep(param, service));
        }

        public Task<RemoteWriteObject?> LocalDeleteTaskBoolDep1(RemoteWriteObject target, int? param)
        {
            var cTarget = (RemoteWriteObject)target ?? throw new Exception("RemoteWriteObject must implement RemoteWriteObject");
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<RemoteWriteObject?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteTaskBoolDep(param, service));
        }

        public virtual Task<RemoteWriteObject?> SaveVoid(RemoteWriteObject target)
        {
            return SaveVoidProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveVoid(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveVoidDelegate), [target]))!;
        }

        async Task<IFactorySaveMeta?> IFactorySave<RemoteWriteObject>.Save(RemoteWriteObject target)
        {
            return (IFactorySaveMeta? )await SaveVoid(target);
        }

        public virtual Task<RemoteWriteObject?> LocalSaveVoid(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveBool(RemoteWriteObject target)
        {
            return SaveBoolProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveBool(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveBoolDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveBool(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTask(RemoteWriteObject target)
        {
            return SaveTaskProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTask(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTask(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskBool(RemoteWriteObject target)
        {
            return SaveTaskBoolProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskBool(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskBoolDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskBool(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveVoidDep(RemoteWriteObject target)
        {
            return SaveVoidDepProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveVoidDep(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveVoidDepDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveVoidDep(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveBoolTrueDep(RemoteWriteObject target)
        {
            return SaveBoolTrueDepProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveBoolTrueDep(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveBoolTrueDepDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveBoolTrueDep(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveBoolFalseDep(RemoteWriteObject target)
        {
            return SaveBoolFalseDepProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveBoolFalseDep(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveBoolFalseDepDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveBoolFalseDep(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskDep(RemoteWriteObject target)
        {
            return SaveTaskDepProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskDep(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskDepDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskDep(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskBoolDep(RemoteWriteObject target)
        {
            return SaveTaskBoolDepProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskBoolDep(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskBoolDepDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskBoolDep(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskBoolFalseDep(RemoteWriteObject target)
        {
            return SaveTaskBoolFalseDepProperty(target);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskBoolFalseDep(RemoteWriteObject target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskBoolFalseDepDelegate), [target]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskBoolFalseDep(RemoteWriteObject target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveVoid(RemoteWriteObject target, int? param)
        {
            return SaveVoid1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveVoid1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveVoid1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveVoid1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveBool(RemoteWriteObject target, int? param)
        {
            return SaveBool1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveBool1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveBool1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveBool1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTask(RemoteWriteObject target, int? param)
        {
            return SaveTask1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTask1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTask1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTask1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskBool(RemoteWriteObject target, int? param)
        {
            return SaveTaskBool1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskBool1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskBool1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskBool1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskBoolFalse(RemoteWriteObject target, int? param)
        {
            return SaveTaskBoolFalseProperty(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskBoolFalse(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskBoolFalseDelegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskBoolFalse(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveVoidDep(RemoteWriteObject target, int? param)
        {
            return SaveVoidDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveVoidDep1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveVoidDep1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveVoidDep1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveBoolTrueDep(RemoteWriteObject target, int? param)
        {
            return SaveBoolTrueDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveBoolTrueDep1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveBoolTrueDep1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveBoolTrueDep1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveBoolFalseDep(RemoteWriteObject target, int? param)
        {
            return SaveBoolFalseDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveBoolFalseDep1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveBoolFalseDep1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveBoolFalseDep1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskDep(RemoteWriteObject target, int? param)
        {
            return SaveTaskDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskDep1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskDep1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskDep1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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

        public virtual Task<RemoteWriteObject?> SaveTaskBoolDep(RemoteWriteObject target, int? param)
        {
            return SaveTaskBoolDep1Property(target, param);
        }

        public virtual async Task<RemoteWriteObject?> RemoteSaveTaskBoolDep1(RemoteWriteObject target, int? param)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<RemoteWriteObject?>(typeof(SaveTaskBoolDep1Delegate), [target, param]))!;
        }

        public virtual Task<RemoteWriteObject?> LocalSaveTaskBoolDep1(RemoteWriteObject target, int? param)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(default(RemoteWriteObject));
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
            services.AddTransient<RemoteWriteObject>();
            services.AddScoped<RemoteWriteObjectFactory>();
            services.AddScoped<IRemoteWriteObjectFactory, RemoteWriteObjectFactory>();
            services.AddScoped<SaveVoidDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveVoid(target);
            });
            services.AddScoped<SaveBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveBool(target);
            });
            services.AddScoped<SaveTaskDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveTask(target);
            });
            services.AddScoped<SaveTaskBoolDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveTaskBool(target);
            });
            services.AddScoped<SaveVoidDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveVoidDep(target);
            });
            services.AddScoped<SaveBoolTrueDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveBoolTrueDep(target);
            });
            services.AddScoped<SaveBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveBoolFalseDep(target);
            });
            services.AddScoped<SaveTaskDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveTaskDep(target);
            });
            services.AddScoped<SaveTaskBoolDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveTaskBoolDep(target);
            });
            services.AddScoped<SaveTaskBoolFalseDepDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target) => factory.LocalSaveTaskBoolFalseDep(target);
            });
            services.AddScoped<SaveVoid1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveVoid1(target, param);
            });
            services.AddScoped<SaveBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveBool1(target, param);
            });
            services.AddScoped<SaveTask1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveTask1(target, param);
            });
            services.AddScoped<SaveTaskBool1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveTaskBool1(target, param);
            });
            services.AddScoped<SaveTaskBoolFalseDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveTaskBoolFalse(target, param);
            });
            services.AddScoped<SaveVoidDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveVoidDep1(target, param);
            });
            services.AddScoped<SaveBoolTrueDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveBoolTrueDep1(target, param);
            });
            services.AddScoped<SaveBoolFalseDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveBoolFalseDep1(target, param);
            });
            services.AddScoped<SaveTaskDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveTaskDep1(target, param);
            });
            services.AddScoped<SaveTaskBoolDep1Delegate>(cc =>
            {
                var factory = cc.GetRequiredService<RemoteWriteObjectFactory>();
                return (RemoteWriteObject target, int? param) => factory.LocalSaveTaskBoolDep1(target, param);
            });
            services.AddScoped<IFactorySave<RemoteWriteObject>, RemoteWriteObjectFactory>();
        }
    }
}