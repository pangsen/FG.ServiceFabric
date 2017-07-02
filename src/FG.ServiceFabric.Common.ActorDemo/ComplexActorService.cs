﻿using System;
using System.Collections.Generic;
using System.Fabric;
using System.Threading;
using System.Threading.Tasks;
using FG.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Actors;
using Microsoft.ServiceFabric.Actors.Runtime;
using Microsoft.ServiceFabric.Services.Communication.Runtime;
using ActorService = Microsoft.ServiceFabric.Actors.Runtime.ActorService;

namespace FG.ServiceFabric.Tests.Actor
{
    public class ComplexActorService : Actors.Runtime.ActorService, IActorService
    {
        public new IRestorableActorStateProvider StateProvider =>  (IRestorableActorStateProvider) base.StateProvider;

        public ComplexActorService(
            StatefulServiceContext context, 
            ActorTypeInformation actorTypeInfo, 
            Func<ActorService, ActorId, Actors.Runtime.ActorBase> actorFactory = null, 
            Func<Microsoft.ServiceFabric.Actors.Runtime.ActorBase, IActorStateProvider, IActorStateManager> stateManagerFactory = null, 
            IActorStateProvider stateProvider = null, ActorServiceSettings settings = null) : 
            base(context, actorTypeInfo, actorFactory, stateManagerFactory, new ComplexFileStoreStateProvider(actorTypeInfo), settings)
        {
        }
        
        protected override IEnumerable<ServiceReplicaListener> CreateServiceReplicaListeners()
        {
            foreach (var serviceReplicaListener in base.CreateServiceReplicaListeners())
            {
                yield return serviceReplicaListener;
            }
        }
    }

    public class ComplexFileStoreStateProvider : TempFileSystemStateProvider
    {
        public ComplexFileStoreStateProvider(ActorTypeInformation actorTypeInfor, IActorStateProvider stateProvider = null) 
            : base(actorTypeInfor, stateProvider)
        {
        }

        public override async Task ActorActivatedAsync(ActorId actorId, CancellationToken cancellationToken = new CancellationToken())
        {
            if (await HasRestorableStateAsync(actorId, "complexType", cancellationToken))
            {
                await RestoreStateAsync(actorId, "complexType", cancellationToken);
            }

            await base.ActorActivatedAsync(actorId, cancellationToken);
        }
    }
}