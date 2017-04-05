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
				// create topics stream
                Console.WriteLine("1 - create topics stream");
				// create persistent subscription registration requested stream
				Console.WriteLine("2 - create persistent subscription registration requested stream");
				// create subscription stream registration requested stream
				Console.WriteLine("3 - create subscription stream registration requested stream");
				// request persistent subscription registration -> persistent subscription registration requested
				Console.WriteLine("4 - request persistent subscription registration");
				// request subscription stream registration -> subscription stream registration requested
				Console.WriteLine("5 - request subscription stream registration");
                switch (Console.ReadKey().KeyChar)
                {
                    case '1':
						topicsProjectionRegistry.CreateOrUpdateTopicsProjection();
                        break;
                    case '2':
						subscriptionProjectionRegistry.RegisterSubscriptionProjection<IPersistentSubscriptionRegistrationRequestedHandler>();
                        break;
					//case '3':
					//	subscriptionProjectionRegistry.RegisterSubscriptionProjection<IProjectionsRequestedHandler>();
					//	break;
					case '4':
                        eventPublisher.PublishEvent(new PersistentSubscriptionRegistrationRequested("*", "*"));
                        break;
					case '5':
						eventPublisher.PublishEvent(new SubscriptionStreamRegistrationRequested("*", "*"));
						break;
					default:
                        return;
                }
                Console.WriteLine();
            }
        }
    }
}


