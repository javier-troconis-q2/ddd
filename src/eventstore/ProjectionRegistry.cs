﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using EventStore.ClientAPI;
using EventStore.ClientAPI.Exceptions;

namespace eventstore
{
	public interface ITopicsProjectionRegistry
	{
		Task RegisterTopicsProjection();
	}

	public interface ISubscriptionProjectionRegistry
	{
		Task RegisterSubscriptionProjection<TSubscription>();
	}

	public class ProjectionRegistry : ITopicsProjectionRegistry, ISubscriptionProjectionRegistry
	{
		private readonly IProjectionManager _projectionManager;

		public ProjectionRegistry(IProjectionManager projectionManager)
		{
			_projectionManager = projectionManager;
		}

		public Task RegisterTopicsProjection()
		{
			const string queryTemplate =
				@"function emitTopic(e) {{
    return function(topic) {{
           var message = {{ streamId: '{0}', eventName: topic, body: e.sequenceNumber + '@' + e.streamId, isJson: false }};
           eventProcessor.emit(message);
    }};
}}

fromAll()
    .when({{
        $any: function(s, e) {{
            var topics;
            if (e.streamId === '{0}' || !e.metadata || !(topics = e.metadata.topics)) {{
                return;
            }}
            topics.forEach(emitTopic(e));
        }}
    }});";
			var query = string.Format(queryTemplate, StreamName.Topics);
			return _projectionManager.CreateOrUpdateContinuousProjection(StreamName.Topics, query);
		}

		public Task RegisterSubscriptionProjection<TSubscription>()
		{
			const string queryTemplate =
				@"var topics = [{0}];

function handle(s, e) {{
    var event = e.bodyRaw;
    if(event !== s.lastEvent) {{ 
        var message = {{ streamId: '{1}', eventName: '$>', body: event, isJson: false }};
        eventProcessor.emit(message);
    }}
	s.lastEvent = event;
}}

var handlers = topics.reduce(
    function(x, y) {{
        x[y] = handle;
        return x;
    }}, 
	{{
		$init: function(){{
			return {{ lastEvent: ''}};
		}}
	}});

fromStream('{2}')
    .when(handlers);";

			var subscriptionType = typeof(TSubscription);
			var subscriptionName = subscriptionType.GetEventStoreName();
			var handlingTypes = subscriptionType.GetMessageHandlerTypes().Select(x => x.GetGenericArguments()[0].GetGenericArguments()[0]);
			var topics = handlingTypes.Select(handlingType => handlingType.GetEventStoreName());
			var query = string.Format(queryTemplate, string.Join(",\n", topics.Select(topic => $"'{topic}'")), subscriptionName, StreamName.Topics);
			return _projectionManager.CreateOrUpdateContinuousProjection(subscriptionName, query);
		}
	}
}