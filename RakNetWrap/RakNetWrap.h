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

namespace RakNetWrapper
{
	public enum struct RakNetMessages : int
    {
        //
		// RESERVED TYPES - DO NOT CHANGE THESE
		// All types from RakPeer
		//
		/// These types are never returned to the user.
		/// Ping from a connected system.  Update timestamps (internal use only)
		ID_CONNECTED_PING,  
		/// Ping from an unconnected system.  Reply but do not update timestamps. (internal use only)
		ID_UNCONNECTED_PING,
		/// Ping from an unconnected system.  Only reply if we have open connections. Do not update timestamps. (internal use only)
		ID_UNCONNECTED_PING_OPEN_CONNECTIONS,
		/// Pong from a connected system.  Update timestamps (internal use only)
		ID_CONNECTED_PONG,
		/// A reliable packet to detect lost connections (internal use only)
		ID_DETECT_LOST_CONNECTIONS,
		/// C2S: Initial query: Header(1), OfflineMesageID(16), Protocol number(1), Pad(toMTU), sent with no fragment set.
		/// If protocol fails on server, returns ID_INCOMPATIBLE_PROTOCOL_VERSION to client
		ID_OPEN_CONNECTION_REQUEST_1,
		/// S2C: Header(1), OfflineMesageID(16), server GUID(8), HasSecurity(1), Cookie(4, if HasSecurity)
		/// , public key (if do security is true), MTU(2). If public key fails on client, returns ID_PUBLIC_KEY_MISMATCH
		ID_OPEN_CONNECTION_REPLY_1,
		/// C2S: Header(1), OfflineMesageID(16), Cookie(4, if HasSecurity is true on the server), clientSupportsSecurity(1 bit),
		/// handshakeChallenge (if has security on both server and client), remoteBindingAddress(6), MTU(2), client GUID(8)
		/// Connection slot allocated if cookie is valid, server is not full, GUID and IP not already in use.
		ID_OPEN_CONNECTION_REQUEST_2,
		/// S2C: Header(1), OfflineMesageID(16), server GUID(8), mtu(2), doSecurity(1 bit), handshakeAnswer (if do security is true)
		ID_OPEN_CONNECTION_REPLY_2,
		/// C2S: Header(1), GUID(8), Timestamp, HasSecurity(1), Proof(32)
		ID_CONNECTION_REQUEST,
		/// RakPeer - Remote system requires secure connections, pass a public key to RakPeerInterface::Connect()
		ID_REMOTE_SYSTEM_REQUIRES_PUBLIC_KEY,
		/// RakPeer - We passed a public key to RakPeerInterface::Connect(), but the other system did not have security turned on
		ID_OUR_SYSTEM_REQUIRES_SECURITY,
		/// RakPeer - Wrong public key passed to RakPeerInterface::Connect()
		ID_PUBLIC_KEY_MISMATCH,
		/// RakPeer - Same as ID_ADVERTISE_SYSTEM, but intended for internal use rather than being passed to the user.
		/// Second byte indicates type. Used currently for NAT punchthrough for receiver port advertisement. See ID_NAT_ADVERTISE_RECIPIENT_PORT
		ID_OUT_OF_BAND_INTERNAL,
		/// If RakPeerInterface::Send() is called where PacketReliability contains _WITH_ACK_RECEIPT, then on a later call to
		/// RakPeerInterface::Receive() you will get ID_SND_RECEIPT_ACKED or ID_SND_RECEIPT_LOSS. The message will be 5 bytes long,
		/// and bytes 1-4 inclusive will contain a number in native order containing a number that identifies this message.
		/// This number will be returned by RakPeerInterface::Send() or RakPeerInterface::SendList(). ID_SND_RECEIPT_ACKED means that
		/// the message arrived
		ID_SND_RECEIPT_ACKED,
		/// If RakPeerInterface::Send() is called where PacketReliability contains UNRELIABLE_WITH_ACK_RECEIPT, then on a later call to
		/// RakPeerInterface::Receive() you will get ID_SND_RECEIPT_ACKED or ID_SND_RECEIPT_LOSS. The message will be 5 bytes long,
		/// and bytes 1-4 inclusive will contain a number in native order containing a number that identifies this message. This number
		/// will be returned by RakPeerInterface::Send() or RakPeerInterface::SendList(). ID_SND_RECEIPT_LOSS means that an ack for the
		/// message did not arrive (it may or may not have been delivered, probably not). On disconnect or shutdown, you will not get
		/// ID_SND_RECEIPT_LOSS for unsent messages, you should consider those messages as all lost.
		ID_SND_RECEIPT_LOSS,
	

