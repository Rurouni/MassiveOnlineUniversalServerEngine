using System;
using System.Collections.Generic;
using System.ComponentModel.Composition.Hosting;
using System.Linq;
using System.Reflection;
using System.Text;
using MOUSE.Core;
using FluentAssertions;
using Xunit;
using Xunit.Extensions;
using Ploeh.AutoFixture.Xunit;
using Protocol.Generated;
using Autofac;
using Ploeh.SemanticComparison.Fluent;
using Ploeh.AutoFixture;
using KellermanSoftware.CompareNetObjects;
using System.IO;

namespace Core.Tests
{
    public class MessageSerializationTests
    {
        [Fact]
        public void MessageShouldBeSameAfterSerializationAndDeserialization()
        {
            BinaryWriter writer = new BinaryWriter(new MemoryStream());
            IMessageFactory msgFactory = new MessageFactory(new Message[]
                {
                    new ISomeServiceComplexRequest()
                });

            var fixture = new Fixture();
            fixture.Customize<ISomeServiceComplexRequest>(ob =>
                ob.With(x => x.datas,
                    fixture.CreateMany<SubData>().ToList()));
            fixture.Customize<ComplexData>(ob => ob
                .With(x => x.SomeArrString, fixture.CreateMany<string>().ToList())
                .With(x => x.SomeArrRec, fixture.CreateMany<SubData>().ToList()));

            ISomeServiceComplexRequest msg = fixture.CreateAnonymous<ISomeServiceComplexRequest>();

            //serialize and deserialize msg1
            msg.Serialize(writer);
            writer.Seek(0, SeekOrigin.Begin);
            BinaryReader reader = new BinaryReader(writer.BaseStream);
            Message retMsg = msgFactory.Deserialize(reader);

            retMsg.Should().BeOfType<ISomeServiceComplexRequest>();
            msg.ShouldBeEqualTo((ISomeServiceComplexRequest)retMsg);
        }
    }

    public static class TestExtension
    {
        public static void ShouldBeEqualTo(this ISomeServiceComplexRequest m1, ISomeServiceComplexRequest m2)
        {
            CompareObjects comparer = new CompareObjects();
            if(!comparer.Compare(m1, m2))
            {
                throw new Exception("Messages are not equal, Details:" + comparer.DifferencesString);
            }
        }
    }
}
