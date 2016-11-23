using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LidgrenTestLobby
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Lobby : Window
    {
        public Lobby()
        {
            InitializeComponent();

            //Set console output to debug panel
            Console.SetOut(new TextBoxOutputter(TestBox));
            
            for (int i = 0; i < 10; i++)
            {
                MenuItem client = new MenuItem{ Title = "Client: " + i };
                client.Items.Add(new MenuItem{ Title = "Client ID: " + i });
                client.Items.Add(new MenuItem{ Title = "Connection: " });
                client.Items.Add(new MenuItem{ Title = "Online since: " });
                ClientTree.Items.Add(client);
            }

            for (int i = 0; i < 4; i++)
            {
                MenuItem room = new MenuItem{ Title = "Room: " + i };
                room.Items.Add(new MenuItem { Title = "Room ID: " + i });
                MenuItem clients = new MenuItem { Title = "Clients" };
                for (int j = 0; j < 4; j++)
                {
                    clients.Items.Add(new MenuItem { Title = "Client with ID" });
                }
                room.Items.Add(clients);
                RoomTree.Items.Add(room);
            }

        }

        public void StartLobbyServer(object sender, RoutedEventArgs routedEventArgs)
        {
            int serverport = 12484;
            int maxconnections = 4;
            const string approvalMessage = "SecretValue";

            LobbyManager.Instance.InitialiseServerManager(serverport, maxconnections, approvalMessage);

            LobbyManager.Instance.StartServer();
        }


        public void StopLobbyServer(object sender, RoutedEventArgs routedEventArgs)
        {
            Console.WriteLine("Stop pressed");

            LobbyManager.Instance.StopServer();
        }

        public void Test(object sender, RoutedEventArgs routedEventArgs)
        {
            Console.WriteLine("Test");
        }
    }

    /// <summary>
    /// Client items for hierarchical tree mapping
    /// </summary>
    public class MenuItem
    {
        public MenuItem()
        {
            this.Items = new ObservableCollection<MenuItem>();
        }

        public string Title { get; set; }

        public ObservableCollection<MenuItem> Items { get; set; }
    }



    /// <summary>
    /// TextBox Outputter for debug logs
    /// </summary>
    public class TextBoxOutputter : TextWriter
    {
        private readonly TextBox _textBox;

        public TextBoxOutputter(TextBox output)
        {
            _textBox = output;
        }

        public override void Write(char value)
        {
            base.Write(value);
            _textBox.Dispatcher.BeginInvoke(new Action(() =>
            {
                _textBox.AppendText(value.ToString());
            }));
        }

        public override Encoding Encoding => Encoding.UTF8;
    }
}
