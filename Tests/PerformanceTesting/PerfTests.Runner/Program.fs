namespace PerfTests.Runner

module Program =  

    open MOUSE.Core.Configuration
    open LidgrenWrap
    open MOUSE.Core.Serialization
    open MOUSE.Core.Logging.Serilog
    open MOUSE.Core
    open Lidgren.Network
    open FSharp.Configuration
    open PerfTests.Protocol
    open Serilog
    open MBrace
    open MBrace.Core
    open MBrace.Azure
    open MBrace.Runtime
    open System
    open System.Threading.Tasks
    open Argu
    open Configuration
    open System.Threading
    open MOUSE.Core.Fibers
    open FSharpx.Task

    type UserScenario<'a> = string*ClientNode*ICloudCancellationToken -> Task<'a>

    let task = TaskBuilder()    

    let statelessLoopUserScenario(userId, node:ClientNode, cancellationToken: ICloudCancellationToken, batchSize, sleepMS:int) = task {
        let! _ = node.SendRequestAsync<OperationResult>(new UseActor(ActorName = userId))

        while (not cancellationToken.IsCancellationRequested) do 
            let!_ = seq { for i in 1 .. batchSize do yield node.SendRequestAsync<OperationResult>(new TestStateless()) } |> Seq.toArray |> Task.WhenAll
            //let! _ = Task.Delay(sleepMS).ContinueWith(fun _ -> Task.FromResult(0))
            ()
    }

    let statefullLoopUserScenario(userId, node:ClientNode, cancellationToken: ICloudCancellationToken, batchSize, sleepMS:int) = task {
        let! _ = node.SendRequestAsync<OperationResult>(new UseActor(ActorName = userId))

        while (not cancellationToken.IsCancellationRequested) do 
            let!_ = seq { for i in 1 .. batchSize do yield node.SendRequestAsync<OperationResult>(new TestStateful()) } |> Seq.toArray |> Task.WhenAll
            ()
    }

    let pingGatewayWithDelayLoopUserScenario(userId, node:ClientNode, cancellationToken: ICloudCancellationToken, batchSize, sleepMS:int) = task {
        let! _ = node.SendRequestAsync<OperationResult>(new UseActor(ActorName = userId))

        while (not cancellationToken.IsCancellationRequested) do 
            seq { for i in 1 .. batchSize do yield node.SendOneWay(new PingGateway()) } |> Seq.toArray |> ignore
            let! _ = Task.Delay(sleepMS).ContinueWith(fun _ -> Task.FromResult(0))
            ()
    }

    let runUserPack(userCount, gatewayEndpoint, cancellationToken:ICloudCancellationToken, userScenario:UserScenario<'a>) = local {
        let logger = LoggerConfiguration()
                        .ConfigureMOUSETypesDestructure()
                        .MinimumLevel.Warning()
                        .WriteTo.Trace()
                        .CreateLogger()

        let messageSerializer = new ProtobufMessageSerializer(typeof<Message>.Assembly, typeof<TestStateless>.Assembly)
            
        let coreEventLogger = new SerilogCoreEvents(logger)
        let lidgrenLogger = new SerilogLidgrenEvents(logger)
        let netPeerConfig = NetPeerConfiguration("PublicNet")
        netPeerConfig.ConnectionTimeout <- 10.0f

        let nodeConfig = NetNodeConfig( SendTimeoutSec = 10, ConnectTimeoutSec = 10, ManualUpdateOnly = true )
        let globalManualFiber = new PollingFiber()
        let nodeFactory() = 
            let transport = new LidgrenNetProvider(netPeerConfig, lidgrenLogger)
            let node = new ClientNode( [ gatewayEndpoint ], transport, coreEventLogger, messageSerializer, (fun _ -> ()), 
                            fiber = globalManualFiber, config= nodeConfig)
            do node.Start()
            node

                
        let userIds = seq { for i in 1 .. userCount do yield Guid.NewGuid().ToString() }
        let concurrentRuns = userIds |> Seq.map (fun userId -> 
                                                    let node = nodeFactory()
                                                    (node, userScenario(userId, node, cancellationToken))) |> Array.ofSeq
        let nodes = concurrentRuns |> Array.map (fun (node, _) -> node)
        let allNodesResource = { new IDisposable with
                                    member x.Dispose() = nodes |> Array.iter (fun n -> n.Dispose())  }

        let userFuncs = concurrentRuns |> Array.map (fun (_, userFunc) -> userFunc)
        
        let updateAllNodesWhilstNotCancelled() = 
            while (not cancellationToken.IsCancellationRequested) do 
                try
                    nodes |> Array.iter (fun n -> n.Update())
                    globalManualFiber.ExecuteAllInplace()
                    Thread.Sleep(1)
                with 
                | ex -> printfn "Update loop exception: %s" (ex.ToString())
            allNodesResource.Dispose()
        
        let updateTask = Task.Run(updateAllNodesWhilstNotCancelled)

        let! results = Cloud.AwaitTask (userFuncs |> Task.WhenAll)

        
        return results
    }

    [<EntryPoint>]
    let Main(args) =
        let parser = ArgumentParser.Create<CLIArguments>()
        let results = parser.Parse(args)
        
        if results.IsUsageRequested then
            printfn "%s" (results.Usage())
            0
        else
            ThreadPool.SetMaxThreads(20,10)
            let runLocally = results.Contains <@ Run_Locally @>
            let mbraceStorageConnectionString = results.GetResult <@MBrace_StorageConnection@>
            let mbraceServiceBusConnectionString = results.GetResult <@MBrace_ServiceBusConnection@>
            let userCountPerVM = results.GetResult <@User_Count@>
            let testDuration = results.GetResult <@Test_Duration@>
            let gatewayEndpoint = results.GetResult <@Gateway_Endpoint@>
            let batchSize = results.GetResult <@Messages_In_Batch@>
            let sleepMS = results.GetResult <@Sleep_Duration@>
        
            let config = Configuration(mbraceStorageConnectionString, mbraceServiceBusConnectionString)

            let userScenario = (fun (userId,node,cT) -> statefullLoopUserScenario(userId, node, cT, batchSize, sleepMS))
        
            let perfTest = cloud {
                let! ct = Cloud.CreateCancellationTokenSource()
                async {
                    do! Async.Sleep(testDuration*1000)
                    do ct.Cancel()
                } |> Async.StartAsTask |> ignore            
                let! results = Cloud.ParallelEverywhere (runUserPack(userCountPerVM, gatewayEndpoint, ct.Token, userScenario))
                ()
            }

            let cluster = AzureCluster.Connect(config)
            cluster.ShowWorkers()
            cluster.ShowProcesses()
            cluster.ClearAllProcesses()

            let cancellationTokenSource = cluster.CreateCancellationTokenSource()
//            async {
//                do! Async.Sleep(testDuration*1000+60000) //kill test in a minute after normal stop time
//                do cancellationTokenSource.Cancel()
//            } |> Async.StartAsTask |> ignore

            try
                let result = 
                    if runLocally then
                        cluster.RunLocally(perfTest, cancellationTokenSource.Token, MemoryEmulation.Copied)
                    else 
                        let cloudProcess = cluster.CreateProcess(perfTest, cancellationTokenSource.Token)
                        while (cloudProcess.Status = CloudProcessStatus.Running || cloudProcess.Status = CloudProcessStatus.WaitingToRun || cloudProcess.Status = CloudProcessStatus.Created)  do
                            Console.Clear()
                            cluster.ShowWorkers()
                            Thread.Sleep(3000)
                         
                printfn "Done"
            with
                | :? System.OperationCanceledException -> printfn "Cancelled as expected"
                | ex -> printfn "Unknown exception: %s" (ex.ToString())
            0
