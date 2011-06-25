// RakNetWrap.cpp : Defines the entry point for the DLL application.
//

#include <vcclr.h>
#include <string.h>

#include "RakPeerInterface.h"
#include "MessageIdentifiers.h"
#include "RakNetStatistics.h"

#include "RakNetWrap.h"

using namespace System;
using namespace System::IO;
using namespace System::Runtime::InteropServices;
using namespace System::Net;
using namespace RakNetWrapper;



RakPeerInterface::RakPeerInterface()
{
    _rakPeer = RakNet::RakPeerInterface::GetInstance();
}

RakPeerInterface::~RakPeerInterface()
{
    if (_rakPeer != NULL)
        RakNet::RakPeerInterface::DestroyInstance(_rakPeer);
}

StartupResult RakPeerInterface::Startup(IPEndPoint^ endpoint, unsigned short maxConnections, int timeoutTimeMs)
{
    RakNet::SocketDescriptor sd;
    if(endpoint != nullptr)
    {
        char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(endpoint->Address->ToString());
        strncpy(sd.hostAddress, str2, sizeof(sd.hostAddress) - 1);
        sd.hostAddress[sizeof(sd.hostAddress) - 1] = '\0';
    }
    else
        sd.hostAddress[0] = '\0';
    sd.port = (unsigned short)endpoint->Port;
    sd.socketFamily = AF_UNSPEC;
    StartupResult res = (StartupResult)_rakPeer->Startup(maxConnections, &sd, 1);
    _rakPeer->SetMaximumIncomingConnections(maxConnections);
    _rakPeer->SetOccasionalPing(true);
    _rakPeer->SetUnreliableTimeout(1000);
    _rakPeer->SetTimeoutTime(timeoutTimeMs, RakNet::UNASSIGNED_SYSTEM_ADDRESS);
    return res;
}

int RakPeerInterface::Send(NetId netId, OutPacket^ packet, MessagePriority priority, MessageReliability reliability,
    char orderingChannel, bool broadcast)

{
    PacketPriority pp = (PacketPriority)(int)priority;
    PacketReliability pr = (PacketReliability)(int)reliability;

    return _rakPeer->Send(packet->GetInternalStream(), pp, pr, orderingChannel, netId.GetRakGuid(), broadcast);
}

int RakPeerInterface::Send(NetId netId, array<Byte>^ data, int length, MessagePriority priority, MessageReliability reliability,
    char orderingChannel, bool broadcast)

{
    PacketPriority pp = (PacketPriority)(int)priority;
    PacketReliability pr = (PacketReliability)(int)reliability;

    pin_ptr<unsigned char> npbuff = &data[0];
    unsigned char *pbuff = npbuff;

    return _rakPeer->Send((char*)pbuff, length, pp, pr, orderingChannel, netId.GetRakGuid(), broadcast);
}

void RakPeerInterface::SendLoopback(NetId netId, array<Byte>^ data, int length)

{
    pin_ptr<unsigned char> npbuff = &data[0];
    unsigned char *pbuff = npbuff;

    return _rakPeer->SendLoopback((char*)pbuff, length);
}


InPacket^ RakPeerInterface::Receive()
{
    RakNet::Packet *np;

    if (_rakPeer == NULL)
        return nullptr;

    np = _rakPeer->Receive();
    if (!np)
        return nullptr;

    return gcnew InPacket(_rakPeer, np);
}

bool RakPeerInterface::Receive(NetId% sourceId, array<Byte>^ buff, int% length)
{
    RakNet::Packet *np;
    
    if (_rakPeer == NULL)
        return false;

    np = _rakPeer->Receive();
    if (!np)
        return false;
    if(np->length > buff->Length)
    {
        _rakPeer->DeallocatePacket(np);
        return false;
    }
    pin_ptr<unsigned char> npbuff = &buff[0];
    unsigned char *pbuff = npbuff;
    
    memcpy(pbuff, np->data, np->length);
    length = np->length;
    sourceId = NetId(np->guid);
    _rakPeer->DeallocatePacket(np);
    return true;
}


ConnectionAttemptResult RakPeerInterface::Connect(IPEndPoint^ endpoint)
{
    int res;
    char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(endpoint->Address->ToString());
    res = _rakPeer->Connect(str2, (unsigned short)endpoint->Port, 0, 0);
    Marshal::FreeHGlobal((System::IntPtr)str2);
    return (RakNetWrapper::ConnectionAttemptResult)res;
}

void RakPeerInterface::CloseConnection(NetId netId, bool sendDisconnectionNotification,
                                        unsigned char orderingChannel)
{
    _rakPeer->CloseConnection(netId.GetRakGuid(), sendDisconnectionNotification, orderingChannel);
}

void RakPeerInterface::Shutdown()
{
    _rakPeer->Shutdown(500, 0);
    RakNet::RakPeerInterface::DestroyInstance(_rakPeer);
    _rakPeer = NULL;

}

void RakPeerInterface::SetDisconnectTimeoutFor(int time, NetId netId)
{
    _rakPeer->SetTimeoutTime(time, _rakPeer->GetSystemAddressFromGuid(netId.GetRakGuid()));
}


