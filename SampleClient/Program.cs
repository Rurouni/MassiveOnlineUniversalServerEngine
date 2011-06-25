using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MOUSE.Core;
using NLog;
using RakNetWrapper;
using System.Diagnostics;
using System.Net;
using SampleProtocol;


namespace SimpleClient
{
    class Program
    {
        private static int RequestId = 0;
        public static Node node;
        static void OnException(Exception ex)
        {
            Console.WriteLine(ex.ToString());                
        }

        static void Main(string[] args)
        {
            //// observable domain logic that pings any new connected node until disconnected every 1 sec
            //var nodeEvents = new ObservableDispatcher();
            //var messages = new ConcurrentQueue<string>();

            //nodeEvents.OnConnected.Subscribe(context =>
            //{
            //    var connectedNode = context.Source;
            //    messages.Enqueue(string.Format("We have connected to Node<Id:{0}>", connectedNode.Id));

            //    int requestId = 1;
            //    Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(10.0))
            //    .TakeUntil(nodeEvents.OnDisconnected.Where(c => c.Source.Id == connectedNode.Id))
            //    .Subscribe((_) =>
            //    {
            //        var ping = new PingRequest();
            //        ping.requestId = requestId++;
            //        messages.Enqueue(string.Format("Sending Ping<RequestId:{0}> to Node<Id:{1}>", ping.requestId, context.Source.Id));
            //        context.Source.Execute(ping);
            //    }, OnException);
            //}, OnException);

            //nodeEvents.OnDisconnected.Subscribe(context => messages.Enqueue(string.Format("We have disconnected from Node<Id:{0}>", context.Source.Id)));
            //nodeEvents.OnOperation.OfType<PingReply>().Subscribe(pong => messages.Enqueue(string.Format("Received Pong<RequestId:{0}> from Node<Id:{1}>",
            //                                                                                            pong.RetVal, pong.Context.Source.Id)));
            var masterEndpoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5679);
            node = new Node(NodeType.Client, null, masterEndpoint);
            node.Start(manualUpdate:true);

            Console.WriteLine("enter help");
            var sb = new StringBuilder();
            
            //imagine this is game loop
            while (true)
            {
                node.Update();
                if(Console.KeyAvailable)
                {
                    var key = Console.ReadKey();
                    if(key.Key == ConsoleKey.Enter)
                    {
                        OnCommand(sb.ToString());
                        sb.Clear();
                    }
                    else
                        sb.Append(Console.ReadKey().KeyChar);
                }
                Thread.Sleep(1);
            }
            
        }

        private static void OnCommand(string input)
        {
            string[] words = input.Split(' ');
            if (words.Length > 0)
            {
                string command = words[0];
                switch (command)
                {
                    case "help": Console.WriteLine("connect <ip> <port>\nshow"); break;
                    case "ping": Ping(); break;
                        
                    default: Console.WriteLine("unknown command, type help for list of commands"); break;
                }
            }
        }

        private static async void Ping()
        {
            IPinger pinger = node.GetEntity<IPinger>();
            var timer = new Stopwatch();
            timer.Start();
            int responseId = await pinger.Ping(RequestId++);
            Console.WriteLine("succesfully pinged responseId:{0}, timeMs:{1}", responseId, timer.ElapsedTicks / Stopwatch.Frequency * 1000);
        }
    }
}
