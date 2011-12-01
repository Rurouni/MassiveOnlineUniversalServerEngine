﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using SampleC2SProtocol;
using SampleS2SProtocol;

namespace SampleServer
{
    [Export(typeof(NodeService))]
    [NodeService(AutoCreate = true, Persistant = false)]
    public class ChatRoom : NodeService, IChatRoom, IChatRoomService
    {
        private long _ticketCounter = 0;
        readonly Dictionary<uint, ChatRoomClient> _usersById = new Dictionary<uint, ChatRoomClient>();
        readonly Dictionary<uint, ChatRoomClient> _usersByChannelId = new Dictionary<uint, ChatRoomClient>();
        readonly Dictionary<long, ChatUserInfo> _awaitingUsers = new Dictionary<long, ChatUserInfo>();
        private readonly List<string> _messages = new List<string>();
        
        public async Task<List<ChatUserInfo>> GetUsersInside()
        {
            return _usersById.Values.Select(x => x.Info).ToList();
        }

        public async Task<long> AwaitUser(ChatUserInfo user)
        {
            var ticket = _ticketCounter++;
            _awaitingUsers.Add(ticket, user);
            ExpireTicketAsync(ticket);
            return ticket;
        }

        private async void ExpireTicketAsync(long ticket)
        {
            await TaskEx.Delay(TimeSpan.FromSeconds(30));
            _awaitingUsers.Remove(ticket);
        }

        public async Task RemoveUser(uint userId)
        {
            ChatRoomClient client;
            if (_usersById.TryGetValue(userId, out client))
                client.Peer.Channel.Close();
        }

        private void OnUserDisconnected(INetPeer peer)
        {
            ChatRoomClient client;
            if (_usersByChannelId.TryGetValue(peer.Channel.Id, out client))
            {
                _usersById.Remove(client.Info.Id);
                _usersByChannelId.Remove(client.Peer.Channel.Id);
                _messages.Add(client.Info.Name + " has disconnected");
            }
        }
        
        public async Task<List<string>> Join(long ticket)
        {
            ChatUserInfo info;
            if(_awaitingUsers.TryGetValue(ticket, out info))
            {
                _awaitingUsers.Remove(ticket);
                var client = new ChatRoomClient(Context.Source, info);
                _usersById.Add(info.Id, client);
                _usersByChannelId.Add(client.Peer.Channel.Id, client);

                Context.Source.DisconnectedEvent.Subscribe(OnUserDisconnected);

                _messages.Add(client.Info.Name + " has connected");
                return _messages;
            }
            throw new InvalidInput(JoinRoomInvalidRetCode.ClientNotAwaited);
        }

        public void Say(string message)
        {
            ChatRoomClient client;
            if (_usersByChannelId.TryGetValue(Context.Source.Channel.Id, out client))
            {
                string fullMsg = client.Info.Name + " : " + message;
                _messages.Add(fullMsg);
                foreach (var otherClient in _usersByChannelId.Values)
                {
                    var callback = otherClient.Peer.As<IChatRoomServiceCallback>();
                    callback.OnRoomMessage(Id, fullMsg);
                }
            }
            else
                Log.Warn("Say from unconnected peer - " + Context.Source);

        }
    }

    public class ChatRoomClient
    {
        public readonly INetPeer Peer;
        public readonly ChatUserInfo Info;

        public ChatRoomClient(INetPeer peer, ChatUserInfo info)
        {
            Peer = peer;
            Info = info;
        }
    }
}
