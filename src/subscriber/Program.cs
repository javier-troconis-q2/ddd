﻿using infra;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.ClientAPI.Common.Log;
using EventStore.ClientAPI.Exceptions;
using EventStore.ClientAPI.Projections;
using EventStore.ClientAPI.SystemData;

namespace subscriber
{
	public class Program
	{
		private static readonly IEventStoreConnection Connection = EventStoreConnectionFactory.Create(x => x.KeepReconnecting());
		private static readonly ProjectionsManager ProjectionsManager = new ProjectionsManager(new ConsoleLogger(), Settings.HttpEndPoint, TimeSpan.FromMilliseconds(5000));
		private const string SubscriptionGroupName = "application1";
		private static readonly string[] AvailableEventTypes = {"applicationsubmitted","applicationstarted"};
		private static readonly Random Random = new Random();

		static Program()
		{
			Connection.Disconnected += (s, a) =>
			{
				Console.WriteLine("disconnected");
			};

			Connection.Closed += (s, a) =>
			{
				Console.WriteLine("closed");
			};

			Connection.Connected += (s, a) =>
			{
				Console.WriteLine("connected");
			};

			Connection.Reconnecting += (s, a) =>
			{
				Console.WriteLine("reconnecting");
			};

			Connection.ConnectAsync().Wait();
		}

		public static void Main(string[] args)
		{
			CreateSubscriberGroup();

			Subscribe();

			while (true)
			{
				//Console.ReadLine();
				//Task.Delay(2000)
				//	.ContinueWith(delegate
				//	{
				//		CreateSubscriberGroup();
				//	}).Wait();
			}
		}

		public static void CreateSubscriberGroup()
		{
			//var eventTypes = AvailableEventTypes.Take(Random.Next(1, AvailableEventTypes.Length + 1)).ToArray();
			var eventTypes = AvailableEventTypes;

			Console.WriteLine("listening to: " + string.Join(", ", eventTypes));

			const string onEventReceivedStatementTemplate = "'{0}': onEventReceived";
			const string projectionDefinitionTemplate =
				@"var onEventReceived = function(s, e) {{ " +
					"linkTo('{0}', e); " +
				"}};" +
				"fromAll().when({{ {1} }});";

			var onEventReceivedStatements = string.Join(",", eventTypes.Select(eventType => string.Format(onEventReceivedStatementTemplate, eventType)));

			//var projectionDestinationStream = $"{SubscriptionGroupName}-{Guid.NewGuid().ToString("N").ToLower()}";

			//Connection.AppendToStreamAsync("projectionsink", ExpectedVersion.Any, new EventData(Guid.NewGuid(), SubscriptionGroupName, false, 
			//	Encoding.UTF8.GetBytes($"{{'destinationStream' : '{projectionDestinationStream}'}}"), new byte[0]));

			//var projectionDefinition = string.Format(projectionDefinitionTemplate, projectionDestinationStream, onEventReceivedStatements);

			var projectionDefinition = string.Format(projectionDefinitionTemplate, SubscriptionGroupName, onEventReceivedStatements);

			CreateOrUpdateProjection(SubscriptionGroupName, projectionDefinition);

			var subcriptionGroupSettingsBuilder = PersistentSubscriptionSettings.Create()
					.ResolveLinkTos()
					.MinimumCheckPointCountOf(2)
					.StartFromCurrent();
			var subcriptionGroupSettings = subcriptionGroupSettingsBuilder.Build();

			try
			{
				Connection.CreatePersistentSubscriptionAsync(SubscriptionGroupName, SubscriptionGroupName, subcriptionGroupSettings, Settings.Credentials).Wait();
			}
			catch (AggregateException ex)
			{
				if (ex.InnerExceptions.Count > 1 || !(ex.InnerException is InvalidOperationException))
				{
					throw;
				}
			}
		}

		public static void Subscribe()
		{
			Connection.ConnectToPersistentSubscription(SubscriptionGroupName, SubscriptionGroupName, 
				(s, e) =>
					{
						Console.WriteLine($"stream: {e.Event.EventStreamId} | event: {e.OriginalEventNumber} - {e.Event.EventType} - {e.Event.EventId}");
						s.Acknowledge(e);
					},
				(s,r,e) => 
					{
						Subscribe();
						Console.WriteLine("subscription dropped");
					});
		}

		private static void CreateOrUpdateProjection(string projectionName, string projectionDefinition)
		{
			if (IsProjectionNew(projectionName))
			{
				CreateProjection(projectionName, projectionDefinition);
			}
			else if(HasProjectionChanged(projectionName, projectionDefinition))
			{
				UpdateProjection(projectionName, projectionDefinition);
			}
		}

		private static bool IsProjectionNew(string projectionName)
		{
			try
			{
				ProjectionsManager.GetQueryAsync(projectionName, Settings.Credentials).Wait();
				return false;
			}
			catch (AggregateException ex)
			{
				ProjectionCommandFailedException innerEx;
				if (ex.InnerExceptions.Count > 1 || 
					(innerEx = ex.InnerException as ProjectionCommandFailedException) == null || 
					innerEx.HttpStatusCode != (int)HttpStatusCode.NotFound)
				{
					throw;
				}
			}
			return true;
		}

		private static void CreateProjection(string projectionName, string projectionDefinition)
		{
			ProjectionsManager.CreateContinuousAsync(projectionName, projectionDefinition, Settings.Credentials).Wait();	
		}

		private static bool HasProjectionChanged(string projectionName, string projectionDefinition)
		{
			var response = ProjectionsManager.GetQueryAsync(projectionName, Settings.Credentials);
			response.Wait();
			return response.Result != projectionDefinition;
		}

		private static void UpdateProjection(string projectionName, string projectionDefinition)
		{
			ProjectionsManager.UpdateQueryAsync(projectionName, projectionDefinition, Settings.Credentials).Wait();
		}
	}
}