		//
		// USER TYPES - DO NOT CHANGE THESE
		//

		/// RakPeer - In a client/server environment, our connection request to the server has been accepted.
		ID_CONNECTION_REQUEST_ACCEPTED,
		/// RakPeer - Sent to the player when a connection request cannot be completed due to inability to connect. 
		ID_CONNECTION_ATTEMPT_FAILED,
		/// RakPeer - Sent a connect request to a system we are currently connected to.
		ID_ALREADY_CONNECTED,
		/// RakPeer - A remote system has successfully connected.
		ID_NEW_INCOMING_CONNECTION,
		/// RakPeer - The system we attempted to connect to is not accepting new connections.
		ID_NO_FREE_INCOMING_CONNECTIONS,
		/// RakPeer - The system specified in Packet::systemAddress has disconnected from us.  For the client, this would mean the
		/// server has shutdown. 
		ID_DISCONNECTION_NOTIFICATION,
		/// RakPeer - Reliable packets cannot be delivered to the system specified in Packet::systemAddress.  The connection to that
		/// system has been closed. 
		ID_CONNECTION_LOST,
		/// RakPeer - We are banned from the system we attempted to connect to.
		ID_CONNECTION_BANNED,
		/// RakPeer - The remote system is using a password and has refused our connection because we did not set the correct password.
		ID_INVALID_PASSWORD,
		// RAKNET_PROTOCOL_VERSION in RakNetVersion.h does not match on the remote system what we have on our system
		// This means the two systems cannot communicate.
		// The 2nd byte of the message contains the value of RAKNET_PROTOCOL_VERSION for the remote system
		ID_INCOMPATIBLE_PROTOCOL_VERSION,
		// Means that this IP address connected recently, and can't connect again as a security measure. See
		/// RakPeer::SetLimitIPConnectionFrequency()
		ID_IP_RECENTLY_CONNECTED,
		/// RakPeer - The sizeof(RakNetTime) bytes following this byte represent a value which is automatically modified by the difference
		/// in system times between the sender and the recipient. Requires that you call SetOccasionalPing.
		ID_TIMESTAMP,
		/// RakPeer - Pong from an unconnected system.  First byte is ID_UNCONNECTED_PONG, second sizeof(RakNet::TimeMS) bytes is the ping,
		/// following bytes is system specific enumeration data.
		/// Read using bitstreams
		ID_UNCONNECTED_PONG,
		/// RakPeer - Inform a remote system of our IP/Port. On the recipient, all data past ID_ADVERTISE_SYSTEM is whatever was passed to
		/// the data parameter
		ID_ADVERTISE_SYSTEM,
		// RakPeer - Downloading a large message. Format is ID_DOWNLOAD_PROGRESS (MessageID), partCount (unsigned int),
		///  partTotal (unsigned int),
		/// partLength (unsigned int), first part data (length <= MAX_MTU_SIZE). See the three parameters partCount, partTotal
		///  and partLength in OnFileProgress in FileListTransferCBInterface.h
		ID_DOWNLOAD_PROGRESS,
	
		/// ConnectionGraph2 plugin - In a client/server environment, a client other than ourselves has disconnected gracefully.
		///   Packet::systemAddress is modified to reflect the systemAddress of this client.
		ID_REMOTE_DISCONNECTION_NOTIFICATION,
		/// ConnectionGraph2 plugin - In a client/server environment, a client other than ourselves has been forcefully dropped.
		///  Packet::systemAddress is modified to reflect the systemAddress of this client.
		ID_REMOTE_CONNECTION_LOST,
		/// ConnectionGraph2 plugin: Bytes 1-4 = count. for (count items) contains {SystemAddress, RakNetGUID, 2 byte ping}
		ID_REMOTE_NEW_INCOMING_CONNECTION,

		/// FileListTransfer plugin - Setup data
		ID_FILE_LIST_TRANSFER_HEADER,
		/// FileListTransfer plugin - A file
		ID_FILE_LIST_TRANSFER_FILE,
		// Ack for reference push, to send more of the file
		ID_FILE_LIST_REFERENCE_PUSH_ACK,

