using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using System.Text;
using System.Threading.Tasks;
using Isis;
using MOUSE.Core.Actors;
using MOUSE.Core.NodeCoordination;
using NLog;
using System.Reactive.Linq;

namespace MOUSE.Core.ActorCoordination
{
    /// <summary>
    /// Uses quorum of size up-to N 
    /// Assumes that isis is already used for node coordination, so it doesn't initialize ISIS system
    /// </summary>
    public class IsisActorCoordinator : IActorCoordinator
    {
        private const int CreateActorOperationId = 1;
        private const int ActorCreatedOperationId = 2;
        private const int RemoveActorOperationId = 3;
        private Timeout _myTO = new Timeout(1000, Timeout.TO_ABORTREPLY);

        private Logger Log;
        private IServerNode _node;
        private uint _primaryNetContractId;
        private Group _isisGroup;
        private readonly ConcurrentDictionary<string, ActorRemoteInfo> _actorsByName = new ConcurrentDictionary<string, ActorRemoteInfo>();
        private readonly TaskCompletionSource<object> _initializedTask = new TaskCompletionSource<object>();
        private readonly Subject<ActorRemoteInfo> _actorAddedEvent = new Subject<ActorRemoteInfo>();
        private readonly Subject<ActorRemoteInfo> _actorRemovedEvent = new Subject<ActorRemoteInfo>();

        private readonly ConcurrentDictionary<string, TaskCompletionSource<ActorRemoteInfo>> _pendingActorsByName = new ConcurrentDictionary<string, TaskCompletionSource<ActorRemoteInfo>>();

        private Func<ClusterView, NodeRemoteInfo> _distributionFunc;

        public void Init(IServerNode node, uint primaryNetContractId)
        {
            _node = node;
            node.Coordinator.OnClusterViewChanged.Subscribe(OnClusterViewChanged);
            _primaryNetContractId = primaryNetContractId;
            Log = LogManager.GetLogger("IsisActorCoordinator:" + node.Dispatcher.GetContract(primaryNetContractId).ContractType.Name);

            _distributionFunc = view =>
                {
                    int i = _node.Rnd.Next(view.Members.Count);
                    return view.Members[i];
                };

            _actorAddedEvent.Subscribe(OnActorAdded);

            _isisGroup = new Group("ActorCoordination-"+primaryNetContractId);
            _isisGroup.RegisterHandler(CreateActorOperationId, (Action<string, ulong>)OnCreateActor);
            _isisGroup.RegisterHandler(ActorCreatedOperationId, (Action<string, ulong, uint>)OnActorCreated);
            _isisGroup.RegisterHandler(RemoveActorOperationId, (Action<string>)OnRemoveActor);
            _isisGroup.RegisterMakeChkpt((ChkptMaker)OnInitialStateRequest);
            _isisGroup.RegisterLoadChkpt((Action<byte[]>)OnSetInitialActorList);
            _isisGroup.RegisterViewHandler((ViewHandler)OnViewChanged);
            _isisGroup.Join();
        }

        private void OnActorAdded(ActorRemoteInfo actor)
        {
            Log.Debug("On ActorAddedEvent " + actor.Name);

            TaskCompletionSource<ActorRemoteInfo> tcs;
            if (_pendingActorsByName.TryRemove(actor.Name, out tcs))
            {
                tcs.SetResult(actor);
            }
        }

        private void OnInitialStateRequest(View view)
        {
            _isisGroup.SendChkpt(new SetInitialActorsList(new List<ActorRemoteInfo>(_actorsByName.Values)).GetSerialized());
            _isisGroup.EndOfChkpt();
        }

        private void OnSetInitialActorList(byte[] data)
        {
            var msg = (SetInitialActorsList)_node.MessageFactory.Deserialize(new BinaryReader(new MemoryStream(data)));
            foreach (ActorRemoteInfo actor in msg.Actors)
            {
                if(!_actorsByName.TryAdd(actor.Name, actor))
                    throw new Exception("Should never happen as _actorsByName should be empty at this point");
            }
        }

        private void OnCreateActor(string actorName, ulong targetNodeId)
        {
            if (targetNodeId == _node.Id)
            {
                Log.Debug("Received create actor operation for Actor<Name:{0}>", actorName);
                Actor actor = _node.Repository.Create(_node, actorName, _primaryNetContractId);

                Task.Run(() => _isisGroup.OrderedSend(ActorCreatedOperationId, actorName, targetNodeId, actor.LocalId));
            }

        }

