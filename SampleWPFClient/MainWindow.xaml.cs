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
using PhotonClientWrap;
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
            //register chat messages and proxies as MEF parts
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetExecutingAssembly()));
            //register core messages as MEF parts
            builder.RegisterComposablePartCatalog(new AssemblyCatalog(Assembly.GetAssembly(typeof(INode))));
            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            builder.Register(c => new PhotonNetClient("MouseChat")).As<INetProvider>().SingleInstance();
            builder.RegisterType<ClientNode>().As<IClientNode>();
            var container = builder.Build();
            
            _node = container.Resolve<IClientNode>();
            //set callback handlers
            _node.SetHandler<IChatRoomServiceCallback>(this);
            _node.DisconnectedEvent.Subscribe((_) => OnMainChannelDisconnect());
            //start node thread and init network
            _node.Start();

            btnJoin.IsEnabled = false;
            Closing += (sender, e) => _node.Stop();

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
            MessageBox.Show("Main Channel has disconnected, you need to reconnect again");
        }

        public void OnRoomMessage(uint roomId, string message)
        {
            txtChat.AppendText(message +"\n");
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            btnJoin.IsEnabled = false;
            string[] addrAndPort = cbConnection.Text.Split(':');

            await _node.ConnectToServer(new IPEndPoint(IPAddress.Parse(addrAndPort[0]), int.Parse(addrAndPort[1])));

            var loginService = await _node.GetService<IChatLogin>();
            LoginResult result = await loginService.Login(txtUserName.Text);
            if (result != LoginResult.Ok)
                MessageBox.Show("Cant Login " + result);

            _chatServiceProxy = await _node.GetService<IChatService>();
            UpdateRooms();
            btnJoin.IsEnabled = true;
        }

        private async void cbChatRooms_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var room = (ChatRoomInfo)cbChatRooms.SelectedItem;

            try
            {
                long ticket = await _chatServiceProxy.JoinRoom(room.Id);
                _chatRoomServiceProxy = await _node.GetService<IChatRoomService>(room.Id);
                List<string> history = await _chatRoomServiceProxy.Join(ticket);
                txtChat.Clear();
                foreach (var msg in history)
                    txtChat.AppendText(msg + "\n");
            }
            catch (InvalidInput iex)
            {
                MessageBox.Show(((JoinRoomInvalidRetCode)iex.ErrorCode).ToString());
            }
            
        }

        private async void btnJoin_Click(object sender, RoutedEventArgs e)
        {
            CreateRoomResponse response = await _chatServiceProxy.JoinOrCreateRoom(cbChatRooms.Text);
            _chatRoomServiceProxy = await _node.GetService<IChatRoomService>(response.RoomId);
            var content = await _chatRoomServiceProxy.Join(response.Ticket);
            txtChat.Clear();
            foreach (var msg in content)
                txtChat.AppendText(msg + "\n");
            UpdateRooms();
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if(txtMyMessage.Text.Trim() != "")
            {
                _chatRoomServiceProxy.Say(txtMyMessage.Text);
                txtMyMessage.Text = "";
            }
        }
       
    }
}
