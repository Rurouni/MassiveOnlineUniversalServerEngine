namespace PerfTests.Runner

open Xunit
open MOUSE.Core
open MOUSE.Core.Serialization
open MOUSE.Core.Configuration
open PerfTests.Protocol
open Serilog
open MOUSE.Core.Logging.Serilog
open LidgrenWrap
open Lidgren.Network
open MOUSE.Core.Interfaces.MessageProcessing
open Swensen.Unquote
open Xunit.Abstractions
open Core.Tests.Integration
open System
open Configuration
open Serilog.Context
open System.Runtime.Remoting.Messaging
open Core.Tests.Xunit
open System.IO
open MBrace.Core
open MBrace.Runtime
open MBrace.Azure
open System.Threading

type UserScenarios(output: ITestOutputHelper)  = 
        
    [<Fact>]
    member this.``Single client should be able to reach service ``() =
        let logger = LoggerConfiguration()
                        .ConfigureMOUSETypesDestructure()
                        .MinimumLevel.Verbose()
                        .WriteTo.XunitTestOutput(output)
                        .CreateLogger()

        let messageSerializer = new ProtobufMessageSerializer(typeof<Message>.Assembly, typeof<TestStateless>.Assembly)
            
        let coreEventLogger = SerilogCoreEvents(logger)
        let netPeerConfig = NetPeerConfiguration("PublicNet")
        netPeerConfig.ConnectionTimeout <- 5.0f
        let transport = new LidgrenNetProvider(netPeerConfig, SerilogLidgrenEvents(logger))

        let config = NetNodeConfig( SendTimeoutSec = 5, ConnectTimeoutSec = 5 )
        let node = 
            new ClientNode( [ Settings.GatewayEndpoint (*"127.0.0.1:20001" "rurounisfcluster.westeurope.cloudapp.azure.com:20001"*) ], 
                transport, coreEventLogger, messageSerializer, (fun _ -> ()), config= config)

        do node.Start()

        do node.SendRequestAsync<OperationResult>(new UseActor(ActorName = "1")).Wait()
        let result = node.SendRequestAsync<OperationResult>(new TestStateless()).Result
        test <@ not (result = null) && result.IsSuccess @>
       
    