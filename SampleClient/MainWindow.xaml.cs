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
using SampleC2SProtocol;

namespace SampleClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window, IChatRoomServiceCallback
    {
        ClientNode _node;
        IChatService _chatServiceProxy;
        List<ChatRoomInfo> _rooms;

        public MainWindow()
        {
            InitializeComponent();

            _node = new ClientNode(typeof(Protocol.Generated.IChatServiceProxy).Assembly);
            _node.RegisterNetContractHandler<IChatRoomServiceCallback>(this);

            _node.Start(manualUpdate: false);
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            NodeProxy mainChannel = await _node.Connect(cbConnection.Text);
            mainChannel.OnDisconnect += OnMainChannelDisconnect;

            _chatServiceProxy = _node.GetService<IChatService>(mainChannel);

            _rooms = await _chatServiceProxy.GetRooms();
        }

        private void OnMainChannelDisconnect()
        {
            Application.Current.Shutdown();
        }

        public void OnSay(uint roomId, string userName, string message)
        {
            throw new NotImplementedException();
        }
    }
}
