﻿#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Conventions;
using Person.Ef;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Runtime.CompilerServices;

/*
Interface Found. TargetType: IPersonModel ConcreteType: PersonModel
Class: Person.DomainModel.PersonModel Name: PersonModel
Ignoring [Fetch] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for Fetch attribute RemoteAttribute
Ignoring [Upsert] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for Upsert attribute RemoteAttribute
Ignoring [Delete] method with attribute [Remote]. Not a Factory or Authorize attribute.
No Factory or Authorize attribute for Delete attribute RemoteAttribute
No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone

*/
namespace Person.DomainModel
{
    public interface IPersonModelFactory
    {
        IPersonModel? Create();
        Task<IPersonModel?> Fetch();
        Task<IPersonModel?> Save(IPersonModel target);
        Task<Authorized<IPersonModel>> TrySave(IPersonModel target);
        Authorized CanCreate();
        Authorized CanFetch();
        Authorized CanUpdate();
        Authorized CanDelete();
        Authorized CanSave();
    }

    internal class PersonModelFactory : FactorySaveBase<IPersonModel>, IFactorySave<PersonModel>, IPersonModelFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        public delegate Task<Authorized<IPersonModel>> FetchDelegate();
        public delegate Task<Authorized<IPersonModel>> SaveDelegate(IPersonModel target);
        // Delegate Properties to provide Local or Remote fork in execution
        public FetchDelegate FetchProperty { get; }
        public SaveDelegate SaveProperty { get; }

        public PersonModelFactory(IServiceProvider serviceProvider, IFactoryCore<IPersonModel> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            FetchProperty = LocalFetch;
            SaveProperty = LocalSave;
        }

        public PersonModelFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IPersonModel> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            FetchProperty = RemoteFetch;
            SaveProperty = RemoteSave;
        }

        public virtual IPersonModel? Create()
        {
            return (LocalCreate()).Result;
        }

        public Authorized<IPersonModel> LocalCreate()
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            authorized = ipersonmodelauth.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            return new Authorized<IPersonModel>(DoFactoryMethodCall(FactoryOperation.Create, () => new PersonModel()));
        }

        public virtual async Task<IPersonModel?> Fetch()
        {
            return (await FetchProperty()).Result;
        }

        public virtual async Task<Authorized<IPersonModel>> RemoteFetch()
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<Authorized<IPersonModel>>(typeof(FetchDelegate), []))!;
        }

        public async Task<Authorized<IPersonModel>> LocalFetch()
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            authorized = ipersonmodelauth.CanFetch();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<PersonModel>();
            var personContext = ServiceProvider.GetRequiredService<IPersonContext>();
            return new Authorized<IPersonModel>(await DoFactoryMethodCallBoolAsync(target, FactoryOperation.Fetch, () => target.Fetch(personContext)));
        }

        public async Task<Authorized<IPersonModel>> LocalUpsert(IPersonModel target)
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            authorized = ipersonmodelauth.CanUpdate();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            var cTarget = (PersonModel)target ?? throw new Exception("IPersonModel must implement PersonModel");
            var personContext = ServiceProvider.GetRequiredService<IPersonContext>();
            return new Authorized<IPersonModel>(await DoFactoryMethodCallAsync(cTarget, FactoryOperation.Update, () => cTarget.Upsert(personContext)));
        }

        public async Task<Authorized<IPersonModel>> LocalUpsert1(IPersonModel target)
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            var cTarget = (PersonModel)target ?? throw new Exception("IPersonModel must implement PersonModel");
            var personContext = ServiceProvider.GetRequiredService<IPersonContext>();
            return new Authorized<IPersonModel>(await DoFactoryMethodCallAsync(cTarget, FactoryOperation.Insert, () => cTarget.Upsert(personContext)));
        }

        public async Task<Authorized<IPersonModel>> LocalDelete(IPersonModel target)
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            authorized = ipersonmodelauth.CanDelete();
            if (!authorized.HasAccess)
            {
                return new Authorized<IPersonModel>(authorized);
            }

            var cTarget = (PersonModel)target ?? throw new Exception("IPersonModel must implement PersonModel");
            var personContext = ServiceProvider.GetRequiredService<IPersonContext>();
            return new Authorized<IPersonModel>(await DoFactoryMethodCallAsync(cTarget, FactoryOperation.Delete, () => cTarget.Delete(personContext)));
        }

        public virtual async Task<IPersonModel?> Save(IPersonModel target)
        {
            var authorized = (await SaveProperty(target));
            if (!authorized.HasAccess)
            {
                throw new NotAuthorizedException(authorized);
            }

            return authorized.Result;
        }

        public virtual async Task<Authorized<IPersonModel>> TrySave(IPersonModel target)
        {
            return await SaveProperty(target);
        }

        public virtual async Task<Authorized<IPersonModel>> RemoteSave(IPersonModel target)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<Authorized<IPersonModel>>(typeof(SaveDelegate), [target]))!;
        }

        async Task<IFactorySaveMeta?> IFactorySave<PersonModel>.Save(PersonModel target)
        {
            return (IFactorySaveMeta? )await Save(target);
        }

        public virtual async Task<Authorized<IPersonModel>> LocalSave(IPersonModel target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return new Authorized<IPersonModel>();
                }

                return await LocalDelete(target);
            }
            else if (target.IsNew)
            {
                return await LocalUpsert1(target);
            }
            else
            {
                return await LocalUpsert(target);
            }
        }

        public virtual Authorized CanCreate()
        {
            return LocalCanCreate();
        }

        public Authorized LocalCanCreate()
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ipersonmodelauth.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanFetch()
        {
            return LocalCanFetch();
        }

        public Authorized LocalCanFetch()
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ipersonmodelauth.CanFetch();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanUpdate()
        {
            return LocalCanUpdate();
        }

        public Authorized LocalCanUpdate()
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ipersonmodelauth.CanUpdate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanDelete()
        {
            return LocalCanDelete();
        }

        public Authorized LocalCanDelete()
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ipersonmodelauth.CanDelete();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanSave()
        {
            return LocalCanSave();
        }

        public Authorized LocalCanSave()
        {
            Authorized authorized;
            IPersonModelAuth ipersonmodelauth = ServiceProvider.GetRequiredService<IPersonModelAuth>();
            authorized = ipersonmodelauth.CanAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ipersonmodelauth.CanUpdate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ipersonmodelauth.CanDelete();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            services.AddScoped<PersonModelFactory>();
            services.AddScoped<IPersonModelFactory, PersonModelFactory>();
            services.AddScoped<FetchDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<PersonModelFactory>();
                return () => factory.LocalFetch();
            });
            services.AddScoped<SaveDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<PersonModelFactory>();
                return (IPersonModel target) => factory.LocalSave(target);
            });
            services.AddTransient<PersonModel>();
            services.AddTransient<IPersonModel, PersonModel>();
            services.AddScoped<IFactorySave<PersonModel>, PersonModelFactory>();
        }
    }
}