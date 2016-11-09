using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lidgren.Network;
using System.Threading;
using UnityEngine;

namespace LidgrenTestServer
{
    class GameServer
    {
        private static List<Player> activePlayers;
        private NetPeerConfiguration config;
        private int idCount;
        private static NetServer server;
        private static string approvalMessage;
        private Thread t1;
        private int beatnum = 0;
        private DateTime lastBeat;
        private TimeSpan beatrate;
        private double last15sec;
        private volatile bool isRunning;
        public int ticksPerSecond = 66;

        public GameServer(int gamePort, int maxPlayers)
        {
            // (needed for RegisterReceivedCallback, TODO replace with AsyncOperationM... see CrabBattleServer)
            if (SynchronizationContext.Current == null)
                SynchronizationContext.SetSynchronizationContext(new SynchronizationContext());

            config = new NetPeerConfiguration("LidgrenTest");
            config.Port = gamePort;
            config.MaximumConnections = maxPlayers;
            config.EnableMessageType(NetIncomingMessageType.ConnectionApproval);

            approvalMessage = "SecretValue";
        }

        public void StartServer()
        {
            activePlayers = new List<Player>();

            server = new NetServer(config);
            server.RegisterReceivedCallback(new SendOrPostCallback(HandleMessages));
            server.Start();
            Console.WriteLine("Server Started");


            t1 = new Thread(new ThreadStart(RunGameLoop));
            t1.Name = "Game Loop Thread";
            t1.Start();
            Thread.Sleep(500);
        }

        public void StopServer()
        {
            //isRunning = false;
            if (server != null)
                server.Shutdown(" Stopping Server");
            if (t1 != null)
                t1.Join();
            Console.WriteLine(" Lidgren Testserver was shutdown.");
        }

        public void RunGameLoop()
        {
            Console.WriteLine(" Starting RunGameLoop Thread.");
            //isRunning = true;

            beatrate = new TimeSpan(0, 0, 1);

            lastBeat = DateTime.Now;

            NetOutgoingMessage outgoingMessage;

            int sendRate = 1000 / ticksPerSecond; // 1 sec = 1000ms as Sleep uses ms.
            for (isRunning = true; isRunning; Thread.Sleep(sendRate))
            {
                // Handle dropping players and long idle connections. Note Client must send a KeepAlive packet within 15s
                if (NetTime.Now > last15sec + 15)
                {
                    foreach (Player player in activePlayers)
                    {
                        //Console.WriteLine("KeepAlive Check "+player.Name+" (Id"+player.Id+") "+ player.Connection.Status+" RTT "+player.Connection.AverageRoundtripTime +" keepAlive "+player.keepAlive+" vs. lastKeepAlive "+player.lastKeepAlive);
                        if (player.keepAlive > player.lastKeepAlive)
                            player.lastKeepAlive = NetTime.Now;
                        else if (player.keepAlive != player.lastKeepAlive)
                        {
                            player.Connection.Deny(player.Name + " (Id" + player.Id + ") idle connection's keepAlive " + player.keepAlive + " is < lastKeepAlive " + player.lastKeepAlive);
                        }
                        else if (player.keepAlive == player.lastKeepAlive)
                            player.lastKeepAlive = NetTime.Now;
                    }
                    last15sec = NetTime.Now;
                }

                //Send update beats.
                if ((lastBeat + beatrate) < DateTime.Now)
                {
                    //Send a beat to all users. Because the server doesn't really know if they disconnected unless we are sending packets to them.
                    //Beats go out every 2 seconds.
                    foreach (Player player in activePlayers)
                    {
                        outgoingMessage = server.CreateMessage();
                        outgoingMessage.Write((byte)PacketTypes.Beat);
                        outgoingMessage.Write((Int16)beatnum);
                        outgoingMessage.Write(player.Connection.AverageRoundtripTime / 2f);
                        server.SendMessage(outgoingMessage, player.Connection, NetDeliveryMethod.ReliableOrdered, 4);
                    }
                    beatnum++;
                    lastBeat = DateTime.Now;
                }
            }
            Console.WriteLine(" Stopped RunGameLoop Thread.");
        }

