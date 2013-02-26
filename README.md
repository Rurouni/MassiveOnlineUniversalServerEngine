##General Info
This is high level C# server framework for small to large mmo's where you need both reliable and unreliable transport, request/reply and full duplex communications, with the idea that client-server and server-server communication is done
in form of asynchronous RPC calls to actors implementing some protocol contract(attributed C# interface). As low level internal transport currently RakNet, Photon and Lidgren are supported. Idea is that if you split you domain model into actors from beforehand then later you will be able to automatically horizontally scale as actors could be distributed across nodes. But in the beginning you can start small with only one node having all performance you can get from one machine as actors communicating on the same node do not use network just putting messages to each other input queue.

##Features
+ Protocol generation from C# assembly via t4
	+ Request\Reply messages are generated from interface methods
	+ serialization is generated for all messages and any data types defined in protocol assembly
	+ proxy/dispatcher is generated from each interface to allow mapping from method call to message and vice versa
	+ statically defined protocol serialization results in maximum performance
+ several generators present
	+ async: for server side(full 4.5 async/await power)
	+ Unity3d: uses custom Future class and targets .Net 3.5 as Task is not available under Unity
+ High level dispatching core working on top of any transport library (you need only to implement 2 interfaces)
	+ Photon and RakNet and Lidgren already supported
+ Nodes discover each other and join into cluster via INodeCoordinator (default one uses Isis2)
	+ cluster view is same across all nodes and is delivered via virtual synchrony epochs 
+ Actors are automatically distributed and managed across nodes via IActorCoordinator for each group of actors (default one uses Isis2)
+ Actors location is transparent as they are accessed via contracts defined in protocol
	+ Single node server ensures max performance as local actors are communicating directly without network

##Concepts
On high level any project using MOUSE consists from such steps:
+ Protocol:  you define protocol as set of interfaces. Any custom types you want to use in interfaces you define here as attributed POCOs.
+ Client:
	+ You generate final protocol with help of platform specific t4 generator
	+ You initialize ClientNode and connect to some endpoint and receive a channel
	+ You use channel to get typed proxies of defined in protocol interfaces and call async methods on them(implementation of asynchrony would depend on which protocol generator you have used)
	+ you implement callback interfaces if any and register your implementation with channel
+ Server:
	+ You generate final protocol with help of async(Task based) t4 generator
	+ You create inheritor from S2CPeer and register it with ServerNode, it will act as persistent connection to any client that has connected via external endpoint of this ServerNode
	+ You create any amount of classes inherited from Actor depending on your design(those would be bread and butter of your future scalability)
	+ You implement protocol interfaces from generated via t4 file. You have basically 2 choices here
		+ You can implement them directly in your custom C2SPeer class
		+ You can implement them inside one or many of your custom actor classes
	+ For each custom actor type you define coordinator type
    + You call actors from S2C peers or other actors, coordinators deals with locating/creating new actors in clustered setup
    + You call client back via callback interfaces from custom C2SPeer or actors that you marked to be able to receive external connections
	+ You start ServerNode configuring everything including what transport libraries use for internal and external connections
	+ You add/remove nodes with this codebase depending on your load

If you know WCF everything above sounds quite familiar. So you can ask why we need something else. There are several reasons why you would not choose WCF for MMO game:
+ WCF is general and huge, it supports all kinds of enterprise scenarios and you pay in performance for things games never need
+ WCF doesn't have reliable/unreliable UDP transport from a box,  many online games need both reliable and unreliable traffic and easy way of defining which message is reliable and which not.
+ Have fun consuming WCF services from C++ clients or old mono runtimes like Unity3d
+ WCF doesn't help you with coordination or consistent group views or multicasts, maximum scalability that you can get with WCF is when you have setup like this: hardware load balancer in front and set of stateless WCF services with same contract in the back. Have fun implementing open world MMO with such setup.


## Tutorial 
Here is very short quickstart that showcases main points of this framework.
For details you better dive into Sample Project(this is simple chat server with rooms and basic user tracking) with such structure:
* SampleC2SProtocol - protocol assembly for client to server communication
* SampleS2SProtocol - protocol assembly for server to server communication as we don't want client even know about any of this operations
* SampleServerLogic - all logic for our server, all implemented actors and custom peers are here
* SampleServerConsoleHost - console runner for our server logic
* SampleWpfClient - simple wpf client 
You should run SampleConsoleHost to start server and SampleWPFClient to start client.

Let's get started:
#### Protocol
We create new assembly and define some network contract in protocol assembly, it is simple C# interface with methods, methods could use any basic types or types you  defined in same assembly

``` C#
[NetContract]
public interface IChatLogin
{
	[NetOperation(Reliability = MessageReliability.ReliableOrdered)]
	LoginResult Login(string name);
}
```
#### Server
We create console project for server and add there such t4 file (it would generate protocol implementation)

``` C#
<#@ output extension=".cs" #>
<#@ Assembly Name="$(PathToOurProtocolAssemble)\OurProtocolAssembly.dll" #>
<#
	//if we have several protocol assemblies we filter all of them to this list
    List<Assembly> asms = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies()
    	.Where(x => x.GetName().Name.Contains("OurProtocolAssembly")));
    GenerateAsyncProtocol("NamespaceThatWeWant", asms);
#>
<#@ include file="..\Core\DomainGeneration\AsyncProtocol.ttinclude" #>
```
We create inheritor from S2CPeer and implement there interface generated from protocol

``` C#
public class ChatClientPeer : S2CPeer, IChatLogin
{
    public override void OnCreated()
    {
        SetHandler<IChatLogin>(this);
    }

    [NetOperationHandler(Lock = LockType.Write)]
    public async Task<LoginResult> Login(string name)
    {
        //Some logic
        return LoginResult.Ok;
    }
}
```
As you have seen in generated interface all methods are actually returning Task<T> thats because on the server we never want to block and original interfaces
are used only for protocol generation. In the end somebody could use xml or thrift here,
but I like using C# interfaces as they provide clear syntax for grouping methods and defining reqest/reply pairs

In main function on server we start ServerNode with our custom ChatClientPeer.
ServerNode needs to know about all messages, proxies and actors.
Here I am using Autofac to discover all this and register them with server node

``` C#
var builder = new ContainerBuilder();

//register core messages
builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(EmptyMessage)))
    .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
    .As<Message>();

//register domain messages
builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
    .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
    .As<Message>();

//register domain service definitions and proxies
builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
    .Where(x => x.IsAssignableTo<NetProxy>() && x != typeof(NetProxy))
    .As<NetProxy>();

//register actors
builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
    .Where(x => x.IsAssignableTo<Actor>() && x != typeof(Actor))
    .As<Actor>();

builder.RegisterType<OperationDispatcher>().As<IOperationDispatcher>().SingleInstance();
builder.RegisterType<ActorRepository>().As<IActorRepository>().SingleInstance();
builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();

var externalNetConf = new NetPeerConfiguration("ChatApp")
    {
        ConnectionTimeout = 30,
        Port = externalEndpoint.Port,
        LocalAddress = externalEndpoint.Address
    };
var internalNetConf = new NetPeerConfiguration("ChatApp")
{
    ConnectionTimeout = 30,
    Port = internalEndpoint.Port,
    LocalAddress = internalEndpoint.Address
};
var coordinator = new IsisNodeCoordinator();
builder.Register(c => new ServerNode(
        new LidgrenNetProvider(externalNetConf),
        new LidgrenNetProvider(internalNetConf),
        coordinator,
        c.Resolve<IMessageFactory>(), c.Resolve<IOperationDispatcher>(), c.Resolve<IActorRepository>(),
        () => new ChatClientPeer())) //factory method for creating our custom peers for connecting clients
    .As<IServerNode>().SingleInstance();

var container = builder.Build();

var node = container.Resolve<IServerNode>();
node.Start();

// wait for some event to close our app, could be while(true) if you don't care
```

It has a bit of code but it is one time setup as we don't need to touch this if we add new network contracts or actors.
For somebody who is curious what's going on here and why we have so many different types:
LidgrenNetProvider is compatible wrapper on top of Lidgren. We have 2 of thouse as we need 2 seperate network listeners:
one for external connections like clients and one for server nodes communicating to each other.
MessageFactory is responsible for serializing/deserializing and pooling of messages.
OperationDispatcher responsible for converting messages to RPC call and vice versa.
ActorRepository will hold all actors and knows how to factory them.
IsisNodeCoordinator will coordinate joining nodes in case of clustered setup.

#### Client

Now let's create client project and add protocol generation t4 file. Here we use .Net 4.5 for client so we can use same async protocol generator as fro server

``` C#
<#@ output extension=".cs" #>
<#@ Assembly Name="$(PathToOurProtocolAssemble)\OurProtocolAssembly.dll" #>
<#
	//if we have several protocol assemblies we filter all of them to this list
    List<Assembly> asms = new List<Assembly>(AppDomain.CurrentDomain.GetAssemblies()
    	.Where(x => x.GetName().Name.Contains("OurProtocolAssembly")));
    GenerateAsyncProtocol("NamespaceThatWeWant", asms);
#>
<#@ include file="..\Core\DomainGeneration\AsyncProtocol.ttinclude" #>
```
Now we can create ClientNode and register all messages, proxies with it. I am using Autofac to simplify registration

var builder = new ContainerBuilder();
``` C#
//register core messages
builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(EmptyMessage)))
    .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
    .As<Message>();

//register domain messages
builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
    .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
    .As<Message>();

//register domain service definitions and proxies
builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
    .Where(x => x.IsAssignableTo<NetProxy>() && x != typeof(NetProxy))
    .As<NetProxy>();

builder.RegisterType<OperationDispatcher>().As<IOperationDispatcher>().SingleInstance();
builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
var netConf = new NetPeerConfiguration("ChatApp")
{
    ConnectionTimeout = 10000,
};
builder.Register(x => new LidgrenNetProvider(netConf)).As<INetProvider>().SingleInstance();
builder.RegisterType<ClientNode>().As<IClientNode>();
var container = builder.Build();

_node = container.Resolve<IClientNode>();
//start node thread and init network
_node.Start();
```
As you have noted we don't have ActorRepository in ClientNode. This is because client can't work actors directly(like create/remove or discover), 
but it doesn't mean it can't call them. Client still able to call actors via network contract but we will see this in other tutorial that includes actors.

Now we are ready to connect to server and issue some request to it.

``` C#
_mainChannel = await _node.ConnectToServer(endpoint);
var loginService = await _mainChannel.GetProxy<IChatLogin>();

LoginResult result = await loginService.Login(txtUserName.Text);
```
As you see, sending request to server and having reply back is as simple as this.
For more complex example you better look at Chat sample projects.

##Details
Each actor and Peer has own logical fiber you can rely on this and forget about writing locks. This works even if you call any async method on actor proxy, fiber waits until Task of result is finished before starting processing another message. If you know that operation doesn't change anything you could attribute it with LockLevel.Read and all Read level operations could be processed simultaneously.
Actors implementing same protocol contract are considered a group and each group of actors has own actor coordinator that is responsible for preserving consistency of the view of the actor group across all nodes in cluster. Actor coordinator also responsible for creating/removing actors on other nodes. Default actor coordinator uses Isis2 (http://isis2.codeplex.com/) internally and guarantees that only one actor with same name exist across all cluster at any moment of time. New actors are created at random nodes by default.

+ Building primitives are:
	+ Actor subclass
		+ has some messaging contract in form of implemented interfaces
 		+ has Name and local node Id if created
   		+ OperationContex could be used to store client peer and send callback messages later
	+ S2CPeer subclass
		+ has some messaging contract in form of implemented interfaces
  		+ could manage exposed contracts using `SetHandler<TNetContract>(TNetContract handler)` method
		+ manages client state and acts as mediator to other actors calls depending on that state
+ All communications are asynchronous RPC (relies on async/await feature of 4.5 Framework) of type
	+ `Task<ReplyType>` if we want to get something back
	+ `Task`  if  you just want to wait for completion on other side
	+ `void` for one way non-async methods only(use with care as received message is recycled immediately upon control returns to proxy),
+ each Actor and S2CPeer has own logical fiber, with blocking chunks fired in thread pool
+ each net contract method could be attributed with such lock levels:
	+ None : processing happens on thread where net receive came
	+ Read : processing via peer or actor fiber in thread pool simultaniously with other Read operations
	+ Write : processing via peer or actor fiber, no other operations would be processed until this one finishes (including all async cont)


##Planned
1. Send broadcasts and queries to group of actors of same type
2. C++ client node and protocol generator
3. Ability to persist actors in Redis
4. ActorCoordinator that maintains required amount of replicas for each named actor
5. Zookeeper based actor and node coordinators for those who don't want to rely on Isis2


## License - Mit




