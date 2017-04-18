﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;

using ImpromptuInterface.InvokeExt;
using shared;

namespace eventstore
{
	public interface IPersistentSubscriptionProvisioner
	{
		IPersistentSubscriptionProvisioner IncludePersistentSubscription<TSubscription>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler;

		IPersistentSubscriptionProvisioner IncludePersistentSubscription<TSubscription, TSubscriptionGroup>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription;

		Task ProvisionPersistentSubscriptions(string subscriptionGroup = "*");
	}

	public class PersistentSubscriptionProvisioner : IPersistentSubscriptionProvisioner
	{
		private static readonly TaskQueue _provisioningTasksQueue = new TaskQueue();
		private readonly IPersistentSubscriptionManager _persistentSubscriptionManager;
		private readonly IDictionary<string, Func<Task>> _provisioningTask;

		public PersistentSubscriptionProvisioner(IPersistentSubscriptionManager persistentSubscriptionManager)
			:
				this(persistentSubscriptionManager, new Dictionary<string, Func<Task>>())
		{
		}

		private PersistentSubscriptionProvisioner(
			IPersistentSubscriptionManager persistentSubscriptionManager,
			IDictionary<string, Func<Task>> provisioningTask
			)
		{
			_persistentSubscriptionManager = persistentSubscriptionManager;
			_provisioningTask = provisioningTask;
		}

		public IPersistentSubscriptionProvisioner IncludePersistentSubscription<TSubscription>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler
		{
			return IncludePersistentSubscription<TSubscription, TSubscription>(configurePersistentSubscription);
		}

		public IPersistentSubscriptionProvisioner IncludePersistentSubscription<TSubscription, TSubscriptionGroup>(
			Func<PersistentSubscriptionSettingsBuilder, PersistentSubscriptionSettingsBuilder> configurePersistentSubscription = null) where TSubscription : IMessageHandler where TSubscriptionGroup : TSubscription
		{
			return new PersistentSubscriptionProvisioner(
				_persistentSubscriptionManager,
				new Dictionary<string, Func<Task>>(_provisioningTask)
				{
					{
						typeof(TSubscriptionGroup).GetEventStoreName(),
						() =>
						{
							configurePersistentSubscription = configurePersistentSubscription ?? (x => x);
							var streamName = typeof(TSubscription).GetEventStoreName();
							var groupName = typeof(TSubscriptionGroup).GetEventStoreName();
							var persistentSubscriptionSettings = configurePersistentSubscription(
								PersistentSubscriptionSettings
									.Create()
									.ResolveLinkTos()
									.StartFromBeginning()
									.MaximumCheckPointCountOf(1)
									.CheckPointAfter(TimeSpan.FromSeconds(1))
									.WithExtraStatistics());
							return _persistentSubscriptionManager.CreateOrUpdatePersistentSubscription(streamName, groupName, persistentSubscriptionSettings);
						}
					}
				});
		}

		public Task ProvisionPersistentSubscriptions(string subscriptionGroup = "*")
		{
			return Task.WhenAll(
				_provisioningTask
					.Where(x => x.Key.MatchesWildcard(subscriptionGroup))
					.Select(x => _provisioningTasksQueue.SendToChannelAsync(x.Key, x.Value))
					);
		}
	}
}
