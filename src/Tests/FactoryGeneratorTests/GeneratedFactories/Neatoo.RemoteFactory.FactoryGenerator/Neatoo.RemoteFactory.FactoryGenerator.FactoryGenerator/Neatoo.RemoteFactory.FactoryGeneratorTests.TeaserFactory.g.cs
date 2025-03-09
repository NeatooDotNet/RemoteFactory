#nullable enable
using Neatoo.RemoteFactory.Internal;
using Neatoo.RemoteFactory;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
                    Debugging Messages:
                    No MethodDeclarationSyntax for GetType
No MethodDeclarationSyntax for MemberwiseClone

                    */
namespace Neatoo.RemoteFactory.FactoryGeneratorTests
{
    public interface ITeaserFactory
    {
        ITeaser Create();
        ITeaser? Fetch();
        Task<ITeaser> CreateTask();
        Task<ITeaser> CreateRemote();
        Task<ITeaser> CreateDependency();
        ITeaser? Save(ITeaser target);
        Authorized<ITeaser> TrySave(ITeaser target);
        Task<ITeaser?> Save(ITeaser target, int p);
        Task<Authorized<ITeaser>> TrySave(ITeaser target, int p);
        Task<ITeaser?> SaveRemote(ITeaser target, int? p);
        Task<Authorized<ITeaser>> TrySaveRemote(ITeaser target, int? p);
        Authorized CanCreate();
        Authorized CanFetch();
        Authorized CanCreateTask();
        Authorized CanCreateRemote();
        Authorized CanCreateDependency();
        Authorized CanInsert();
        Authorized CanInsert(int p);
        Authorized CanInsertRemote(int? p);
        Authorized CanSave();
        Authorized CanSave(int p);
        Authorized CanSaveRemote(int? p);
    }

    internal class TeaserFactory : FactorySaveBase<Teaser>, IFactorySave<Teaser>, ITeaserFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        public delegate Task<Authorized<ITeaser>> CreateRemoteDelegate();
        public delegate Task<Authorized<ITeaser>> SaveRemoteDelegate(ITeaser target, int? p);
        // Delegate Properties to provide Local or Remote fork in execution
        public IAuthorizeTeaser IAuthorizeTeaser { get; }
        public CreateRemoteDelegate CreateRemoteProperty { get; }
        public SaveRemoteDelegate SaveRemoteProperty { get; }

        public TeaserFactory(IServiceProvider serviceProvider, IAuthorizeTeaser iauthorizeteaser)
        {
            this.ServiceProvider = serviceProvider;
            this.IAuthorizeTeaser = iauthorizeteaser;
            CreateRemoteProperty = LocalCreateRemote;
            SaveRemoteProperty = LocalSaveRemote;
        }

