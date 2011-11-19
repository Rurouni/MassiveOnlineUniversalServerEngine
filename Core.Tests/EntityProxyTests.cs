using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using Moq;
using MOUSE.Core;
using NUnit.Framework;
using Autofac.Integration.Mef;
using Protocol.Generated;
using RakNetWrapper;
using TestDomain;
using FluentAssertions;

namespace Core.Tests
{
    [TestFixture]
    public class EntityProxyTests
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(TestEntity))));
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ITestEntityProxy))));
            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>();
            builder.RegisterType<ServiceRepository>().As<IServiceRepository>();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>();
            builder.RegisterType<RakPeerInterface>().As<INetProvider>();
            builder.RegisterType<EntityClusterNode>().As<IEntityClusterNode>();
            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>();
            container = builder.Build();
        }

        [Test]
        public void NodeShouldReturnValidProxyForRegisteredTypes()
        {
            var node = container.Resolve<IEntityClusterNode>();
            var entityProxy = node.Get<ITestEntity>(42);

            entityProxy.Should().NotBeNull();
            entityProxy.GetType().Should().Be(typeof(ITestEntityProxy));
        }

        [Test]
        public void NodeShouldReturnSameProxyForAllSubsequentCallsWithSameId()
        {
            var node = container.Resolve<IEntityClusterNode>();

            var entityProxy1 = node.Get<ITestEntity>(42);
            var entityProxy2 = node.Get<ITestEntity>(42);
            var entityProxy3 = node.Get<ITestEntity>(43);

            entityProxy1.Should().Be(entityProxy2);
            entityProxy1.Should().NotBe(entityProxy3);
        }

        [Test]
        public void EntityProxyShouldTranslateMethodCallToMessage()
        {
            var node = container.Resolve<IEntityClusterNode>();
            
            int input = 1123;
            int output = 11235;
            int calls = 0;
            ITestEntitySimpleRequest requestMsg = null;
            
            var entityProxy = node.Get<ITestEntity>(42);

            var mockedNode = new Mock<IEntityClusterNode>();
            mockedNode.Setup(x => x.MessageFactory).Returns(node.MessageFactory);
            mockedNode.Setup(x => x.Execute(It.IsAny<Message>(), ((NodeServiceProxy)entityProxy)))
                .Returns(() =>
                         {
                             var tcs = new TaskCompletionSource<Message>();
                             tcs.SetResult(new ITestEntitySimpleReply() { RetVal = output });
                             return tcs.Task;
                         })
                .Callback((Message msg, NodeServiceProxy proxy) =>
                        {
                            requestMsg = msg as ITestEntitySimpleRequest;
                            calls++;
                        });


            ((NodeServiceProxy)entityProxy).Node = mockedNode.Object;

            Task<int> retTask = entityProxy.Simple(input);

            calls.Should().Be(1);
            retTask.Result.Should().Be(output);
            requestMsg.Should().NotBeNull();
            requestMsg.requestId.Should().Be(input);
        }
    }
}
