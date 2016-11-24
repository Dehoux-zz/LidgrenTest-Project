using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestLobby
{
    class LobbyManager
    {
        public static NetServer Server { get; private set; }
        private NetPeerConfiguration _configuration;
        private string _approvalMessage;

        private Task _gameLoopTask;
        private volatile bool _isRunning;
        private int _beatnum;
        private TimeSpan _beatrate;
        private DateTime _lastBeat;
        private double _last15Sec;

        private List<Client> _clients;
        private List<Room> _rooms;

        #region Singleton Creation
        private static volatile LobbyManager _instance;
        private static object _syncRoot = new object();

        private LobbyManager() { }

        public static LobbyManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new LobbyManager();
                }

                return _instance;
            }
        }
        #endregion

        #region ServerControl

        public void InitialiseServerManager(int gamePort, int maxPlayers, string approvalMessage)
        {
            // (needed for RegisterReceivedCallback, replace with AsyncOperationM... see CrabBattleServer)
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            _configuration = new NetPeerConfiguration("LidgrenTest")
            {
                Port = gamePort,
                MaximumConnections = maxPlayers
            };
            _configuration.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _approvalMessage = approvalMessage;
        }

        public void StartServer()
        {
            _clients = new List<Client>();
            _rooms = new List<Room>();

            Server = new NetServer(_configuration);
            Server.RegisterReceivedCallback(MessageHandler.Register);
            Server.Start();
            Console.WriteLine("Server Started");

            _gameLoopTask = new Task(RunGameLoop);
            _gameLoopTask.Start();
        }

        public void StopServer()
        {
            Server?.Shutdown("Stopping Server");
            _isRunning = false;
            _gameLoopTask?.Wait();
            Console.WriteLine("Lidgren Testserver was shutdown.");
        }

        #endregion

        #region GameLoop

        /// <summary>
        /// Checking if every client is still alive every 15 seconds
        /// </summary>
        private async void RunGameLoop()
        {
            Console.WriteLine("Starting RunGameLoop Thread.");

            _beatrate = new TimeSpan(0, 0, 1);

            _lastBeat = DateTime.Now;

            int sendRate = 1000 / 66; // 1 sec = 1000ms as Sleep uses ms.
            for (_isRunning = true; _isRunning; await Task.Delay(sendRate))
            {
                // Handle dropping players and long idle connections. Note Client must send a KeepAlive packet within 15s
                if (NetTime.Now > _last15Sec + 15)
                {
                    foreach (Client client in _clients)
                    {
                        if (client.KeepAlive > client.LastKeepAlive)
                            client.LastKeepAlive = NetTime.Now;
                        else if (client.KeepAlive != client.LastKeepAlive)
                        {
                            client.Connection.Deny("Client ID: " + client.Id + " idle connection's keepAlive " + client.KeepAlive + " is < lastKeepAlive " + client.LastKeepAlive);
                        }
                        else if (client.KeepAlive == client.LastKeepAlive)
                            client.LastKeepAlive = NetTime.Now;
                    }
                    _last15Sec = NetTime.Now;
                }
            }
            Console.WriteLine("Stopped RunGameLoop Thread.");
        }

        #endregion

        #region ManageClients

        public Client SearchClient(NetConnection connection)
        {
            return _clients.Find(c => c.Connection == connection);
        }

        public List<Client> GetClients()
        {
            return _clients;
        }

        public void ManageConnectionAppovalClient(NetIncomingMessage incomingMessage)
        {
            string s = incomingMessage.ReadString();
            if (s == _approvalMessage)
            {
                Console.WriteLine("Incoming ConnectionApproval: Approved: " + incomingMessage.SenderConnection);
                incomingMessage.SenderConnection.Approve();

                _clients.Add(new Client(_clients.Count, incomingMessage.SenderConnection, _beatnum));
            }
            else
            {
                Console.WriteLine("Incoming ConnectionApproval: Denied: " + incomingMessage.SenderConnection);
                incomingMessage.SenderConnection.Deny();
            }
        }

        public void ManageDisonnectionClient(Client client)
        {
            Console.WriteLine("Client with client ID " + client.Id + " left the server, byebye.");

            _clients.Remove(client);
        }
        
        #endregion

        #region ManageRooms

        public List<Room> GetRooms()
        {
            return _rooms;
        }

        public Room GetRoom(int roomId)
        {
            return _rooms.Find(r => r.Id == roomId);
        }

        public Room AddRoom()
        {
            Room newRoom = new Room();
            _rooms.Add(newRoom);
            return newRoom;
        }

        public void SentRooms(NetIncomingMessage incomingMessage)
        {
            foreach (Room room in _rooms)
            {
                NetOutgoingMessage outgoingMessage = Server.CreateMessage();
                outgoingMessage.Write((byte)PacketTypes.AddRoom);
                outgoingMessage.Write(room.Id);
                outgoingMessage.Write(room.Name);
                Server.SendMessage(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 4);
            }

        }

        #endregion
    }
}
