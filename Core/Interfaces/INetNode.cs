using System;
using System.Collections.Generic;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Metrics;
using MOUSE.Core.Interfaces.Serialization;

namespace MOUSE.Core.Interfaces
{
    /// <summary>
    /// Different peers could simultaneously receive events from network level, so implementation should be aware of this
    /// </summary>
    public interface INetNode : IDisposable
    {
        void Start();
        /// <summary>
        /// should be called only for manually updated Nodes
        /// </summary>
        void Update();

        Task<INetChannel> Connect(IPEndPoint endPoint);
        Task<INetChannel> Connect(IPEndPoint endPoint, CancellationToken cancellationToken);
        Task<INetChannel> Connect(string endpoint, CancellationToken cancellationToken);

        IObservable<INetChannel> ChannelOpenedEvent { get; }
        IObservable<INetChannel> ChannelClosedEvent { get; }

        string InstanceName { get; }
        IPEndPoint Address { get; }

        INetChannel GetChannel(uint channelId);
        IEnumerable<INetChannel> OpenedChannels { get; }

        IMessageSerializer MessageSerializer { get; }
    }
}