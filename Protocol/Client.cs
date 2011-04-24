using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using MOUSE.Core;

namespace Protocol
{
    [NodeEntityContract]
    internal interface IClient : IChatParticipant
    {
    }

    public interface IChatParticipant
    {
        void OnChatMessage(long accountId, long characterId, string text);
    }
}
