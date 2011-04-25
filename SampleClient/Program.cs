using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using MOUSE.Core;
using NLog;
using Protocol.Generated;
using RakNetWrapper;


namespace SimpleClient
{
    public class PingPongDomain : BasicDispatcher
    {
        int i = 1;

        public PingPongDomain(){}

        public override void OnConnected(INode self, INode source)
        {
            Console.WriteLine("We have connected to Node<Id:{0}>", source.Id);
            source.Execute(new Ping(i++));
        }

        public override void  OnOperation(INode self, INode source, IOperation operation)
        {
            if(operation.Header.OperationId == Pong.OperationId)
            {
                Pong pong = (Pong)operation;
                Console.WriteLine("Received Pong:{0}", pong.RequestId);
                source.Execute(new Ping(i++));
            }
        }

        public override void OnDisconnected(INode self, INode source)
        {
            Console.WriteLine("We have disconnected from Node<Id:{0}>", source.Id);
        }
    }

    class Program
    {
        static void OnException(Exception ex)
        {
            Console.WriteLine(ex.ToString());                
        }

        static void Main(string[] args)
        {
            // observable domain logic that pings any new connected node until disconnected every 1 sec
            var nodeEvents = new ObservableDispatcher();
            var messages = new ConcurrentQueue<string>();

            nodeEvents.OnConnected.Subscribe(context =>
            {
                var connectedNode = context.Source;
                messages.Enqueue(string.Format("We have connected to Node<Id:{0}>", connectedNode.Id));

                int requestId = 1;
                Observable.Timer(TimeSpan.Zero, TimeSpan.FromSeconds(10.0))
                .TakeUntil(nodeEvents.OnDisconnected.Where(c => c.Source.Id == connectedNode.Id))
                .Subscribe((_) =>
                {
                    var ping = new Ping(requestId++);
                    messages.Enqueue(string.Format("Sending Ping<RequestId:{0}> to Node<Id:{1}>", ping.RequestId, context.Source.Id));
                    context.Source.Execute(ping);
                }, OnException);
            }, OnException);

            nodeEvents.OnDisconnected.Subscribe(context => messages.Enqueue(string.Format("We have disconnected from Node<Id:{0}>", context.Source.Id)));
            nodeEvents.OnOperation.OfType<Pong>().Subscribe(pong => messages.Enqueue(string.Format("Received Pong<RequestId:{0}> from Node<Id:{1}>", pong.RequestId, pong.Context.Source.Id)));

            var node = new Node(NodeType.Client, new PingPongProtocol(), nodeEvents, "127.0.0.1", 5678, 10, 10000, 1);
            node.Start();

            Console.WriteLine("enter help");
            while (true)
            {
                string input = (Console.ReadLine()??"").ToLower();
                string[] words = input.Split(' ');
                if (words.Length > 0)
                {
                    string command = words[0];
                    switch (command)
                    {
                        case "help": Console.WriteLine("connect <ip> <port>\nshow");break;
                        case "connect": 
                            if(words.Length == 3)
                            {
                                string ip = words[1];
                                ushort port = 0;
                                if (ushort.TryParse(words[2], out port))
                                    node.Connect(ip, port);
                            }
                            break;
                        case "show": 
                            string msg;
                            while(messages.TryDequeue(out msg))
                                Console.WriteLine(msg);
                            break;
                        default: Console.WriteLine("unknown command, type help for list of commands"); break;
                    }
                }
            }
            
        }
    }
}
