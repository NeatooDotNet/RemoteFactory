#nullable enable
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using static Neatoo.RemoteFactory.FactoryGeneratorTests.NullableParameterTests;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

/*
                    Debugging Messages:
                    Parent class: NullableParameterTests
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone
No AuthorizeAttribute

                    */
namespace Neatoo.RemoteFactory.FactoryGeneratorTests
{
    public interface INullableParameterFactory
    {
        NullableParameter Create(int? p);
        Task<NullableParameter> CreateRemote(int? p);
    }

    internal class NullableParameterFactory : FactoryBase, INullableParameterFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        public delegate Task<NullableParameter> CreateRemoteDelegate(int? p);
        // Delegate Properties to provide Local or Remote fork in execution
        public CreateRemoteDelegate CreateRemoteProperty { get; }

        public NullableParameterFactory(IServiceProvider serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
            CreateRemoteProperty = LocalCreateRemote;
        }

        public NullableParameterFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            CreateRemoteProperty = RemoteCreateRemote;
        }

        public virtual NullableParameter Create(int? p)
        {
            return LocalCreate(p);
        }

        public NullableParameter LocalCreate(int? p)
        {
            var target = ServiceProvider.GetRequiredService<NullableParameter>();
            return DoFactoryMethodCall<NullableParameter>(target, FactoryOperation.Create, () => target.Create(p));
        }

        public virtual Task<NullableParameter> CreateRemote(int? p)
        {
            return CreateRemoteProperty(p);
        }

        public virtual async Task<NullableParameter> RemoteCreateRemote(int? p)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<NullableParameter>(typeof(CreateRemoteDelegate), [p]))!;
        }

        public Task<NullableParameter> LocalCreateRemote(int? p)
        {
            var target = ServiceProvider.GetRequiredService<NullableParameter>();
            return Task.FromResult(DoFactoryMethodCall<NullableParameter>(target, FactoryOperation.Create, () => target.CreateRemote(p)));
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<NullableParameter>();
            services.AddScoped<NullableParameterFactory>();
            services.AddScoped<INullableParameterFactory, NullableParameterFactory>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<NullableParameterFactory>();
                return (int? p) => factory.LocalCreateRemote(p);
            });
        }
    }
}