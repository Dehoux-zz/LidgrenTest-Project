using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Policy;
using Lidgren.Network;
using System.Threading;

namespace LidgrenTestServer
{
    public class ServerManager
    {
        public NetServer Server { get; private set; }
        private List<Client> _activeClients;
        private List<ServerRoom> _serverRooms;
        private NetPeerConfiguration _config;
        private int _clientIdCount;
        private int _roomIdCount;
        private string _approvalMessage;
        private Thread _t1;
        private int _beatnum;
        private DateTime _lastBeat;
        private TimeSpan _beatrate;
        private double _last15Sec;
        private volatile bool _isRunning;
        private const int TicksPerSecond = 66;
        private NetOutgoingMessage _outgoingMessage;

        private static volatile ServerManager _instance;
        private static object _syncRoot = new object();

        private ServerManager() { }

        public static ServerManager Instance
        {
            get
            {
                if (_instance != null) return _instance;
                lock (_syncRoot)
                {
                    if (_instance == null)
                        _instance = new ServerManager();
                }

                return _instance;
            }
        }

        #region ServerControl
        public void InitialiseServerManager(int gamePort, int maxPlayers, string approvalMessage)
        {
            // (needed for RegisterReceivedCallback, replace with AsyncOperationM... see CrabBattleServer)
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            _config = new NetPeerConfiguration("LidgrenTest")
            {
                Port = gamePort,
                MaximumConnections = maxPlayers
            };
            _config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            _approvalMessage = approvalMessage;
        }

        public void StartServer()
        {
            _activeClients = new List<Client>();
            _serverRooms = new List<ServerRoom>();

            Server = new NetServer(_config);
            Server.RegisterReceivedCallback(ServerMessageHandler.Register);
            Server.Start();
            Console.WriteLine("Server Started");

            _serverRooms.Add(new ServerRoom(2));

            _t1 = new Thread(RunGameLoop) { Name = "Game Loop Thread" };
            _t1.Start();
            Thread.Sleep(500);
        }

        public void StopServer()
        {
            Server?.Shutdown("Stopping Server");
            _t1?.Join();
            Console.WriteLine("Lidgren Testserver was shutdown.");
        }
        #endregion

        #region GameLoop
        private void RunGameLoop()
        {
            Console.WriteLine("Starting RunGameLoop Thread.");

            _beatrate = new TimeSpan(0, 0, 1);

            _lastBeat = DateTime.Now;

            int sendRate = 1000 / TicksPerSecond; // 1 sec = 1000ms as Sleep uses ms.
            for (_isRunning = true; _isRunning; Thread.Sleep(sendRate))
            {
                // Handle dropping players and long idle connections. Note Client must send a KeepAlive packet within 15s
                if (NetTime.Now > _last15Sec + 15)
                {
                    foreach (Client client in _activeClients)
                    {
                        //Console.WriteLine("KeepAlive Check "+player.Name+" (Id"+player.Id+") "+ player.Connection.Status+" RTT "+player.Connection.AverageRoundtripTime +" keepAlive "+player.keepAlive+" vs. lastKeepAlive "+player.lastKeepAlive);
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

                //Send update beats.
                if ((_lastBeat + _beatrate) < DateTime.Now)
                {
                    //Send a beat to all users. Because the server doesn't really know if they disconnected unless we are sending packets to them.
                    //Beats go out every 2 seconds.
                    foreach (Client client in _activeClients)
                    {
                        NetOutgoingMessage outgoingMessage = Server.CreateMessage();
                        outgoingMessage.Write((byte)PacketTypes.Beat);
                        outgoingMessage.Write((Int16)_beatnum);
                        outgoingMessage.Write((float)client.Connection.AverageRoundtripTime / 2f);
                        Server.SendMessage(outgoingMessage, client.Connection, NetDeliveryMethod.ReliableOrdered, 4);
                    }
                    _beatnum++;
                    _lastBeat = DateTime.Now;
                }
            }
            Console.WriteLine("Stopped RunGameLoop Thread.");
        }
        #endregion

        #region ManageServer

        public void ManageConnectionAppoval(NetIncomingMessage incomingMessage)
        {
            string s = incomingMessage.ReadString();
            if (s == _approvalMessage)
            {
                Console.WriteLine("Incoming message ConnectionApproval: Approved: " + incomingMessage.SenderConnection);
                incomingMessage.SenderConnection.Approve();

                if (_clientIdCount == 0)
                {
                    _clientIdCount = 0;
                    _roomIdCount = 0;
                }

                AddClient(incomingMessage);
            }
            else
            {
                Console.WriteLine("Incoming message ConnectionApproval: Deny: " + incomingMessage.SenderConnection);
                incomingMessage.SenderConnection.Deny();
            }
        }

        public void ManageDisonnectionOfClient(Client client)
        {
            Console.WriteLine("Client with client ID " + client.Id + " left the server, byebye.");

            _activeClients.Remove(client);

            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
            _outgoingMessage.Write((Int16)_activeClients.Count);
            Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
        }

        //Console.WriteLine(player.Name + " (Id" + player.Id + ") has disconnected, removing player object.");

        //    _outgoingMessage = Server.CreateMessage();
        //    _outgoingMessage.Write((byte)PacketTypes.RemovePlayer);
        //    _outgoingMessage.Write((Int16)player.Id);
        //    Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 1);

        //    _activeClients.Remove(player);

        //    _outgoingMessage = Server.CreateMessage();
        //    _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
        //    _outgoingMessage.Write((Int16)_activeClients.Count);
        //    Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 6);

        public Client SearchClient(NetConnection connection)
        {
            return _activeClients.Find(c => c.Connection == connection);
        }

        public ServerRoom SearchServerRoom(int roomId)
        {
            return _serverRooms.Find(r => r.Id == roomId);
        }

        #endregion

        #region ManagePlayer

        public void AddClient(NetIncomingMessage incomingMessage)
        {
            Console.WriteLine("Assigning new player the name of: Player " + ++_clientIdCount);
            Client newClient = new Client(_clientIdCount, incomingMessage.SenderConnection, _beatnum);

            _activeClients.Add(newClient);

            //Wait before sending back, client may be slower! <-- Tricky code
            Thread.Sleep(2000);

            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.Message);
            _outgoingMessage.Write("You are now connected to Lidgren TestServer.");
            Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

            Console.WriteLine("Sending to: " + incomingMessage.SenderConnection);

            // Assign Id number to client
            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.AssignId);
            _outgoingMessage.Write(_clientIdCount);
            Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);

