﻿using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using MOUSE.Core;
using NSubstitute;
using NUnit.Framework;
using Protocol.Generated;
using TestDomain;
using FluentAssertions;
using Autofac.Integration.Mef;

namespace Core.Tests
{
    [TestFixture]
    public class EntityOperationDispatcherTests
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(TestEntity))));
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ITestEntityProxy))));
            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>();
            container = builder.Build();
        }

        [Test]
        public void ShouldProperlyInvokeRequestReplyMethod()
        {
            #region Arrange
            var domain = container.Resolve<IServiceProtocol>();
            
            var output = new ComplexData(2, 3, "24", new List<string>{"T1","T2"}, new List<ComplexData>());
            var inputMsg = new ITestEntityComplexRequest();
            inputMsg.requestId = 1;
            inputMsg.name = "Some";
            inputMsg.data = output;
            inputMsg.datas = null;
            var tcs = new TaskCompletionSource<ComplexData>();
            tcs.SetResult(output);
            
            int calls = 0;
            var testEntity = Substitute.For<ITestEntity>();
            testEntity.Complex(inputMsg.requestId, inputMsg.data, inputMsg.name, inputMsg.datas)
                .Returns(x=>
                        {
                            calls++;
                            return tcs.Task;
                        });

            #endregion

            //Act
            Task<Message> ret = domain.Dispatch(testEntity, inputMsg);

            //Assert
            ret.Should().NotBeNull();
            ret.Status.Should().Be(TaskStatus.RanToCompletion);
            var replyMsg = ret.Result as ITestEntityComplexReply;
            replyMsg.Should().NotBeNull();
            replyMsg.RetVal.Should().Be(output);
            calls.Should().Be(1);
        }

        [Test]
        public void ShouldProperlyInvokeRobustOneWayMethod()
        {
            #region Arrange
            var domain = container.Resolve<IServiceProtocol>();

            var inputMsg = new ITestEntitySimpleRequest();
            inputMsg.requestId = 42;
            int output = 24;
            var tcs = new TaskCompletionSource<int>();
            tcs.SetResult(output);
            int calls = 0;
            var testEntity = Substitute.For<ITestEntity>();
            testEntity.Simple(inputMsg.requestId)
                .Returns(x =>
                             {
                                 calls++;
                                 return tcs.Task;
                             });

            #endregion

            //Act
            Task<Message> ret = domain.Dispatch(testEntity, inputMsg);

            //Assert
            ret.Should().NotBeNull();
            ret.Status.Should().Be(TaskStatus.RanToCompletion);
            var replyMsg = ret.Result as ITestEntitySimpleReply;
            replyMsg.Should().NotBeNull();
            replyMsg.RetVal.Should().Be(output);
            calls.Should().Be(1);
        }

        [Test]
        public void ShouldProperlyInvokeOneWayMethod()
        {
            #region Arrange
            var domain = container.Resolve<IServiceProtocol>();
            var messageFactory = container.Resolve<IMessageFactory>();

            var node = new Mock<IEntityClusterNode>();
            node.Setup(x => x.Protocol).Returns(domain);
            node.Setup(x => x.MessageFactory).Returns(messageFactory);

            var inputMsg = new ITestEntitySimpleOneWayRequest();

            int calls = 0;
            var testEntity = new Mock<ITestEntity>();
            testEntity.Setup(x => x.SimpleOneWay())
                .Callback(()=>calls++);

            var operationContext = new OperationContext(node.Object, inputMsg, null);
            var nodeEntity = testEntity.As<INodeService>();
            nodeEntity.Setup(x=>x.Context).Returns(operationContext);
            #endregion

            //Act
            Task<Message> ret = domain.Dispatch(nodeEntity.Object, inputMsg);

            //Assert
            ret.Should().NotBeNull();
            ret.Status.Should().Be(TaskStatus.RanToCompletion);
            ret.Result.Should().BeNull();
            calls.Should().Be(1);
        }
    }
}
