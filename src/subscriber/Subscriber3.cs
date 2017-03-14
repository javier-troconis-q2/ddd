﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using contracts;

using shared;

namespace subscriber
{
    public class Subscriber3 : 
		IMessageHandler<IRecordedEvent<IApplicationStartedV3>, Task>, 
		IMessageHandler<IRecordedEvent<IApplicationStartedV2>, Task>, 
		IMessageHandler<IRecordedEvent<IApplicationStartedV1>, Task>,
		IMessageHandler<IRecordedEvent<IApplicationSubmittedV1>, Task>
    {
		public Task Handle(IRecordedEvent<IApplicationStartedV3> message)
		{
			Console.WriteLine(nameof(IApplicationStartedV3) + " - " + message.EventId + " - " + message.EventNumber);
			return Task.CompletedTask;
		}

	    public Task Handle(IRecordedEvent<IApplicationStartedV2> message)
	    {
			Console.WriteLine(nameof(IApplicationStartedV2) + " - " + message.EventId + " - " + message.EventNumber);
			return Task.CompletedTask;
		}

	    public Task Handle(IRecordedEvent<IApplicationStartedV1> message)
	    {
			Console.WriteLine(nameof(IApplicationStartedV1) + " - " + message.EventId + " - " + message.EventNumber);
			return Task.CompletedTask;
		}

	    public Task Handle(IRecordedEvent<IApplicationSubmittedV1> message)
	    {
		    var data = message.Event;
			Console.WriteLine(nameof(IApplicationSubmittedV1) + " - " + message.EventId + " - " + message.EventNumber + " - " + data.SubmittedBy);
			return Task.CompletedTask;
		}
    }
}
