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



RakPeerInterface::RakPeerInterface()
{
    _rakPeer = RakNet::RakPeerInterface::GetInstance();
    _reader = gcnew NativeReader();
    _buff = gcnew array<unsigned char>(1024*1024*10);
    _buff[0] = (unsigned char)ID_USER_PACKET_ENUM;
}

RakPeerInterface::~RakPeerInterface()
{
    if (_rakPeer != NULL)
        RakNet::RakPeerInterface::DestroyInstance(_rakPeer);
}

bool RakPeerInterface::Startup(IPEndPoint^ endpoint, int maxConnections)
{
    RakNet::SocketDescriptor sd;
    if(endpoint != nullptr)
    {
        char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(endpoint->Address->ToString());
        strncpy(sd.hostAddress, str2, sizeof(sd.hostAddress) - 1);
        sd.hostAddress[sizeof(sd.hostAddress) - 1] = '\0';
		sd.port = (unsigned short)endpoint->Port;
		Marshal::FreeHGlobal((System::IntPtr)str2);
    }
    else
        sd.hostAddress[0] = '\0';
    
    //sd.socketFamily = AF_UNSPEC;
    RakNet::StartupResult res = _rakPeer->Startup(maxConnections, &sd, 1, THREAD_PRIORITY_NORMAL);
    _rakPeer->SetMaximumIncomingConnections(maxConnections);
    _rakPeer->SetOccasionalPing(true);
    _rakPeer->SetUnreliableTimeout(1000);
    _rakPeer->SetTimeoutTime(10000, RakNet::UNASSIGNED_SYSTEM_ADDRESS);
	int retCode = res;
	Console::WriteLine("NetInitialization code="+retCode);
    return res == RakNet::RAKNET_STARTED;
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

void RakPeerInterface::SendLoopback(array<Byte>^ data, int length)

{
    pin_ptr<unsigned char> npbuff = &data[0];
    unsigned char *pbuff = npbuff;

    return _rakPeer->SendLoopback((char*)pbuff, length);
}


bool RakPeerInterface::ProcessNetEvent(INetEventProcessor^ processor)
{
    RakNet::Packet *np;
    
    if (_rakPeer == NULL)
        return false;

    np = _rakPeer->Receive();
    if (!np)
        return false;
    if(np->length > _reader->Length)
    {
        _rakPeer->DeallocatePacket(np);
        throw gcnew Exception("Received packet is too big");
    }

    switch(np->data[0])
    {
        case ID_NEW_INCOMING_CONNECTION :
            _rakPeer->SetTimeoutTime(10000, np->systemAddress);//TODO : move to config
            processor->OnNetConnect(np->systemAddress.systemIndex);
            break;
        case ID_CONNECTION_REQUEST_ACCEPTED:
            processor->OnNetConnectionAccepted(np->systemAddress.systemIndex);
            break;
        case ID_CONNECTION_LOST:
        case ID_DISCONNECTION_NOTIFICATION:
            processor->OnNetDisconnect(np->systemAddress.systemIndex);
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
            
            _reader->SetBuffer(_buff, 1);//skip rakNet control byte
            processor->OnNetData(np->systemAddress.systemIndex, _reader);
            break;
    }
        
    _rakPeer->DeallocatePacket(np);
    return true;
}




