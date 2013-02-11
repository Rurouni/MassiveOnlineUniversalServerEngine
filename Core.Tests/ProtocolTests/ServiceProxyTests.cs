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
            var fixture = new Fixture();
            IMessageFactory msgFactory = new MessageFactory(new List<Message>() { new ISomeServiceSimpleRequest(), new ISomeServiceSimpleReply() });
            int input = fixture.CreateAnonymous<int>();
            int output = fixture.CreateAnonymous<int>();
            var proxy = new ISomeServiceProxy();
            var executor = Substitute.For<IOperationExecutor>();
            executor.ExecuteOperation(Arg.Is<OperationContext>(x => ((ISomeServiceSimpleRequest)x.Message).requestId == input))
                .Returns(x => Task.FromResult<Message>(new ISomeServiceSimpleReply() { RetVal = output }));
            proxy.Init(null, msgFactory, executor);

            Task<int> retTask = proxy.Simple(input);

            executor.Received(1).ExecuteOperation(Arg.Is<OperationContext>(x => ((ISomeServiceSimpleRequest)x.Message).requestId == input));
            retTask.Should().NotBeNull();
            retTask.Result.Should().Be(output);
        }
    }
}
