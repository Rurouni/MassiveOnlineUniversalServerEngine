using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Composition;
using MOUSE.Core;
using System.Threading.Tasks;
using System.Runtime.Serialization;

namespace TestDomain
{
    [NetContract]
    public interface ISomeService
    {
        [NetOperation]
        int Simple(int requestId);

        [NetOperation(Lock = LockType.None, Reliability = MessageReliability.Unreliable)]
        void SimpleOneWay();

        [NetOperation(Lock = LockType.Write, Priority = MessagePriority.High, Reliability = MessageReliability.ReliableOrdered)]
        ComplexData Complex(int requestId, ComplexData data, string name, List<SubData> datas);

    }

    [DataContract]
    public class SubData
    {
        [DataMember]
        public int SomeInt1;
        [DataMember]
        public int SomeInt2;
    }
    
    [DataContract]
    public class ComplexData
    {
        [DataMember]
        public byte SomeByte;
        [DataMember]
        public short SomeShort;
        [DataMember]
        public ushort SomeUShort;
        [DataMember]
        public int SomeInt;
        [DataMember]
        public uint SomeUInt;
        [DataMember]
        public long SomeLong;
        [DataMember]
        public ulong SomeULong;
        [DataMember]
        public string SomeString;
        [DataMember]
        public List<string> SomeArrString;
        [DataMember]
        public List<SubData> SomeArrRec;
    }
}
