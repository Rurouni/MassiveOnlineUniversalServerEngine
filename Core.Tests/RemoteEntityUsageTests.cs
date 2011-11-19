using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Autofac;
using NUnit.Framework;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using Autofac.Integration.Mef;
using MOUSE.Core;
using Protocol.Generated;
using RakNetWrapper;
using TestDomain;
using FluentAssertions;

namespace Core.Tests
{
    [TestFixture]
    class RemoteEntityUsageTests
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
            builder.RegisterType<EntityClusterNode>().As<INetNode>();
            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>();
            container = builder.Build();
        }

        [Test]
        public void OperationOnLocallyAvailableEntityShouldGoWithoutNetwork()
        {
            var node = container.Resolve<INetNode>();
            node.Start(true);
            //ITestEntity entity = node.Create<ITestEntity>(42);
            var proxy = node.Get<ITestEntity>(42);

            var retTask = proxy.Simple(1123);

            Stopwatch timer = Stopwatch.StartNew();
            while ( !retTask.IsCompleted && timer.Elapsed < TimeSpan.FromSeconds(3))
                node.Update();

            retTask.IsCompleted.Should().BeTrue();
            //entity.SimpleOperationCalled.Should().Be(1);
            retTask.Result.Should().Be(TestEntity.SimpleOperationReturned);

        }
    }
}
