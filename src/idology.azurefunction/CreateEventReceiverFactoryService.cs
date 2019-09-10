﻿using eventstore;
using EventStore.ClientAPI;
using System;
using System.Threading.Tasks.Dataflow;
using ILogger = Microsoft.Extensions.Logging.ILogger;

namespace idology.azurefunction
{
    public class CreateEventReceiverFactoryService
    {
        private readonly Uri _eventStoreConnectionUri;
        private readonly Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> _configureConnection;

        public CreateEventReceiverFactoryService(Uri eventStoreConnectionUri, Func<ConnectionSettingsBuilder, ConnectionSettingsBuilder> configureConnection)
        {
            _eventStoreConnectionUri = eventStoreConnectionUri;
            _configureConnection = configureConnection;
        }

        public CreateEventReceiver<ResolvedEvent> CreateEventReceiverFactory(string streamName, ILogger logger)
        {
            var bb = new BroadcastBlock<ResolvedEvent>(x => x);
            var eventBus = EventBus.CreateEventBus
            (
                () =>
                {
                    var connectionSettingsBuilder = _configureConnection(ConnectionSettings.Create());
                    var connectionSettings = connectionSettingsBuilder.Build();
                    var connection = EventStoreConnection.Create(connectionSettings, _eventStoreConnectionUri, connectionName: streamName);
                    return connection;
                },
                registry => registry.RegisterVolatileSubscriber(streamName, streamName, bb.SendAsync)
            );
            var startAllSubscribersTask = eventBus.StartAllSubscribers();
            return async eventFilter =>
            {
                await startAllSubscribersTask;
                var wob = new WriteOnceBlock<ResolvedEvent>(x => x);
                bb.LinkTo(wob, new DataflowLinkOptions { MaxMessages = 1 }, eventFilter);
                return wob;
            };
        }
    }
}
