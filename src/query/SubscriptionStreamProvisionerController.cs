﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;
using EventStore.ClientAPI.SystemData;
using shared;

namespace query
{
	public class SubscriptionStreamProvisionerController : IProvisionSubscriptionStreamRequests
	{
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public SubscriptionStreamProvisionerController(ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<IProvisionSubscriptionStreamRequested> message)
		{
			return _subscriptionStreamProvisioner
				.RegisterSubscriptionStream<Subscriber1>()
				.RegisterSubscriptionStream<Subscriber2>()
				.RegisterSubscriptionStream<Subscriber3>()
				.ProvisionSubscriptionStream(new UserCredentials(EventStoreSettings.Username, EventStoreSettings.Password), message.Data.SubscriptionStream);
		}
	}
}

