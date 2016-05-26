namespace MOUSE.Core.Interfaces.Configuration
{
    public interface INetNodeConfig
    {
        bool ManualUpdateOnly { get; }
        int MaxMessageToProcessPerTick { get; }
        int ConnectTimeoutSec { get; }
        int SendTimeoutSec { get; }
        int MaxMessageSizeBts { get; }
        int SlowUpdateThresholdMs { get; }
    }
}