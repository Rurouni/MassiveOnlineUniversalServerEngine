// RakNetWrap.cpp : Defines the entry point for the DLL application.
//

#include <vcclr.h>
#include <string.h>

#include "RakPeerInterface.h"
#include "MessageIdentifiers.h"
#include "RakNetStatistics.h"

#include "RakNetWrap.h"
#include <RakNetTypes.h>

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Net;
using namespace RakNetWrapper;



RakPeerInterface::RakPeerInterface(IPEndPoint^ listenEndpoint, unsigned short maxConnections)
{
	_endpoint = listenEndpoint;
	_maxConnections = maxConnections;
    _rakPeer = RakNet::RakPeerInterface::GetInstance();
	_stream = gcnew MemoryStream();
    _reader = gcnew BinaryReader(_stream);
    _buff = gcnew array<unsigned char>(1024*1024*10);
    _buff[0] = (unsigned char)ID_USER_PACKET_ENUM;
    _channels = gcnew Dictionary<int, RakChannel^>();
}

RakPeerInterface::RakPeerInterface()
{
	_endpoint = nullptr;
	_maxConnections = 1000;
    _rakPeer = RakNet::RakPeerInterface::GetInstance();
    _stream = gcnew MemoryStream();
    _reader = gcnew BinaryReader(_stream);
    _buff = gcnew array<unsigned char>(1024*1024*10);
    _buff[0] = (unsigned char)ID_USER_PACKET_ENUM;
    _channels = gcnew Dictionary<int, RakChannel^>();
}

RakPeerInterface::~RakPeerInterface()
{
    if (_rakPeer != NULL)
        RakNet::RakPeerInterface::DestroyInstance(_rakPeer);
}

bool RakPeerInterface::Startup()
{
    RakNet::SocketDescriptor sd;
    if(_endpoint != nullptr)
    {
        char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(_endpoint->Address->ToString());
        strncpy(sd.hostAddress, str2, sizeof(sd.hostAddress) - 1);
        sd.hostAddress[sizeof(sd.hostAddress) - 1] = '\0';
        sd.port = (unsigned short)_endpoint->Port;
        Marshal::FreeHGlobal((System::IntPtr)str2);
    }
    else
        sd.hostAddress[0] = '\0';
    
    //sd.socketFamily = AF_UNSPEC;
    RakNet::StartupResult res = _rakPeer->Startup(_maxConnections, &sd, 1, THREAD_PRIORITY_NORMAL);
	if(res == RakNet::RAKNET_STARTED)
	{
		_rakPeer->SetMaximumIncomingConnections(_maxConnections);
		_rakPeer->SetOccasionalPing(true);
		_rakPeer->SetUnreliableTimeout(1000);
		_rakPeer->SetTimeoutTime(10000, RakNet::UNASSIGNED_SYSTEM_ADDRESS);
	
		RakNet::SystemAddress addr = _rakPeer->GetMyBoundAddress();
		String^ str = gcnew String(addr.ToString(false));
		if(str == "UNASSIGNED_SYSTEM_ADDRESS")
			str = "0.0.0.0";
		_endpoint = gcnew IPEndPoint(IPAddress::Parse(str), addr.GetPort());
	}
	else
	{
		int retCode = res;
		Console::WriteLine("NetInitialization code="+retCode);
	}
    
    
    return res == RakNet::RAKNET_STARTED;
}

bool RakPeerInterface::Init(INetChannelConsumer^ peerFactory)
{
	_peerFactory = peerFactory;
	return Startup();
}

void RakPeerInterface::Connect(IPEndPoint^ endpoint)
{
    int res;
    char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(endpoint->Address->ToString());
    res = _rakPeer->Connect(str2, (unsigned short)endpoint->Port, 0, 0);
    Marshal::FreeHGlobal((System::IntPtr)str2);
}

