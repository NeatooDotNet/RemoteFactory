﻿#nullable enable
using Neatoo.RemoteFactory;
using Neatoo.RemoteFactory.Internal;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

/*
							READONLY - DO NOT EDIT!!!!
							Generated by Neatoo.RemoteFactory
*/
namespace Neatoo.RemoteFactory.FactoryGeneratorTests.Mapper
{
    public interface IMapperObjFactory
    {
    }

    internal class MapperObjFactory : FactoryBase<MapperObj>, IMapperObjFactory
    {
        private readonly IServiceProvider ServiceProvider;
        private readonly IMakeRemoteDelegateRequest? MakeRemoteDelegateRequest;
        // Delegates
        // Delegate Properties to provide Local or Remote fork in execution
        public MapperObjFactory(IServiceProvider serviceProvider, IFactoryCore<MapperObj> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
        }

        public MapperObjFactory(IServiceProvider serviceProvider, IMakeRemoteDelegateRequest remoteMethodDelegate, IFactoryCore<MapperObj> factoryCore) : base(factoryCore)
        {
            this.ServiceProvider = serviceProvider;
            this.MakeRemoteDelegateRequest = remoteMethodDelegate;
        }

        public static void FactoryServiceRegistrar(IServiceCollection services, NeatooFactory remoteLocal)
        {
            services.AddScoped<MapperObjFactory>();
            services.AddScoped<IMapperObjFactory, MapperObjFactory>();
        }
    }
}