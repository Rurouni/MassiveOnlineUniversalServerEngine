using System.Net;
using Lidgren.Network;

namespace LidgrenWrap
{
    public interface ILidgrenEvents
    {
        void ConnectingTo(IPEndPoint target);
        void Warning(string msg);
        void Error(string msg);
        void Debug(string msg);
        void StatusChanged(string status, long connectionId);
        void OnDataReceived(long connectionId, int numBytes);
    }
}