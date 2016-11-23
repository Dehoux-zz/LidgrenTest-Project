using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Security.Policy;
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
        private TextBoxOutputter _textBoxOutputter;

        public bool IsVisble ;

        public Lobby()
        {
            InitializeComponent();

            //Set console output to debug panel
            _textBoxOutputter = new TextBoxOutputter(TestBox);
            Console.SetOut(_textBoxOutputter);

            //for (int i = 0; i < 4; i++)
            //{
            //    MenuItem room = new MenuItem { Title = "Room: " + i };
            //    room.Items.Add(new MenuItem { Title = "Room ID: " + i });
            //    MenuItem clients = new MenuItem { Title = "Clients" };
            //    for (int j = 0; j < 4; j++)
            //    {
            //        clients.Items.Add(new MenuItem { Title = "Client with ID" });
            //    }
            //    room.Items.Add(clients);
            //    RoomTree.Items.Add(room);
            //}

        }

        public void StartLobbyServer(object sender, RoutedEventArgs routedEventArgs)
        {
            int serverport = 12484;
            int maxconnections = 4;
            const string approvalMessage = "SecretValue";

            LobbyManager.Instance.InitialiseServerManager(serverport, maxconnections, approvalMessage);

            LobbyManager.Instance.StartServer();

            StartServerButton.Visibility = Visibility.Collapsed;
            StopServerButton.Visibility = Visibility.Visible;
            RefreshRoomsButton.IsEnabled = true;
            RefreshClientsButton.IsEnabled = true;
            AddRoomButton.IsEnabled = true;
        }


        public void StopLobbyServer(object sender, RoutedEventArgs routedEventArgs)
        {
            Console.WriteLine("Stop pressed");

            LobbyManager.Instance.StopServer();

            StartServerButton.Visibility = Visibility.Visible;
            StopServerButton.Visibility = Visibility.Collapsed;
            RefreshRoomsButton.IsEnabled = false;
            RefreshClientsButton.IsEnabled = false;
            AddRoomButton.IsEnabled = false;

            ClientTree.Items.Clear();
            RoomTree.Items.Clear();
        }

        public void ClearConsole(object sender, RoutedEventArgs routedEventArgs)
        {
            _textBoxOutputter.Flush();
        }

        public void RefreshClients(object sender, RoutedEventArgs routedEventArgs)
        {
            ClientTree.Items.Clear();
            List<Client> clients = LobbyManager.Instance.GetClients();
            foreach (Client client in clients)
            {
                MenuItem clientMenutItem = new MenuItem { Title = "Client: " + client.Id };
                clientMenutItem.Items.Add(new MenuItem { Title = "Client ID: " + client.Id });
                clientMenutItem.Items.Add(new MenuItem { Title = "Connection: " + client.Connection });
                clientMenutItem.Items.Add(new MenuItem { Title = "Online since: " + client.LastBeat });
                ClientTree.Items.Add(clientMenutItem);
            }
        }

        public void RefreshRooms(object sender, RoutedEventArgs routedEventArgs)
        {
            RoomTree.Items.Clear();
            List<Room> rooms = LobbyManager.Instance.GetRooms();
            foreach (Room room in rooms)
            {
                MenuItem roomMenutItem = new MenuItem { Title = "Room: " + room.Id };
                roomMenutItem.Items.Add(new MenuItem { Title = "Room ID: " + room.Id });
                MenuItem players = new MenuItem { Title = "Players" };
                foreach (Player player in room.Players)
                {
                    players.Items.Add(new MenuItem { Title = "Client ID: " + player.Id });
                }
                roomMenutItem.Items.Add(players);
                RoomTree.Items.Add(roomMenutItem);
            }
        }

        private void AddRoom(object sender, RoutedEventArgs routedEventArgs)
        {
            LobbyManager.Instance.AddRoom();
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

        public override void Flush()
        {
            _textBox.Clear();
        }


        public override Encoding Encoding => Encoding.UTF8;
    }
}
