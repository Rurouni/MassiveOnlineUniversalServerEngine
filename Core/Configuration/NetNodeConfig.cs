using MOUSE.Core.Interfaces.Configuration;

namespace MOUSE.Core.Configuration
{
    public class NetNodeConfig : INetNodeConfig
    {
        bool _manualUpdateOnly = false;
        int _maxMessageToProcessPerTick = 10000;
        int _connectTimeoutSec = 100;
        int _slowUpdateThresholdMs = 500;

        int _sendTimeoutSec = 100;
        int _maxMessageSizeBts = 1024 * 1024;

        public int SendTimeoutSec
        {
            get { return _sendTimeoutSec; }
            set { _sendTimeoutSec = value; }
        }

        public int MaxMessageSizeBts
        {
            get { return _maxMessageSizeBts; }
            set { _maxMessageSizeBts = value; }
        }

        public bool ManualUpdateOnly
        {
            get { return _manualUpdateOnly; }
            set { _manualUpdateOnly = value; }
        }

        public int MaxMessageToProcessPerTick
        {
            get { return _maxMessageToProcessPerTick; }
            set { _maxMessageToProcessPerTick = value; }
        }

        public int ConnectTimeoutSec
        {
            get { return _connectTimeoutSec; }
            set { _connectTimeoutSec = value; }
        }

        public int SlowUpdateThresholdMs
        {
            get { return _slowUpdateThresholdMs; }
            set { _slowUpdateThresholdMs = value; }
        }


    }
}