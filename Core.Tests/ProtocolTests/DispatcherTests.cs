using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MOUSE.Core;
using NSubstitute;
using Protocol.Generated;
using FluentAssertions;
using Xunit;
using Ploeh.AutoFixture;

namespace Core.Tests
{
    public class EntityOperationDispatcherTests
    {
        private ServiceProtocol _protocolDesc;

        public EntityOperationDispatcherTests()
        {
            _protocolDesc = new ServiceProtocol(Substitute.For<IMessageFactory>(),
                new NodeServiceProxy[] { new ISomeServiceProxy() });
        }

        [Fact]
        public void ShouldProperlyInvokeRequestReplyMethod()
        {
            #region Arrange
            var fixture = new Fixture();
            fixture.Customize<ISomeServiceComplexRequest>(ob =>
                ob.With(x => x.datas,
                    fixture.CreateMany<SubData>().ToList()));
            fixture.Customize<ComplexData>(ob => ob
                .With(x => x.SomeArrString, fixture.CreateMany<string>().ToList())
                .With(x => x.SomeArrRec, fixture.CreateMany<SubData>().ToList()));

            ISomeServiceComplexRequest inputMsg = fixture.CreateAnonymous<ISomeServiceComplexRequest>();
            var output = inputMsg.data;
            
            var tcs = new TaskCompletionSource<ComplexData>();
            tcs.SetResult(output);

            var service = Substitute.For<ISomeService>();
            service.Complex(inputMsg.requestId, inputMsg.data, inputMsg.name, inputMsg.datas)
                .Returns(x => tcs.Task);

            #endregion

            //Act
            Task<Message> ret = _protocolDesc.Dispatch(service, inputMsg);

            //Assert
            service.Received(1).Complex(inputMsg.requestId, inputMsg.data, inputMsg.name, inputMsg.datas);
            ret.Should().NotBeNull();
            ret.Status.Should().Be(TaskStatus.RanToCompletion);
            var replyMsg = ret.Result as ISomeServiceComplexReply;
            replyMsg.Should().NotBeNull();
            replyMsg.RetVal.Should().Be(output);
            
        }

        [Fact]
        public void ShouldProperlyInvokeRobustOneWayMethod()
        {
            #region Arrange
            var fixture = new Fixture();
            ISomeServiceSimpleRequest inputMsg = fixture.CreateAnonymous<ISomeServiceSimpleRequest>();
            int output = inputMsg.requestId;

            var tcs = new TaskCompletionSource<int>();
            tcs.SetResult(output);

            var service = Substitute.For<ISomeService>();
            service.Simple(inputMsg.requestId)
                .Returns(x => tcs.Task);

            #endregion

            //Act
            Task<Message> ret = _protocolDesc.Dispatch(service, inputMsg);

            //Assert
            service.Received(1).Simple(inputMsg.requestId);
            ret.Should().NotBeNull();
            ret.Status.Should().Be(TaskStatus.RanToCompletion);
            var replyMsg = ret.Result as ISomeServiceSimpleReply;
            replyMsg.Should().NotBeNull();
            replyMsg.RetVal.Should().Be(output);
        }

        [Fact]
        public void ShouldProperlyInvokeOneWayMethod()
        {
            var inputMsg = new ISomeServiceSimpleOneWayRequest();
            var service = Substitute.For<ISomeService>();

            //Act
            Task<Message> ret = _protocolDesc.Dispatch(service, inputMsg);

            //Assert
            service.Received(1).SimpleOneWay();
            ret.Should().NotBeNull();
            ret.Status.Should().Be(TaskStatus.RanToCompletion);
            ret.Result.Should().BeNull();
        }
    }
}
