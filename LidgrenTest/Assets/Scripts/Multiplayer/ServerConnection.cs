using System.Collections;
using Lidgren.Network;
using System.Collections.Generic;
using System;
using UnityEngine;

public enum PackageTypes
{
    Disconnect,
    RemovePlayer,
    Message,
    AssignId,
    PlayerCount,
    PlayerMovement,
    AddPlayer,
    Beat,
    KeepAlive,
    PlayerJump
}

public sealed class ServerConnection
{

    private static volatile ServerConnection _instance;
    private static object syncRoot = new object();
    private static float lastSec = 0f;

    public static NetClient Client;
    public static int ClientId;
    public static float Roundtriptime = 0f;

    public static string HostIp;
    public static int HostPort;
    public static string ConnectionValue;

    public static int TestCounter = 0;

    /// <summary>
    /// Getter and creator of the ServerConnecton class.
    /// </summary>
    public static ServerConnection Instance
    {
        get
        {
            if (_instance == null)
                lock (syncRoot)
                    if (_instance == null)
                        _instance = new ServerConnection();
            return _instance;
        }
    }

    private ServerConnection() { }

    /// <summary>
    /// Establish connection to server.
    /// </summary>
    /// <param name="serverGameName">GameName to find server</param>
    /// <param name="hostIp">IP of the host</param>
    /// <param name="hostPort">Port where the server is running</param>
    /// <param name="connectionValue">Secret value to connection approval</param>
    public static void CreateConnection(string serverGameName, string hostIp, int hostPort, string connectionValue) //more constructors for additional options (like maxnumberconnections)
    {
        NetPeerConfiguration connectionConfig = new NetPeerConfiguration(serverGameName);
        //additional config options...
        Client = new NetClient(connectionConfig);
        Client.Start();

        HostIp = hostIp;
        HostPort = hostPort;
        ConnectionValue = connectionValue;

        NetOutgoingMessage outgoingMessage = Client.CreateMessage();
        outgoingMessage.Write(connectionValue);
        Client.Connect(hostIp, hostPort, outgoingMessage);
        lastSec = Time.time;
    }

    public static NetOutgoingMessage CreateNetOutgoingMessage()
    {
        return Client.CreateMessage();
    }

    public static void SendNetOutgoingMessage(NetOutgoingMessage outgoingMessage, NetDeliveryMethod networkDeliveryMethod, int channel)
    {
        Client.SendMessage(outgoingMessage, networkDeliveryMethod, channel);
    }

    public static void StopConnection()
    {
        NetOutgoingMessage outgoingMessage = Client.CreateMessage();
        outgoingMessage.Write((byte)PackageTypes.Disconnect);
        Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 1);
        Client.Shutdown(": Bye All");
    }

    public static void SendKeepAlive()
    {
        NetOutgoingMessage outgoingMessage = Client.CreateMessage();
        outgoingMessage.Write((byte)PackageTypes.KeepAlive);
        Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableUnordered, 4);
    }

    public static void CheckIncomingMessage()
    {
        if (Client.Status == NetPeerStatus.Running)
        {

            NetIncomingMessage incomingMessage;
            if ((incomingMessage = Client.ReadMessage()) != null)
            {
                Debug.Log(incomingMessage.MessageType);
                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.StatusChanged:
                        {
                            switch ((NetConnectionStatus)incomingMessage.ReadByte())
                            {
                                //When connected to the server
                                case NetConnectionStatus.Connected:
                                    {
                                        DebugConsole.Log("yey conected: " + TestCounter);

                                    }
                                    break;
                                //When disconnected from the server
                                case NetConnectionStatus.Disconnected:
                                    {
                                        string reason = incomingMessage.ReadString();
                                        if (string.IsNullOrEmpty(reason))
                                            NetworkManager.Instance.WriteConsoleMessage("Disconnected");
                                        else
                                            NetworkManager.Instance.WriteConsoleMessage("Disconnected, Reason: " + reason);
                                    }
                                    break;
                            }
                        }
                        break;


                    case NetIncomingMessageType.Data:
                        {
                            TestCounter++;
                            switch ((PackageTypes)incomingMessage.ReadByte())
                            {
                                case PackageTypes.Message:
                                    {
                                        NetworkManager.Instance.WriteConsoleMessage(incomingMessage.ReadString());
                                    }
                                    break;
                                case PackageTypes.AssignId:
                                    {
                                        ClientId = incomingMessage.ReadInt32();
                                    }
                                    break;
                                case PackageTypes.AddPlayer:
                                    {
                                        NetworkManager.Instance.AddPlayer(incomingMessage);
                                        lastSec = Time.time;
                                    }
                                    break;
                                case PackageTypes.PlayerMovement:
                                    {
                                        int playerId = incomingMessage.ReadInt16();
                                        Player player = NetworkManager.Instance.FindPlayer(playerId);
                                        player.NetIncomingMessageMovePlayer(incomingMessage);
                                    }
                                    break;
                                case PackageTypes.PlayerJump:
                                    {
                                        int playerId = incomingMessage.ReadInt16();
                                        Player player = NetworkManager.Instance.FindPlayer(playerId);
                                        player.NetIncomingMessageJumpPlayer(incomingMessage);
                                    }
                                    break;
                                case PackageTypes.Beat:
                                    {
                                        Player localPlayer = NetworkManager.Instance.GetLocalPlayer();

                                        NetOutgoingMessage outgoingMessage = CreateNetOutgoingMessage();
                                        outgoingMessage.Write((byte)PackageTypes.Beat);
                                        outgoingMessage.Write(incomingMessage.ReadInt16());
                                        if (localPlayer != null)
                                        {
                                            outgoingMessage.Write(localPlayer.transform.position);
                                        }
                                        else
                                        {
                                            outgoingMessage.Write(Vector2.zero);
                                        }
                                        Client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 4);
                                        Roundtriptime = incomingMessage.ReadFloat();
                                    }
                                    break;
                            }
                        }
                        break;
                    case NetIncomingMessageType.WarningMessage:
                        {
                            DebugConsole.Log(incomingMessage.ReadString());
                        }
                        break;
                }
            }
        }

        if (Time.time > lastSec + 1)
        {
            lastSec = Time.time;
            SendKeepAlive();

            if (Client.ConnectionStatus == NetConnectionStatus.Disconnected)
            {
                NetworkManager.Instance.WriteConsoleMessage("Lost connection to server. Attempting to reconnect...");
                NetOutgoingMessage outgoingMessage = Client.CreateMessage();
                outgoingMessage.Write(ConnectionValue);

                Client.Connect(HostIp, HostPort, outgoingMessage);
            }
        }
    }
}