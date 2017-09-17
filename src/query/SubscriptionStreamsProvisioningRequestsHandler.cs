﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using eventstore;

using shared;

namespace query
{
	public class SubscriptionStreamsProvisioningRequestsHandler : ISubscriptionStreamsProvisioningRequests
	{
		private readonly ISubscriptionStreamProvisioner _subscriptionStreamProvisioner;

		public SubscriptionStreamsProvisioningRequestsHandler(ISubscriptionStreamProvisioner subscriptionStreamProvisioner)
		{
			_subscriptionStreamProvisioner = subscriptionStreamProvisioner;
		}

		public Task Handle(IRecordedEvent<ISubscriptionStreamsProvisioningRequested> message)
		{
			return _subscriptionStreamProvisioner
				.RegisterSubscriptionStreamProvisioning<Subscriber1>()
				.RegisterSubscriptionStreamProvisioning<Subscriber2>()
				.RegisterSubscriptionStreamProvisioning<Subscriber3>()
				.ProvisionSubscriptionStream(message.Event.SubscriptionStream);
		}
	}
}

