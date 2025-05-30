﻿#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using Neatoo.RemoteFactory.FactoryGeneratorTests.Shared;

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Showcase
{
    public interface IShowcaseAuthObjFactory
    {
        IShowcaseAuthObj? Fetch();
        IShowcaseAuthObj? Create();
        IShowcaseAuthObj? Save(IShowcaseAuthObj target);
        Authorized<IShowcaseAuthObj> TrySave(IShowcaseAuthObj target);
        Authorized CanFetch();
        Authorized CanCreate();
        Authorized CanInsert();
        Authorized CanUpdate();
        Authorized CanDelete();
        Authorized CanSave();
    }

    internal class ShowcaseAuthObjFactory : FactorySaveBase<IShowcaseAuthObj>, IFactorySave<ShowcaseAuthObj>, IShowcaseAuthObjFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public ShowcaseAuthObjFactory(IServiceProvider serviceProvider, IFactoryCore<IShowcaseAuthObj> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public ShowcaseAuthObjFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<IShowcaseAuthObj> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public virtual IShowcaseAuthObj? Fetch()
        {
            return (LocalFetch()).Result;
        }

        public Authorized<IShowcaseAuthObj> LocalFetch()
        {
            Authorized authorized;
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            authorized = ishowcaseauthorize.CanRead();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            authorized = ishowcaseauthorize.CanFetch();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<IShowcaseAuthObj>(DoFactoryMethodCall(FactoryOperation.Fetch, () => new ShowcaseAuthObj(service)));
        }

        public virtual IShowcaseAuthObj? Create()
        {
            return (LocalCreate()).Result;
        }

        public Authorized<IShowcaseAuthObj> LocalCreate()
        {
            Authorized authorized;
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            authorized = ishowcaseauthorize.CanRead();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            authorized = ishowcaseauthorize.CanCreate();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<IShowcaseAuthObj>(DoFactoryMethodCall(FactoryOperation.Create, () => new ShowcaseAuthObj(service)));
        }

        public Authorized<IShowcaseAuthObj> LocalInsert(IShowcaseAuthObj target)
        {
            Authorized authorized;
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            var cTarget = (ShowcaseAuthObj)target ?? throw new Exception("IShowcaseAuthObj must implement ShowcaseAuthObj");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<IShowcaseAuthObj>(DoFactoryMethodCall(cTarget, FactoryOperation.Insert, () => cTarget.Insert(service)));
        }

        public Authorized<IShowcaseAuthObj> LocalUpdate(IShowcaseAuthObj target)
        {
            Authorized authorized;
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            var cTarget = (ShowcaseAuthObj)target ?? throw new Exception("IShowcaseAuthObj must implement ShowcaseAuthObj");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<IShowcaseAuthObj>(DoFactoryMethodCall(cTarget, FactoryOperation.Update, () => cTarget.Update(service)));
        }

        public Authorized<IShowcaseAuthObj> LocalDelete(IShowcaseAuthObj target)
        {
            Authorized authorized;
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            authorized = ishowcaseauthorize.CanDelete();
            if (!authorized.HasAccess)
            {
                return new Authorized<IShowcaseAuthObj>(authorized);
            }

            var cTarget = (ShowcaseAuthObj)target ?? throw new Exception("IShowcaseAuthObj must implement ShowcaseAuthObj");
            var service = ServiceProvider.GetRequiredService<IService>();
            return new Authorized<IShowcaseAuthObj>(DoFactoryMethodCall(cTarget, FactoryOperation.Delete, () => cTarget.Delete(service)));
        }

        public virtual IShowcaseAuthObj? Save(IShowcaseAuthObj target)
        {
            var authorized = (LocalSave(target));
            if (!authorized.HasAccess)
            {
                throw new NotAuthorizedException(authorized);
            }

            return authorized.Result;
        }

        public virtual Authorized<IShowcaseAuthObj> TrySave(IShowcaseAuthObj target)
        {
            return LocalSave(target);
        }

        async Task<IFactorySaveMeta?> IFactorySave<ShowcaseAuthObj>.Save(ShowcaseAuthObj target)
        {
            return await Task.FromResult((IFactorySaveMeta? )Save(target));
        }

        public virtual Authorized<IShowcaseAuthObj> LocalSave(IShowcaseAuthObj target)
        {
            if (target.IsDeleted)
            {
                if (target.IsNew)
                {
                    return new Authorized<IShowcaseAuthObj>();
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

        public virtual Authorized CanFetch()
        {
            return LocalCanFetch();
        }

        public Authorized LocalCanFetch()
        {
            Authorized authorized;
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ishowcaseauthorize.CanRead();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ishowcaseauthorize.CanFetch();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public virtual Authorized CanCreate()
        {
            return LocalCanCreate();
        }

        public Authorized LocalCanCreate()
        {
            Authorized authorized;
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ishowcaseauthorize.CanRead();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ishowcaseauthorize.CanCreate();
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
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
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
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
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
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ishowcaseauthorize.CanDelete();
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
            IShowcaseAuthorize ishowcaseauthorize = ServiceProvider.GetRequiredService<IShowcaseAuthorize>();
            authorized = ishowcaseauthorize.AnyAccess();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            authorized = ishowcaseauthorize.CanDelete();
            if (!authorized.HasAccess)
            {
                return authorized;
            }

            return new Authorized(true);
        }

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            services.AddScoped<ShowcaseAuthObjFactory>();
            services.AddScoped<IShowcaseAuthObjFactory, ShowcaseAuthObjFactory>();
            services.AddTransient<ShowcaseAuthObj>();
            services.AddTransient<IShowcaseAuthObj, ShowcaseAuthObj>();
            services.AddScoped<IFactorySave<ShowcaseAuthObj>, ShowcaseAuthObjFactory>();
        }
    }
}