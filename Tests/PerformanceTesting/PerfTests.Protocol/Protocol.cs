using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using MOUSE.Core.Interfaces.Actors;

namespace PerfTests.Protocol
{
    [DataContract]
    public class PingGateway : Message
    {
        public override MessageReliability Reliability => MessageReliability.Reliable;

        [DataMember(Order = 1)]
        public int SleepDurationMs { get; set; }

        [DataMember(Order = 2)]
        public List<int> Data { get; set; }
    }

    [DataContract]
    public class TestStateless : Message
    {
        public override MessageReliability Reliability => MessageReliability.Reliable;

        [DataMember(Order = 1)]
        public int SleepDurationMs { get; set; }

        [DataMember(Order = 2)]
        public List<int> Data { get; set; }
    }

    [DataContract]
    public class TestStatelessUnreliable : Message
    {
        public override MessageReliability Reliability => MessageReliability.Unreliable;

        [DataMember(Order = 1)]
        public int SleepDurationMs { get; set; }

        [DataMember(Order = 2)]
        public List<int> Data { get; set; }
    }

    [DataContract]
    public class TestStateful : Message
    {
        public override MessageReliability Reliability => MessageReliability.Reliable;
    }

    [DataContract]
    public class UseActor : Message
    {
        [DataMember(Order = 1)]
        public string ActorName { get; set; }
    }


    public interface ITestActor : IActor { }
}
