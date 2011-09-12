##General Info
Main goal of this project is to build a C# server framework for small to large mmo's,
with idea that server team could start with one piece of hardware and in case of need simply add new hardware,
without rewriting of current game logic and restarting game server.
In general this is some kind of OOP in the cloud.

To achieve this every object in game server's BL is divided into this 2 categories:
* NodeEntity - has Id that is used for distribution in cluster, could be moved anywhere/anytime in cluster so could be accessible only over proxy
* Plain - can be used/stored only in context of some NodeEntity
Because any NodeEntity is allocated in cluster only on one node in one instance in any time we have share nothing principle so we can forget about locks in game logic.
All communication between NodeEntities is async only so in reality each Node has only one updating thread for all game logic or could be even update manually in clients main loop,
to achieve this every method of any NodeEntity contract should return only: void, Task, Task<Reply>.
Using Async CTP all this allows us to write very straightforward code.

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
// if we wont provide any id it will use 0 and you could think of it like singleton entity
IDeepPinger proxy = Node.GetProxy<IDeepPinger>(GetRandomId(100));
int result = await proxy.Ping(requestId++, 10);
```
##Roadmap
1. **Basic networking**
2. **Messages/Serialization generation**
3. **Proxy/Dispatcher generation**
4. *Single node entity operation flow*
5. Multi node entity operation flow
6. Client node(different transport, routing, security issues, etc) 
7. Node removing/adding keyspace redistribution 
8. Task system, for repeatable entity operations
9. Logical lock system(None, Method, Entity)
10. internal persistance for entities + possibility of adding custom persistance
11. internal metrics system(perf mon)
12. game metrics system (analytics)
13. Master reelection in case of failover
14. NodeEntity implementation logic hotswap without stopping server





