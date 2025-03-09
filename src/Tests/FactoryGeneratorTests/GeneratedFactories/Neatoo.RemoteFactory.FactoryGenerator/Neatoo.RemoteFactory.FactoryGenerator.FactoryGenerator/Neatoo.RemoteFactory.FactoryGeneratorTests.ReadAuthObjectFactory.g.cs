#nullable enable
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using static Neatoo.RemoteFactory.FactoryGeneratorTests.ReadAuthTests;
using Xunit;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

/*
                    Debugging Messages:
                    Parent class: ReadAuthTests
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No MethodDeclarationSyntax for get_CanReadCalled
No MethodDeclarationSyntax for set_CanReadCalled
No MethodDeclarationSyntax for get_CanCreateCalled
No MethodDeclarationSyntax for set_CanCreateCalled
No MethodDeclarationSyntax for get_CanFetchCalled
No MethodDeclarationSyntax for set_CanFetchCalled
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for .ctor
No MethodDeclarationSyntax for Equals
No MethodDeclarationSyntax for Equals
No MethodDeclarationSyntax for Finalize
No MethodDeclarationSyntax for GetHashCode
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No MethodDeclarationSyntax for ReferenceEquals
No MethodDeclarationSyntax for ToString
Parameter type mismatch for CanReadBoolFalse and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadBoolFalse and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalse and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalse and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalse and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalse and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalse and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService

                    */
namespace Neatoo.RemoteFactory.FactoryGeneratorTests
{
    public interface IReadAuthObjectFactory
    {
        ReadAuthObject CreateVoid();
        ReadAuthObject? CreateBool();
        Task<ReadAuthObject> CreateTask();
        Task<ReadAuthObject?> CreateTaskBool();
        ReadAuthObject CreateVoid(int? param);
        ReadAuthObject? CreateBool(int? param);
        Task<ReadAuthObject> CreateTask(int? param);
        Task<ReadAuthObject?> CreateTaskBool(int? param);
        Task<ReadAuthObject?> CreateTaskBoolFalse(int? param);
        ReadAuthObject CreateVoidDep();
        ReadAuthObject? CreateBoolTrueDep();
        ReadAuthObject? CreateBoolFalseDep();
        Task<ReadAuthObject> CreateTaskDep();
        Task<ReadAuthObject?> CreateTaskBoolDep();
        Task<ReadAuthObject?> CreateTaskBoolFalseDep();
        ReadAuthObject CreateVoidDep(int? param);
        ReadAuthObject? CreateBoolTrueDep(int? param);
        ReadAuthObject? CreateBoolFalseDep(int? param);
        Task<ReadAuthObject> CreateTaskDep(int? param);
        Task<ReadAuthObject?> CreateTaskBoolDep(int? param);
        ReadAuthObject FetchVoid();
        ReadAuthObject? FetchBool();
        Task<ReadAuthObject> FetchTask();
        Task<ReadAuthObject?> FetchTaskBool();
        ReadAuthObject FetchVoid(int? param);
        ReadAuthObject? FetchBool(int? param);
        Task<ReadAuthObject> FetchTask(int? param);
        Task<ReadAuthObject?> FetchTaskBool(int? param);
        ReadAuthObject FetchVoidDep();
        ReadAuthObject? FetchBoolTrueDep();
        ReadAuthObject? FetchBoolFalseDep();
        Task<ReadAuthObject> FetchTaskDep();
        Task<ReadAuthObject?> FetchTaskBoolDep();
        ReadAuthObject FetchVoidDep(int? param);
        ReadAuthObject? FetchBoolTrueDep(int? param);
        ReadAuthObject? FetchBoolFalseDep(int? param);
        Task<ReadAuthObject> FetchTaskDep(int? param);
        Task<ReadAuthObject?> FetchTaskBoolDep(int? param);
        Task<ReadAuthObject?> FetchTaskBoolFalseDep(int? param);
        Authorized CanCreateVoid();
        Authorized CanCreateBool();
        Authorized CanCreateTask();
        Authorized CanCreateTaskBool();
        Authorized CanCreateVoid(int? param);
        Authorized CanCreateBool(int? param);
        Authorized CanCreateTask(int? param);
        Authorized CanCreateTaskBool(int? param);
        Authorized CanCreateTaskBoolFalse(int? param);
        Authorized CanCreateVoidDep();
        Authorized CanCreateBoolTrueDep();
        Authorized CanCreateBoolFalseDep();
        Authorized CanCreateTaskDep();
        Authorized CanCreateTaskBoolDep();
        Authorized CanCreateTaskBoolFalseDep();
        Authorized CanCreateVoidDep(int? param);
        Authorized CanCreateBoolTrueDep(int? param);
        Authorized CanCreateBoolFalseDep(int? param);
        Authorized CanCreateTaskDep(int? param);
        Authorized CanCreateTaskBoolDep(int? param);
        Authorized CanFetchVoid();
        Authorized CanFetchBool();
        Authorized CanFetchTask();
        Authorized CanFetchTaskBool();
        Authorized CanFetchVoid(int? param);
        Authorized CanFetchBool(int? param);
        Authorized CanFetchTask(int? param);
        Authorized CanFetchTaskBool(int? param);
        Authorized CanFetchVoidDep();
        Authorized CanFetchBoolTrueDep();
        Authorized CanFetchBoolFalseDep();
        Authorized CanFetchTaskDep();
        Authorized CanFetchTaskBoolDep();
        Authorized CanFetchVoidDep(int? param);
        Authorized CanFetchBoolTrueDep(int? param);
        Authorized CanFetchBoolFalseDep(int? param);
        Authorized CanFetchTaskDep(int? param);
        Authorized CanFetchTaskBoolDep(int? param);
        Authorized CanFetchTaskBoolFalseDep(int? param);
    }