		/// DirectoryDeltaTransfer plugin - Request from a remote system for a download of a directory
		ID_DDT_DOWNLOAD_REQUEST,
	
		/// RakNetTransport plugin - Transport provider message, used for remote console
		ID_TRANSPORT_STRING,

 		/// ReplicaManager plugin - Create an object
		ID_REPLICA_MANAGER_CONSTRUCTION,
 		/// ReplicaManager plugin - Changed scope of an object
 		ID_REPLICA_MANAGER_SCOPE_CHANGE,
 		/// ReplicaManager plugin - Serialized data of an object
		ID_REPLICA_MANAGER_SERIALIZE,
 		/// ReplicaManager plugin - New connection, about to send all world objects
		ID_REPLICA_MANAGER_DOWNLOAD_STARTED,
 		/// ReplicaManager plugin - Finished downloading all serialized objects
		ID_REPLICA_MANAGER_DOWNLOAD_COMPLETE,
		/// Serialize construction for an object that already exists on the remote system
		ID_REPLICA_MANAGER_3_SERIALIZE_CONSTRUCTION_EXISTING,
		ID_REPLICA_MANAGER_3_LOCAL_CONSTRUCTION_REJECTED,
		ID_REPLICA_MANAGER_3_LOCAL_CONSTRUCTION_ACCEPTED,

		/// RakVoice plugin - Open a communication channel
		ID_RAKVOICE_OPEN_CHANNEL_REQUEST,
		/// RakVoice plugin - Communication channel accepted
		ID_RAKVOICE_OPEN_CHANNEL_REPLY,
		/// RakVoice plugin - Close a communication channel
		ID_RAKVOICE_CLOSE_CHANNEL,
		/// RakVoice plugin - Voice data
		ID_RAKVOICE_DATA,

		/// Autopatcher plugin - Get a list of files that have changed since a certain date
		ID_AUTOPATCHER_GET_CHANGELIST_SINCE_DATE,
		/// Autopatcher plugin - A list of files to create
		ID_AUTOPATCHER_CREATION_LIST,
		/// Autopatcher plugin - A list of files to delete
		ID_AUTOPATCHER_DELETION_LIST,
		/// Autopatcher plugin - A list of files to get patches for
		ID_AUTOPATCHER_GET_PATCH,
		/// Autopatcher plugin - A list of patches for a list of files
		ID_AUTOPATCHER_PATCH_LIST,
		/// Autopatcher plugin - Returned to the user: An error from the database repository for the autopatcher.
		ID_AUTOPATCHER_REPOSITORY_FATAL_ERROR,
		/// Autopatcher plugin - Finished getting all files from the autopatcher
		ID_AUTOPATCHER_FINISHED_INTERNAL,
		ID_AUTOPATCHER_FINISHED,
		/// Autopatcher plugin - Returned to the user: You must restart the application to finish patching.
		ID_AUTOPATCHER_RESTART_APPLICATION,

		/// NATPunchthrough plugin: internal
		ID_NAT_PUNCHTHROUGH_REQUEST,
		/// NATPunchthrough plugin: internal
		ID_NAT_GROUP_PUNCHTHROUGH_REQUEST,
		/// NATPunchthrough plugin: internal
		ID_NAT_GROUP_PUNCHTHROUGH_REPLY,
		/// NATPunchthrough plugin: internal
		ID_NAT_CONNECT_AT_TIME,
		/// NATPunchthrough plugin: internal
		ID_NAT_GET_MOST_RECENT_PORT,
		/// NATPunchthrough plugin: internal
		ID_NAT_CLIENT_READY,
		/// NATPunchthrough plugin: internal
		ID_NAT_CONFIRM_CONNECTION_TO_SERVER,
		/// NATPunchthrough plugin: internal
		ID_NAT_GROUP_PUNCHTHROUGH_FAILURE_NOTIFICATION,

