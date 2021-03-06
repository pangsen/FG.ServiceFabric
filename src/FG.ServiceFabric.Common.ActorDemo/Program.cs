﻿using System;
using System.Threading;
using Microsoft.ServiceFabric.Actors.Runtime;

namespace FG.ServiceFabric.Tests.Actor
{
    internal static class Program
    {
        /// <summary>
        /// This is the entry point of the service host process.
        /// </summary>
        private static void Main()
        {
            try
            {
                // This line registers an Actor Service to host your actor class with the Service Fabric runtime.
                // The contents of your ServiceManifest.xml and ApplicationManifest.xml files
                // are automatically populated when you build this project.
                // For more information, see https://aka.ms/servicefabricactorsplatform
                ActorRuntime.RegisterActorAsync<PersonEventStoredActor>(
                    (context, actorType) => new PersonEventStoredActorService(context, actorType, settings:
                        new ActorServiceSettings()
                        {
                            ActorGarbageCollectionSettings =
                                new ActorGarbageCollectionSettings(idleTimeoutInSeconds: 15, scanIntervalInSeconds: 15)
                        })).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<TempEventStoredActor>(
                    (context, actorType) => new TempEventStoredActorService(context, actorType, settings:
                        new ActorServiceSettings()
                        {
                            ActorGarbageCollectionSettings =
                                new ActorGarbageCollectionSettings(idleTimeoutInSeconds: 15, scanIntervalInSeconds: 15)
                        })).GetAwaiter().GetResult();

                ActorRuntime.RegisterActorAsync<ActorDemo>(
                    (context, actorType) => new ActorDemoActorService(context, actorType)).GetAwaiter().GetResult();

                Thread.Sleep(Timeout.Infinite);
            }
            catch (Exception e)
            {
                ActorDemoEventSource.Current.ActorHostInitializationFailed(e.ToString());
                throw;
            }
        }
    }
}
