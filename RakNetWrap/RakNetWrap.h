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
using namespace System::Collections::Generic;
using namespace System::ComponentModel::Composition;

namespace RakNetWrapper
{
    ref class RakChannel;

    [Export(INetProvider::typeid)]
    public ref class RakPeerInterface : INetProvider
    {
        RakNet::RakPeerInterface* _rakPeer;
        NativeReader^ _reader;
        array<unsigned char>^ _buff;
        Dictionary<int, RakChannel^ >^ _channels;
        INetPeerFactory^ _peerFactory;
		IPEndPoint^ _endpoint;
		unsigned short _maxConnections;

    public:
        RakPeerInterface(IPEndPoint^ listenEndpoint, unsigned short maxConnections);
		RakPeerInterface();
        ~RakPeerInterface();

        virtual bool Init(INetPeerFactory^ peerFactory);
		bool Startup();

        virtual void Connect(IPEndPoint^ endpoint);
        void CloseConnection(int netId);

        void Send(int netId, array<Byte>^ data, int length, MessagePriority priority, MessageReliability reliability);
        virtual bool PumpEvents();
        virtual void Shutdown();
        
        virtual property IPEndPoint^ EndPoint
        {
            IPEndPoint^ get()
            {
                return _endpoint;
            }
        }

        IPEndPoint^ GetEndPointOf(int netId)
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

    public ref class RakChannel : INetChannel
    {
        int _netId;
        IPEndPoint^ _ipEndPoint;
        RakPeerInterface^ _rakPeer;
    public:
		INetChannelListener^ ChannelListener;

        RakChannel(RakPeerInterface^ rakPeer, RakNet::SystemAddress& addr)
        {
            _netId = addr.systemIndex;
            _rakPeer = rakPeer;
            _ipEndPoint = gcnew IPEndPoint(IPAddress::Parse(gcnew String(addr.ToString(false))), addr.GetPort());
        }

        virtual property unsigned int Id
        {
            unsigned int get()
            {
                return _netId;
            }
        }

        virtual property IPEndPoint^ EndPoint
        {
            IPEndPoint^ get()
            {
                return _ipEndPoint;
            }
        }

        
        virtual void Send(Message^ msg)
        {
            NativeWriter^ writer = msg->GetSerialized();
            _rakPeer->Send(_netId, writer->Buff, writer->Position, msg->Priority, msg->Reliability);
        }

        virtual void Close()
        {
            _rakPeer->CloseConnection(_netId);
        }
    };
}