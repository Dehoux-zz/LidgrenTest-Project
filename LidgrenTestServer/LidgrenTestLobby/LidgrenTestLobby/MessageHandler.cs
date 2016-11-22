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
        EnterRoom
    }

    class MessageHandler
    {
        private static LobbyManager _lobbyManager = LobbyManager.Instance;

        public static void Register(object fromPlayer)
        {
            NetIncomingMessage incomingMessage;
            if ((incomingMessage = ((NetServer)fromPlayer).ReadMessage()) != null && incomingMessage.SenderConnection != null)
            {
                switch (incomingMessage.MessageType)
                {
                    case NetIncomingMessageType.ConnectionApproval:
                        {

                            //_lobbyManager.ManageConnectionAppoval(incomingMessage);

                        }
                        break;
                    case NetIncomingMessageType.Data:




                        //Client client = _lobbyManager.SearchClient(incomingMessage.SenderConnection);
                        //if (client == null || incomingMessage.LengthBytes < 1)
                        //    break;


                        switch ((PacketTypes)incomingMessage.ReadByte())
                        {
                            #region ManageServer
                            //Manage disconnect from client, remove player from ServerManager
                            case PacketTypes.Disconnect:
                                {

                                }
                                break;
                            #endregion


                            #region ManagePlayer
                            //Save last best en position it has, for dubble check
                            case PacketTypes.Beat:
                                {


                                }
                                break;

                            //KeepAlive flag is raised, setting timestamp for further checks
                            case PacketTypes.KeepAlive:
                                {

                                }
                                break;

                            //Handle player movement en send to all but the incommingmessage player
                            case PacketTypes.PlayerMovement:
                                {

                                }
                                break;

                            //Handle player jump en send to all but the incommingmessage player
                            case PacketTypes.PlayerJump:
                                {

                                }
                                break;
                            case PacketTypes.EnterRoom:
                                {

                                }
                                break;
                            #endregion


                            #region ManageClient
                            //Handle message send from a client and send a nice return message
                            case PacketTypes.Message:
                                {

                                }
                                break;
                                #endregion
                        }
                        break;
                    case NetIncomingMessageType.StatusChanged:

                        break;
                    default:
                        Console.WriteLine("Other kind of Message");
                        break;
                }
            }
        }
    }
}
