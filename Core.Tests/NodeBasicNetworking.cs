using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Text;
using Autofac;
using MOUSE.Core;
using NUnit.Framework;
using Protocol.Generated;
using FluentAssertions;
using RakNetWrapper;
using TestDomain;
using Autofac.Integration.Mef;

namespace Core.Tests
{
    [TestFixture]
    public class NodeBasicNetworking
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ITestEntityProxy))));
            builder.RegisterType<MOUSE.Core.MessageFactory>().As<IMessageFactory>();
            builder.RegisterType<RakPeerInterface>().As<INetPeer>();
            container = builder.Build();
        }

        public void NodeShouldBeAbleToSendMessagesToOtherNode()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            //var node1 = new Node()
        }
    }
}
