using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using CompiledProtocol;
using MOUSE.Core;
using NLog;
using RakNetWrapper;


namespace SimpleClient
{
    public class PingPongDomain : BasicDispatcher
    {
        int i = 1;

        public PingPongDomain(){}

        public override void OnConnected(INode self, INode source)
        {
            Console.WriteLine("We have connected to Node<Id:{0}>", target.Id);
            target.Send(new Ping(i++));
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
            Console.WriteLine("We have disconnected from Node<Id:{0}>", target.Id);
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Observable
            var node = new Node(NodeType.Client, new PingPongProtocol(), new PingPongDomain(), "127.0.0.1", 5678, 1, 10000, 1);
            node.Start();
            node.Connect("127.0.0.1", 4567);
            
            while (true)
            {
                Thread.Sleep(10);
            }
            
        }

        //private static void EnterLoginMenu(Node node)
        //{
        //    Console.Write("Enter Login: ");
        //    string login = Console.ReadLine();
        //    Console.WriteLine();
        //    Console.Write("Enter Password: ");
        //    string password = Console.ReadLine();
        //    Console.WriteLine();

        //    node.Send(new FindAccountRequest(login, password));
        //    node.OnMessage = LoginMenuMessageHander;
        //}

        //public static void LoginMenuMessageHander(Node node, IMessage message)
        //{
        //    switch (message.Id)
        //    {
        //        case MessageType.FindAccountReply:
        //            var msg = (FindAccountReply) message;
        //            if (msg.IsValid)
        //                EnterCharactersMenu(node);
        //            else
        //            {
        //                Console.WriteLine("There is no such account");
        //                EnterLoginMenu(node);
        //            }
        //            break;
        //    }
        //}

        //private static void EnterCharactersMenu(Node node)
        //{
            
        //    uint val = 0;
        //    while (val == 0)
        //    {
        //        Console.WriteLine("1)Create character\n2)Delete character\n3)Select character");
        //        string input = Console.ReadLine();
        //        uint.TryParse(input, out val)
        //        if(val > 3 || val == 0)
        //        {
        //            Console.WriteLine("Incorrect input");
        //            continue;
        //        }
        //    }
        //    switch (val)
        //    {
        //        case 1:
        //            Console.ReadLine()
        //    }
        //}
    }
}
