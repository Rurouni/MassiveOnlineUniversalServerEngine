using System;
using System.ComponentModel.Composition.Hosting;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Autofac;
using NSubstitute;
using MOUSE.Core;
using FluentAssertions;
using Protocol.Generated;
using Xunit;

namespace Core.Tests
{
    public class EntityDomainDescriptionTests
    {
        private const uint ServiceTypeId = 3211691920;

        private ServiceProtocol _protocolDesc;
        private NodeServiceContractDescription _contractDesc;

        public EntityDomainDescriptionTests()
        {
            _protocolDesc = new ServiceProtocol(Substitute.For<IMessageFactory>(),
                new NodeServiceProxy[] { new ISomeServiceProxy() });
            _contractDesc = _protocolDesc.GetDescription(ServiceTypeId);
        }

        [Fact]
        public void TypeIdOfITestEntityShouldBeConstant()
        {
            uint typeId = _protocolDesc.GetContractId(typeof(ISomeService));
            typeId.Should().Be(ServiceTypeId);
        }

        [Fact]
        public void ShouldHaveValidEntityDescriptionForITestEntity()
        {
            _contractDesc.Should().NotBeNull();
            _contractDesc.AllowExternalConnections.Should().BeFalse();
            _contractDesc.ContractType.Should().Be(typeof(ISomeService));
            _contractDesc.ProxyType.Should().Be(typeof(ISomeServiceProxy));
            _contractDesc.TypeId.Should().Be(ServiceTypeId);
            _contractDesc.Attribute.Should().Be(typeof(ISomeService).GetAttribute<NetContractAttribute>());
        }

        [Fact]
        public void EntityDescriptionForITestEntityShouldHaveValidOperations()
        {
            _contractDesc.Operations.Should().HaveCount(3);
            _contractDesc.Operations[0].Name.Should().Be("Simple");
            _contractDesc.Operations[0].Dispatch.Should().NotBeNull();
            _contractDesc.Operations[0].RequestMessageId.Should().Be(774712997);
            _contractDesc.Operations[0].ReplyMessageId.Should().NotBeNull();
            _contractDesc.Operations[0].ReplyMessageId.Value.Should().Be(836643916);

        }
    }
}