        public TeaserFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IAuthorizeTeaser iauthorizeteaser)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
            this.IAuthorizeTeaser = iauthorizeteaser;
            this.IAuthorizeTeaser = iauthorizeteaser;
            CreateRemoteProperty = RemoteCreateRemote;
            SaveRemoteProperty = RemoteSaveRemote;
        }

        public virtual ITeaser Create()
        {
            return (LocalCreate()).Result!;
        }

        public Authorized<ITeaser> LocalCreate()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<Teaser>();
            return new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser>(target, FactoryOperation.Create, () => target.Create()));
        }

        public virtual ITeaser? Fetch()
        {
            return (LocalFetch()).Result!;
        }

        public Authorized<ITeaser> LocalFetch()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<Teaser>();
            return new Authorized<ITeaser>(DoFactoryMethodCallBool<ITeaser>(target, FactoryOperation.Fetch, () => target.Fetch()));
        }

        public virtual async Task<ITeaser> CreateTask()
        {
            return (await LocalCreateTask()).Result!;
        }

        public async Task<Authorized<ITeaser>> LocalCreateTask()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<Teaser>();
            return new Authorized<ITeaser>(await DoFactoryMethodCallAsync<ITeaser>(target, FactoryOperation.Create, () => target.CreateTask()));
        }

        public virtual async Task<ITeaser> CreateRemote()
        {
            return (await CreateRemoteProperty()).Result!;
        }

        public virtual async Task<Authorized<ITeaser>> RemoteCreateRemote()
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<Authorized<ITeaser>>(typeof(CreateRemoteDelegate), []))!;
        }

        public Task<Authorized<ITeaser>> LocalCreateRemote()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ITeaser>(authorized));
            }

            var target = ServiceProvider.GetRequiredService<Teaser>();
            return Task.FromResult(new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser>(target, FactoryOperation.Create, () => target.CreateRemote())));
        }

        public virtual async Task<ITeaser> CreateDependency()
        {
            return (await LocalCreateDependency()).Result!;
        }

        public async Task<Authorized<ITeaser>> LocalCreateDependency()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var target = ServiceProvider.GetRequiredService<Teaser>();
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ITeaser>(await DoFactoryMethodCallAsync<ITeaser>(target, FactoryOperation.Create, () => target.CreateDependency(service)));
        }

        public Authorized<ITeaser> LocalInsert(ITeaser target)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser?>(cTarget, FactoryOperation.Insert, () => cTarget.Insert(service)));
        }

        public Authorized<ITeaser> LocalUpdate(ITeaser target)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser?>(cTarget, FactoryOperation.Update, () => cTarget.Update(service)));
        }

        public Authorized<ITeaser> LocalDelete(ITeaser target)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser?>(cTarget, FactoryOperation.Delete, () => cTarget.Delete(service)));
        }

        public Authorized<ITeaser> LocalInsert1(ITeaser target, int p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser?>(cTarget, FactoryOperation.Insert, () => cTarget.Insert(p, service)));
        }

        public async Task<Authorized<ITeaser>> LocalUpdate1(ITeaser target, int p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ITeaser>(await DoFactoryMethodCallAsync<ITeaser?>(cTarget, FactoryOperation.Update, () => cTarget.Update(p, service)));
        }

        public async Task<Authorized<ITeaser>> LocalDelete1(ITeaser target, int p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<ITeaser>(authorized);
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<ITeaser>(await DoFactoryMethodCallAsync<ITeaser?>(cTarget, FactoryOperation.Delete, () => cTarget.Delete(p, service)));
        }

        public Task<Authorized<ITeaser>> LocalInsertRemote(ITeaser target, int? p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ITeaser>(authorized));
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ITeaser>(authorized));
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser?>(cTarget, FactoryOperation.Insert, () => cTarget.InsertRemote(p, service))));
        }

        public Task<Authorized<ITeaser>> LocalUpdateRemote(ITeaser target, int? p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ITeaser>(authorized));
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser?>(cTarget, FactoryOperation.Update, () => cTarget.UpdateRemote(p, service))));
        }

        public Task<Authorized<ITeaser>> LocalDeleteRemote(ITeaser target, int? p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return Task.FromResult(new Authorized<ITeaser>(authorized));
            }

            var cTarget = (Teaser)target ?? throw new Exception("ITeaser must implement Teaser");
            var service = ServiceProvider.GetRequiredService<IService>();
            return Task.FromResult(new Authorized<ITeaser>(DoFactoryMethodCall<ITeaser?>(cTarget, FactoryOperation.Delete, () => cTarget.DeleteRemote(p, service))));
        }

        public virtual ITeaser? Save(ITeaser target)
        {
            var authorized = (LocalSave(target));
            if (!authorized.HasAccess)
            {
                throw new NotAuthorizedException(authorized);
            }

            return authorized.Result;
        }

        public virtual Authorized<ITeaser> TrySave(ITeaser target)
        {
            return LocalSave(target);
        }

        async Task<IFactorySaveMeta?> IFactorySave<Teaser>.Save(Teaser target)
        {
            return await Task.FromResult((IFactorySaveMeta? )Save(target));
        }

        public virtual Authorized<ITeaser> LocalSave(ITeaser target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return new Authorized<ITeaser>();
                }

                return LocalDelete(target);
            }
            else if (target.IsNew)
            {
                return LocalInsert(target);
            }
            else
            {
                return LocalUpdate(target);
            }
        }

        public virtual async Task<ITeaser?> Save(ITeaser target, int p)
        {
            var authorized = (await LocalSave1(target, p));
            if (!authorized.HasAccess)
            {
                throw new NotAuthorizedException(authorized);
            }

            return authorized.Result;
        }

        public virtual async Task<Authorized<ITeaser>> TrySave(ITeaser target, int p)
        {
            return await LocalSave1(target, p);
        }

        public virtual async Task<Authorized<ITeaser>> LocalSave1(ITeaser target, int p)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return new Authorized<ITeaser>();
                }

                return await LocalDelete1(target, p);
            }
            else if (target.IsNew)
            {
                return LocalInsert1(target, p);
            }
            else
            {
                return await LocalUpdate1(target, p);
            }
        }

        public virtual async Task<ITeaser?> SaveRemote(ITeaser target, int? p)
        {
            var authorized = (await SaveRemoteProperty(target, p));
            if (!authorized.HasAccess)
            {
                throw new NotAuthorizedException(authorized);
            }

            return authorized.Result;
        }

        public virtual Task<Authorized<ITeaser>> TrySaveRemote(ITeaser target, int? p)
        {
            return SaveRemoteProperty(target, p);
        }

        public virtual async Task<Authorized<ITeaser>> RemoteSaveRemote(ITeaser target, int? p)
        {
            return (await MakeRemoteDelegateRequest!.ForDelegate<Authorized<ITeaser>>(typeof(SaveRemoteDelegate), [target, p]))!;
        }

        public virtual Task<Authorized<ITeaser>> LocalSaveRemote(ITeaser target, int? p)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return Task.FromResult(new Authorized<ITeaser>());
                }

                return LocalDeleteRemote(target, p);
            }
            else if (target.IsNew)
            {
                return LocalInsertRemote(target, p);
            }
            else
            {
                return LocalUpdateRemote(target, p);
            }
        }

        public virtual Authorized CanCreate()
        {
            return LocalCanCreate();
        }

        public Authorized LocalCanCreate()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
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
            authorized = IAuthorizeTeaser.CanCreate();
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
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateRemote()
        {
            return LocalCanCreateRemote();
        }

        public Authorized LocalCanCreateRemote()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreateDependency()
        {
            return LocalCanCreateDependency();
        }

        public Authorized LocalCanCreateDependency()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanInsert()
        {
            return LocalCanInsert();
        }

        public Authorized LocalCanInsert()
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanInsert(int p)
        {
            return LocalCanInsert1(p);
        }

        public Authorized LocalCanInsert1(int p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanInsertRemote(int? p)
        {
            return LocalCanInsertRemote(p);
        }

        public Authorized LocalCanInsertRemote(int? p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = IAuthorizeTeaser.CanInsert();
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
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanSave(int p)
        {
            return LocalCanSave1(p);
        }

        public Authorized LocalCanSave1(int p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanSaveRemote(int? p)
        {
            return LocalCanSaveRemote(p);
        }

        public Authorized LocalCanSaveRemote(int? p)
        {
            Authorized authorized;
            authorized = IAuthorizeTeaser.CanCreate();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = IAuthorizeTeaser.CanInsert();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public static void FactoryServiceRegistrar(IServiceCollection services)
        {
            services.AddTransient<Teaser>();
            services.AddScoped<TeaserFactory>();
            services.AddScoped<ITeaserFactory, TeaserFactory>();
            services.AddTransient<ITeaser, Teaser>();
            services.AddScoped<CreateRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<TeaserFactory>();
                return () => factory.LocalCreateRemote();
            });
            services.AddScoped<SaveRemoteDelegate>(cc =>
            {
                var factory = cc.GetRequiredService<TeaserFactory>();
                return (ITeaser target, int? p) => factory.LocalSaveRemote(target, p);
            });
            services.AddScoped<IFactorySave<Teaser>, TeaserFactory>();
        }
    }
}