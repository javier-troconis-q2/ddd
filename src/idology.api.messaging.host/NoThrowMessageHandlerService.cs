﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using eventstore;
using EventStore.ClientAPI;
using shared;

namespace idology.api.messaging.host
{
    public class NoThrowMessageHandlerService<T1> : IMessageHandler<T1, Task<IEnumerable<Message<byte[]>>>>
    {
        private readonly string _messageHandlerName;
        private readonly IMessageHandler<T1, Task<IEnumerable<Message<byte[]>>>> _messageHandler;

        public NoThrowMessageHandlerService(string messageHandlerName, IMessageHandler<T1, Task<IEnumerable<Message<byte[]>>>> messageHandler)
        {
            _messageHandlerName = messageHandlerName;
            _messageHandler = messageHandler;
        }

        public async Task<IEnumerable<Message<byte[]>>> Handle(T1 message)
        {
            try
            {
                return await _messageHandler.Handle(message);
            }
            catch (Exception ex)
            {
                var data = new
                {
                    OperationName = _messageHandlerName,
                    Reason = ex.Message
                };
                return new[]
                {
                    new Message<byte[]>("operationfailed", data.ToJsonBytes())
                };
            }
        }
    }
}
