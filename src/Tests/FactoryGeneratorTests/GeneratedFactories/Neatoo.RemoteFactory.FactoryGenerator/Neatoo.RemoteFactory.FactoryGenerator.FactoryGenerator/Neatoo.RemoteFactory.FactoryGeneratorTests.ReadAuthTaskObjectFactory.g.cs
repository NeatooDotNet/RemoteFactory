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
No MethodDeclarationSyntax for .ctor
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
Parameter type mismatch for CanReadBoolFalseTask and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and CreateVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
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
Parameter type mismatch for CanReadBoolFalseTask and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and CreateBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and CreateBoolTrueDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and CreateBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and CreateBoolFalseDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and CreateTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and CreateTaskDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and CreateTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and CreateTaskBoolDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and CreateTaskBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and CreateTaskBoolFalseDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and FetchVoidDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and FetchVoidDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and FetchBoolTrueDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and FetchBoolTrueDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and FetchBoolFalseDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and FetchBoolFalseDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and FetchTaskDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and FetchTaskDep parameter p int? 
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
Parameter type mismatch for CanReadBoolFalseTask and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanReadStringFalseTask and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateBoolFalseTask and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanCreateStringFalseTask and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchBoolFalseTask and FetchTaskBoolDep parameter p int? 
int? != Neatoo.RemoteFactory.FactoryGeneratorTests.Shared.IService
Parameter type mismatch for CanFetchStringFalseTask and FetchTaskBoolDep parameter p int? 
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
    public interface IReadAuthTaskObjectFactory
    {
        Task<ReadAuthTaskObject> CreateVoid();
        Task<ReadAuthTaskObject?> CreateBool();
        Task<ReadAuthTaskObject> CreateTask();
        Task<ReadAuthTaskObject?> CreateTaskBool();
        Task<ReadAuthTaskObject> CreateVoid(int? param);
        Task<ReadAuthTaskObject?> CreateBool(int? param);
        Task<ReadAuthTaskObject> CreateTask(int? param);
        Task<ReadAuthTaskObject?> CreateTaskBool(int? param);
        Task<ReadAuthTaskObject?> CreateTaskBoolFalse(int? param);
        Task<ReadAuthTaskObject> CreateVoidDep();
        Task<ReadAuthTaskObject?> CreateBoolTrueDep();
        Task<ReadAuthTaskObject?> CreateBoolFalseDep();
        Task<ReadAuthTaskObject> CreateTaskDep();
        Task<ReadAuthTaskObject?> CreateTaskBoolDep();
        Task<ReadAuthTaskObject?> CreateTaskBoolFalseDep();
        Task<ReadAuthTaskObject> CreateVoidDep(int? param);
        Task<ReadAuthTaskObject?> CreateBoolTrueDep(int? param);
        Task<ReadAuthTaskObject?> CreateBoolFalseDep(int? param);
        Task<ReadAuthTaskObject> CreateTaskDep(int? param);
        Task<ReadAuthTaskObject?> CreateTaskBoolDep(int? param);
        Task<ReadAuthTaskObject> FetchVoid();
        Task<ReadAuthTaskObject?> FetchBool();
        Task<ReadAuthTaskObject> FetchTask();
        Task<ReadAuthTaskObject?> FetchTaskBool();
        Task<ReadAuthTaskObject> FetchVoid(int? param);
        Task<ReadAuthTaskObject?> FetchBool(int? param);
        Task<ReadAuthTaskObject> FetchTask(int? param);
        Task<ReadAuthTaskObject?> FetchTaskBool(int? param);
        Task<ReadAuthTaskObject> FetchVoidDep();
        Task<ReadAuthTaskObject?> FetchBoolTrueDep();
        Task<ReadAuthTaskObject?> FetchBoolFalseDep();
        Task<ReadAuthTaskObject> FetchTaskDep();
        Task<ReadAuthTaskObject?> FetchTaskBoolDep();
        Task<ReadAuthTaskObject> FetchVoidDep(int? param);
        Task<ReadAuthTaskObject?> FetchBoolTrueDep(int? param);
        Task<ReadAuthTaskObject?> FetchBoolFalseDep(int? param);
        Task<ReadAuthTaskObject> FetchTaskDep(int? param);
        Task<ReadAuthTaskObject?> FetchTaskBoolDep(int? param);
        Task<ReadAuthTaskObject?> FetchTaskBoolFalseDep(int? param);
        Task<Authorized> CanCreateVoid();
        Task<Authorized> CanCreateBool();
        Task<Authorized> CanCreateTask();
        Task<Authorized> CanCreateTaskBool();
        Task<Authorized> CanCreateVoid(int? param);
        Task<Authorized> CanCreateBool(int? param);
        Task<Authorized> CanCreateTask(int? param);
        Task<Authorized> CanCreateTaskBool(int? param);
        Task<Authorized> CanCreateTaskBoolFalse(int? param);
        Task<Authorized> CanCreateVoidDep();
        Task<Authorized> CanCreateBoolTrueDep();
        Task<Authorized> CanCreateBoolFalseDep();
        Task<Authorized> CanCreateTaskDep();
        Task<Authorized> CanCreateTaskBoolDep();
        Task<Authorized> CanCreateTaskBoolFalseDep();
        Task<Authorized> CanCreateVoidDep(int? param);
        Task<Authorized> CanCreateBoolTrueDep(int? param);
        Task<Authorized> CanCreateBoolFalseDep(int? param);
        Task<Authorized> CanCreateTaskDep(int? param);
        Task<Authorized> CanCreateTaskBoolDep(int? param);
        Task<Authorized> CanFetchVoid();
        Task<Authorized> CanFetchBool();
        Task<Authorized> CanFetchTask();
        Task<Authorized> CanFetchTaskBool();
        Task<Authorized> CanFetchVoid(int? param);
        Task<Authorized> CanFetchBool(int? param);
        Task<Authorized> CanFetchTask(int? param);
        Task<Authorized> CanFetchTaskBool(int? param);
        Task<Authorized> CanFetchVoidDep();
        Task<Authorized> CanFetchBoolTrueDep();
        Task<Authorized> CanFetchBoolFalseDep();
        Task<Authorized> CanFetchTaskDep();
        Task<Authorized> CanFetchTaskBoolDep();
        Task<Authorized> CanFetchVoidDep(int? param);
        Task<Authorized> CanFetchBoolTrueDep(int? param);
        Task<Authorized> CanFetchBoolFalseDep(int? param);
        Task<Authorized> CanFetchTaskDep(int? param);
        Task<Authorized> CanFetchTaskBoolDep(int? param);
        Task<Authorized> CanFetchTaskBoolFalseDep(int? param);
    }

    internal class ReadAuthTaskObjectFactory : FactoryBase, IReadAuthTaskObjectFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ReadAuthTask ReadAuthTask { get; }
        public ReadAuth ReadAuth { get; }

        public ReadAuthTaskObjectFactory(IServiceProvider serviceProvider, ReadAuthTask readauthtask, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.ReadAuthTask = readauthtask;
            this.ReadAuth = readauth;
        }

        public ReadAuthTaskObjectFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, ReadAuthTask readauthtask, ReadAuth readauth)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            this.ReadAuthTask = readauthtask;
            this.ReadAuthTask = readauthtask;
            this.ReadAuth = readauth;
            this.ReadAuth = readauth;
        }

        public virtual async Task<ReadAuthTaskObject> CreateVoid()
        {
            return (await LocalCreateVoid()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateVoid()));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateBool()
        {
            return (await LocalCreateBool()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateBool()));
        }

        public virtual async Task<ReadAuthTaskObject> CreateTask()
        {
            return (await LocalCreateTask()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTask()));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateTaskBool()
        {
            return (await LocalCreateTaskBool()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskBool()));
        }

        public virtual async Task<ReadAuthTaskObject> CreateVoid(int? param)
        {
            return (await LocalCreateVoid1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateVoid(param)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateBool(int? param)
        {
            return (await LocalCreateBool1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateBool(param)));
        }

        public virtual async Task<ReadAuthTaskObject> CreateTask(int? param)
        {
            return (await LocalCreateTask1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTask(param)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateTaskBool(int? param)
        {
            return (await LocalCreateTaskBool1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskBool(param)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateTaskBoolFalse(int? param)
        {
            return (await LocalCreateTaskBoolFalse(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolFalse(param)));
        }

        public virtual async Task<ReadAuthTaskObject> CreateVoidDep()
        {
            return (await LocalCreateVoidDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateVoidDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateBoolTrueDep()
        {
            return (await LocalCreateBoolTrueDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateBoolTrueDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateBoolFalseDep()
        {
            return (await LocalCreateBoolFalseDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateBoolFalseDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject> CreateTaskDep()
        {
            return (await LocalCreateTaskDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateTaskBoolDep()
        {
            return (await LocalCreateTaskBoolDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateTaskBoolFalseDep()
        {
            return (await LocalCreateTaskBoolFalseDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolFalseDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject> CreateVoidDep(int? param)
        {
            return (await LocalCreateVoidDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateVoidDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateBoolTrueDep(int? param)
        {
            return (await LocalCreateBoolTrueDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateBoolTrueDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateBoolFalseDep(int? param)
        {
            return (await LocalCreateBoolFalseDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateBoolFalseDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject> CreateTaskDep(int? param)
        {
            return (await LocalCreateTaskDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject?> CreateTaskBoolDep(int? param)
        {
            return (await LocalCreateTaskBoolDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanCreateStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Create, () => target.CreateTaskBoolDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject> FetchVoid()
        {
            return (await LocalFetchVoid()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchVoid()));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchBool()
        {
            return (await LocalFetchBool()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchBool()));
        }

        public virtual async Task<ReadAuthTaskObject> FetchTask()
        {
            return (await LocalFetchTask()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTask()));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchTaskBool()
        {
            return (await LocalFetchTaskBool()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBool()));
        }

        public virtual async Task<ReadAuthTaskObject> FetchVoid(int? param)
        {
            return (await LocalFetchVoid1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchVoid(param)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchBool(int? param)
        {
            return (await LocalFetchBool1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchBool(param)));
        }

        public virtual async Task<ReadAuthTaskObject> FetchTask(int? param)
        {
            return (await LocalFetchTask1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTask(param)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchTaskBool(int? param)
        {
            return (await LocalFetchTaskBool1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBool(param)));
        }

        public virtual async Task<ReadAuthTaskObject> FetchVoidDep()
        {
            return (await LocalFetchVoidDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchVoidDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchBoolTrueDep()
        {
            return (await LocalFetchBoolTrueDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchBoolTrueDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchBoolFalseDep()
        {
            return (await LocalFetchBoolFalseDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchBoolFalseDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject> FetchTaskDep()
        {
            return (await LocalFetchTaskDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTaskDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchTaskBoolDep()
        {
            return (await LocalFetchTaskBoolDep()).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolDep(service)));
        }

        public virtual async Task<ReadAuthTaskObject> FetchVoidDep(int? param)
        {
            return (await LocalFetchVoidDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCall<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchVoidDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchBoolTrueDep(int? param)
        {
            return (await LocalFetchBoolTrueDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchBoolTrueDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchBoolFalseDep(int? param)
        {
            return (await LocalFetchBoolFalseDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(DoFactoryMethodCallBool<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchBoolFalseDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject> FetchTaskDep(int? param)
        {
            return (await LocalFetchTaskDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTaskDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchTaskBoolDep(int? param)
        {
            return (await LocalFetchTaskBoolDep1(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolDep(param, service)));
        }

        public virtual async Task<ReadAuthTaskObject?> FetchTaskBoolFalseDep(int? param)
        {
            return (await LocalFetchTaskBoolFalseDep(param)).Result!;
        }

        public async Task<Authorized<ReadAuthTaskObject>> LocalFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanReadStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBool();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchBoolFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchString();
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            authorized = ReadAuth.CanFetchStringFalse(param);
            if (!authorized.HasAccess)
            {
                return new Authorized<ReadAuthTaskObject>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<ReadAuthTaskObject>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ReadAuthTaskObject>(await DoFactoryMethodCallBoolAsync<ReadAuthTaskObject>(target, FactoryOperation.Fetch, () => target.FetchTaskBoolFalseDep(param, service)));
        }

        public virtual Task<Authorized> CanCreateVoid()
        {
            return LocalCanCreateVoid();
        }

        public async Task<Authorized> LocalCanCreateVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBool()
        {
            return LocalCanCreateBool();
        }

        public async Task<Authorized> LocalCanCreateBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTask()
        {
            return LocalCanCreateTask();
        }

        public async Task<Authorized> LocalCanCreateTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBool()
        {
            return LocalCanCreateTaskBool();
        }

        public async Task<Authorized> LocalCanCreateTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateVoid(int? param)
        {
            return LocalCanCreateVoid1(param);
        }

        public async Task<Authorized> LocalCanCreateVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBool(int? param)
        {
            return LocalCanCreateBool1(param);
        }

        public async Task<Authorized> LocalCanCreateBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTask(int? param)
        {
            return LocalCanCreateTask1(param);
        }

        public async Task<Authorized> LocalCanCreateTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBool(int? param)
        {
            return LocalCanCreateTaskBool1(param);
        }

        public async Task<Authorized> LocalCanCreateTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolFalse(int? param)
        {
            return LocalCanCreateTaskBoolFalse(param);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolFalse(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateVoidDep()
        {
            return LocalCanCreateVoidDep();
        }

        public async Task<Authorized> LocalCanCreateVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolTrueDep()
        {
            return LocalCanCreateBoolTrueDep();
        }

        public async Task<Authorized> LocalCanCreateBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolFalseDep()
        {
            return LocalCanCreateBoolFalseDep();
        }

        public async Task<Authorized> LocalCanCreateBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskDep()
        {
            return LocalCanCreateTaskDep();
        }

        public async Task<Authorized> LocalCanCreateTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolDep()
        {
            return LocalCanCreateTaskBoolDep();
        }

        public async Task<Authorized> LocalCanCreateTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolFalseDep()
        {
            return LocalCanCreateTaskBoolFalseDep();
        }

        public async Task<Authorized> LocalCanCreateTaskBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateVoidDep(int? param)
        {
            return LocalCanCreateVoidDep1(param);
        }

        public async Task<Authorized> LocalCanCreateVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolTrueDep(int? param)
        {
            return LocalCanCreateBoolTrueDep1(param);
        }

        public async Task<Authorized> LocalCanCreateBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateBoolFalseDep(int? param)
        {
            return LocalCanCreateBoolFalseDep1(param);
        }

        public async Task<Authorized> LocalCanCreateBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskDep(int? param)
        {
            return LocalCanCreateTaskDep1(param);
        }

        public async Task<Authorized> LocalCanCreateTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanCreateTaskBoolDep(int? param)
        {
            return LocalCanCreateTaskBoolDep1(param);
        }

        public async Task<Authorized> LocalCanCreateTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanCreateStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoid()
        {
            return LocalCanFetchVoid();
        }

        public async Task<Authorized> LocalCanFetchVoid()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBool()
        {
            return LocalCanFetchBool();
        }

        public async Task<Authorized> LocalCanFetchBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTask()
        {
            return LocalCanFetchTask();
        }

        public async Task<Authorized> LocalCanFetchTask()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBool()
        {
            return LocalCanFetchTaskBool();
        }

        public async Task<Authorized> LocalCanFetchTaskBool()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoid(int? param)
        {
            return LocalCanFetchVoid1(param);
        }

        public async Task<Authorized> LocalCanFetchVoid1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBool(int? param)
        {
            return LocalCanFetchBool1(param);
        }

        public async Task<Authorized> LocalCanFetchBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTask(int? param)
        {
            return LocalCanFetchTask1(param);
        }

        public async Task<Authorized> LocalCanFetchTask1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBool(int? param)
        {
            return LocalCanFetchTaskBool1(param);
        }

        public async Task<Authorized> LocalCanFetchTaskBool1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoidDep()
        {
            return LocalCanFetchVoidDep();
        }

        public async Task<Authorized> LocalCanFetchVoidDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolTrueDep()
        {
            return LocalCanFetchBoolTrueDep();
        }

        public async Task<Authorized> LocalCanFetchBoolTrueDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolFalseDep()
        {
            return LocalCanFetchBoolFalseDep();
        }

        public async Task<Authorized> LocalCanFetchBoolFalseDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskDep()
        {
            return LocalCanFetchTaskDep();
        }

        public async Task<Authorized> LocalCanFetchTaskDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBoolDep()
        {
            return LocalCanFetchTaskBoolDep();
        }

        public async Task<Authorized> LocalCanFetchTaskBoolDep()
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchVoidDep(int? param)
        {
            return LocalCanFetchVoidDep1(param);
        }

        public async Task<Authorized> LocalCanFetchVoidDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolTrueDep(int? param)
        {
            return LocalCanFetchBoolTrueDep1(param);
        }

        public async Task<Authorized> LocalCanFetchBoolTrueDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchBoolFalseDep(int? param)
        {
            return LocalCanFetchBoolFalseDep1(param);
        }

        public async Task<Authorized> LocalCanFetchBoolFalseDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskDep(int? param)
        {
            return LocalCanFetchTaskDep1(param);
        }

        public async Task<Authorized> LocalCanFetchTaskDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBoolDep(int? param)
        {
            return LocalCanFetchTaskBoolDep1(param);
        }

        public async Task<Authorized> LocalCanFetchTaskBoolDep1(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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

        public virtual Task<Authorized> CanFetchTaskBoolFalseDep(int? param)
        {
            return LocalCanFetchTaskBoolFalseDep(param);
        }

        public async Task<Authorized> LocalCanFetchTaskBoolFalseDep(int? param)
        {
            Authorized authorized;
            authorized = await ReadAuthTask.CanReadBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanReadStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchBoolFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringTask();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = await ReadAuthTask.CanFetchStringFalseTask(param);
            if (!authorized.HasAccess)
            {
                return authorized;
            }

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
            services.AddTransient<ReadAuthTaskObject>();
            services.AddScoped<ReadAuthTaskObjectFactory>();
            services.AddScoped<IReadAuthTaskObjectFactory, ReadAuthTaskObjectFactory>();
        }
    }
}