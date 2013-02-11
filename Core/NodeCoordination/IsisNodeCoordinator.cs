using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Isis;
using NLog;
using Timeout = Isis.Timeout;

namespace MOUSE.Core.NodeCoordination
{
    /// <summary>
    /// Synchronizes via ServerNode Fiber
    /// </summary>
    public class IsisNodeCoordinator : INodeCoordinator
    {
        private const int CustomBroadcast = 1;
        private const int CustomQuery = 2;


        private Timeout _myTO = new Timeout(1000, Timeout.TO_ABORTREPLY);
        private EOLMarker _myEOL = new EOLMarker();
        private Group _nodesGroup;
        private View _lastIsisView;

        private readonly Logger Log = LogManager.GetCurrentClassLogger();
        
        private IServerNode _node;
        private readonly Dictionary<Address, NodeRemoteInfo> _nodesByIsisAddress = new Dictionary<Address, NodeRemoteInfo>();

        private readonly Subject<ClusterView> _onClusterViewChanged = new Subject<ClusterView>();
        private readonly Dictionary<uint, Action<Message>> _broadcastsHandlersByMessageId = new Dictionary<uint, Action<Message>>();
        private readonly Dictionary<uint, Func<Message, Message>> _queryHandlersByMessageId = new Dictionary<uint, Func<Message, Message>>();
        
        public IsisNodeCoordinator()
        {
            IsisSystem.Start();

            _queryHandlersByMessageId.Add((uint)MessageId.ConnectionRequest, OnConnectRequest);

            _broadcastsHandlersByMessageId.Add((uint)MessageId.Empty, message => Log.Info("Received empty message"));
        }

        public void JoinWith(IServerNode node)
        {
            _node = node;

            //node.InternalNet.PeerConnectedEvent.Subscribe(OnNodeConnected);

            _nodesGroup = new Group("MOUSENodes");
            _nodesGroup.Trace(true);
            _nodesGroup.RegisterHandler(CustomBroadcast, (Action<byte[]>)OnBroadcastMessage);
            _nodesGroup.RegisterHandler(CustomQuery, (Action<byte[]>)OnCustomQuery);
            _nodesGroup.RegisterViewHandler((ViewHandler)ClusterViewChanged);
            _nodesGroup.Join();

        }

        public void Broadcast(Message msg)
        {
            _nodesGroup.Send(CustomBroadcast, msg.GetSerialized());
        }

        private List<TReplyMessage> queryAll<TRequestMessage, TReplyMessage>(TRequestMessage request)
            where TRequestMessage : Message
            where TReplyMessage : Message
        {
            var replies = new List<byte[]>();
            int numReplies = _nodesGroup.Query(Group.ALL, _myTO, CustomQuery, request.GetSerialized(), _myEOL, replies);

            var messages = new List<TReplyMessage>(replies.Count);

            foreach (byte[] bytes in replies)
            {
                var reader = new BinaryReader(new MemoryStream(bytes));
                messages.Add((TReplyMessage)_node.MessageFactory.Deserialize(reader));
            }

            return messages;
        }

        private List<TReplyMessage> safeQueryAll<TRequestMessage, TReplyMessage>(TRequestMessage request)
            where TRequestMessage : Message
            where TReplyMessage : Message
        {
            var replies = new List<byte[]>();
            int numReplies = _nodesGroup.SafeQuery(Group.ALL, _myTO, CustomQuery, request.GetSerialized(), _myEOL, replies);

            var messages = new List<TReplyMessage>(replies.Count);

            foreach (byte[] bytes in replies)
            {
                var reader = new BinaryReader(new MemoryStream(bytes));
                messages.Add((TReplyMessage)_node.MessageFactory.Deserialize(reader));
            }

            return messages;
        }

        private List<TReplyMessage> orderedQueryAll<TRequestMessage, TReplyMessage>(TRequestMessage request)
            where TRequestMessage : Message
            where TReplyMessage : Message
        {
            var replies = new List<byte[]>();
            int numReplies = _nodesGroup.OrderedQuery(Group.ALL, _myTO, CustomQuery, request.GetSerialized(), _myEOL, replies);

            var messages = new List<TReplyMessage>(replies.Count);

            foreach (byte[] bytes in replies)
            {
                var reader = new BinaryReader(new MemoryStream(bytes));
                messages.Add((TReplyMessage)_node.MessageFactory.Deserialize(reader));
            }

            return messages;
        }

        public Task<List<TReplyMessage>> QueryAll<TRequestMessage, TReplyMessage>(TRequestMessage request)
            where TRequestMessage : Message
            where TReplyMessage : Message
        {
            return _node.Fiber.Call(() => Task.FromResult(queryAll<TRequestMessage, TReplyMessage>(request)), LockType.Write);
        }

        public ulong GenerateNodeId()
        {
            return (ulong)IsisSystem.GetMyAddress().GetHashCode();
        }

