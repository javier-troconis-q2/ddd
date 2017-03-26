﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

using EventStore.ClientAPI;

namespace infra
{
	public class PersistentSubscription : ISubscription
	{
		private readonly string _consumerGroupName;
		private readonly Func<IEventStoreConnection> _createConnection;
		private readonly string _streamName;
		private readonly Func<ResolvedEvent, Task> _handleEvent;
		private readonly TimeSpan _reconnectDelay;

		public PersistentSubscription(
			Func<IEventStoreConnection> createConnection,
			string streamName,
			string consumerGroupName,
			Func<ResolvedEvent, Task> handleEvent,
			TimeSpan reconnectDelay)
		{
			_createConnection = createConnection;
			_streamName = streamName;
			_consumerGroupName = consumerGroupName;
			_handleEvent = handleEvent;
			_reconnectDelay = reconnectDelay;
		}

		public async Task Start()
		{
			while (true)
			{
				var connection = _createConnection();
				try
				{
					await connection.ConnectAsync();
					await connection.ConnectToPersistentSubscriptionAsync(_streamName, _consumerGroupName, OnEventReceived, OnSubscriptionDropped(connection), autoAck: false);
					return;
				}
				catch
				{
					connection.Dispose();
				}
				await Task.Delay(_reconnectDelay);
			}
		}

		private async void OnEventReceived(EventStorePersistentSubscriptionBase subscription, ResolvedEvent resolvedEvent)
		{
			try
			{
				await _handleEvent(resolvedEvent);
			}
			catch (Exception ex)
			{
				subscription.Fail(resolvedEvent, PersistentSubscriptionNakEventAction.Unknown, ex.Message);
				return;
			}
			subscription.Acknowledge(resolvedEvent);
		}

		private Action<EventStorePersistentSubscriptionBase, SubscriptionDropReason, Exception> OnSubscriptionDropped(IDisposable connection)
		{
			return async (subscription, reason, exception) =>
			{
				connection.Dispose();
				await Start();
			};
		}
	}
}
