using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public bool IsVisble;
        

        public Lobby()
        {
            InitializeComponent();

            //Set console output to debug panel
            _textBoxOutputter = new TextBoxOutputter(ConsoleLog);
            Console.SetOut(_textBoxOutputter);



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
            RoomList.ItemsSource = null;
        }

        public void ClearConsoleLog(object sender, RoutedEventArgs routedEventArgs)
        {
            _textBoxOutputter.Flush();
        }

        public void RefreshClients(object sender, RoutedEventArgs routedEventArgs)
        {
            ClientTree.Items.Clear();
            foreach (Client client in LobbyManager.Instance.GetClients())
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
            RoomList.ItemsSource = LobbyManager.Instance.GetRooms();
            ICollectionView view = CollectionViewSource.GetDefaultView(RoomList);
            view.Refresh();
        }

        private void AddRoom(object sender, RoutedEventArgs routedEventArgs)
        {
            LobbyManager.Instance.AddRoom();
        }

        private void RoomList_OnMouseDown(object sender, MouseButtonEventArgs routedEventArgs)
        {
            if (RoomList.SelectedItem != null)
            {
                Room room = (Room)RoomList.SelectedItem;
                RoomLog.Text = string.Join("\r\n", room.RoomConsole.ToArray());
                room.LogOutputBox = RoomLog;
                playerCount.Text = "PlayerCount: " + room.Players.Count;
                ClearRoomLogButton.IsEnabled = true;
            }
        }

        private void ClearRoomLog(object sender, RoutedEventArgs routedEventArgs)
        {
            if (RoomList.SelectedItem != null)
            {
                Room room = (Room)RoomList.SelectedItem;
                room.RoomConsole = new List<string>();
                RoomLog.Text = string.Join("\r\n", room.RoomConsole.ToArray());
            }
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