		/// NATPunchthrough plugin: Destination system is not connected to the server. Bytes starting at offset 1 contains the
		///  RakNetGUID destination field of NatPunchthroughClient::OpenNAT().
		ID_NAT_TARGET_NOT_CONNECTED,
		/// NATPunchthrough plugin: Destination system is not responding to ID_NAT_GET_MOST_RECENT_PORT. Possibly the plugin is not installed.
		///  Bytes starting at offset 1 contains the RakNetGUID  destination field of NatPunchthroughClient::OpenNAT().
		ID_NAT_TARGET_UNRESPONSIVE,
		/// NATPunchthrough plugin: The server lost the connection to the destination system while setting up punchthrough.
		///  Possibly the plugin is not installed. Bytes starting at offset 1 contains the RakNetGUID  destination
		///  field of NatPunchthroughClient::OpenNAT().
		ID_NAT_CONNECTION_TO_TARGET_LOST,
		/// NATPunchthrough plugin: This punchthrough is already in progress. Possibly the plugin is not installed.
		///  Bytes starting at offset 1 contains the RakNetGUID destination field of NatPunchthroughClient::OpenNAT().
		ID_NAT_ALREADY_IN_PROGRESS,
		/// NATPunchthrough plugin: This message is generated on the local system, and does not come from the network.
		///  packet::guid contains the destination field of NatPunchthroughClient::OpenNAT(). Byte 1 contains 1 if you are the sender, 0 if not
		ID_NAT_PUNCHTHROUGH_FAILED,
		/// NATPunchthrough plugin: Punchthrough succeeded. See packet::systemAddress and packet::guid. Byte 1 contains 1 if you are the sender,
		///  0 if not. You can now use RakPeer::Connect() or other calls to communicate with this system.
		ID_NAT_PUNCHTHROUGH_SUCCEEDED,
		/// NATPunchthrough plugin: OpenNATGroup failed.
		/// packet::guid contains the destination field of NatPunchthroughClient::OpenNAT()
		/// Data format starts at byte 1: (unsigned short) passedSystemsCount, List<SystemAddress> passedSystems, (unsigned short) failedSystemsCount, List<RakNetGuid> failedSystems
		ID_NAT_GROUP_PUNCH_FAILED,
		/// NATPunchthrough plugin: OpenNATGroup succeeded.
		/// packet::guid contains the destination field of NatPunchthroughClient::OpenNATGroup()
		/// You can now use RakPeer::Connect() or other calls to communicate with these systems
		/// Data format starts at byte 1: (unsigned short) passedSystemsCount, List<SystemAddress> passedSystems
		ID_NAT_GROUP_PUNCH_SUCCEEDED,

		/// ReadyEvent plugin - Set the ready state for a particular system
		/// First 4 bytes after the message contains the id
		ID_READY_EVENT_SET,
		/// ReadyEvent plugin - Unset the ready state for a particular system
		/// First 4 bytes after the message contains the id
		ID_READY_EVENT_UNSET,
		/// All systems are in state ID_READY_EVENT_SET
		/// First 4 bytes after the message contains the id
		ID_READY_EVENT_ALL_SET,
		/// \internal, do not process in your game
		/// ReadyEvent plugin - Request of ready event state - used for pulling data when newly connecting
		ID_READY_EVENT_QUERY,

		/// Lobby packets. Second byte indicates type.
		ID_LOBBY_GENERAL,

		// RPC3, RPC4Plugin error
		ID_RPC_REMOTE_ERROR,
		/// Plugin based replacement for RPC system
		ID_RPC_PLUGIN,

		/// FileListTransfer transferring large files in chunks that are read only when needed, to save memory
		ID_FILE_LIST_REFERENCE_PUSH,
		/// Force the ready event to all set
		ID_READY_EVENT_FORCE_ALL_SET,

		/// Rooms function
		ID_ROOMS_EXECUTE_FUNC,
		ID_ROOMS_LOGON_STATUS,
		ID_ROOMS_HANDLE_CHANGE,

		/// Lobby2 message
		ID_LOBBY2_SEND_MESSAGE,
		ID_LOBBY2_SERVER_ERROR,

		/// Informs user of a new host GUID. Packet::Guid contains this new host RakNetGuid. The old host can be read out using BitStream->Read(RakNetGuid) starting on byte 1
		ID_FCM2_NEW_HOST,
		/// \internal For FullyConnectedMesh2 plugin
		ID_FCM2_REQUEST_FCMGUID,
		/// \internal For FullyConnectedMesh2 plugin
		ID_FCM2_RESPOND_CONNECTION_COUNT,
		/// \internal For FullyConnectedMesh2 plugin
		ID_FCM2_INFORM_FCMGUID,
		/// \internal For FullyConnectedMesh2 plugin
		ID_FCM2_UPDATE_MIN_TOTAL_CONNECTION_COUNT,