        private void HandleMessages(object fromPlayer)
        {
            NetIncomingMessage incomingMessage;
            if ((incomingMessage = ((NetServer)fromPlayer).ReadMessage()) != null && incomingMessage.SenderConnection != null)
            {
                NetOutgoingMessage outgoingMessage;

                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:

                        string s = incomingMessage.ReadString();
                        if (s == approvalMessage)
                        {
                            Console.WriteLine("Incoming message ConnectionApproval: Approved: " + incomingMessage.SenderConnection.ToString());
                            incomingMessage.SenderConnection.Approve();

                            if (activePlayers.Count == 0)
                                idCount = 0;
                        }
                        else
                        {
                            Console.WriteLine("Incoming message ConnectionApproval: Deny: " + incomingMessage.SenderConnection.ToString());
                            incomingMessage.SenderConnection.Deny();
                        }
                        break;
                    case NetIncomingMessageType.Data:
                        Player player = activePlayers.Find(p => p.Connection == incomingMessage.SenderConnection);
                        if (player == null || incomingMessage.LengthBytes < 1)
                        {
                            //You are not a player!!
                            break;
                        }

                        switch ((PacketTypes)incomingMessage.ReadByte())
                        {
                            case PacketTypes.Disconnect:
                                {
                                    //Player requests to disconnect from the server.
                                    Console.WriteLine(player.Name + " (Id" + player.Id + ") has disconnected, removing player object.");

                                    outgoingMessage = server.CreateMessage();
                                    outgoingMessage.Write((byte)PacketTypes.RemovePlayer);
                                    outgoingMessage.Write((Int16)player.Id);
                                    server.SendMessage(outgoingMessage, server.Connections, NetDeliveryMethod.ReliableOrdered, 1);

                                    activePlayers.Remove(player);

                                    outgoingMessage = server.CreateMessage();
                                    outgoingMessage.Write((byte)PacketTypes.PlayerCount);
                                    outgoingMessage.Write((Int16)activePlayers.Count);
                                    server.SendMessage(outgoingMessage, server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
                                }
                                break;
                            case PacketTypes.PlayerMovement:
                                {
                                    //Player did something!?

                                    //Set player object in server
                                    player.Position = incomingMessage.ReadVector2();
                                    player.VelocityX = incomingMessage.ReadFloat();
                                    player.VelocityY = incomingMessage.ReadFloat();

                                    //Console.WriteLine("Player moved to: " + player.Position.x + " " + player.Position.y);

                                    //Tell everyone something happened to this player
                                    outgoingMessage = server.CreateMessage();
                                    outgoingMessage.Write((byte)PacketTypes.PlayerMovement);
                                    outgoingMessage.Write((Int16)player.Id);
                                    outgoingMessage.Write(player.Position);
                                    outgoingMessage.Write(player.VelocityX);
                                    outgoingMessage.Write(player.VelocityY);
                                    outgoingMessage.Write(player.Connection.AverageRoundtripTime / 2f);
                                    server.SendToAll(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.UnreliableSequenced, 10);
                                }
                                break;
                            case PacketTypes.Message:
                                {
                                    Console.WriteLine("Incoming message: " + incomingMessage.ReadString() + " from " + incomingMessage.SenderConnection);
                                    outgoingMessage = server.CreateMessage();
                                    outgoingMessage.Write((byte)PacketTypes.Message);
                                    outgoingMessage.Write("Weow returns!");
                                    server.SendMessage(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 2);
                                }
                                break;
                            case PacketTypes.Beat:
                                {
                                    if (incomingMessage.LengthBytes < 2) break;// .PeekInt16()) break;
                                                                               //if (!player.Ready) break;
                                    player.LastBeat = incomingMessage.ReadInt16();
                                    player.Position = incomingMessage.ReadVector2();
                                }
                                break;
                            case PacketTypes.KeepAlive:
                                {
                                    // one way heartbeat from client. Server sets a timestamp.
                                    // Unity can potentially keep unstable client threads open that waist server resources
                                    player.keepAlive = NetTime.Now;
                                }
                                break;
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        Console.WriteLine(incomingMessage.SenderConnection.ToString() + " status changed. " + (NetConnectionStatus)incomingMessage.SenderConnection.Status);

                        player = activePlayers.Find(p => p.Connection == incomingMessage.SenderConnection);
                        if (player == null && incomingMessage.SenderConnection.Status == NetConnectionStatus.Connected)
                        {
                            AddNewPlayer(incomingMessage); break;
                        }
                        if (player == null)
                        {
                            break; //Don't accept data from connections that don't have a player attached
                        }

                        NetConnectionStatus status = (NetConnectionStatus)incomingMessage.ReadByte();
                        string reason = incomingMessage.ReadString();

                        if (player.Connection.Status == NetConnectionStatus.Disconnected || player.Connection.Status == NetConnectionStatus.Disconnecting)
                        {
                            //SendLobbyMessage("Server", player.Name + " (Id" + player.Id + ") has disconnected.");
                            activePlayers.Remove(player);

                            if (server.Connections.Count > 0)
                            {
                                outgoingMessage = server.CreateMessage();
                                outgoingMessage.Write((byte)PacketTypes.RemovePlayer);
                                outgoingMessage.Write((Int16)player.Id);
                                server.SendMessage(outgoingMessage, server.Connections, NetDeliveryMethod.ReliableOrdered, 1);

                                outgoingMessage = server.CreateMessage();
                                outgoingMessage.Write((byte)PacketTypes.PlayerCount);
                                outgoingMessage.Write((Int16)activePlayers.Count);
                                server.SendMessage(outgoingMessage, server.Connections, NetDeliveryMethod.ReliableOrdered, 6);
                            }
                        }
                        Console.WriteLine(player.Name + " (Id" + player.Id + ") status changed to " + status + " (" + reason + ") " + activePlayers.Count);
                        break;
                    default:
                        Console.WriteLine("Other kind of Message");

                        break;
                }
            }
        }

        public void AddNewPlayer(NetIncomingMessage incomingMessage)
        {
            NetOutgoingMessage outgoingMessage;

            Console.WriteLine("Assigning new player the name of Player " + (++idCount) + ".");
            Player newPlayer = new Player(idCount, incomingMessage.SenderConnection, "Player " + idCount, beatnum);

            activePlayers.Add(newPlayer);
            //SendLobbyMessage("Server", "Player " + idCount + " has connected. Connected players: " + activePlayers.Count);

            outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.Message);
            outgoingMessage.Write("You are now connected to Lidgren TestServer.");
            server.SendMessage(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 0);

            // Assign Id number to client
            outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.AssignId);
            outgoingMessage.Write((Int32)(idCount));
            server.SendMessage(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 1);

