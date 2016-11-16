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
        private List<Player> _activePlayers;
        private NetPeerConfiguration _config;
        private int _idCount;
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
            _activePlayers = new List<Player>();

            Server = new NetServer(_config);
            Server.RegisterReceivedCallback(ServerMessageHandler.Register);
            Server.Start();
            Console.WriteLine("Server Started");

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
                    foreach (Player player in _activePlayers)
                    {
                        //Console.WriteLine("KeepAlive Check "+player.Name+" (Id"+player.Id+") "+ player.Connection.Status+" RTT "+player.Connection.AverageRoundtripTime +" keepAlive "+player.keepAlive+" vs. lastKeepAlive "+player.lastKeepAlive);
                        if (player.KeepAlive > player.LastKeepAlive)
                            player.LastKeepAlive = NetTime.Now;
                        else if (player.KeepAlive != player.LastKeepAlive)
                        {
                            player.Connection.Deny(player.Name + " (Id" + player.Id + ") idle connection's keepAlive " + player.KeepAlive + " is < lastKeepAlive " + player.LastKeepAlive);
                        }
                        else if (player.KeepAlive == player.LastKeepAlive)
                            player.LastKeepAlive = NetTime.Now;
                    }
                    _last15Sec = NetTime.Now;
                }

                //Send update beats.
                if ((_lastBeat + _beatrate) < DateTime.Now)
                {
                    //Send a beat to all users. Because the server doesn't really know if they disconnected unless we are sending packets to them.
                    //Beats go out every 2 seconds.
                    foreach (Player player in _activePlayers)
                    {
                        NetOutgoingMessage outgoingMessage = Server.CreateMessage();
                        outgoingMessage.Write((byte)PacketTypes.Beat);
                        outgoingMessage.Write((Int16)_beatnum);
                        outgoingMessage.Write((float)player.Connection.AverageRoundtripTime / 2f);
                        Server.SendMessage(outgoingMessage, player.Connection, NetDeliveryMethod.ReliableOrdered, 4);
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

                if (_activePlayers.Count == 0)
                    _idCount = 0;

                AddPlayerToGame(incomingMessage);
            }
            else
            {
                Console.WriteLine("Incoming message ConnectionApproval: Deny: " + incomingMessage.SenderConnection);
                incomingMessage.SenderConnection.Deny();
            }
        }

        public void ManageDisonnectionOfPlayer(Player player)
        {
            Console.WriteLine(player.Name + " (Id" + player.Id + ") has disconnected, removing player object.");

            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.RemovePlayer);
            _outgoingMessage.Write((Int16)player.Id);
            Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 1);

            _activePlayers.Remove(player);

            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
            _outgoingMessage.Write((Int16)_activePlayers.Count);
            Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
        }

        public Player SearchPlayer(NetConnection connection)
        {
            return _activePlayers.Find(p => p.Connection == connection);
        }

        #endregion

        #region ManagePlayer

        public void AddPlayerToGame(NetIncomingMessage incomingMessage)
        {
            Console.WriteLine("Assigning new player the name of: Player " + ++_idCount);
            Player newPlayer = new Player(_idCount, incomingMessage.SenderConnection, "Player " + _idCount, _beatnum);

            _activePlayers.Add(newPlayer);
            Thread.Sleep(2000);

            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.Message);
            _outgoingMessage.Write("You are now connected to Lidgren TestServer.");
            Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

            Console.WriteLine("Sending to: " + incomingMessage.SenderConnection);

            // Assign Id number to client
            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.AssignId);
            _outgoingMessage.Write(_idCount);
            Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);

            newPlayer.SentPlayerToOthers(incomingMessage);

            //Send all otherPlayers to new player excl. himself
            foreach (Player otherPlayer in _activePlayers.Where(otherPlayer => otherPlayer.Id != newPlayer.Id))
            {
                _outgoingMessage = Server.CreateMessage();
                _outgoingMessage.Write((byte)PacketTypes.AddPlayer);
                _outgoingMessage.Write(otherPlayer.Id);
                _outgoingMessage.Write(otherPlayer.Name);
                _outgoingMessage.Write(otherPlayer.Position);
                Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 7);
            }

            //Send the current playercount to current player (yes sending all isn't enough for new player)
            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
            _outgoingMessage.Write((Int16)_activePlayers.Count);
            Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);

            //Send the current playercount to all but current player
            _outgoingMessage = Server.CreateMessage();
            _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
            _outgoingMessage.Write((Int16)_activePlayers.Count);
            Server.SendToAll(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);

            //for (int i = 0; i < 100; i++)
            //{
            //    Console.WriteLine("Sending message");
            //    _outgoingMessage = Server.CreateMessage();
            //    _outgoingMessage.Write((byte)PacketTypes.Message);
            //    _outgoingMessage.Write("You are now connected to Lidgren TestServer.");
            //    Server.SendMessage(_outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);
            //}
        }

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

            Player player = _activePlayers.Find(p => p.Connection == incomingMessage.SenderConnection);

            if (player == null)
            {
                return; //Don't accept data from connections that don't have a player attached
            }

            NetConnectionStatus status = (NetConnectionStatus)incomingMessage.ReadByte();
            string reason = incomingMessage.ReadString();

            if (player.Connection.Status == NetConnectionStatus.Disconnected || player.Connection.Status == NetConnectionStatus.Disconnecting)
            {
                //SendLobbyMessage("Server", player.Name + " (Id" + player.Id + ") has disconnected.");
                _activePlayers.Remove(player);

                if (Server.Connections.Count > 0)
                {
                    _outgoingMessage = Server.CreateMessage();
                    _outgoingMessage.Write((byte)PacketTypes.RemovePlayer);
                    _outgoingMessage.Write((Int16)player.Id);
                    Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 1);

                    _outgoingMessage = Server.CreateMessage();
                    _outgoingMessage.Write((byte)PacketTypes.PlayerCount);
                    _outgoingMessage.Write((Int16)_activePlayers.Count);
                    Server.SendMessage(_outgoingMessage, Server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
                }
            }
            Console.WriteLine(player.Name + " (Id" + player.Id + ") status changed to " + status + " (" + reason + ") " + _activePlayers.Count);
        }

        #endregion

    }
}