		/// UDP proxy messages. Second byte indicates type.
		ID_UDP_PROXY_GENERAL,

		/// SQLite3Plugin - execute
		ID_SQLite3_EXEC,
		/// SQLite3Plugin - Remote database is unknown
		ID_SQLite3_UNKNOWN_DB,
		/// Events happening with SQLiteClientLoggerPlugin
		ID_SQLLITE_LOGGER,

		/// Sent to NatTypeDetectionServer
		ID_NAT_TYPE_DETECTION_REQUEST,
		/// Sent to NatTypeDetectionClient. Byte 1 contains the type of NAT detected.
		ID_NAT_TYPE_DETECTION_RESULT,

		/// Used by the router2 plugin
		ID_ROUTER_2_INTERNAL,
		/// No path is available or can be established to the remote system
		/// Packet::guid contains the endpoint guid that we were trying to reach
		ID_ROUTER_2_FORWARDING_NO_PATH,
		/// \brief You can now call connect, ping, or other operations to the destination system.
		///
		/// Connect as follows:
		///
		/// RakNet::BitStream bs(packet->data, packet->length, false);
		/// bs.IgnoreBytes(sizeof(MessageID));
		/// RakNetGUID endpointGuid;
		/// bs.Read(endpointGuid);
		/// unsigned short sourceToDestPort;
		/// bs.Read(sourceToDestPort);
		/// char ipAddressString[32];
		/// packet->systemAddress.ToString(false, ipAddressString);
		/// rakPeerInterface->Connect(ipAddressString, sourceToDestPort, 0,0);
		ID_ROUTER_2_FORWARDING_ESTABLISHED,
		/// The IP address for a forwarded connection has changed
		/// Read endpointGuid and port as per ID_ROUTER_2_FORWARDING_ESTABLISHED
		ID_ROUTER_2_REROUTED,

		/// \internal Used by the team balancer plugin
		ID_TEAM_BALANCER_INTERNAL,
		/// Cannot switch to the desired team because it is full. However, if someone on that team leaves, you will
		///  get ID_TEAM_BALANCER_SET_TEAM later. Byte 1 contains the team you requested to join.
		ID_TEAM_BALANCER_REQUESTED_TEAM_CHANGE_PENDING,
		/// Cannot switch to the desired team because all teams are locked. However, if someone on that team leaves,
		///  you will get ID_TEAM_BALANCER_SET_TEAM later. Byte 1 contains the team you requested to join.
		ID_TEAM_BALANCER_TEAMS_LOCKED,
		/// Team balancer plugin informing you of your team. Byte 1 contains the team you requested to join.
		ID_TEAM_BALANCER_TEAM_ASSIGNED,

		/// Gamebryo Lightspeed integration
		ID_LIGHTSPEED_INTEGRATION,

		/// XBOX integration
		ID_XBOX_LOBBY,

		/// The password we used to challenge the other system passed, meaning the other system has called TwoWayAuthentication::AddPassword() with the same password we passed to TwoWayAuthentication::Challenge()
		/// You can read the identifier used to challenge as follows:
		/// RakNet::BitStream bs(packet->data, packet->length, false); bs.IgnoreBytes(sizeof(RakNet::MessageID)); RakNet::RakString password; bs.Read(password);
		ID_TWO_WAY_AUTHENTICATION_INCOMING_CHALLENGE_SUCCESS,
		ID_TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_SUCCESS,
		/// A remote system sent us a challenge using TwoWayAuthentication::Challenge(), and the challenge failed.
		/// If the other system must pass the challenge to stay connected, you should call RakPeer::CloseConnection() to terminate the connection to the other system. 
		ID_TWO_WAY_AUTHENTICATION_INCOMING_CHALLENGE_FAILURE,
		/// The other system did not add the password we used to TwoWayAuthentication::AddPassword()
		/// You can read the identifier used to challenge as follows:
		/// RakNet::BitStream bs(packet->data, packet->length, false); bs.IgnoreBytes(sizeof(MessageID)); RakNet::RakString password; bs.Read(password);
		ID_TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_FAILURE,
		/// The other system did not respond within a timeout threshhold. Either the other system is not running the plugin or the other system was blocking on some operation for a long time.
		/// You can read the identifier used to challenge as follows:
		/// RakNet::BitStream bs(packet->data, packet->length, false); bs.IgnoreBytes(sizeof(MessageID)); RakNet::RakString password; bs.Read(password);
		ID_TWO_WAY_AUTHENTICATION_OUTGOING_CHALLENGE_TIMEOUT,
		/// \internal
		ID_TWO_WAY_AUTHENTICATION_NEGOTIATION,

