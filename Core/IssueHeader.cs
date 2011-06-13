using System;
using RakNetWrapper;

namespace Core
{
    public enum IssueHeaderType : byte
    {
        Message,
        NodeMessage
    }

    public class IssueHeader
    {
        public IssueHeaderType Id;

        public IssueHeader(IssueHeaderType id)
        {
            Id = id;
        }

        public virtual void Serialize(OutPacket packet)
        {
            packet.WriteUByte((byte)Id);
        }

        public static IssueHeader Deserialize(InPacket packet)
        {
            var type = (IssueHeaderType)packet.ReadUByte();
            switch (type)
            {
                case IssueHeaderType.Message:
                    return new IssueHeader(IssueHeaderType.Message);
                    break;
                case IssueHeaderType.NodeMessage:
                    return new IssueHeader(IssueHeaderType.NodeMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }
    }
}