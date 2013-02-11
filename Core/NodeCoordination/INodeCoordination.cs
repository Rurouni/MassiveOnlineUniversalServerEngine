using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOUSE.Core.NodeCoordination
{
    public interface INodeCoordinator
    {
        void JoinWith(IServerNode node);
        void Stop();
        IObservable<ClusterView> OnClusterViewChanged { get; }
        void Broadcast(Message msg);
        Task<List<TReplyMessage>> QueryAll<TRequestMessage, TReplyMessage>(TRequestMessage request) where TRequestMessage : Message where TReplyMessage : Message;
        ulong GenerateNodeId();
    }
}
