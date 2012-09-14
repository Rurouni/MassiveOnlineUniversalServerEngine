using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MOUSE.Core;
using Protocol.Generated;
using FluentAssertions;
using Xunit;
using Ploeh.AutoFixture;
using NSubstitute;

namespace Core.Tests
{
    public class EntityProxyTests
    {
        [Fact]
        public void EntityProxyShouldTranslateMethodCallToMessage()
        {
            Fixture fixture = new Fixture();
            IMessageFactory msgFactory = new MessageFactory(new List<Message>() { new ISomeServiceSimpleRequest(), new ISomeServiceSimpleReply() });
            int input = fixture.CreateAnonymous<int>();
            int output = fixture.CreateAnonymous<int>();
            var proxy = new ISomeServiceProxy();
            var dispatcher = Substitute.For<IServiceOperationDispatcher>();
            dispatcher.ExecuteServiceOperation(Arg.Is<Message>(x => ((ISomeServiceSimpleRequest)x).requestId == input))
                .Returns(x => Task.FromResult<Message>(new ISomeServiceSimpleReply() { RetVal = output }));
            proxy.Init(new NodeServiceKey(0, 0), null, msgFactory, dispatcher);

            Task<int> retTask = proxy.Simple(input);

            dispatcher.Received(1).ExecuteServiceOperation(Arg.Is<Message>(x => ((ISomeServiceSimpleRequest)x).requestId == input));
            retTask.Should().NotBeNull();
            retTask.Result.Should().Be(output);
        }
    }
}
