using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using Orleans;
using Orleans.CodeGeneration;
using PerfTests.Protocol;

namespace OrleansTestActor.Interface
{
    public interface ITestGrain : IGrainWithStringKey
    {
        Task<OperationResult> TestStateless(TestStateless msg);
        Task<OperationResult> TestStateful(TestStateful msg);
    }
}