void RakPeerInterface::CloseConnection(int netId)
{
    _rakPeer->CloseConnection(_rakPeer->GetSystemAddressFromIndex(netId), true, 0);
}

void RakPeerInterface::Shutdown()
{
    _rakPeer->Shutdown(500, 0);
    RakNet::RakPeerInterface::DestroyInstance(_rakPeer);
    _rakPeer = NULL;

}

void RakPeerInterface::Send(int netId, array<Byte>^ data, int length, MessagePriority priority, MessageReliability reliability)
{
    if(_buff->Length < length+1)
        throw gcnew Exception(" Data array is too big");

    PacketPriority pp = MEDIUM_PRIORITY;
    switch(priority)
    {
        case MessagePriority::High : pp = HIGH_PRIORITY; break;
        case MessagePriority::Medium : pp = MEDIUM_PRIORITY; break;
        case MessagePriority::Low : pp = MEDIUM_PRIORITY; break;
    }

    PacketReliability pr = RELIABLE_ORDERED;
    switch(reliability)
    {
        case MessageReliability::Reliable : pr = RELIABLE; break;
        case MessageReliability::ReliableOrdered : pr = RELIABLE_ORDERED; break;
        case MessageReliability::Unreliable : pr = UNRELIABLE; break;
        case MessageReliability::UnreliableOrdered : pr = UNRELIABLE_SEQUENCED; break;
    }
    
    pin_ptr<unsigned char> npdata = &data[0];
    unsigned char *pdata = npdata;

    
    pin_ptr<unsigned char> npbuff = &_buff[1];
    unsigned char *pbuff = npbuff;
    
    memcpy(pbuff, pdata, length);
    pbuff--;//go to _buff[0]

    _rakPeer->Send((char*)pbuff, length+1, pp, pr, 0, _rakPeer->GetSystemAddressFromIndex(netId), false);
}

bool RakPeerInterface::PumpEvents()
{
    RakNet::Packet *np;
    RakChannel^ channel;
    
    if (_rakPeer == NULL)
        return false;

    np = _rakPeer->Receive();
    if (!np)
        return false;
    if(np->length > _buff->Length)
    {
        _rakPeer->DeallocatePacket(np);
        throw gcnew Exception("Received packet is too big");
    }

    switch(np->data[0])
    {
        case ID_NEW_INCOMING_CONNECTION :
            _rakPeer->SetTimeoutTime(10000, np->systemAddress);//TODO : move to config
            channel = gcnew RakChannel(this, np->systemAddress);
            _channels->Add(channel->Id, channel);
            channel->ChannelListener = _peerFactory->OnNetConnect(channel);
            break;
        case ID_CONNECTION_REQUEST_ACCEPTED:
             channel= gcnew RakChannel(this, np->systemAddress);
            _channels->Add(channel->Id, channel);
            channel->ChannelListener = _peerFactory->OnNetConnect(channel);
            break;
        case ID_CONNECTION_LOST:
        case ID_DISCONNECTION_NOTIFICATION:
            if(_channels->TryGetValue(np->systemAddress.systemIndex, channel))
            {
				channel->ChannelListener->OnDisconnected();
                _channels->Remove(channel->Id);
            }
            break;	

        case ID_USER_PACKET_ENUM :
            if (np->length < 1 + sizeof(unsigned short))
            {
                _rakPeer->DeallocatePacket(np);
                throw gcnew Exception("Received packet is too small");
            }

            pin_ptr<unsigned char> npbuff = &_buff[0];
            unsigned char *pbuff = npbuff;

            memcpy(pbuff, np->data, np->length);
            
            if(_channels->TryGetValue(np->systemAddress.systemIndex, channel))
            {
				_stream->Position = 0;
				_stream->Write(_buff, 1, np->length-1);
				_stream->Position = 0;
                channel->ChannelListener->OnNetData(_reader);
            }
            break;
    }
        
    _rakPeer->DeallocatePacket(np);
    return true;
}




