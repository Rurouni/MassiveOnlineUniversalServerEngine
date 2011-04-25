// RakNetWrap.cpp : Defines the entry point for the DLL application.
//

#include <vcclr.h>
#include <string.h>

#include "RakPeerInterface.h"
#include "MessageIdentifiers.h"
#include "RakNetStatistics.h"

#include "RakNetWrap.h"

using namespace System;
using namespace System::Runtime::InteropServices;
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

StartupResult RakPeerInterface::Startup(String ^ip, unsigned short port, unsigned short maxConnections, int timeoutTimeMs)
{
    RakNet::SocketDescriptor sd ;
    char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(ip);
    strncpy(sd.hostAddress, str2, sizeof(sd.hostAddress) - 1);
    sd.hostAddress[sizeof(sd.hostAddress) - 1] = 0;
    sd.port = port;
    StartupResult res = (StartupResult)_rakPeer->Startup(maxConnections, &sd, 1);
	_rakPeer->SetMaximumIncomingConnections(maxConnections);
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


ConnectionAttemptResult RakPeerInterface::Connect(String ^host, unsigned short remotePort)
{
    int res;
    char* str2 = (char*)(void*)Marshal::StringToHGlobalAnsi(host);
    res = _rakPeer->Connect(str2, remotePort, 0, 0);
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
