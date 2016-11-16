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

                        Player player = _serverManager.ActivePlayers.Find(p => p.Connection == incomingMessage.SenderConnection);
                        if (player == null || incomingMessage.LengthBytes < 1)
                            break;
                        switch ((PacketTypes)incomingMessage.ReadByte())
                        {
                            #region ManageServer
                            //Manage disconnect from client, remove player from ServerManager
                            case PacketTypes.Disconnect:
                                {
                                    _serverManager.ManageDisonnectionOfPlayer(player);
                                }
                                break;
                            #endregion


                            #region ManagePlayer
                            //Save last best en position it has, for dubble check
                            case PacketTypes.Beat:
                                {
                                    if (incomingMessage.LengthBytes < 2) break;
                                    player.LastBeat = incomingMessage.ReadInt16();
                                    player.Position = incomingMessage.ReadVector2();
                                }
                                break;

                            //KeepAlive flag is raised, setting timestamp for further checks
                            case PacketTypes.KeepAlive:
                                {
                                    player.KeepAlive = NetTime.Now;
                                }
                                break;

                            //Handle player movement en send to all but the incommingmessage player
                            case PacketTypes.PlayerMovement:
                                {
                                    player.HandlePlayerMovement(incomingMessage);
                                }
                                break;

                            //Handle player jump en send to all but the incommingmessage player
                            case PacketTypes.PlayerJump:
                                {
                                    player.HandlePlayerJump(incomingMessage);
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
        PlayerJump
    }
}
