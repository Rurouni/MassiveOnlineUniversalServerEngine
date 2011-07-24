using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using System.Runtime.Serialization;

namespace SampleProtocol
{
    [NodeEntityContract]
    public interface ISampleEntity
    {
        [NodeEntityOperation]
        Task<int> Ping(int requestId);

        [NodeEntityOperation(Priority = MessagePriority.High, Reliability = MessageReliability.ReliableOrdered)]
        Task<ComplexData> HeavyFunc(int requestId, ComplexData data, string name, List<ComplexData> datas);

        [NodeEntityOperation(Reliability = MessageReliability.Unreliable)]
        void SimpleOneWay();
    }

    public class ComplexData
    {
        public int SomeInt;
        public ulong SomeULong;
        public string SomeString;
        public List<string> SomeArrString;
        public List<ComplexData> SomeArrRec;
    }

}