            //Send the new player info to all
            outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.AddPlayer);
            outgoingMessage.Write(newPlayer.Id);
            outgoingMessage.Write(newPlayer.Name);
            outgoingMessage.Write(newPlayer.Position);
            server.SendToAll(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 7);

            //Send all players to new player excl. himself
            foreach (Player player in activePlayers)
            {
                if (player.Id != newPlayer.Id)
                {
                    outgoingMessage = server.CreateMessage();
                    outgoingMessage.Write((byte)PacketTypes.AddPlayer);
                    outgoingMessage.Write(player.Id);
                    outgoingMessage.Write(player.Name);
                    outgoingMessage.Write(player.Position);
                    server.SendMessage(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 7);
                }
            }


            //Send the current playercount to current player (yes sending all isn't enough for new player)
            outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.PlayerCount);
            outgoingMessage.Write((Int16)activePlayers.Count);
            server.SendMessage(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);

            //Send the current playercount to all but current player
            outgoingMessage = server.CreateMessage();
            outgoingMessage.Write((byte)PacketTypes.PlayerCount);
            outgoingMessage.Write((Int16)activePlayers.Count);
            server.SendToAll(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 6);
        }
    }

    /// <summary>
    /// Character class
    /// 
    /// This class is passed around.
    /// It holds the position, name ( not used in this example ) ( even thou it gets sent all over )
    /// Connection (ip+port)
    /// 
    /// </summary>
    class Player
    {
        public int Id;
        public string Name;
        public NetConnection Connection;
        public Vector2 Position;
        public float VelocityX;
        public float VelocityY;
        public int LastBeat;
        public double keepAlive = 0, lastKeepAlive = 0;
        public Player(int id, NetConnection connection, string name, int curBeat)
        {
            Id = id;
            Connection = connection;
            Name = name;
            Position = new Vector2();
            LastBeat = curBeat;
            VelocityX = 0;
            VelocityY = 0;
        }
    }


    // This is good way to handle different kind of packets
    // there has to be some way, to detect, what kind of packet/message is incoming.
    // With this, you can read message in correct order ( ie. Can't read int, if its string etc )

    // Best thing about this method is, that even if you change the order of the entrys in enum, the system won't break up
    // Enum can be casted ( converted ) to byte
    enum PacketTypes
    {
        Connect,
        Disconnect,
        RemovePlayer,
        Message,
        AssignId,
        PlayerCount,
        PlayerMovement,
        AddPlayer,
        Beat,
        KeepAlive
    }

    //class LoginPacket
    //{
    //    public string MyName { get; set; }
    //    public LoginPacket(string name)
    //    {
    //        MyName = name;
    //    }
    //}

    // Movement directions
    // This way we can just send byte over net and no need to send anything bigger
    enum MoveDirection
    {
        UP,
        DOWN,
        LEFT,
        RIGHT,
        NONE
    }
}