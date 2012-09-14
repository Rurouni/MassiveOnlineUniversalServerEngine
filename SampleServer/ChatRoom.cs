using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using NLog;
using Protocol.Generated;

namespace SampleServer
{
    [NodeService(AutoCreate = true, Persistant = false)]
    public class ChatRoom : NodeService, IChatRoom, IChatRoomService
    {
        private long _ticketCounter = 0;
        private Dictionary<uint, ChatRoomClient> _usersById;
        private Dictionary<uint, ChatRoomClient> _usersByChannelId;
        private Dictionary<long, ChatUserInfo> _awaitingUsers;
        private List<string> _messages;

        public override void OnCreated()
        {
            Log = LogManager.GetLogger("ChatRoom");
            _usersById = new Dictionary<uint, ChatRoomClient>();
            _usersByChannelId = new Dictionary<uint, ChatRoomClient>();
            _awaitingUsers = new Dictionary<long, ChatUserInfo>();
            _messages = new List<string>();
        }
        
        [NetOperationHandler]
        public async Task<List<ChatUserInfo>> GetUsersInside()
        {
            Log.Info("GetUsersInside");
            return _usersById.Values.Select(x => x.Info).ToList();
        }

        [NetOperationHandler]
        public async Task<long> AwaitUser(ChatUserInfo user)
        {
            Log.Info("Awaiting for " + user.Name);
            var ticket = _ticketCounter++;
            _awaitingUsers.Add(ticket, user);
            Fiber.Shedule(() => ExpireTicketAsync(ticket), TimeSpan.FromSeconds(10), LockType.Write);
            return ticket;
        }

        private void ExpireTicketAsync(long ticket)
        {
            _awaitingUsers.Remove(ticket);
            Log.Info("Ticket {0} has expired", ticket);
        }

        [NetOperationHandler]
        public async Task<bool> RemoveUser(uint userId)
        {
            Log.Info("User<Id:{0}> will be removed", userId);
            ChatRoomClient client;
            if (_usersById.TryGetValue(userId, out client))
            {
                OnUserDisconnected(client.Peer);
            }

            return true;
        }

        private void OnUserDisconnected(INetPeer peer)
        {
            ChatRoomClient client;
            if (_usersByChannelId.TryGetValue(peer.Channel.Id, out client))
            {
                client.DisconnectionSubscription.Dispose();
                _usersById.Remove(client.Info.Id);
                _usersByChannelId.Remove(client.Peer.Channel.Id);
                string disconnectMsg = client.Info.Name + " has disconnected";
                _messages.Add(disconnectMsg);

                SendToAll(disconnectMsg);
                Log.Info("User<Name:{0}> has disconnected", client.Info.Name);
            }
        }

        [NetOperationHandler]
        public async Task<List<string>> Join(long ticket)
        {
            ChatUserInfo info;
            if(!_awaitingUsers.TryGetValue(ticket, out info))
                throw new InvalidInput(JoinRoomInvalidRetCode.ClientNotAwaited);

            _awaitingUsers.Remove(ticket);
            ChatRoomClient client;
            if (!_usersById.TryGetValue(info.Id, out client))
            {
                client = new ChatRoomClient(Context.Source, info);
                _usersById.Add(info.Id, client);
                _usersByChannelId.Add(client.Peer.Channel.Id, client);

                client.DisconnectionSubscription = Context.Source.DisconnectedEvent.Subscribe(OnUserDisconnected);
            }
            string connectMsg = client.Info.Name + " has connected";
            _messages.Add(connectMsg);

            SendToAll(connectMsg);

            Log.Info("User<Name:{0}> has joined", client.Info.Name);
            return _messages;
            
        }

        private void SendToAll(string msg)
        {
            foreach (var client in _usersByChannelId.Values)
            {
                var callback = client.Peer.As<IChatRoomServiceCallback>();
                callback.OnRoomMessage(Id, msg);
            }
        }

        [NetOperationHandler]
        public void Say(string message)
        {
            ChatRoomClient client;
            if (_usersByChannelId.TryGetValue(Context.Source.Channel.Id, out client))
            {
                string fullMsg = client.Info.Name + " : " + message;
                _messages.Add(fullMsg);
                SendToAll(fullMsg);
            }
            else
                Log.Warn("Say from unconnected peer - " + Context.Source);

        }

        [NetOperationHandler]
        public void Leave()
        {
            ChatRoomClient client;
            if (_usersByChannelId.TryGetValue(Context.Source.Channel.Id, out client))
            {
                Log.Info("User<Name:{0}> has left", client.Info.Name);
                OnUserDisconnected(client.Peer);
            }
            else
                Log.Warn("Leave from unconnected peer - " + Context.Source);
        }
    }

    public class ChatRoomClient
    {
        public readonly INetPeer Peer;
        public readonly ChatUserInfo Info;
        public IDisposable DisconnectionSubscription;

        public ChatRoomClient(INetPeer peer, ChatUserInfo info)
        {
            Peer = peer;
            Info = info;
        }
    }
}
