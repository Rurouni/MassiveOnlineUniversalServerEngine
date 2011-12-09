﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using SampleS2SProtocol;
using System.Threading.Tasks;
using SampleC2SProtocol;
using MOUSE.Core;

namespace SampleServer
{
    [Export(typeof(NodeService))]
    [NodeService(AutoCreate = true, Persistant = false)]
    public class ChatManager : NodeService, IChatManager
    {
        private uint _userCounter;
        private uint _roomCounter;
        private Dictionary<string, ChatUserInfo> _usersByName;
        private Dictionary<uint, ChatUserInfo> _usersById;
        private List<ChatRoomInfo> _rooms;


        public override void OnCreated()
        {
            _usersByName = new Dictionary<string, ChatUserInfo>();
            _usersById = new Dictionary<uint, ChatUserInfo>();
            _rooms = new List<ChatRoomInfo>();
            _userCounter = 1;
            _roomCounter = 1;
        }

        public async Task<ChatUserInfo> GetUser(string name)
        {
            ChatUserInfo user;
            if(_usersByName.TryGetValue(name, out user))
                return user;
            return null;
        }

        public async Task<ChatUserInfo> TryRegisterUser(string name)
        {
            if (_usersByName.ContainsKey(name))
                return null;
            uint id = _userCounter++;
            var user = new ChatUserInfo(id, name);
            _usersByName.Add(name, user);
            _usersById.Add(id, user);
            return user;
        }

        public void UnregisterUser(uint id)
        {
            ChatUserInfo user;
            if(_usersById.TryGetValue(id, out user))
            {
                _usersById.Remove(id);
                _usersByName.Remove(user.Name);    
            }
        }

        public async Task<List<ChatRoomInfo>> GetRooms()
        {
            return _rooms;
        }

        public async Task<uint> GetOrCreateRoom(string roomName)
        {
            ChatRoomInfo room = _rooms.Find(x=>x.Name == roomName);
            if(room == null)
            {
                room = new ChatRoomInfo(_roomCounter++, roomName);
                _rooms.Add(room);
                Log.Info("New ChatRoom<Id:{0}, Name:{1}> is created", room.Id, room.Name);
            }
            return room.Id;
        }

        public async Task DeleteRoom(uint roomId)
        {
            _rooms.RemoveAll(x => x.Id == roomId);
        }
    }
}