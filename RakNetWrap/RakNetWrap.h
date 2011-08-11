// The following ifdef block is the standard way of creating macros which make exporting 
// from a DLL simpler. All files within this DLL are compiled with the RAKNETWRAP_EXPORTS
// symbol defined on the command line. this symbol should not be defined on any project
// that uses this DLL. This way any other project whose source files include this file see 
// RAKNETWRAP_API functions as being imported from a DLL, whereas this DLL sees symbols
// defined with this macro as being exported.
#ifdef RAKNETWRAP_EXPORTS
#define RAKNETWRAP_API __declspec(dllexport)
#else
#define RAKNETWRAP_API __declspec(dllimport)
#endif

#include <vcclr.h>

#include "MessageIdentifiers.h"
#include "RakPeerInterface.h"
#include "PacketPriority.h"
#include "BitStream.h"

using namespace System;
using namespace System::IO;
using namespace System::Net;
using namespace MOUSE::Core;
using namespace System::ComponentModel::Composition;

namespace RakNetWrapper
{
	[Export(INetPeer::typeid)]
    public ref class RakPeerInterface : INetPeer
    {
        RakNet::RakPeerInterface* _rakPeer;
        NativeReader^ _reader;
        array<unsigned char>^ _buff;

    public:
        RakPeerInterface();
        ~RakPeerInterface();

        virtual bool Startup(IPEndPoint^ endpoint, int maxConnections);

        virtual void Connect(IPEndPoint^ endpoint);
        virtual void CloseConnection(int netId);

        virtual void Send(int netId, array<Byte>^ data, int length, MessagePriority priority, MessageReliability reliability);
        virtual void SendLoopback(array<Byte>^ data, int length);
        virtual bool ProcessNetEvent(INetEventProcessor^ processor);
        virtual void Shutdown();
        
        virtual property IPEndPoint^ EndPoint
        {
            IPEndPoint^ get()
            {
                RakNet::SystemAddress addr = _rakPeer->GetMyBoundAddress();
                return gcnew IPEndPoint(IPAddress::Parse(gcnew String(addr.ToString(false))), addr.GetPort());
            }
        }

        virtual IPEndPoint^ GetEndPointOf(int netId)
        {
            RakNet::SystemAddress addr = _rakPeer->GetSystemAddressFromIndex(netId);
            return gcnew IPEndPoint(IPAddress::Parse(gcnew String(addr.ToString(false))), addr.GetPort());
        }

        virtual String^ ToString() override
        {
            char text[2048];
            RakNet::RakNetStatistics* rss =_rakPeer->GetStatistics(_rakPeer->GetSystemAddressFromIndex(0));
            RakNet::StatisticsToString(rss, text, 2);
            return gcnew String(&text[0]);
        }
    };
}