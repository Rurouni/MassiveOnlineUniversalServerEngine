using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using MOUSE.Core;
using NUnit.Framework;
using FluentAssertions;
using Protocol.Generated;
using TestDomain;
using Autofac.Integration.Mef;

namespace Core.Tests
{
    [TestFixture]
    public class EntityDomain
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(TestEntity))));
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ITestEntityProxy))));
            builder.RegisterType<MOUSE.Core.EntityDomain>().As<IEntityDomain>();
            container = builder.Build();    
        }

        [Test]
        public void TypeIdOfITestEntityShouldBeConstant()
        {
            var domain = container.Resolve<IEntityDomain>();

            uint typeId = domain.GetTypeId(typeof(ITestEntity));
            typeId.Should().Be(1996445736);
        }

        [Test]
        public void ShouldHaveValidEntityDescriptionForITestEntity()
        {
            var domain = container.Resolve<IEntityDomain>();

            NodeEntityContractDescription desc = domain.GetDescription(1996445736);

            desc.Should().NotBeNull();
            desc.Connectionfull.Should().BeFalse();
            desc.ContractType.Should().Be(typeof (ITestEntity));
            desc.ProxyType.Should().Be(typeof (ITestEntityProxy));
            desc.TypeId.Should().Be(1996445736);
            desc.Attribute.Should().Be(typeof(ITestEntity).GetAttribute<NodeEntityContractAttribute>());
        }

        [Test]
        public void EntityDescriptionForITestEntityShouldHaveValidOperations()
        {
            var domain = container.Resolve<IEntityDomain>();

            NodeEntityContractDescription desc = domain.GetDescription(1996445736);

            desc.Operations.Should().HaveCount(3);
            desc.Operations[0].Name.Should().Be("Simple");
            desc.Operations[0].Dispatch.Should().NotBeNull();
            desc.Operations[0].RequestMessageId.Should().Be(2653326779);
            desc.Operations[0].ReplyMessageId.Should().NotBeNull();
            desc.Operations[0].ReplyMessageId.Value.Should().Be(1194432348);

        }
    }
}
