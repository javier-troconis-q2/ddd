﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using EventStore.ClientAPI.Common.Log;

using management.contracts;

namespace management
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var projectionManager = new ProjectionManager(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.ExternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password,
                new ConsoleLogger());

            var connectionFactory = new EventStoreConnectionFactory(
                EventStoreSettings.ClusterDns,
                EventStoreSettings.InternalHttpPort,
                EventStoreSettings.Username,
                EventStoreSettings.Password);

			ITopicsProjectionRegistry topicsProjectionRegistry = new ProjectionRegistry(projectionManager);
			ISubscriptionProjectionRegistry subscriptionProjectionRegistry = new ProjectionRegistry(projectionManager);

			var connection = connectionFactory.CreateConnection();
			connection.ConnectAsync().Wait();
            IEventPublisher eventPublisher = new EventPublisher(new eventstore.EventStore(connection));
            

            while (true)
            {
                Console.WriteLine("1 - create topics projection");
                Console.WriteLine("2 - create persistent subscriptions requested projection");
				Console.WriteLine("3 - create projections requested projection");
				Console.WriteLine("4 - publish persistent subscriptions requested");
				Console.WriteLine("5 - publish projections requested");
                switch (Console.ReadKey().KeyChar)
                {
                    case '1':
						topicsProjectionRegistry.CreateOrUpdateTopicsProjection();
                        break;
                    case '2':
						subscriptionProjectionRegistry.CreateOrUpdateSubscriptionProjection<IPersistentSubscriptionsRequestedHandler>();
                        break;
					case '3':
						subscriptionProjectionRegistry.CreateOrUpdateSubscriptionProjection<IProjectionsRequestedHandler>();
						break;
					case '4':
                        eventPublisher.PublishEvent(new PersistentSubscriptionsRequested("*", "*"));
                        break;
					case '5':
						eventPublisher.PublishEvent(new ProjectionsRequested("*", "*"));
						break;
					default:
                        return;
                }
                Console.WriteLine();
            }
        }
    }
}