            foreach (ServerRoom serverRoom in _serverRooms)
            {
                _outgoingMessage = Server.CreateMessage();
                _outgoingMessage.Write((byte)PacketTypes.AddRoom);
                _outgoingMessage.Write(serverRoom.Id);
                _outgoingMessage.Write(serverRoom.Name);
                Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 2);
            }
        }
        
        //public void AddPlayerToGame(NetIncomingMessage incomingMessage)
        //{
        //    Console.WriteLine("Assigning new player the name of: Player " + ++_playerIdCount);
        //    Player newPlayer = new Player(_playerIdCount, incomingMessage.SenderConnection, "Player " + _playerIdCount, _beatnum);

        //    _activePlayers.Add(newPlayer);

        //    //Wait before sending back, client may be slower! <-- Tricky code
        //    Thread.Sleep(2000);

        //    _outgoingMessage = Server.CreateMessage();
        //    _outgoingMessage.Write((byte)PacketTypes.Message);
        //    _outgoingMessage.Write("You are now connected to Lidgren TestServer.");
        //    Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

        //    Console.WriteLine("Sending to: " + incomingMessage.SenderConnection);

        //    // Assign Id number to client
        //    _outgoingMessage = Server.CreateMessage();
        //    _outgoingMessage.Write((byte)PacketTypes.AssignId);
        //    _outgoingMessage.Write(_playerIdCount);
        //    Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);

        //    newPlayer.SentPlayerToOthers(incomingMessage);

        //    //Send all otherPlayers to new player excl. himself
        //    foreach (Player otherPlayer in _activePlayers.Where(otherPlayer => otherPlayer.Id != newPlayer.Id))
        //    {
        //        _outgoingMessage = Server.CreateMessage();
        //        _outgoingMessage.Write((byte)PacketTypes.AddPlayer);
        //        _outgoingMessage.Write(otherPlayer.Id);
        //        _outgoingMessage.Write(otherPlayer.Name);
        //        _outgoingMessage.Write(otherPlayer.Position);
        //        Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 7);
        //    }

        //    //Send the current playercount to current player (yes sending all isn't enough for new player)
        //    _outgoingMessage = Server.CreateMessage();
        //    _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
        //    _outgoingMessage.Write((Int16)_activePlayers.Count);
        //    Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);

        //    //Send the current playercount to all but current player
        //    _outgoingMessage = Server.CreateMessage();
        //    _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
        //    _outgoingMessage.Write((Int16)_activePlayers.Count);
        //    Server.SendToAll(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);

        //    //for (int i = 0; i < 100; i++)
        //    //{
        //    //    Console.WriteLine("Sending message");
        //    //    _outgoingMessage = Server.CreateMessage();
        //    //    _outgoingMessage.Write((byte)PacketTypes.Message);
        //    //    _outgoingMessage.Write("You are now connected to Lidgren TestServer.");
        //    //    Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
        //    //}
        //}

        #endregion

        #region ManageClient

        public void ManageMessageOfClient(NetIncomingMessage incomingMessage)
        {
            string clientMessage = incomingMessage.ReadString();
            Console.WriteLine("Incoming message: " + clientMessage + " from " + incomingMessage.SenderConnection);
            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.Message);
            _outgoingMessage.Write("-THE SERVER has heard you-");
            Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 2);
        }

        public void ManageStatusChangeOfClient(NetIncomingMessage incomingMessage)
        {
            Console.WriteLine(incomingMessage.SenderConnection + " status changed. " + incomingMessage.SenderConnection.Status);

            Client client = _activeClients.Find(c => c.Connection == incomingMessage.SenderConnection);

            if (client == null)
            {
                return; //Don't accept data from connections that don't have a client attached
            }

            NetConnectionStatus status = (NetConnectionStatus)incomingMessage.ReadByte();
            string reason = incomingMessage.ReadString();

            if (client.Connection.Status == NetConnectionStatus.Disconnected || client.Connection.Status == NetConnectionStatus.Disconnecting)
            {
                //SendLobbyMessage("Server", player.Name + " (Id" + player.Id + ") has disconnected.");
                _activeClients.Remove(client);

                if (Server.Connections.Count > 0)
                {
                    _outgoingMessage = Server.CreateMessage();
                    _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
                    _outgoingMessage.Write((Int16)_activeClients.Count);
                    Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
                }
            }
            Console.WriteLine("Client id " + client.Id + "; status changed to " + status + " (" + reason + ") " + ". Clients left: " + _activeClients.Count);
        }

        #endregion


        public void SendPlayerToPlayer(Client sendPlayer, Client toPlayer)
        {
            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.AddPlayer);
            _outgoingMessage.Write(sendPlayer.AttachedPlayer.Name);
            _outgoingMessage.Write(sendPlayer.AttachedPlayer.Position);
            Server.SendMessage(_outgoingMessage, toPlayer.Connection, NetDeliveryMethod.ReliableOrdered, 7);
        }
    }
}