﻿#nullable enable
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using static Neatoo.RemoteFactory.FactoryGeneratorTests.ReadTests;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using Xunit;

/*
                    Debugging Messages:
                    Parent class: ReadTests
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute

                    */
namespace Neatoo.RemoteFactory.FactoryGeneratorTests
{
    public interface IReadObjectFactory
    {
        ReadObject CreateVoid();
        ReadObject? CreateBool();
        Task<ReadObject> CreateTask();
        Task<ReadObject?> CreateTaskBool();
        ReadObject CreateVoid(int? param);
        ReadObject? CreateBool(int? param);
        Task<ReadObject> CreateTask(int? param);
        Task<ReadObject?> CreateTaskBool(int? param);
        Task<ReadObject?> CreateTaskBoolFalse(int? param);
        ReadObject CreateVoidDep();
        ReadObject? CreateBoolTrueDep();
        ReadObject? CreateBoolFalseDep();
        Task<ReadObject> CreateTaskDep();
        Task<ReadObject?> CreateTaskBoolDep();
        Task<ReadObject?> CreateTaskBoolFalseDep();
        ReadObject CreateVoidDep(int? param);
        ReadObject? CreateBoolTrueDep(int? param);
        ReadObject? CreateBoolFalseDep(int? param);
        Task<ReadObject> CreateTaskDep(int? param);
        Task<ReadObject?> CreateTaskBoolDep(int? param);
        ReadObject FetchVoid();
        ReadObject? FetchBool();
        Task<ReadObject> FetchTask();
        Task<ReadObject?> FetchTaskBool();
        ReadObject FetchVoid(int? param);
        ReadObject? FetchBool(int? param);
        Task<ReadObject> FetchTask(int? param);
        Task<ReadObject?> FetchTaskBool(int? param);
        ReadObject FetchVoidDep();
        ReadObject? FetchBoolTrueDep();
        ReadObject? FetchBoolFalseDep();
        Task<ReadObject> FetchTaskDep();
        Task<ReadObject?> FetchTaskBoolDep();
        ReadObject FetchVoidDep(int? param);
        ReadObject? FetchBoolTrueDep(int? param);
        ReadObject? FetchBoolFalseDep(int? param);
        Task<ReadObject> FetchTaskDep(int? param);
        Task<ReadObject?> FetchTaskBoolDep(int? param);
        Task<ReadObject?> FetchTaskBoolFalseDep(int? param);
    }

