﻿using System;
using System.Collections.Concurrent;
using System.Reflection;
using FG.ServiceFabric.Services.Remoting.FabricTransport;
using Microsoft.ServiceFabric.Services.Remoting.Builder;

namespace FG.ServiceFabric.Services.Remoting.Runtime.Client
{
    public abstract class ServiceProxyFactoryBase
    {
        private readonly object _lock = new object();

        private static readonly ConcurrentDictionary<Type, MethodDispatcherBase> ServiceMethodDispatcherMap = new ConcurrentDictionary<Type, MethodDispatcherBase>();

        protected MethodDispatcherBase GetOrDiscoverServiceMethodDispatcher(Type serviceInterfaceType)
        {
            if (serviceInterfaceType == null) return null;

            if (ServiceMethodDispatcherMap.ContainsKey(serviceInterfaceType))
            {
                return ServiceMethodDispatcherMap[serviceInterfaceType];
            }

            lock (_lock)
            {
                if (ServiceMethodDispatcherMap.ContainsKey(serviceInterfaceType))
                {
                    return ServiceMethodDispatcherMap[serviceInterfaceType];
                }
                var serviceMethodDispatcher = GetServiceMethodInformation(serviceInterfaceType);
                ServiceMethodDispatcherMap[serviceInterfaceType] = serviceMethodDispatcher;
                return serviceMethodDispatcher;
            }
        }

        private MethodDispatcherBase GetServiceMethodInformation(Type serviceInterfaceType)
        {
            var codeBuilderType = typeof(Microsoft.ServiceFabric.Services.Remoting.Client.ServiceProxyFactory)?.Assembly.GetType(
                "Microsoft.ServiceFabric.Services.Remoting.Builder.ServiceCodeBuilder");

            var getOrCreateMethodDispatcher = codeBuilderType?.GetMethod("GetOrCreateMethodDispatcher", BindingFlags.Public | BindingFlags.Static);
            var methodDispatcherBase = getOrCreateMethodDispatcher?.Invoke(null, new object[] { serviceInterfaceType }) as MethodDispatcherBase;

            return methodDispatcherBase;
        }

        protected static void UpdateRequestContext(Uri serviceUri)
        {
            if (ServiceRequestContext.Current == null) return;

            ServiceRequestContext.Current[ServiceRequestContextKeys.RequestUri] = serviceUri?.ToString();
            if (ServiceRequestContext.Current[ServiceRequestContextKeys.CorrelationId] == null)
            {
                ServiceRequestContext.Current[ServiceRequestContextKeys.CorrelationId] = Guid.NewGuid().ToString();
            }
        }
    }
}