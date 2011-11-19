using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Reactive.Joins;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using MOUSE.Core;
using NUnit.Framework;
using Protocol.Generated;
using FluentAssertions;
using RakNetWrapper;
using TestDomain;
using Autofac.Integration.Mef;
using System.Reactive;
using System.Reactive.Linq;

namespace Core.Tests
{
    [TestFixture]
    public class NodeBasicNetworkingTests
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(NetNode))));
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(TestMessage))));
            builder.RegisterType<MOUSE.Core.MessageFactory>().As<IMessageFactory>();
            builder.RegisterType<RakPeerInterface>().As<INetProvider>();
            builder.RegisterType<NetNode>().As<INetNode>();
            container = builder.Build();
        }

        [Test]
        public void NodeShouldBeAbleToConnectToOtherNode()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node1 = container.Resolve<INetNode>();
            var node2 = container.Resolve<INetNode>();

            node1.Start(true, endpoint);
            node2.Start(true);

            try
            {
                NetPeer node1ProxyInNode2 = null;
                NetPeer node2ProxyInNode1 = null;
                int node1OnConnectCalls = 0;
                int node2OnConnectCalls = 0;

                node1.NodeConnectedEvent.Subscribe((proxy) =>
                                                    {
                                                        node2ProxyInNode1 = proxy;
                                                        node1OnConnectCalls++;
                                                    });

                node2.NodeConnectedEvent.Subscribe((proxy) =>
                                                    {
                                                        node1ProxyInNode2 = proxy;
                                                        node2OnConnectCalls++;
                                                    });


                Task<NetPeer> connectTask = node2.Connect(endpoint);

                connectTask.IsCompleted.Should().BeFalse();

                Stopwatch timer = Stopwatch.StartNew();
                while ((node1ProxyInNode2 == null || node2ProxyInNode1 == null)
                       && timer.Elapsed < TimeSpan.FromSeconds(3))
                {
                    node1.Update();
                    node2.Update();
                }

                node1OnConnectCalls.Should().Be(1);
                node2OnConnectCalls.Should().Be(1);

                node1ProxyInNode2.Should().NotBeNull();
                node2ProxyInNode1.Should().NotBeNull();

                connectTask.IsCompleted.Should().BeTrue();
                connectTask.Result.Should().Be(node1ProxyInNode2);
            }
            finally
            {
                node1.Stop();
                node2.Stop();
            }
        }

        [Test]
        public void NodeShouldBeAbleToSendAndReceiveMessages()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node1 = container.Resolve<INetNode>();
            var node2 = container.Resolve<INetNode>();

            node1.Start(true, endpoint);
            node2.Start(true);

            NetPeer node1ProxyInNode2 = null;
            NetPeer node2ProxyInNode1 = null;
            Message msg = null;
            NetPeer senderNode = null;
            INetNode receiverNode = null;

            node1.NodeConnectedEvent.Subscribe((proxy) =>
            {
                node2ProxyInNode1 = proxy;
            });

            node2.NodeConnectedEvent.Subscribe((proxy) =>
            {
                node1ProxyInNode2 = proxy;
            });

            node1.NodeMessageEvent.Subscribe((context) =>
            {
                msg = context.Message;
                senderNode = context.Source;
                receiverNode = context.Node;
            });

            try
            {
                Task<NetPeer> connectTask = node2.Connect(endpoint);

                Stopwatch timer = Stopwatch.StartNew();
                while (!connectTask.IsCompleted
                      && timer.Elapsed < TimeSpan.FromSeconds(3))
                {
                    node1.Update();
                    node2.Update();
                }

                connectTask.IsCompleted.Should().BeTrue();

                var sndMsg = node2.MessageFactory.New<TestMessage>();
                sndMsg.Data = 42;
                node1ProxyInNode2.Send(sndMsg);

                timer = Stopwatch.StartNew();
                while (msg == null && timer.Elapsed < TimeSpan.FromSeconds(3))
                {
                    node1.Update();
                    node2.Update();
                }

                node1ProxyInNode2.Should().NotBeNull();
                node2ProxyInNode1.Should().NotBeNull();

                senderNode.Should().Be(node2ProxyInNode1);
                receiverNode.Should().Be(node1);

                msg.Should().NotBeNull();
                msg.Should().BeOfType<TestMessage>();
                (msg as TestMessage).Data.Should().Be(42);

            }
            finally
            {
                node1.Stop();
                node2.Stop();
            }
            
        }

        [Test]
        public void ShouldImmediatelyReturnSameProxyIfAlreadyConnected()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node1 = container.Resolve<INetNode>();
            var node2 = container.Resolve<INetNode>();

            node1.Start(true, endpoint);
            node2.Start(true);

            try
            {
                NetPeer node1ProxyInNode2 = null;
                NetPeer node2ProxyInNode1 = null;

                node1.NodeConnectedEvent.Subscribe((proxy) => node2ProxyInNode1 = proxy);
                node2.NodeConnectedEvent.Subscribe((proxy) => node1ProxyInNode2 = proxy);

                Task<NetPeer> connectTask = node2.Connect(endpoint);

                Stopwatch timer = Stopwatch.StartNew();
                while ((node1ProxyInNode2 == null || node2ProxyInNode1 == null)
                      && timer.Elapsed < TimeSpan.FromSeconds(5))
                {
                    node1.Update();
                    node2.Update();
                }

                //Assert
                Task<NetPeer> connectTask2 = node2.Connect(endpoint);

                connectTask2.IsCompleted.Should().BeTrue();
                connectTask2.Result.Should().Be(node1ProxyInNode2);
            }
            finally
            {
                node1.Stop();
                node2.Stop();
            }
        }

        [Test]
        public void ShouldNotifyOnDisconnect()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node1 = container.Resolve<INetNode>();
            var node2 = container.Resolve<INetNode>();

            node1.Start(true, endpoint);
            node2.Start(true);
            try
            {

                NetPeer node1ProxyInNode2 = null;
                NetPeer node2ProxyInNode1 = null;

                NetPeer disconectedProxy = null;
                int disconnectCalls = 0;

                node1.NodeConnectedEvent.Subscribe((proxy) => node2ProxyInNode1 = proxy);
                node2.NodeConnectedEvent.Subscribe((proxy) => node1ProxyInNode2 = proxy);

                node2.NodeDisconnectedEvent.Subscribe((proxy) =>
                                                   {
                                                       disconectedProxy = proxy;
                                                       disconnectCalls++;
                                                   });

                Task<NetPeer> connectTask = node2.Connect(endpoint);

                Stopwatch timer = Stopwatch.StartNew();
                while ((node1ProxyInNode2 == null || node2ProxyInNode1 == null)
                      && timer.Elapsed < TimeSpan.FromSeconds(3))
                {
                    node1.Update();
                    node2.Update();
                }

                node1.CloseConnection(node2ProxyInNode1);

                timer = Stopwatch.StartNew();
                while (disconnectCalls == 0 && timer.Elapsed < TimeSpan.FromSeconds(3))
                    node2.Update();

                disconectedProxy.Should().Be(node1ProxyInNode2);
                disconnectCalls.Should().Be(1);
            }
            finally
            {
                node1.Stop();
                node2.Stop();
            }
        }
    }
}
