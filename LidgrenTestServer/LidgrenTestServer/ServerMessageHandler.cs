using System;
using Lidgren.Network;

namespace LidgrenTestServer
{
    public static class ServerMessageHandler
    {
        private static ServerManager _serverManager = ServerManager.Instance;

        public static void Register(object fromPlayer)
        {
            NetIncomingMessage incomingMessage;
            if ((incomingMessage = ((NetServer)fromPlayer).ReadMessage()) != null && incomingMessage.SenderConnection != null)
            {
                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        {
                            _serverManager.ManageConnectionAppoval(incomingMessage);
                        }
                        break;
                    case NetIncomingMessageType.Data:




                        Client client = _serverManager.SearchClient(incomingMessage.SenderConnection);
                        if (client == null || incomingMessage.LengthBytes < 1)
                            break;
                        switch ((PacketTypes)incomingMessage.ReadByte())
                        {
                            #region ManageServer
                            //Manage disconnect from client, remove player from ServerManager
                            case PacketTypes.Disconnect:
                                {
                                    _serverManager.ManageDisonnectionOfClient(client);
                                }
                                break;
                            #endregion


                            #region ManagePlayer
                            //Save last best en position it has, for dubble check
                            case PacketTypes.Beat:
                                {
                                    if (incomingMessage.LengthBytes < 2) break;
                                    client.LastBeat = incomingMessage.ReadInt16();
                                    client.AttachedPlayer.Position = incomingMessage.ReadVector2();
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
                                    client.HandlePlayerMovement(incomingMessage);
                                }
                                break;

                            //Handle player jump en send to all but the incommingmessage player
                            case PacketTypes.PlayerJump:
                                {
                                    client.HandlePlayerJump(incomingMessage);
                                }
                                break;
                            case PacketTypes.EnterRoom:
                                {
                                    client.JoinServerRoom(incomingMessage);
                                }
                                break;
                            #endregion


                            #region ManageClient
                            //Handle message send from a client and send a nice return message
                            case PacketTypes.Message:
                                {
                                    _serverManager.ManageMessageOfClient(incomingMessage);
                                }
                                break;
                                #endregion
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:
                        _serverManager.ManageStatusChangeOfClient(incomingMessage);
                        break;
                    default:
                        Console.WriteLine("Other kind of Message");
                        break;
                }
            }
        }
    }

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
        EnterRoom
    }
}
