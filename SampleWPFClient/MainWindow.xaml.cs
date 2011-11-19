using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using Autofac.Integration.Mef;
using MOUSE.Core;
using SampleC2SProtocol;
using System.Threading.Tasks;
using Autofac;
using System.ComponentModel.Composition.Hosting;
using System.Collections.ObjectModel;
using Protocol.Generated;
using System.Reflection;

namespace SampleWPFClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IChatRoomServiceCallback
    {
        IClientNode _node;
        IChatService _chatServiceProxy;
        IChatRoomService _chatRoomServiceProxy;
        ObservableCollection<ChatRoomInfo> _rooms = new ObservableCollection<ChatRoomInfo>();

        public MainWindow()
        {
            InitializeComponent();
            
            txtUserName.Text = "User" + new Random().Next(10000);
            cbChatRooms.ItemsSource = _rooms;

            var builder = new ContainerBuilder();
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            builder.RegisterType<PhotonNetClient>().As<INetProvider>().SingleInstance();
            builder.RegisterType<ClientNode>().As<IClientNode>().SingleInstance();
            var container = builder.Build();

            _node = container.Resolve<IClientNode>();
            _node.SetHandler<IChatRoomServiceCallback>(this);

            _node.Start(manualUpdate: false);
        }

        private async Task UpdateRooms()
        {
            _rooms.Clear();
            List<ChatRoomInfo> rooms = await _chatServiceProxy.GetRooms();
            foreach (var room in rooms)
                _rooms.Add(room);
        }

        private void OnMainChannelDisconnect()
        {
            Application.Current.Shutdown();
        }

        public void OnSay(uint roomId, string message)
        {
            txtChat.AppendText(message +"\n");
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            string[] addrAndPort = cbConnection.Text.Split(':');

            await _node.ConnectToServer(new IPEndPoint(IPAddress.Parse(addrAndPort[0]), int.Parse(addrAndPort[1])));
            _node.DisconnectedEvent.Subscribe((_)=> OnMainChannelDisconnect());

            var loginService = await _node.GetService<IChatLogin>();
            LoginResult result = await loginService.Login(txtUserName.Text);
            if (result != LoginResult.Ok)
            {
                MessageBox.Show("Cant Login " + result);
            }
            _chatServiceProxy = await _node.GetService<IChatService>();
            UpdateRooms();
        }

        private void cbChatRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var room = (ChatRoomInfo)cbChatRooms.SelectedItem;

            long ticket = await _chatServiceProxy.JoinRoom(room.RoomId);
            _chatRoomServiceProxy = _node.GetService<IChatRoomService>(room.RoomId);
            List<string> history = await _chatRoomServiceProxy.Join(ticket);
            txtChat.Clear();
            foreach (var msg in history)
                txtChat.AppendText(msg + "\n");
        }
    }
}
