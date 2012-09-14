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
using MOUSE.Core;
using PhotonClientWrap;
using System.Threading.Tasks;
using Autofac;
using System.ComponentModel.Composition.Hosting;
using System.Collections.ObjectModel;
using Protocol.Generated;
using System.Reflection;
using System.Reactive.Linq;
using System.Reactive.Threading;
using RakNetWrapper;
using System.Reactive.Concurrency;

namespace SampleWPFClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IChatRoomServiceCallback
    {
        IClientNode _node;
        IServerPeer _mainChannel;
        IServerPeer _chatChannel;
        IDisposable _chatChannelDisconnectionSubscription;
        IChatService _chatServiceProxy;
        IChatRoomService _chatRoomServiceProxy;
        ObservableCollection<ChatRoomModel> _rooms = new ObservableCollection<ChatRoomModel>();

        public MainWindow()
        {
            InitializeComponent();
            
            txtUserName.Text = "User" + new Random().Next(10000);
            cbChatRooms.ItemsSource = _rooms;

            var builder = new ContainerBuilder();

            //register core messages
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(EmptyMessage)))
                .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
                .As<Message>();

            //register domain messages
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
                .Where(x => x.IsAssignableTo<Message>() && x != typeof(Message))
                .As<Message>();

            //register domain service definitions and proxies
            builder.RegisterAssemblyTypes(Assembly.GetAssembly(typeof(IChatLogin)))
                .Where(x => x.IsAssignableTo<NodeServiceProxy>() && x != typeof(NodeServiceProxy))
                .As<NodeServiceProxy>();

            builder.RegisterType<ServiceProtocol>().As<IServiceProtocol>().SingleInstance();
            builder.RegisterType<MessageFactory>().As<IMessageFactory>().SingleInstance();
            //builder.Register(c => new PhotonNetClient("MouseChat")).As<INetProvider>().SingleInstance();
            builder.RegisterType<RakPeerInterface>().As<INetProvider>().SingleInstance();
            builder.RegisterType<ClientNode>().As<IClientNode>();
            var container = builder.Build();
            
            _node = container.Resolve<IClientNode>();
            //start node thread and init network
            _node.Start();

            btnJoin.IsEnabled = false;
            Closing += (sender, e) => _node.Stop();

        }

        private async void UpdateRooms()
        {
            _rooms.Clear();
            List<ChatRoomInfo> rooms = await _chatServiceProxy.GetRooms();
            foreach (var room in rooms)
                _rooms.Add(new ChatRoomModel(room.Id,room.Name));
        }

        private async void OnMainChannelDisconnect()
        {
            SetStatus("Disconnected");
            MessageBox.Show("Connection lost - reconnecting");
            Connect();
        }

        public void OnRoomMessage(uint roomId, string message)
        {
            txtChat.AppendText(message +"\n");
        }

        private async void Connect()
        {
            try
            {
                btnJoin.IsEnabled = false;
                string[] connectionPool = cbConnection.Text.Split(';');

                SetStatus("Connecting");

                bool connected = await ConnectToFirstWorking(connectionPool.Select(connectionString =>
                    {
                        string[] addrAndPort = connectionString.Split(':');
                        return new IPEndPoint(IPAddress.Parse(addrAndPort[0]), int.Parse(addrAndPort[1]));
                    }).ToList());

                if (connected)
                {
                    _mainChannel.DisconnectedEvent
                        .ObserveOn(new DispatcherScheduler(Dispatcher))
                        .Subscribe((_) => OnMainChannelDisconnect());

                    var loginService = await _mainChannel.GetService<IChatLogin>();
                    LoginResult result = await loginService.Login(txtUserName.Text);

                    if (result != LoginResult.Ok)
                    {
                        MessageBox.Show("Cant Login " + result);
                        SetStatus("Disconnected");
                    }
                    else
                    {
                        _chatServiceProxy = await _mainChannel.GetService<IChatService>();
                        UpdateRooms();
                        SetStatus("Connected");
                        btnJoin.IsEnabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("Can't connect to any server");
                    SetStatus("Disconnected");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString());
            }
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            Connect();
        }

        private async Task<bool> ConnectToFirstWorking(List<IPEndPoint> endpoints)
        {
            bool connected = false;
            while (!connected && endpoints.Count > 0)
            {
                IPEndPoint endpoint = null;
                try
                {
                    endpoint = endpoints.Last();
                    _mainChannel = await _node.ConnectToServer(endpoint);
                    connected = true;
                }
                catch (Exception)
                {
                    endpoints.RemoveAt(endpoints.Count - 1);
                    MessageBox.Show("Can't connect to " + endpoint);
                }
            }

            return connected;
        }

        private async void btnJoin_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (_chatRoomServiceProxy != null)
                {
                    _chatRoomServiceProxy.Leave();
                }
                JoinRoomResponse response = await _chatServiceProxy.JoinOrCreateRoom(cbChatRooms.Text);
                if (_chatChannelDisconnectionSubscription != null)
                {
                    _chatChannelDisconnectionSubscription.Dispose();
                }
                _chatChannel = await _node.ConnectToServer(response.ServerEndpoint);
                _chatRoomServiceProxy = await _chatChannel.GetService<IChatRoomService>(response.RoomId);
                _chatChannel.SetHandler<IChatRoomServiceCallback>(this);
                _chatChannelDisconnectionSubscription = 
                    _chatChannel.DisconnectedEvent.Subscribe((_) => MessageBox.Show("Disconnected from chat room, try connect again"));
                var content = await _chatRoomServiceProxy.Join(response.Ticket);
                txtChat.Clear();
                foreach (var msg in content)
                    txtChat.AppendText(msg + "\n");
                UpdateRooms();

            }
            catch (Exception exception)
            {
                MessageBox.Show(exception.Message);
            }
        }

        private void btnSend_Click(object sender, RoutedEventArgs e)
        {
            if(txtMyMessage.Text.Trim() != "")
            {
                _chatRoomServiceProxy.Say(txtMyMessage.Text);
                txtMyMessage.Text = "";
            }
        }

        private void SetStatus(string text)
        {
            lblStatus.Content = text;
        }
    }

    public class ChatRoomModel
    {
        public uint Id;
        public String Name;

        public ChatRoomModel(uint id, string name)
        {
            Id = id;
            Name = name;
        }

        public override string ToString()
        {
            return Name;
        }
    }
}
