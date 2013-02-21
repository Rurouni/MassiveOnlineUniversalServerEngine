##General Info
This is high level C# server framework for small to large mmo's where you need both reliable and unreliable transport, request/reply and full duplex communications, with the idea that client-server and server-server communication is done
in form of asynchronous RPC calls to actors implementing some protocol contract(attributed C# interface). As low level internal transport currently RakNet, Photon and Lidgren are supported. Idea is that if you split you domain model into actors from beforehand then later you will be able to automatically horizontally scale as actors could be distributed across nodes. But in the beginning you can start small with only one node having all performance you can get from one machine as actors communicating on the same node do not use network just putting messages to each other input queue.
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
	+ You create inheritor from C2SPeer and register it with ServerNode, it will act as persistent connection to any client that has connected via external endpoint of this ServerNode
	+ You create any amount of classes inherited from Actor depending on your design
	+ You implement protocol interfaces from generated via t4 file. You have basically 2 choices here
		+ You can implement them directly in your custom C2SPeer class
		+ You can implement them inside one or many of your custom actor classes
	+ For each custom actor type you define coordinator type
    + You call actors from C2S peers or other actors, coordinators deals with locating/creating new actors in clustered setup
    + You call client back via callback interfaces from custom C2SPeer or actors that you marked to be able to receive external connections
	+ You start ServerNode configuring everything including what transport libraries use for internal and external connections
	+ You add/remove nodes with this codebase depending on your load

If you know WCF everything above sounds quite familiar. So you can ask why we need something else. There are several reasons why you would not choose WCF for MMO game:
+ WCF is general and huge, it supports all kinds of enterprise scenarios and you pay in performance for things games never need
+ WCF doesn't have reliable/unreliable UDP transport from a box,  many online games need both reliable and unreliable traffic and easy way of defining which message is reliable and which not.
+ Have fun consuming WCF services from C++ clients or old mono runtimes like Unity3d
+ WCF doesn't help you with coordination or consistent group views or multicasts, maximum scalability that you can get with WCF is when you have setup like this: hardware load balancer in front and set of stateless WCF services with same contract in the back. Have fun implementing open world MMO with such setup.

##Details
Each actor and Peer has own logical fiber you can rely on this and forget about writing locks. This works even if you call any async method on actor proxy, fiber waits until Task of result is finished before starting processing another message. If you know that operation doesn't change anything you could attribute it with LockLevel.Read and all Read level operations could be processed simultaneously.
Actors implementing same protocol contract are considered a group and each group of actors has own actor coordinator that is responsible for preserving consistency of the view of the actor group across all nodes in cluster. Actor coordinator also responsible for creating/removing actors on other nodes. Default actor coordinator uses Isis2 (http://isis2.codeplex.com/) internally and guarantees that only one actor with same name exist across all cluster at any moment of time. New actors are created at random nodes by default.

##Main aspects

+ Building primitives are:
	+ NodeService subclass
		+ has some messaging contract in form of implemented interfaces
 		+ Id is used for distribution in a cluster
   		+ could be moved anywhere/anytime in a cluster, so it could be externally accessible only over proxy 
     		+ OperationContex could be used to store client peer and send callback messages later
 		+ auto persistance using provided IPersistanceProvider for attributed fields
	+ C2SPeer subclass
		+ has some messaging contract in form of implemented interfaces
  		+ could manage exposed contracts using `SetHandler<>()` method
		+ can manage client state and call internal services depending on that state
+ All communications between C2S and S2S are asynchronous RPC (relies on async/await feature of 4.5 Framework)
	+ async/await keeps code simple but return semantics for all methods in net contract is limited to:
		+ `void` for one way methods,
		+ `Task`  if  you just want to wait for completion
		+ `Task<ReplyType>` if we want to get something back
  	+ each NodeService and C2SPeer has own logical fiber, with blocking chunks fired in thread pool
	+ each net contract method could be attributed with such lock levels:
		+ None : processing happens on thread where net receive came
		+ ReadReentrant : processing in thread pool simultaniously with other Read operations
		+ WriteReentrant : sequential processing in thread pool(doesnt protect from state changes during async wait)
		+ Full : no other operations would be processed until this one finishes (including all async cont)
+ All messages, proxies and dispatchers are generated using t4 for maximum performance
+ Currently supports Photon and RakNet as transport engines (binaries are not included in the project, only wrappers)

##Roadmap
1. **Basic networking** - done
2. **Messages/Serialization generation** - done
3. **Proxy/Dispatcher generation** - done
4. **Single node server** - done
5. **Multi node server** - done
6. Automatical persistance for services
7. cluster monitoring tools
8. detailed problem analyzis tools (what service takes most CPU, operations processing times etc.)


## Example and details
In ChatSample folder you can find simple chat server with rooms and basic user tracking as showcase
### Client
First of all you need protocol described in some dll (SampleC2SProtocol)
It should contain net contracts of such form:

``` C#
[NetContract]
public interface IChatService
{
	[NetOperation(Lock = LockType.ReadReentrant)]
	Task<List<ChatRoomInfo>> GetRooms();
	
	[NetOperation]
	Task<CreateRoomResponse> JoinOrCreateRoom(string roomName);
	
	/// <returns>Ticket</returns>
	[NetOperation(InvalidRetCode = typeof(JoinRoomInvalidRetCode))]
	Task<long> JoinRoom(uint roomId);
}
```
You also need to add t4 file that will generate all messages/proxies, look GeneratedDomain.tt, here you just need to define assemblies that contain your net contracts

Then you need to create client node. I am using Autofac to wire all things up, you can use anything you want,
just note that all messages and proxies are exported as MEF parts, and here autofac helps me to correctly initialize ServiceProtocol and MessageFactory using this information

``` C#
var builder = new ContainerBuilder();
//register chat messages and proxies as MEF parts
builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
//register core messages as MEF parts
builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(INode))));
builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
builder.Register(c => new PhotonNetClient("MouseChat")).As<INetProvider>().SingleInstance();
builder.RegisterType<ClientNode>().As<IClientNode>();
var container = builder.Build();

_node = container.Resolve<IClientNode>();
//set callback handlers
_node.SetHandler<IChatRoomServiceCallback>(this);
_node.DisconnectedEvent.Subscribe((_)=> OnMainChannelDisconnect());
//start node thread and init network
_node.Start();
```

Next important part is how we connect to server and communicate with it:

``` C#
await _node.ConnectToServer(new IPEndPoint(IPAddress.Parse(addrAndPort[0]), int.Parse(addrAndPort[1])));
var loginService = await _node.GetService<IChatLogin>();
LoginResult result = await loginService.Login(txtUserName.Text);
```
Interesting thing here is that you also await proxy object in `_node.GetService<IChatLogin>();`,
that's because before returning proxy object on client server checks if client could access this service.
And the last bit:

``` C#
try
{
	long ticket = await _chatServiceProxy.JoinRoom(room.Id);
	_chatRoomServiceProxy = await _node.GetService<IChatRoomService>(room.Id);
	List<string> history = await _chatRoomServiceProxy.Join(ticket);
	txtChat.Clear();
	foreach (var msg in history)
		txtChat.AppendText(msg + "\n");
}
catch (InvalidInput iex)
{
	MessageBox.Show(((JoinRoomInvalidRetCode)iex.ErrorCode).ToString());
}
```
it shows 2 important things:

+ here `await _node.GetService<IChatRoomService>(room.Id);` we get proxy to service with id `room.Id`. If we dont provide any id when getting proxy it gets proxy to service with 0 id. It like singleton service.
+ try-catch(InvalidInput) is used to process expected Incorrect Results, when server code throws InvalidInput exception it is rethrown on client, all other exceptions aren't rethrown

Thats all for client, you can run SampleWPFClient to see how it works(currently it uses Photon as INetProvider, so you would need to host SampleServerPhotonHost in photon)
### Server
For server logic see SampleServerLogic, it's very straightforward, you can find 3 main entities there:
First and minimum that you should have is C2SPeer inheritor ChatClient - it will be created for each connected client. Each peer has separate logical thread(fiber)

``` C# 
[Export(typeof(C2SPeer))]
public class ChatClient : C2SPeer, IChatLogin, IChatService
{
	ClientState _state;
	private ChatUserInfo _user;
	uint _roomId;

	public override void OnCreated()
	{
		Log = LogManager.GetLogger(string.Format("ChatClient<NetId:{0}>", Channel.Id));
		SetHandler<IChatLogin>(this);
		DisconnectedEvent.Subscribe(OnDisconnectAsync);
		Log.Info("connected");
	}
	...
```
all initialization should be in `OnCreated()` method because neither constructor nor initializers will be called.
C2SPeer could also enable/disable or delegate processing of various net contracts using `SetHandler<TNetContract>(obj);`

Second you can find ChatManager service, it's not really needed for simple Chat,
but it shows concept of internal manager over something(here over rooms) that is not available to client directly but usefull for server logic:
like checking that we have no 2 users with the same name or finding rooms.
Theoretically this service is bottleneck for users logging in but making this service scalable would overcomplicate sample

``` C#
[Export(typeof(NodeService))]
[NodeService(AutoCreate = true, Persistant = false)]
public class ChatManager : NodeService, IChatManager
{
	private uint _userCounter;
	private uint _roomCounter;
	private Dictionary<string, ChatUserInfo> _usersByName;
	private Dictionary<uint, ChatUserInfo> _usersById;
	private List<ChatRoomInfo> _rooms;


	public override void OnCreated()
	{
	    _usersByName = new Dictionary<string, ChatUserInfo>();
	    _usersById = new Dictionary<uint, ChatUserInfo>();
	    _rooms = new List<ChatRoomInfo>();
	    _userCounter = 1;
	    _roomCounter = 1;
	}
        ...
```

You can see that here, as in C2SPeer, all initializations should also happen only in OnCreated method.

The Last one is `ChatRoom` it's mostly like ChatManager, exept the fact that Id of service here really makes sense because 
it's also the Id of the room. Also this service has externally visible net contract `IChatRoomService`
and saves all C2SPeers joined this room into internal dictionaries as `ChatRoomClient`
Sender C2SPeer could be accessed from `Context.Source`. Only note here: `Context` property could be safely used only before any `await` keyword. In continuations it becames invalid.

Next interesting thing is how ChatRoom sends messages to other users of the room:

``` C#
var callback = client.Peer.As<IChatRoomServiceCallback>();
callback.OnRoomMessage(Id, msg);
```

Other stuff looks similar to client code, everywhere you can get proxy to other services using Node.GetService<TNetContract>(id)
and use this proxy to invoke RPC

## License - Mit




