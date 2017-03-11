﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using appservices;
using core;
using EventStore.ClientAPI;
using EventStore.ClientAPI.SystemData;
using infra;

using Newtonsoft.Json;

using shared;

namespace host
{
    class TimeFramedTaskHandler<TIn, TOut> : IMessageHandler<Task<TIn>, Task<TOut>> where TIn : TOut
    {
        private readonly TimeSpan _timeout;

        public TimeFramedTaskHandler(TimeSpan timeout)
        {
            _timeout = timeout;
        }

        public async Task<TOut> Handle(Task<TIn> message)
        {
            var cancellationTokenSource = new CancellationTokenSource(_timeout);
            return await message.WithCancellation(cancellationTokenSource.Token);
        }
    }

    class TaskCompletedLoggerHandler<TIn, TOut> : IMessageHandler<Task<TIn>, Task<TOut>> where TIn : TOut
    {
        private readonly Action<string> _logger;
        private readonly Func<TOut, string> _whatToLog;

        public TaskCompletedLoggerHandler(Action<string> logger, Func<TOut, string> whatToLog)
        {
            _logger = logger;
            _whatToLog = whatToLog;
        }

        public async Task<TOut> Handle(Task<TIn> message)
        {
            var @out = await message;
            _logger(_whatToLog(@out));
            return @out;
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
			var connection = new EventStoreConnectionFactory(x => x
                .KeepReconnecting().SetDefaultUserCredentials(EventStoreSettings.Credentials)).CreateConnection();
            connection.ConnectAsync().Wait();
            var eventStore = new infra.EventStore(connection);

            var startApplicationHandler = new StartApplicationCommandHandler(eventStore)
                .ComposeForward(new TimeFramedTaskHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(TimeSpan.FromSeconds(2)))
                .ComposeForward(new TaskCompletedLoggerHandler<Message<StartApplicationCommand>, Message<StartApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: started"));

            var submitApplicationHandler = new SubmitApplicationCommandHandler(eventStore)
                .ComposeForward(new TimeFramedTaskHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(TimeSpan.FromSeconds(2)))
                .ComposeForward(new TaskCompletedLoggerHandler<Message<SubmitApplicationCommand>, Message<SubmitApplicationCommand>>(Console.WriteLine, message => $"application {message.Body.ApplicationId}: submitted"));

            while (true)
            {
                var applicationId = Guid.NewGuid();
               
                startApplicationHandler.Handle(new Message<StartApplicationCommand> { Body = new StartApplicationCommand { ApplicationId = applicationId } }).Wait();
                //submitApplicationHandler.Handle(new Message<SubmitApplicationCommand> { Body = new SubmitApplicationCommand { ApplicationId = applicationId, Version = 0, Submitter = "blah" } }).Wait();
                
                Task.Delay(TimeSpan.FromSeconds(5)).Wait();
            }
        }

    
      
        
    }
}
