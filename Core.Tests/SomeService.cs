using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using MOUSE.Core.Actors;
using Protocol.Generated;

namespace Core.Tests
{
    public class SomeService : Actor, ISomeService
    {
        public const int SimpleOperationReturned = 42;
        public int SimpleOneWayOperationCalled = 0;
        public int SimpleOperationCalled = 0;

        public Task<int> Simple(int requestId)
        {
            SimpleOperationCalled++;
            return Task.FromResult(SimpleOperationReturned);
        }

        public void SimpleOneWay()
        {
            SimpleOneWayOperationCalled++;
        }

        public Task<ComplexData> Complex(int requestId, ComplexData data, string name, List<SubData> datas)
        {
            return Task.FromResult(
                new ComplexData
                {
                    SomeInt = requestId,
                    SomeULong = 0,
                    SomeString = name,
                    SomeArrString = new List<string> { "Test1", "Test2" },
                    SomeArrRec = datas
                });
        }
    }
}
