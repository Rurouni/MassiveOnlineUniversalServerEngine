using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MOUSE.Core.NodeCoordination
{
    public class ClusterView
    {
        private readonly int _viewId;
        private readonly ulong _leaderId;
        private readonly ImmutableList<NodeRemoteInfo> _members;
        private readonly ImmutableList<NodeRemoteInfo> _joiners;
        private readonly ImmutableList<NodeRemoteInfo> _leavers;

        public ClusterView(int viewId, ulong leaderId, ImmutableList<NodeRemoteInfo> members, ImmutableList<NodeRemoteInfo> joiners, ImmutableList<NodeRemoteInfo> leavers)
        {
            _viewId = viewId;
            _leaderId = leaderId;
            _members = members;
            _joiners = joiners;
            _leavers = leavers;
        }

        public int ViewId
        {
            get { return _viewId; }
        }

        public ulong LeaderId
        {
            get { return _leaderId; }
        }

        public ImmutableList<NodeRemoteInfo> Members
        {
            get { return _members; }
        }

        public ImmutableList<NodeRemoteInfo> Joiners
        {
            get { return _joiners; }
        }

        public ImmutableList<NodeRemoteInfo> Leavers
        {
            get { return _leavers; }
        }

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("\tViewId: " + _viewId);
            sb.AppendLine("\tLeaderId: " + _leaderId);

            if (_members.Count > 0)
            {
                sb.AppendLine("\tMembers: ");
                foreach (var nodeRemoteInfo in _members.AsEnumerable())
                {
                    sb.Append("\t");
                    sb.AppendLine(nodeRemoteInfo.ToString());
                }
            }

            if (_leavers.Count > 0)
            {
                sb.AppendLine("\tLeavers: ");
                foreach (var nodeRemoteInfo in _leavers.AsEnumerable())
                {
                    sb.Append("\t");
                    sb.AppendLine(nodeRemoteInfo.ToString());
                }
            }

            if (_joiners.Count > 0)
            {
                sb.AppendLine("\tJoiners: ");
                foreach (var nodeRemoteInfo in _joiners.AsEnumerable())
                {
                    sb.Append("\t");
                    sb.AppendLine(nodeRemoteInfo.ToString());
                }
            }

            return sb.ToString();
        }

        public NodeRemoteInfo GetNode(ulong nodeId)
        {
            return _members.Find(x => x.NodeId == nodeId);
        }
    }
}