		/// CloudClient / CloudServer
		ID_CLOUD_POST_REQUEST,
		ID_CLOUD_RELEASE_REQUEST,
		ID_CLOUD_GET_REQUEST,
		ID_CLOUD_GET_RESPONSE,
		ID_CLOUD_UNSUBSCRIBE_REQUEST,
		ID_CLOUD_SERVER_TO_SERVER_COMMAND,
		ID_CLOUD_SUBSCRIPTION_NOTIFICATION,

		// So I can add more without changing user enumerations
		ID_RESERVED_1,
		ID_RESERVED_2,
		ID_RESERVED_3,
		ID_RESERVED_4,
		ID_RESERVED_5,
		ID_RESERVED_6,
		ID_RESERVED_7,
		ID_RESERVED_8,
		ID_RESERVED_9,

		// For the user to use.  Start your first enumeration at this value.
		ID_USER_PACKET_ENUM,
    };

	public enum struct StartupResult : int
	{
		RAKNET_STARTED,
		RAKNET_ALREADY_STARTED,
		INVALID_SOCKET_DESCRIPTORS,
		INVALID_MAX_CONNECTIONS,
		SOCKET_PORT_ALREADY_IN_USE,
		SOCKET_FAILED_TO_BIND,
		SOCKET_FAILED_TEST_SEND,
		FAILED_TO_CREATE_NETWORK_THREAD,
	};

	public enum struct ConnectionAttemptResult : int
	{
		CONNECTION_ATTEMPT_STARTED,
		INVALID_PARAMETER,
		CANNOT_RESOLVE_DOMAIN_NAME,
		ALREADY_CONNECTED_TO_ENDPOINT,
		CONNECTION_ATTEMPT_ALREADY_IN_PROGRESS,
		SECURITY_INITIALIZATION_FAILED
	};

	/// Returned from RakPeerInterface::GetConnectionState()
	public enum struct ConnectionState : int
	{
		/// Not applicable, because the passed address is the loopback address
		IS_LOOPBACK,
		/// Connect() was called, but the process hasn't started yet
		IS_PENDING,
		/// Processing the connection attempt
		IS_CONNECTING,
		/// Is connected and able to communicate
		IS_CONNECTED,
		/// Was connected, but will disconnect as soon as the remaining messages are delivered
		IS_DISCONNECTING,
		/// A connection attempt failed and will be aborted
		IS_SILENTLY_DISCONNECTING,
		/// No longer connected
		IS_DISCONNECTED,
		/// Was never connected, or else was disconnected long enough ago that the entry has been discarded
		IS_NOT_CONNECTED,
	};

    /// These enumerations are used to describe when packets are delivered.
	public enum struct MessagePriority : int
	{
		/// The highest possible priority. These message trigger sends immediately, and are generally not buffered or aggregated into a single datagram.
		IMMEDIATE_PRIORITY,

		/// For every 2 IMMEDIATE_PRIORITY messages, 1 HIGH_PRIORITY will be sent.
		/// Messages at this priority and lower are buffered to be sent in groups at 10 millisecond intervals to reduce UDP overhead and better measure congestion control. 
		HIGH_PRIORITY,

		/// For every 2 HIGH_PRIORITY messages, 1 MEDIUM_PRIORITY will be sent.
		/// Messages at this priority and lower are buffered to be sent in groups at 10 millisecond intervals to reduce UDP overhead and better measure congestion control. 
		MEDIUM_PRIORITY,   

		/// For every 2 MEDIUM_PRIORITY messages, 1 LOW_PRIORITY will be sent.
		/// Messages at this priority and lower are buffered to be sent in groups at 10 millisecond intervals to reduce UDP overhead and better measure congestion control. 
		LOW_PRIORITY,

		/// \internal
		NUMBER_OF_PRIORITIES
	};

