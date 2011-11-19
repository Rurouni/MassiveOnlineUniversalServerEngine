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

namespace ChatClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        ClientNode _node;

        public MainWindow()
        {
            InitializeComponent();

            _node = new ClientNode();
            _node.Start(manualUpdate: false);
        }

        private void btnConnect_Click(object sender, RoutedEventArgs e)
        {
            await _node.Connect(cbConnection.Text);
        }
    }
}
