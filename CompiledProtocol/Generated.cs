using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using MOUSE.Core;
using SampleProtocol;
using Protocol.Generated;

namespace SampleDomain.Generated
{
    public static class GeneratedDomainDescription
    {
        public static List<NodeEntityDescription> GetEntities()
        {
            return new List<NodeEntityDescription>
            {
                new NodeEntityDescription
                {
                    TypeId = 12345,
                    ProxyType = typeof(ISampleEntityProxy),
                    ContractType = typeof(ISampleEntity),
                    Operations = new List<NodeEntityOperationDescription>
                    {
                        new NodeEntityOperationDescription
                        {
                            Name = "Ping",
                            RequestMessageId = 1234567,
                            ReplyMessageId = 1234568,
                            Dispatch = ISampleEntityProxy.DispatchPing
                        }
                    }
                }
            };
        }
    }
}
