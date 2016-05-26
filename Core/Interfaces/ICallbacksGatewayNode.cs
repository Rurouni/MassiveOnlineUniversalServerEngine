using System.Threading.Tasks;

namespace MOUSE.Core.Interfaces
{
    public interface ICallbacksGatewayNode : INetNode
    {
        CallbackChannelRef ExposeAsCallbackChannel(INetChannel channel);

        Task<INetChannel> ConnectToCallbackChannel(CallbackChannelRef callbackChannelRef);
    }
}