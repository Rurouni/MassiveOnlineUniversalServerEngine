using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using SampleProtocol;

namespace SampleServer
{
    [Export(typeof(NodeEntity))]
    [NodeEntity(typeof(ISampleEntity))]
    public class SampleEntity : NodeEntity, ISampleEntity
    {
        public async Task<int> Ping(int requestId)
        {
            return requestId;
        }


        public async Task<ComplexData> HeavyFunc(int requestId, ComplexData data, string name, List<ComplexData> datas)
        {
            return new ComplexData();
        }

        public void SimpleOneWay()
        {
        }
    }
}