    internal class ReadAuthObjectFactory : FactoryBase, IReadAuthObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ReadAuth ReadAuth { get; }

        public ReadAuthObjectFactory(IServiceProvider serviceProvider, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.ReadAuth = readauth;
        }

        public ReadAuthObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            this.ReadAuth = readauth;
            this.ReadAuth = readauth;
        }

        public virtual ReadAuthObject CreateVoid()
        {
            return (LocalCreateVoid()).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateVoid()));
        }

        public virtual ReadAuthObject? CreateBool()
        {
            return (LocalCreateBool()).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateBool()));
        }

        public virtual async Task<ReadAuthObject> CreateTask()
        {
            return (await LocalCreateTask()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTask()));
        }

        public virtual async Task<ReadAuthObject?> CreateTaskBool()
        {
            return (await LocalCreateTaskBool()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskBool()));
        }

        public virtual ReadAuthObject CreateVoid(int? param)
        {
            return (LocalCreateVoid1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateVoid(param)));
        }

        public virtual ReadAuthObject? CreateBool(int? param)
        {
            return (LocalCreateBool1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateBool(param)));
        }

        public virtual async Task<ReadAuthObject> CreateTask(int? param)
        {
            return (await LocalCreateTask1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTask(param)));
        }

        public virtual async Task<ReadAuthObject?> CreateTaskBool(int? param)
        {
            return (await LocalCreateTaskBool1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskBool(param)));
        }

        public virtual async Task<ReadAuthObject?> CreateTaskBoolFalse(int? param)
        {
            return (await LocalCreateTaskBoolFalse(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolFalse(param)));
        }

        public virtual ReadAuthObject CreateVoidDep()
        {
            return (LocalCreateVoidDep()).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateVoidDep(service)));
        }

        public virtual ReadAuthObject? CreateBoolTrueDep()
        {
            return (LocalCreateBoolTrueDep()).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateBoolTrueDep(service)));
        }

        public virtual ReadAuthObject? CreateBoolFalseDep()
        {
            return (LocalCreateBoolFalseDep()).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateBoolFalseDep(service)));
        }

        public virtual async Task<ReadAuthObject> CreateTaskDep()
        {
            return (await LocalCreateTaskDep()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskDep(service)));
        }

        public virtual async Task<ReadAuthObject?> CreateTaskBoolDep()
        {
            return (await LocalCreateTaskBoolDep()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolDep(service)));
        }

        public virtual async Task<ReadAuthObject?> CreateTaskBoolFalseDep()
        {
            return (await LocalCreateTaskBoolFalseDep()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolFalseDep(service)));
        }

        public virtual ReadAuthObject CreateVoidDep(int? param)
        {
            return (LocalCreateVoidDep1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateVoidDep(param, service)));
        }

        public virtual ReadAuthObject? CreateBoolTrueDep(int? param)
        {
            return (LocalCreateBoolTrueDep1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateBoolTrueDep(param, service)));
        }

        public virtual ReadAuthObject? CreateBoolFalseDep(int? param)
        {
            return (LocalCreateBoolFalseDep1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateBoolFalseDep(param, service)));
        }

        public virtual async Task<ReadAuthObject> CreateTaskDep(int? param)
        {
            return (await LocalCreateTaskDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskDep(param, service)));
        }

        public virtual async Task<ReadAuthObject?> CreateTaskBoolDep(int? param)
        {
            return (await LocalCreateTaskBoolDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolDep(param, service)));
        }

        public virtual ReadAuthObject FetchVoid()
        {
            return (LocalFetchVoid()).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchVoid()));
        }

        public virtual ReadAuthObject? FetchBool()
        {
            return (LocalFetchBool()).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchBool()));
        }

        public virtual async Task<ReadAuthObject> FetchTask()
        {
            return (await LocalFetchTask()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTask()));
        }

        public virtual async Task<ReadAuthObject?> FetchTaskBool()
        {
            return (await LocalFetchTaskBool()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBool()));
        }

        public virtual ReadAuthObject FetchVoid(int? param)
        {
            return (LocalFetchVoid1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchVoid(param)));
        }

        public virtual ReadAuthObject? FetchBool(int? param)
        {
            return (LocalFetchBool1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchBool(param)));
        }

        public virtual async Task<ReadAuthObject> FetchTask(int? param)
        {
            return (await LocalFetchTask1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTask(param)));
        }

        public virtual async Task<ReadAuthObject?> FetchTaskBool(int? param)
        {
            return (await LocalFetchTaskBool1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBool(param)));
        }

        public virtual ReadAuthObject FetchVoidDep()
        {
            return (LocalFetchVoidDep()).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchVoidDep(service)));
        }

        public virtual ReadAuthObject? FetchBoolTrueDep()
        {
            return (LocalFetchBoolTrueDep()).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchBoolTrueDep(service)));
        }

        public virtual ReadAuthObject? FetchBoolFalseDep()
        {
            return (LocalFetchBoolFalseDep()).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchBoolFalseDep(service)));
        }

        public virtual async Task<ReadAuthObject> FetchTaskDep()
        {
            return (await LocalFetchTaskDep()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTaskDep(service)));
        }

        public virtual async Task<ReadAuthObject?> FetchTaskBoolDep()
        {
            return (await LocalFetchTaskBoolDep()).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolDep(service)));
        }

        public virtual ReadAuthObject FetchVoidDep(int? param)
        {
            return (LocalFetchVoidDep1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCall<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchVoidDep(param, service)));
        }

        public virtual ReadAuthObject? FetchBoolTrueDep(int? param)
        {
            return (LocalFetchBoolTrueDep1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchBoolTrueDep(param, service)));
        }

        public virtual ReadAuthObject? FetchBoolFalseDep(int? param)
        {
            return (LocalFetchBoolFalseDep1(param)).Result!;
        }

        public Authorized<ReadAuthObject> LocalFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(DoFactoryMethodCallBool<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchBoolFalseDep(param, service)));
        }

        public virtual async Task<ReadAuthObject> FetchTaskDep(int? param)
        {
            return (await LocalFetchTaskDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTaskDep(param, service)));
        }

        public virtual async Task<ReadAuthObject?> FetchTaskBoolDep(int? param)
        {
            return (await LocalFetchTaskBoolDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolDep(param, service)));
        }

        public virtual async Task<ReadAuthObject?> FetchTaskBoolFalseDep(int? param)
        {
            return (await LocalFetchTaskBoolFalseDep(param)).Result!;
        }

        public async Task<Authorized<ReadAuthObject>> LocalFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthObject>(await DoFactoryMethodCallBoolAsync<ReadAuthObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolFalseDep(param, service)));
        }

        public virtual Authorized CanCreateVoid()
        {
            return LocalCanCreateVoid();
        }

        public Authorized LocalCanCreateVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBool()
        {
            return LocalCanCreateBool();
        }

        public Authorized LocalCanCreateBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTask()
        {
            return LocalCanCreateTask();
        }

        public Authorized LocalCanCreateTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBool()
        {
            return LocalCanCreateTaskBool();
        }

        public Authorized LocalCanCreateTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateVoid(int? param)
        {
            return LocalCanCreateVoid1(param);
        }

        public Authorized LocalCanCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBool(int? param)
        {
            return LocalCanCreateBool1(param);
        }

        public Authorized LocalCanCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTask(int? param)
        {
            return LocalCanCreateTask1(param);
        }

        public Authorized LocalCanCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBool(int? param)
        {
            return LocalCanCreateTaskBool1(param);
        }

        public Authorized LocalCanCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolFalse(int? param)
        {
            return LocalCanCreateTaskBoolFalse(param);
        }

        public Authorized LocalCanCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateVoidDep()
        {
            return LocalCanCreateVoidDep();
        }

        public Authorized LocalCanCreateVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolTrueDep()
        {
            return LocalCanCreateBoolTrueDep();
        }

        public Authorized LocalCanCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolFalseDep()
        {
            return LocalCanCreateBoolFalseDep();
        }

        public Authorized LocalCanCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskDep()
        {
            return LocalCanCreateTaskDep();
        }

        public Authorized LocalCanCreateTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolDep()
        {
            return LocalCanCreateTaskBoolDep();
        }

        public Authorized LocalCanCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolFalseDep()
        {
            return LocalCanCreateTaskBoolFalseDep();
        }

        public Authorized LocalCanCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateVoidDep(int? param)
        {
            return LocalCanCreateVoidDep1(param);
        }

        public Authorized LocalCanCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolTrueDep(int? param)
        {
            return LocalCanCreateBoolTrueDep1(param);
        }

        public Authorized LocalCanCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateBoolFalseDep(int? param)
        {
            return LocalCanCreateBoolFalseDep1(param);
        }

        public Authorized LocalCanCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskDep(int? param)
        {
            return LocalCanCreateTaskDep1(param);
        }

        public Authorized LocalCanCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateTaskBoolDep(int? param)
        {
            return LocalCanCreateTaskBoolDep1(param);
        }

        public Authorized LocalCanCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoid()
        {
            return LocalCanFetchVoid();
        }

        public Authorized LocalCanFetchVoid()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBool()
        {
            return LocalCanFetchBool();
        }

        public Authorized LocalCanFetchBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTask()
        {
            return LocalCanFetchTask();
        }

        public Authorized LocalCanFetchTask()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBool()
        {
            return LocalCanFetchTaskBool();
        }

        public Authorized LocalCanFetchTaskBool()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoid(int? param)
        {
            return LocalCanFetchVoid1(param);
        }

        public Authorized LocalCanFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBool(int? param)
        {
            return LocalCanFetchBool1(param);
        }

        public Authorized LocalCanFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTask(int? param)
        {
            return LocalCanFetchTask1(param);
        }

        public Authorized LocalCanFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBool(int? param)
        {
            return LocalCanFetchTaskBool1(param);
        }

        public Authorized LocalCanFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoidDep()
        {
            return LocalCanFetchVoidDep();
        }

        public Authorized LocalCanFetchVoidDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolTrueDep()
        {
            return LocalCanFetchBoolTrueDep();
        }

        public Authorized LocalCanFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolFalseDep()
        {
            return LocalCanFetchBoolFalseDep();
        }

        public Authorized LocalCanFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskDep()
        {
            return LocalCanFetchTaskDep();
        }

        public Authorized LocalCanFetchTaskDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBoolDep()
        {
            return LocalCanFetchTaskBoolDep();
        }

        public Authorized LocalCanFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchVoidDep(int? param)
        {
            return LocalCanFetchVoidDep1(param);
        }

        public Authorized LocalCanFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolTrueDep(int? param)
        {
            return LocalCanFetchBoolTrueDep1(param);
        }

        public Authorized LocalCanFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchBoolFalseDep(int? param)
        {
            return LocalCanFetchBoolFalseDep1(param);
        }

        public Authorized LocalCanFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskDep(int? param)
        {
            return LocalCanFetchTaskDep1(param);
        }

        public Authorized LocalCanFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBoolDep(int? param)
        {
            return LocalCanFetchTaskBoolDep1(param);
        }

        public Authorized LocalCanFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetchTaskBoolFalseDep(int? param)
        {
            return LocalCanFetchTaskBoolFalseDep(param);
        }

        public Authorized LocalCanFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<ReadAuthObject>();
            services.AddScoped<ReadAuthObjectFactory>();
            services.AddScoped<IReadAuthObjectFactory, ReadAuthObjectFactory>();
        }
    }
}