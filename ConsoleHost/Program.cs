using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MOUSE.Core;
using NLog;
using Protocol.Generated;
using RakNetWrapper;
using SampleServer;


namespace MOUSE.ConsoleHost
{
    static class Program
    {
        public static Logger Log = LogManager.GetLogger("ConsoleHost");

        static void Main(string[] args)
        {
            Log.Info("Started");
            try
            {
                var domain = new PingPongServer();
                domain.Init();
                var masterEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
                var node = new Node(NodeType.Master, new GeneratedDomain(), masterEndpoint, null);
                node.Start();

                while (true)
                {
                    Thread.Sleep(100);
                }
            }
            catch (Exception ex)
            {
                Log.FatalException("Critical Error, Host will be closed", ex);
            }
            Log.Info("Exited");
        }
    }
}
