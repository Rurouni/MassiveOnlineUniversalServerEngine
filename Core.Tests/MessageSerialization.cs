using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using Autofac;
using MOUSE.Core;
using NUnit.Framework;
using Protocol.Generated;
using FluentAssertions;
using TestDomain;
using Autofac.Integration.Mef;

namespace Core.Tests
{
    [TestFixture]
    public class MessageSerialization
    {
        private IContainer container;

        [SetUp]
        public void Init()
        {
            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(ITestEntityProxy))));
            builder.RegisterType<MOUSE.Core.MessageFactory>().As<IMessageFactory>();
            container = builder.Build();
        }

        [Test]
        public void MessageShouldBeSameAfterSerializationAndDeserialization()
        {
            NativeWriter writer = new NativeWriter();
            NativeReader reader = new NativeReader();
            
            var messageFactory = container.Resolve<IMessageFactory>();

            var data = new ComplexData(2, 3, "24",
                new List<string> { "T1", "T2" },
                new List<ComplexData>
                    {
                        new ComplexData(5,6,"33453", null, null),
                        new ComplexData(56,0, null, null, null)
                    });
            var dataList = new List<ComplexData> { data, data };
            var msg1 = new ITestEntityComplexRequest();
            msg1.requestId = 1;
            msg1.name = "Some";
            msg1.data = data;
            msg1.datas = dataList;

            msg1.Serialize(writer);
            reader.SetBuffer(writer.Buff, 0);
            Message retMsg = messageFactory.Deserialize(reader);

            var msg2 = retMsg as ITestEntityComplexRequest;
            msg2.Should().NotBeNull();
            msg2.ShouldHave().AllProperties().EqualTo(msg1);
            msg2.Priority.Should().Be(msg1.Priority);
            msg2.Id.Should().Be(msg1.Id);
            msg2.Reliability.Should().Be(msg1.Reliability);
            msg2.requestId.Should().Be(msg1.requestId);
            msg2.name.Should().Be(msg1.name);
            msg2.data.Should().Be(msg1.data);
            msg2.datas.Should().Equal(msg1.datas);


        }
    }
}