        private void OnActorCreated(string actorName, ulong targetNodeId, uint actorLocalId)
        {
            Log.Debug("Received actor created operation for Actor<Name:{0}>", actorName);
            var actor = new ActorRemoteInfo(actorName, new ActorKey(targetNodeId, actorLocalId));
            //first one win race condition resolving, we can safely use this only because we use globaly Ordered broadcasts
            if (_actorsByName.TryAdd(actorName, actor))
            {
                //because actor is created in repository before all other coordinators discover this 
                //so in case if this is not target node to check and remove all garbage
                //actual owner of this broadcast would still have proper actor
                if (targetNodeId != _node.Id)
                {
                    _node.Repository.Remove(_primaryNetContractId, actorName);
                }

                _actorAddedEvent.OnNext(actor);
            }
            else
            {
                Log.Debug("Received duplicate actor creation for Actor<Name:{0}>", actorName);
            }
        }

        private void OnRemoveActor(string actorName)
        {
            ActorRemoteInfo actorInfo;
            if (_actorsByName.TryRemove(actorName, out actorInfo))
            {
                if (actorInfo.Key.OwnerNodeId == _node.Id)
                {
                    _node.Repository.Remove(actorInfo.Key.LocalActorId);
                }

                _actorRemovedEvent.OnNext(actorInfo);
            }
            else
            {
                Log.Debug("Received remove actor operation for non-registered Actor<Name:{0}>", actorName);
            }
        }

        private void OnViewChanged(View view)
        {
            if(_initializedTask.Task.Status != TaskStatus.RanToCompletion)
                _initializedTask.SetResult(null);//we are initialized only after first view is delivered because all initial state transfer happens before this
        }

        private void OnClusterViewChanged(ClusterView view)
        {
            var removedActors = new List<ActorRemoteInfo>();
            foreach (NodeRemoteInfo leaver in view.Leavers)
            {
                foreach (ActorRemoteInfo actor in _actorsByName.Values)
                {
                    if (actor.Key.OwnerNodeId == leaver.NodeId)
                    {
                        ActorRemoteInfo dummy;
                        if (_actorsByName.TryRemove(actor.Name, out dummy))
                            removedActors.Add(actor);
                    }
                }
            }

            foreach (ActorRemoteInfo actor in removedActors)
            {
                _actorRemovedEvent.OnNext(actor);
            }
        }

        public async Task<ActorKey> GetOrCreate(string actorName)
        {
            await _initializedTask.Task;
            Log.Debug("GetOrCreate name:" + actorName);

            ActorRemoteInfo actorInfo;
            if (_actorsByName.TryGetValue(actorName, out actorInfo))
            {
                Log.Debug("GetOrCreate->Resolved {0} from cache", actorInfo);
                return actorInfo.Key;
            }
            else
            {
                NodeRemoteInfo targetNode = _distributionFunc(_node.ClusterView);
                Log.Debug("GetOrCreate->Creating Actor<Name:{0}> on NodeId:{1}", actorName, targetNode.NodeId);
                if (targetNode.NodeId != _node.Id)
                {
                    _isisGroup.OrderedSend(CreateActorOperationId, actorName, targetNode.NodeId);    
                }
                else
                {
                    Actor newActor = _node.Repository.Create(_node, actorName, _primaryNetContractId);
                    _isisGroup.OrderedSend(ActorCreatedOperationId, actorName, _node.Id, newActor.LocalId);
                }
                

                var tcs = new TaskCompletionSource<ActorRemoteInfo>();
                ActorRemoteInfo actor = null;
                _pendingActorsByName.TryAdd(actorName, tcs);
                
                actor = await tcs.Task;

                return actor.Key;
            }
        }

        public async Task RemoveActor(string actorName)
        {
            await _initializedTask.Task;

            Log.Debug("RemoveActor->Actor<Name:{0}>", actorName);
            if(_actorsByName.ContainsKey(actorName))
                _isisGroup.OrderedSend(RemoveActorOperationId, actorName);

            await _actorRemovedEvent.Where(actor => actorName == actor.Name).FirstAsync();
        }

        public async Task<IReadOnlyList<ActorRemoteInfo>> GetActors()
        {
            await _initializedTask.Task;

            return _actorsByName.Values.ToImmutableList();
        }

        public async Task<IReadOnlyList<TReply>> QueryGroup<TNetContract, TReply>(Func<TNetContract, Task<TReply>> funcToCall)
        {
            await _initializedTask.Task;

            throw new NotImplementedException();
        }

        public IObservable<ActorRemoteInfo> ActorAddedEvent
        {
            get { return _actorAddedEvent; }
        }

        public IObservable<ActorRemoteInfo> ActorRemovedEvent
        {
            get { return _actorRemovedEvent; }
        }
    }
}
