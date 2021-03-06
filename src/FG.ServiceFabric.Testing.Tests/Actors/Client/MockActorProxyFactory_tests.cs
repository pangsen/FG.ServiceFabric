﻿using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FG.Common.Async;
using FG.ServiceFabric.Testing.Mocks;
using FG.ServiceFabric.Testing.Mocks.Actors.Runtime;
using FG.ServiceFabric.Tests.Actor;
using FG.ServiceFabric.Tests.Actor.Interfaces;
using FluentAssertions;
using Microsoft.ServiceFabric.Actors;
using NUnit.Framework;

namespace FG.ServiceFabric.Testing.Tests.Actors.Client
{
    // ReSharper disable once InconsistentNaming
    public class MockActorProxyFactory_tests
    {
        [Test]
        public async Task MockActorProxy_should_SaveState_after_Actor_method()
        {

            var fabricRuntime = new MockFabricRuntime("Overlord");
            var stateActions = new List<string>();
            var mockActorStateProvider = new MockActorStateProvider(fabricRuntime, stateActions);
            fabricRuntime.SetupActor((service, actorId) => new ActorDemo(service, actorId), createStateProvider: () => mockActorStateProvider);

            // Only to get around the kinda stupid introduced 1/20 msec 'bug'
            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                var actor = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return actor.SetCountAsync(5);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

            stateActions.Should().BeEquivalentTo(new[] { "ContainsStateAsync", "ActorActivatedAsync", "SaveStateAsync" });
        }
        
        [Test]
        public async Task MockActorProxy_should_should_be_able_to_create_proxy_for_Actor_with_specific_ActorService()
        {
            var fabricRuntime = new MockFabricRuntime("Overlord");
            
            fabricRuntime.SetupActor<ActorDemo, ActorDemoActorService>(
                (service, actorId) => new ActorDemo(service, actorId),
                (context, actorTypeInformation, stateProvider, stateManagerFactory) => new ActorDemoActorService(context, actorTypeInformation,
                stateProvider: stateProvider, stateManagerFactory: stateManagerFactory));

            IActorDemo proxy = null;
           
            // Only to get around the kinda stupid introduced 1/20 msec 'bug'
            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                proxy = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return Task.FromResult(true);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

            proxy.Should().NotBeNull();
        }

        [Test]
        public async Task MockActorProxy_should_should_be_able_to_create_proxy_for_specific_ActorService()
        {
            var fabricRuntime = new MockFabricRuntime("Overlord");
            fabricRuntime.SetupActor<ActorDemo, ActorDemoActorService>(
                (service, actorId) => new ActorDemo(service, actorId),
                (context, actorTypeInformation, stateProvider, stateManagerFactory) => new ActorDemoActorService(context, actorTypeInformation));

            IActorDemoActorService proxy = null;
            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                proxy = fabricRuntime.ActorProxyFactory.CreateActorServiceProxy<IActorDemoActorService>(
                  fabricRuntime.ApplicationUriBuilder.Build("ActorDemoActorService").ToUri(), new ActorId("testivus"));
                return Task.FromResult(true);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);

            proxy.Should().NotBeNull();
        }

        [Test]
        public async Task MockActorProxy_should_should_persist_state_across_multiple_proxies()
        {

            var fabricRuntime = new MockFabricRuntime("Overlord");
            var stateActions = new List<string>();
            var mockActorStateProvider = new MockActorStateProvider(fabricRuntime, stateActions);
            fabricRuntime.SetupActor(
                (service, actorId) => new ActorDemo(service, actorId),                
                createStateProvider: () => mockActorStateProvider
            );

            await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                var actor = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return actor.SetCountAsync(5);
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);


            var count = await ExecutionHelper.ExecuteWithRetriesAsync((ct) =>
            {
                var sameActor = fabricRuntime.ActorProxyFactory.CreateActorProxy<IActorDemo>(new ActorId("testivus"));
                return sameActor.GetCountAsync();
            }, 3, TimeSpan.FromMilliseconds(3), CancellationToken.None);
            
            count.Should().Be(5);
        }

    }
}
