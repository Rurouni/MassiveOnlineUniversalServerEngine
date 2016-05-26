using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Lidgren.Network;
using LidgrenWrap;
using MOUSE.Core;
using MOUSE.Core.Configuration;
using MOUSE.Core.Interfaces;
using MOUSE.Core.Interfaces.Configuration;
using MOUSE.Core.Interfaces.Logging;
using MOUSE.Core.Serialization;
using NSubstitute;
using Xunit;

namespace Core.Tests.Integration
{
    public class BasicNetworkingTests
    {
        [Fact]
        public void GivenNobodyListens_WhenConnecting_ThenConnectionWillTimeout()
        {
            var config = new NetNodeConfig() { ConnectTimeoutSec = 1 };
            using (var node = CreateNode("node1", 40001, config))
            {
                node.Start();

                Func<Task<INetChannel>> act = () => node.Connect(new IPEndPoint(IPAddress.Loopback, 50123));

                act.ShouldThrow<ConnectionFailedException>();
            }
        }

        [Fact]
        async public Task GivenNodeListens_WhenConnecting_ThenConnectionShouldSucceed()
        {
            const int connectingPort = 40002;
            var ipEndpoint = new IPEndPoint(IPAddress.Loopback, connectingPort);
            using (var node = CreateNode("node1", 40001))
            using (var otherNode = CreateNode("node2", connectingPort))
            {
                node.Start();
                otherNode.Start();

                //act
                var netPeer = await node.Connect(ipEndpoint);


                //assert
                netPeer.Should().NotBeNull();
                netPeer.TransportChannel.EndPoint.Should().Be(ipEndpoint);
                node.OpenedChannels.Should().Contain(netPeer);
            }
        }

        [Fact]
        async public Task GivenNodeListens_WhenConnecting_ThenShouldRaiseConnectEvent()
        {
            const int connectingPort = 40002;
            var ipEndpoint = new IPEndPoint(IPAddress.Loopback, connectingPort);
            using (var node = CreateNode("node1", 40001))
            using (var otherNode = CreateNode("node2", connectingPort))
            {
                node.Start();
                otherNode.Start();

                //capture first connected channel
                Task<NetChannel> connectedPeerTask = otherNode.ChannelOpenedEvent
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(3))
                    .ToTask();

                //act
                await node.Connect(ipEndpoint);

                //assert
                var connectedPeer = await connectedPeerTask;
                connectedPeer.Should().NotBeNull();
                otherNode.OpenedChannels.Should().Contain(connectedPeer);
            }
        }

        [Fact]
        async public Task GivenNodeIsConnected_WhenSendingMessage_ThenOtherNodeShouldReceiveIt()
        {
            const int connectingPort = 40002;
            const int number = 42;
            var ipEndpoint = new IPEndPoint(IPAddress.Loopback, connectingPort);
            using (var node = CreateNode("node1", 40001))
            using (var otherNode = CreateNode("node2", connectingPort))
            {
                node.Start();
                otherNode.Start();

                //capture first connected channel
                Task<NetChannel> connectedPeerTask = otherNode.ChannelOpenedEvent
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(3))
                    .ToTask();

                var netPeer = await node.Connect(ipEndpoint);

                var connectedPeer = await connectedPeerTask;

                Task<Message> receivedMessageTask = connectedPeer.OperationReceivedEvent
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(300))
                    .Select(x => x.Message)
                    .ToTask();

                //act
                netPeer.Send(new TestRequestMessage() { Number = number });

                var message = await receivedMessageTask;

                message.Should().NotBeNull();
                message.Should().BeOfType<TestRequestMessage>();
                message.As<TestRequestMessage>().Number.Should().Be(number);
            }
        }

        [Fact]
        async public Task GivenNodeIsConnectedAndMessageProcessorIsConfigured_WhenSendingRequest_ThenOtherNodeShouldReceiveRequestProcessItAndSendBackReply()
        {
            const int connectingPort = 40002;
            const int number = 42;
            var ipEndpoint = new IPEndPoint(IPAddress.Loopback, connectingPort);
            using (var node = CreateNode("node1", 40001))
            using (var otherNode = CreateNode("node2", connectingPort))
            {
                node.Start();
                otherNode.Start();

                //capture first connected channel
                Task<NetChannel> connectedPeerTask = otherNode.ChannelOpenedEvent
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(3))
                    .ToTask();

                var netPeer = await node.Connect(ipEndpoint);

                var connectedPeer = await connectedPeerTask;

                using (connectedPeer.OperationReceivedEvent
                    .Where(operation => operation.Message is TestRequestMessage)
                    .Subscribe(operation =>
                        operation.ReplyChannel.Reply(operation,
                            new TestReplyMessage() { Number = ((TestRequestMessage) operation.Message).Number })))
                {
                    //act
                    TestReplyMessage reply = await netPeer.SendRequestAsync<TestReplyMessage>(new TestRequestMessage() { Number = number });

                    //assert
                    reply.Should().NotBeNull();
                    reply.Number.Should().Be(number);
                }
            }
        }

        [Fact]
        async public Task GivenNodeIsConnectedAndMessageProcessorIsNotConfigured_WhenSendingRequest_ThenRequestShouldTimeOut()
        {
            const int connectingPort = 40002;
            const int number = 42;
            var ipEndpoint = new IPEndPoint(IPAddress.Loopback, connectingPort);
            using (var node = CreateNode("node1", 40001))
            using (var otherNode = CreateNode("node2", connectingPort))
            {
                node.Start();
                otherNode.Start();

                var netPeer = await node.Connect(ipEndpoint);

                Func<Task<TestReplyMessage>> act = () => netPeer.SendRequestAsync<TestReplyMessage>(new TestRequestMessage() { Number = number });

                act.ShouldThrow<TimeoutException>();
            }
        }

        static NetNode<NetChannel> CreateNode(string nodeName, int port, INetNodeConfig nodeConfig = null)
        {
            var messageSerializer = new ProtobufMessageSerializer(
                new Message[] { new EmptyMessage(), new TestRequestMessage(), new TestReplyMessage() },
                new MessageHeader[] { new OperationHeader() });

            var coreEventLogger = Substitute.For<ICoreEvents>();

            var config = nodeConfig ?? new NetNodeConfig() {ConnectTimeoutSec = 1, SendTimeoutSec = 1};

            return new NetNode<NetChannel>(nodeName,
                new LidgrenNetProvider(
                    new NetPeerConfiguration("Test") { PingInterval = 0.5f, ConnectionTimeout = 1, MaximumConnections = 10, AcceptIncomingConnections = true, Port = port},
                    Substitute.For<ILidgrenEvents>()),
                coreEventLogger, messageSerializer, (node, transport) => new NetChannel(node, transport, messageSerializer, coreEventLogger, config, new WcfBufferPool()),  nodeConfig ?? config);
        }
    }

    [DataContract]
    public class TestRequestMessage : Message
    {
        [DataMember(Order = 1)]
        public int Number;
    }

    [DataContract]
    public class TestReplyMessage : Message
    {
        [DataMember(Order = 1)]
        public int Number;
    }
}