	/// These enumerations are used to describe how packets are delivered.
	/// \note  Note to self: I write this with 3 bits in the stream.  If I add more remember to change that
	/// \note In ReliabilityLayer::WriteToBitStreamFromInternalPacket I assume there are 5 major types
	/// \note Do not reorder, I check on >= UNRELIABLE_WITH_ACK_RECEIPT
	public enum struct MessageReliability : int
	{
		/// Same as regular UDP, except that it will also discard duplicate datagrams.  RakNet adds (6 to 17) + 21 bits of overhead, 16 of which is used to detect duplicate packets and 6 to 17 of which is used for message length.
		UNRELIABLE,

		/// Regular UDP with a sequence counter.  Out of order messages will be discarded.  This adds an additional 13 bits on top what is used for UNRELIABLE.
		UNRELIABLE_SEQUENCED,

		/// The message is sent reliably, but not necessarily in any order.  Same overhead as UNRELIABLE.
		RELIABLE,

		/// This message is reliable and will arrive in the order you sent it.  Messages will be delayed while waiting for out of order messages.  Same overhead as UNRELIABLE_SEQUENCED.
		RELIABLE_ORDERED,

		/// This message is reliable and will arrive in the sequence you sent it.  Out or order messages will be dropped.  Same overhead as UNRELIABLE_SEQUENCED.
		RELIABLE_SEQUENCED,

		/// Same as UNRELIABLE, however the user will get either ID_SND_RECEIPT_ACKED or ID_SND_RECEIPT_LOSS based on the result of sending this message when calling RakPeerInterface::Receive(). Bytes 1-4 will contain the number returned from the Send() function. On disconnect or shutdown, all messages not previously acked should be considered lost.
		UNRELIABLE_WITH_ACK_RECEIPT,

		/// Same as UNRELIABLE_SEQUENCED, however the user will get either ID_SND_RECEIPT_ACKED or ID_SND_RECEIPT_LOSS based on the result of sending this message when calling RakPeerInterface::Receive(). Bytes 1-4 will contain the number returned from the Send() function. On disconnect or shutdown, all messages not previously acked should be considered lost.
		/// 05/04/10 You can't have sequenced and ack receipts, because you don't know if the other system discarded the message, meaning you don't know if the message was processed
		// UNRELIABLE_SEQUENCED_WITH_ACK_RECEIPT,

		/// Same as UNRELIABLE_SEQUENCED. The user will also get ID_SND_RECEIPT_ACKED after the message is delivered when calling RakPeerInterface::Receive(). Bytes 1-4 will contain the number returned from the Send() function. On disconnect or shutdown, all messages not previously acked should be considered lost.
		RELIABLE_WITH_ACK_RECEIPT,

		/// Same as RELIABLE_ORDERED_ACK_RECEIPT. The user will also get ID_SND_RECEIPT_ACKED after the message is delivered when calling RakPeerInterface::Receive(). Bytes 1-4 will contain the number returned from the Send() function. On disconnect or shutdown, all messages not previously acked should be considered lost.
		RELIABLE_ORDERED_WITH_ACK_RECEIPT,

		/// Same as RELIABLE_SEQUENCED. The user will also get ID_SND_RECEIPT_ACKED after the message is delivered when calling RakPeerInterface::Receive(). Bytes 1-4 will contain the number returned from the Send() function. On disconnect or shutdown, all messages not previously acked should be considered lost.
		/// 05/04/10 You can't have sequenced and ack receipts, because you don't know if the other system discarded the message, meaning you don't know if the message was processed
		// RELIABLE_SEQUENCED_WITH_ACK_RECEIPT,

		/// \internal
		NUMBER_OF_RELIABILITIES
	};

    public value class NetId
    {
		int RakSystemIndex;
	public:

		UInt64 Id;
		
		NetId(RakNet::RakNetGUID addr)
		{
			Id = addr.g;
			RakSystemIndex = addr.systemIndex;
		}

		
		RakNet::RakNetGUID GetRakGuid()
		{
			RakNet::RakNetGUID  guid(Id);
			guid.systemIndex = RakSystemIndex;
			return guid;
		}
    };

