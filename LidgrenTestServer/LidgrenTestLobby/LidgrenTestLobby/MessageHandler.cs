using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Lidgren.Network;

namespace LidgrenTestLobby
{
    /// <summary>
    /// Enum with the PacketTypes the server and client can send and recieve
    /// With this you can read message at the correct place in the correct order
    /// The enum can be casted to byte
    /// Must be same enum as Client's PacketTypes
    /// </summary>
    public enum PacketTypes
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
        PlayerJump,
        AddRoom,
        EnterRoom,
        RefreshRooms
    }

    class MessageHandler
    {
        private static LobbyManager _lobbyManager = LobbyManager.Instance;
        private static List<NetConnection> _handlingApproval = new List<NetConnection>();

        public static void Register(object fromPlayer)
        {
            NetIncomingMessage incomingMessage;
            if ((incomingMessage = ((NetServer)fromPlayer).ReadMessage()) != null && incomingMessage.SenderConnection != null)
            {
                Client client = _lobbyManager.SearchClient(incomingMessage.SenderConnection);
                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        {
                            if (client != null)
                                break;

                            _handlingApproval.Add(incomingMessage.SenderConnection);
                            _lobbyManager.ManageConnectionAppovalClient(incomingMessage);
                        }
                        break;
                    case NetIncomingMessageType.Data:

                        if (client == null || incomingMessage.LengthBytes < 1)
                            break;

                        switch ((PacketTypes)incomingMessage.ReadByte())
                        {
                            #region ManageClient
                            //Manage disconnect from client, remove player from ServerManager
                            case PacketTypes.Disconnect:
                                {
                                    _lobbyManager.ManageDisonnectionClient(client);
                                }
                                break;

                            //Beat return, ??
                            case PacketTypes.Beat:
                                {


                                }
                                break;

                            //KeepAlive flag is raised, setting timestamp for further checks
                            case PacketTypes.KeepAlive:
                                {
                                    client.KeepAlive = NetTime.Now;
                                }
                                break;

                            //Handle player movement en send to all but the incommingmessage player
                            case PacketTypes.PlayerMovement:
                                {
                                    //Not yet implemented, maybe different kind of server system.
                                }
                                break;

                            //Handle player jump en send to all but the incommingmessage player
                            case PacketTypes.PlayerJump:
                                {
                                    //Not yet implemented, maybe different kind of server system.
                                }
                                break;
                            case PacketTypes.EnterRoom:
                                {
                                    int enterRoomId = incomingMessage.ReadInt32();
                                    _lobbyManager.GetRoom(enterRoomId).ClientEntersRoom(client);
                                }
                                break;
                            case PacketTypes.RefreshRooms:
                                {
                                    _lobbyManager.SentRooms(incomingMessage);
                                }
                                break;

                            //Handle message send from a client and send a nice return message
                            case PacketTypes.Message:
                                {
                                    //Read incoming message
                                    string clientMessage = incomingMessage.ReadString();
                                    Console.WriteLine("Incoming message: " + clientMessage + " from " + incomingMessage.SenderConnection);

                                    //Send return message
                                    NetOutgoingMessage outgoingMessage = LobbyManager.Server.CreateMessage();
                                    outgoingMessage.Write((byte)PacketTypes.Message);
                                    outgoingMessage.Write("-THE SERVER has heard you, thank you for your kind message-");
                                    LobbyManager.Server.SendMessage(outgoingMessage, incomingMessage.SenderConnection, NetDeliveryMethod.ReliableOrdered, 2);
                                }
                                break;
                                #endregion
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        if (client == null)
                            break;
                        client.StatusChange(incomingMessage);
                        break;
                    default:
                        Console.WriteLine("Other kind of Message");
                        break;
                }
            }
        }
    }
}
