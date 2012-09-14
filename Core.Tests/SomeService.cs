using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using Protocol.Generated;

namespace Core.Tests
{
    [NodeService(AutoCreate = true)]
    public class SomeService : NodeService, ISomeService
    {
        public const int SimpleOperationReturned = 42;
        public int SimpleOneWayOperationCalled = 0;
        public int SimpleOperationCalled = 0;

        public async Task<int> Simple(int requestId)
        {
            SimpleOperationCalled++;
            return SimpleOperationReturned;
        }

        public void SimpleOneWay()
        {
            SimpleOneWayOperationCalled++;
        }

        public async Task<ComplexData> Complex(int requestId, ComplexData data, string name, List<SubData> datas)
        {
            return new ComplexData
            {
                SomeInt = requestId,
                SomeULong = 0,
                SomeString = name,
                SomeArrString = new List<string> { "Test1", "Test2" },
                SomeArrRec = datas
            };
        }
    }
}
