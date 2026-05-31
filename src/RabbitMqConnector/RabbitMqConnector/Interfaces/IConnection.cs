using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Channels;
using RabbitMQ.Client;

namespace RabbitMqConnector.Interfaces
{
    public interface IPersistentConnection : IDisposable
    {
        bool IsConnected { get; }
        Task<IChannel> CreateChannelAsync();
    }
}