    internal class ReadObjectFactory : FactoryBase, IReadObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ReadObjectFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }

        public ReadObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual ReadObject CreateVoid()
        {
            return LocalCreateVoid();
        }

        public ReadObject LocalCreateVoid()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Create, () => target.CreateVoid());
        }

        public virtual ReadObject? CreateBool()
        {
            return LocalCreateBool();
        }

        public ReadObject? LocalCreateBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Create, () => target.CreateBool());
        }

        public virtual Task<ReadObject> CreateTask()
        {
            return LocalCreateTask();
        }

        public Task<ReadObject> LocalCreateTask()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTask());
        }

        public virtual Task<ReadObject?> CreateTaskBool()
        {
            return LocalCreateTaskBool();
        }

        public Task<ReadObject?> LocalCreateTaskBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskBool());
        }

        public virtual ReadObject CreateVoid(int? param)
        {
            return LocalCreateVoid1(param);
        }

        public ReadObject LocalCreateVoid1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Create, () => target.CreateVoid(param));
        }

        public virtual ReadObject? CreateBool(int? param)
        {
            return LocalCreateBool1(param);
        }

        public ReadObject? LocalCreateBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Create, () => target.CreateBool(param));
        }

        public virtual Task<ReadObject> CreateTask(int? param)
        {
            return LocalCreateTask1(param);
        }

        public Task<ReadObject> LocalCreateTask1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTask(param));
        }

        public virtual Task<ReadObject?> CreateTaskBool(int? param)
        {
            return LocalCreateTaskBool1(param);
        }

        public Task<ReadObject?> LocalCreateTaskBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskBool(param));
        }

        public virtual Task<ReadObject?> CreateTaskBoolFalse(int? param)
        {
            return LocalCreateTaskBoolFalse(param);
        }

        public Task<ReadObject?> LocalCreateTaskBoolFalse(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolFalse(param));
        }

        public virtual ReadObject CreateVoidDep()
        {
            return LocalCreateVoidDep();
        }

        public ReadObject LocalCreateVoidDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Create, () => target.CreateVoidDep(service));
        }

        public virtual ReadObject? CreateBoolTrueDep()
        {
            return LocalCreateBoolTrueDep();
        }

        public ReadObject? LocalCreateBoolTrueDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Create, () => target.CreateBoolTrueDep(service));
        }

        public virtual ReadObject? CreateBoolFalseDep()
        {
            return LocalCreateBoolFalseDep();
        }

        public ReadObject? LocalCreateBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Create, () => target.CreateBoolFalseDep(service));
        }

        public virtual Task<ReadObject> CreateTaskDep()
        {
            return LocalCreateTaskDep();
        }

        public Task<ReadObject> LocalCreateTaskDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskDep(service));
        }

        public virtual Task<ReadObject?> CreateTaskBoolDep()
        {
            return LocalCreateTaskBoolDep();
        }

        public Task<ReadObject?> LocalCreateTaskBoolDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolDep(service));
        }

        public virtual Task<ReadObject?> CreateTaskBoolFalseDep()
        {
            return LocalCreateTaskBoolFalseDep();
        }

        public Task<ReadObject?> LocalCreateTaskBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolFalseDep(service));
        }

        public virtual ReadObject CreateVoidDep(int? param)
        {
            return LocalCreateVoidDep1(param);
        }

        public ReadObject LocalCreateVoidDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Create, () => target.CreateVoidDep(param, service));
        }

        public virtual ReadObject? CreateBoolTrueDep(int? param)
        {
            return LocalCreateBoolTrueDep1(param);
        }

        public ReadObject? LocalCreateBoolTrueDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Create, () => target.CreateBoolTrueDep(param, service));
        }

        public virtual ReadObject? CreateBoolFalseDep(int? param)
        {
            return LocalCreateBoolFalseDep1(param);
        }

        public ReadObject? LocalCreateBoolFalseDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Create, () => target.CreateBoolFalseDep(param, service));
        }

        public virtual Task<ReadObject> CreateTaskDep(int? param)
        {
            return LocalCreateTaskDep1(param);
        }

        public Task<ReadObject> LocalCreateTaskDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskDep(param, service));
        }

        public virtual Task<ReadObject?> CreateTaskBoolDep(int? param)
        {
            return LocalCreateTaskBoolDep1(param);
        }

        public Task<ReadObject?> LocalCreateTaskBoolDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolDep(param, service));
        }

        public virtual ReadObject FetchVoid()
        {
            return LocalFetchVoid();
        }

        public ReadObject LocalFetchVoid()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchVoid());
        }

        public virtual ReadObject? FetchBool()
        {
            return LocalFetchBool();
        }

        public ReadObject? LocalFetchBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchBool());
        }

        public virtual Task<ReadObject> FetchTask()
        {
            return LocalFetchTask();
        }

        public Task<ReadObject> LocalFetchTask()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTask());
        }

        public virtual Task<ReadObject?> FetchTaskBool()
        {
            return LocalFetchTaskBool();
        }

        public Task<ReadObject?> LocalFetchTaskBool()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBool());
        }

        public virtual ReadObject FetchVoid(int? param)
        {
            return LocalFetchVoid1(param);
        }

        public ReadObject LocalFetchVoid1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchVoid(param));
        }

        public virtual ReadObject? FetchBool(int? param)
        {
            return LocalFetchBool1(param);
        }

        public ReadObject? LocalFetchBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchBool(param));
        }

        public virtual Task<ReadObject> FetchTask(int? param)
        {
            return LocalFetchTask1(param);
        }

        public Task<ReadObject> LocalFetchTask1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTask(param));
        }

        public virtual Task<ReadObject?> FetchTaskBool(int? param)
        {
            return LocalFetchTaskBool1(param);
        }

        public Task<ReadObject?> LocalFetchTaskBool1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBool(param));
        }

        public virtual ReadObject FetchVoidDep()
        {
            return LocalFetchVoidDep();
        }

        public ReadObject LocalFetchVoidDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchVoidDep(service));
        }

        public virtual ReadObject? FetchBoolTrueDep()
        {
            return LocalFetchBoolTrueDep();
        }

        public ReadObject? LocalFetchBoolTrueDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchBoolTrueDep(service));
        }

        public virtual ReadObject? FetchBoolFalseDep()
        {
            return LocalFetchBoolFalseDep();
        }

        public ReadObject? LocalFetchBoolFalseDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchBoolFalseDep(service));
        }

        public virtual Task<ReadObject> FetchTaskDep()
        {
            return LocalFetchTaskDep();
        }

        public Task<ReadObject> LocalFetchTaskDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTaskDep(service));
        }

        public virtual Task<ReadObject?> FetchTaskBoolDep()
        {
            return LocalFetchTaskBoolDep();
        }

        public Task<ReadObject?> LocalFetchTaskBoolDep()
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolDep(service));
        }

        public virtual ReadObject FetchVoidDep(int? param)
        {
            return LocalFetchVoidDep1(param);
        }

        public ReadObject LocalFetchVoidDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCall<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchVoidDep(param, service));
        }

        public virtual ReadObject? FetchBoolTrueDep(int? param)
        {
            return LocalFetchBoolTrueDep1(param);
        }

        public ReadObject? LocalFetchBoolTrueDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchBoolTrueDep(param, service));
        }

        public virtual ReadObject? FetchBoolFalseDep(int? param)
        {
            return LocalFetchBoolFalseDep1(param);
        }

        public ReadObject? LocalFetchBoolFalseDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBool<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchBoolFalseDep(param, service));
        }

        public virtual Task<ReadObject> FetchTaskDep(int? param)
        {
            return LocalFetchTaskDep1(param);
        }

        public Task<ReadObject> LocalFetchTaskDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTaskDep(param, service));
        }

        public virtual Task<ReadObject?> FetchTaskBoolDep(int? param)
        {
            return LocalFetchTaskBoolDep1(param);
        }

        public Task<ReadObject?> LocalFetchTaskBoolDep1(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolDep(param, service));
        }

        public virtual Task<ReadObject?> FetchTaskBoolFalseDep(int? param)
        {
            return LocalFetchTaskBoolFalseDep(param);
        }

        public Task<ReadObject?> LocalFetchTaskBoolFalseDep(int? param)
        {
            var target = ServiceProvider.GetRequiredService<ReadObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return DoFactoryMethodCallBoolAsync<ReadObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolFalseDep(param, service));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<ReadObject>();
            services.AddScoped<ReadObjectFactory>();
            services.AddScoped<IReadObjectFactory, ReadObjectFactory>();
        }
    }
}