	public ref class InPacket : IDisposable
    {
		RakNet::BitStream* _stream;
		RakNet::Packet* _packet;
		RakNet::RakPeerInterface* _peer;
    public:
        /// The system that send this packet.
        NetId SenderNetId;
		
		InPacket(RakNet::RakPeerInterface* peer, RakNet::Packet* packet)
		{
			_peer = peer;
			_packet = packet;
			SenderNetId =  NetId(packet->guid);
			_stream = new RakNet::BitStream(packet->data, packet->length, false);
		}

		RakNet::BitStream* GetInternalStream()
		{
			return _stream;
		}

		~InPacket()
		{
			this->!InPacket();
		}

		!InPacket()
		{
			_peer->DeallocatePacket(_packet);
			delete _stream;
		}

		bool ReadBool()
		{
			bool val;
			_stream->Read(val);
			return val;
		}

		unsigned char ReadUByte()
		{
			unsigned char val;
			_stream->Read(val);
			return val;
		}

		char ReadByte()
		{
			char val;
			_stream->Read(val);
			return val;
		}

		unsigned short ReadUInt16()
		{
			unsigned short val;
			_stream->Read(val);
			return val;
		}

		short ReadInt16()
		{
			short val;
			_stream->Read(val);
			return val;
		}

		int ReadInt32()
		{
			int val;
			_stream->Read(val);
			return val;
		}

		unsigned int ReadUInt32()
		{
			unsigned int val;
			_stream->Read(val);
			return val;
		}

		float ReadSingle()
		{
			float val;
			_stream->Read(val);
			return val;
		}

		double ReadDouble()
		{
			double val;
			_stream->Read(val);
			return val;
		}

		Int64 ReadInt64()
		{
			Int64 val;
			_stream->Read(val);
			return val;
		}

		UInt64 ReadUInt64()
		{
			UInt64 val;
			_stream->Read(val);
			return val;
		}

		String^ ReadString()
		{
			RakNet::RakString val;
			_stream->Read(val);
			return gcnew String(val.C_String());
		}
    };

	public ref class OutPacket : IDisposable
	{
		RakNet::BitStream* _stream;
	public :

		OutPacket()
		{
			_stream = new RakNet::BitStream();
		}

		~OutPacket()
		{
			this->!OutPacket();
		}

		!OutPacket()
		{
			if(_stream != nullptr)
				delete _stream;
			_stream = nullptr;
		}

		RakNet::BitStream* GetInternalStream()
		{
			return _stream;
		}

		void WriteBool(bool val)
		{
			_stream->Write(val);
		}

		void WriteUByte(unsigned char val)
		{
			_stream->Write(val);
		}

		void WriteByte(char val)
		{
			_stream->Write(val);
		}

		void WriteUInt16(unsigned short val)
		{
			_stream->Write(val);
		}

		void WriteInt16(short val)
		{
			_stream->Write(val);
		}

		void WriteInt32(int val)
		{
			_stream->Write(val);
		}
		
		void WriteUInt32(unsigned int val)
		{
			_stream->Write(val);
		}

		void WriteSingle(float val)
		{
			_stream->Write(val);
		}

		void WriteDouble(double val)
		{
			_stream->Write(val);
		}

		void WriteInt64(Int64 val)
		{
			_stream->Write(val);
		}

		void WriteUInt64(UInt64 val)
		{
			_stream->Write(val);
		}

		void WriteString(String^ str)
		{
			char* str2 = (char*)(void*)System::Runtime::InteropServices::Marshal::StringToHGlobalAnsi(str);
			_stream->Write(RakNet::RakString(str2));
			System::Runtime::InteropServices::Marshal::FreeHGlobal((System::IntPtr)str2);
			//array<System::Char>^ chars = str->ToCharArray();
			//pin_ptr<Char> pchars = &chars[0];
		}
	};

    public ref class RakPeerInterface 
    {
        RakNet::RakPeerInterface* _rakPeer;

    public:
	    RakPeerInterface();
        ~RakPeerInterface();

        StartupResult Startup(String ^ip, unsigned short port, unsigned short maxConnections, int timeOutMs);

		ConnectionAttemptResult Connect(String ^host, unsigned short remotePort);
		void CloseConnection(NetId netId, bool sendDisconnectionNotification, unsigned char orderingChannel);

        int Send(NetId netId, OutPacket^ packet, MessagePriority priority, MessageReliability reliability, char orderingChannel, bool broadcast);
		InPacket^ Receive();
        
        void Shutdown();
        void SetDisconnectTimeoutFor(int timeMs, NetId netId);

		NetId Id() 
		{
			NetId id(_rakPeer->GetMyGUID());
			return id;
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