        private void OnBroadcastMessage(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
            Message message = _node.MessageFactory.Deserialize(reader);
            Action<Message> handler;
            if (_broadcastsHandlersByMessageId.TryGetValue(message.Id, out handler))
            {
                handler(message);
            }
            else
            {
                Log.Warn("No handler was found for {0}", message);
            }
        }

        private void OnCustomQuery(byte[] data)
        {
            var reader = new BinaryReader(new MemoryStream(data));
            Message message = _node.MessageFactory.Deserialize(reader);
            Func<Message, Message> handler;
            if (_queryHandlersByMessageId.TryGetValue(message.Id, out handler))
            {
                Message replyMsg = handler(message);
                if(replyMsg != null)
                    _nodesGroup.Reply(replyMsg.GetSerialized());
                else
                    _nodesGroup.NullReply();
            }
            else
            {
                Log.Warn("No handler was found for {0}", message);
                _nodesGroup.NullReply();
            }

        }

        private void ClusterViewChanged(View view)
        {
            _node.Fiber.Process(() =>
            {
                Log.Info("ViewId:" + view.viewid);

                if (_lastIsisView == null)
                {
                    foreach (var address in view.members)
                    {
                        Log.Info("{0} is in cluster", address);
                    }
                    _node.Fiber.Process(ConnectToAll, LockType.Write);
                }
                else
                {
                    foreach (var address in view.joiners)
                    {
                        Log.Info("{0} has joined cluster", address);
                    }

                    var removedNodes = ImmutableList<NodeRemoteInfo>.Empty.ToBuilder();
                    foreach (var address in view.leavers)
                    {
                        Log.Info("{0} has left cluster", address);
                        NodeRemoteInfo nodeInfo;
                        if (_nodesByIsisAddress.TryGetValue(address, out nodeInfo))
                        {
                            removedNodes.Add(nodeInfo);
                            _nodesByIsisAddress.Remove(address);
                        }
                    }

                    if (removedNodes.Count > 0)
                    {
                        //notify on server fiber thread for good
                        _node.Fiber.Process(() =>
                            {
                                _onClusterViewChanged.OnNext(
                                    new ClusterView(_lastIsisView.viewid,
                                                    (ulong)_lastIsisView.members.First().GetHashCode(), //Leader
                                                    _nodesByIsisAddress.Values.ToImmutableList(),
                                                    ImmutableList<NodeRemoteInfo>.Empty,
                                                    removedNodes.ToImmutable()));
                            }, LockType.Read);
                    }
                }
                _lastIsisView = view;
            }, LockType.Write);
        }

        private void ConnectToAll()
        {
            List<ConnectReply> replies = orderedQueryAll<ConnectRequest, ConnectReply>(new ConnectRequest { Info = _node.Info });

            var nodes = new List<NodeRemoteInfo>();
            foreach (var reply in replies)
            {
                var isisAddr = _lastIsisView.members.FirstOrDefault(x => (ulong)x.GetHashCode() == reply.Info.NodeId);
                if (isisAddr != null)
                {
                    nodes.Add(reply.Info);
                    _nodesByIsisAddress.Add(isisAddr, reply.Info);
                }

            }
            //notify on server fiber thread for good
            _node.Fiber.Process(() =>
                {
                    _onClusterViewChanged.OnNext(
                        new ClusterView(_lastIsisView.viewid,
                                        (ulong)_lastIsisView.members.First().GetHashCode(),//leader
                                        _nodesByIsisAddress.Values.ToImmutableList(),
                                        nodes.ToImmutableList(),
                                        ImmutableList<NodeRemoteInfo>.Empty));
                }, LockType.Read);
        }

        private Message OnConnectRequest(Message msg)
        {
            Contract.Requires(msg is ConnectRequest);
            var connectMsg = msg as ConnectRequest;

            Log.Info("Received OnConnectRequest with {0}", connectMsg.Info);

            var isisAddr = _lastIsisView.GetLiveMembers().FirstOrDefault(x => (ulong) x.GetHashCode() == connectMsg.Info.NodeId);

            if (isisAddr != null)
            {
                if (isisAddr.Equals(IsisSystem.GetMyAddress()))
                {
                    return new ConnectReply { Info = _node.Info };
                }
                else
                {

                    _nodesByIsisAddress.Add(isisAddr, connectMsg.Info);
                    _onClusterViewChanged.OnNext(new ClusterView(_lastIsisView.viewid,
                        (ulong)_lastIsisView.members.First().GetHashCode(),
                        _nodesByIsisAddress.Values.ToImmutableList(),
                        ImmutableList<NodeRemoteInfo>.Empty.Add(connectMsg.Info),
                        ImmutableList<NodeRemoteInfo>.Empty));
                    return new ConnectReply { Info = _node.Info };
                }
            }
            else
            {
                Log.Warn("Received connect from non group member " + connectMsg.Info);
                return new InvalidOperation((ushort)BasicErrorCode.NonAuthorized, "Received connect from non group member");
            }
        }

        public void Stop()
        {
            IsisSystem.Shutdown();
        }

        public IObservable<ClusterView> OnClusterViewChanged
        {
            get { return _onClusterViewChanged; }
        }
    }
}
