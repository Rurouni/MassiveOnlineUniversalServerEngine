##General Info
The goal of the project is to build a C# server framework for small to large mmo's,
with the idea that a server team could start with a single server configuration and in case of need simply add new hardware and scale,
without rewriting of current game logic and restarting game server.
In general it is some kind of an OOP in the cloud.

##Main aspects

+ Primary building primitive is NodeEntity subclass
	+ implements some messaging contract
 	+ Id is used for distribution in a cluster
   	+ could be moved anywhere/anytime in a cluster, so it could be externally accessible only over proxy 
 	+ auto persistance in some NoSQL storage if needed  
+ All communications between NodeEntities are asynchronous RPC
	+ async CTP keeps code simple but return semantics for all methods in entity contract is limited to:
		+ `void` for one way methods,
		+ `Task`  if we want to wait for completion
		+ `Task<ReplyType>` if we want to get some data back
	+ all NodeEntities operations and their continuations are invoked only on one thread, no threads - no locks
	+ logical locks still needed in some cases, because state could change before continuation is hit
 		+ each entity method could be attributed with:  none, method or entity lock levels
   	+ all messages, proxies and dispatchers are generated using t4 for maximum performance


##Simple example
``` C#
//Define entity contract
[NodeEntityContract]
public interface IDeepPinger
{
	[NodeEntityOperation(Priority = MessagePriority.Low, Reliability = MessageReliability.Unreliable)]
	Task<int> Ping(int requestId, int deep);
}

//Implement this contract
[Export(typeof(NodeEntity))]
[NodeEntity(typeof(IDeepPinger), AutoCreate = true, Persistant = false)]
public class DeepPinger : NodeEntity, IDeepPinger
{
	async Task<int> Ping(int requestId, int deep)
	{
		deep--;
		IDeepPinger proxy = Node.GetProxy<IDeepPinger>(GetRandomIdExcludingOurs(100, Id));
		if(deep <= 0)
			return requestId;
		
		int replyId = await proxy.Ping(requestId, deep);
		//in case of complex logic here could be many different awaits,
		//but beauty of Async CTP  - even in that case, code would look straightforward
		return replyId;
	}
}

// use in any place where you have access to Node
// in case of some logic we obviously would know some id, this could be id of character, npc or anything else
// if we wont provide any id it will use 0 and you could think of it as singleton entity
IDeepPinger proxy = Node.GetProxy<IDeepPinger>(GetRandomId(100));
int result = await proxy.Ping(requestId++, 10);
```
##Roadmap
1. **Basic networking**
2. **Messages/Serialization generation**
3. **Proxy/Dispatcher generation**
4. *Single node server entity operation flow*
5. Multi node server entity operation flow
6. Client node(different transport, routing, security issues, etc) 
7. Node removing/adding keyspace redistribution 
8. Task system, for repeatable entity operations
9. Logical lock system(None, Method, Entity)
10. internal persistance for entities + possibility of adding custom persistance
11. internal metrics system(perf mon)
12. game metrics system (analytics)
13. Master reelection in case of failover
14. NodeEntity implementation logic hotswap without stopping server





