using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Autofac;
using Autofac.Integration.Mef;
using TestDomain;
using Protocol.Generated;
using System.Reflection;
using System.ComponentModel.Composition.Hosting;
using MOUSE.Core;
using RakNetWrapper;
using System.Threading.Tasks;
using System.Diagnostics;
using FluentAssertions;
using System.Net;

namespace Core.Tests
{
    [TestFixture]
    class EntityUsageTests
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(TestEntity))));
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ITestEntityProxy))));
            builder.RegisterType<EntityDomain>().As<IEntityDomain>();
            builder.RegisterType<EntityRepository>().As<IEntityRepository>();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>();
            builder.RegisterType<RakPeerInterface>().As<INetPeer>();
            builder.RegisterType<EntityClusterNode>().As<IEntityClusterNode>();
            builder.RegisterType<NullPersistanceProvider>().As<IPersistanceProvider>();
            container = builder.Build();
        }

        [Test]
        public void SingleNodeNonActivatedEntityTest()
        {
            var endpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            var node = container.Resolve<IEntityClusterNode>();
            
            node.Start(true, endpoint);

            try
            {
                var testEntityProxy = node.GetProxy<ITestEntity>();

                // because this test is executed in test runner as one thread we cant simply use await keyword
                // but in real usage it should look as simple as:
                // int retVal = await testEntityProxy.Simple(42);

                Task<int> retTask = testEntityProxy.Simple(42);

                Stopwatch timer = Stopwatch.StartNew();
                while (!retTask.IsCompleted
                      && timer.Elapsed < TimeSpan.FromSeconds(3))
                {
                    node.Update();
                }


                retTask.IsCompleted.Should().BeTrue();
                retTask.Result.Should().Be(42);
            }
            finally
            {
                node.Stop();
            }
        }

        
    }
}
