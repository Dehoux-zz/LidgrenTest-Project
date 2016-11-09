//using UnityEngine;
//using System.Collections;
//using Lidgren.Network;
//using System.Collections.Generic;

//public class Connection : MonoBehaviour { //not MonoBehaviour

//    private static NetClient _client;

//    // Clients list of characters
//    private static List<Player> _activePlayers;

//    // Use this for initialization
//    void Start () {
//        var config = new NetPeerConfiguration("LidgrenTest");
//        _client = new NetClient(config);

//        NetOutgoingMessage outgoingMessage = _client.CreateMessage();
//        outgoingMessage.Write("UnityClient");

//        _client.Start();
//        _client.Connect("127.0.0.1", 12484, outgoingMessage);
//        Debug.Log("Client started");

//        _activePlayers = new List<Player>();

//        WaitForStartingInfo();
//    }
	
//	// Update is called once per frame
//	void Update () {
//        CheckServerMessages();
//        GetInputAndSendItToServer();
//    }

//    private static void WaitForStartingInfo()
//    {
//        bool CanStart = false;
//        NetIncomingMessage incomingMessage;
//        while(!CanStart)
//        {
//            if((incomingMessage = _client.ReadMessage()) != null)
//            {
//                switch (incomingMessage.MessageType)
//                {
//                    case NetIncomingMessageType.Data:
//                        if(incomingMessage.ReadByte() == (byte)PacketTypes.ACTIVEPLAYERS)
//                        {
//                            // Worldstate packet structure
//                            //
//                            // int = count of players
//                            // character obj * count

//                            _activePlayers.Clear();
//                            int count = incomingMessage.ReadInt32();

//                            for (int i = 0; i < count; i++)
//                            {
//                                Player player = new Player();
//                                incomingMessage.ReadAllProperties(player);
//                                _activePlayers.Add(player);
//                            }
//                            CanStart = true;
//                        }
//                        break;
//                    default:
//                        Debug.Log(incomingMessage.ReadString() + " Strange message");
//                        break;
//                }
//            }
//        }
//    }

//    private static void CheckServerMessages()
//    {
//        NetIncomingMessage incomingMessage;

//        if ((incomingMessage = _client.ReadMessage()) != null)
//        {
//            if (incomingMessage.MessageType == NetIncomingMessageType.Data) //switch for more packettypes
//            {
//                if (incomingMessage.ReadByte() == (byte)PacketTypes.ACTIVEPLAYERS)
//                {
//                    _activePlayers.Clear();
//                    int count = incomingMessage.ReadInt32();
//                    for (int i = 0; i < count; i++)
//                    {
//                        Player player = new Player();
//                        incomingMessage.ReadAllProperties(player);
//                        _activePlayers.Add(player);
//                    }
//                }
//            }
//        }


//    }

//    private static void GetInputAndSendItToServer()
//    {
//        MoveDirection moveDirection = MoveDirection.NONE;
//        if (Input.GetKeyDown(KeyCode.W))
//        {
//            Debug.Log("Moving uupp");
//            moveDirection = MoveDirection.UP;
//        }

//        NetOutgoingMessage outgoingMessage = _client.CreateMessage();
//        outgoingMessage.Write((byte)PacketTypes.MOVE);
//        outgoingMessage.Write((byte)moveDirection);
//        _client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 1);
//    }

//    public void OnApplicationQuit()
//    {
//        Shutdown();
//    }

//    private void Shutdown()
//    {
//        //NetOutgoingMessage outgoingMessage = _client.CreateMessage();
//        //outgoingMessage.Write((byte)PacketTypes.DISCONNECT);
//        //_client.SendMessage(outgoingMessage, NetDeliveryMethod.ReliableOrdered, 1);
//        //_client.Shutdown("Peace out");
//        _client.Disconnect("Zwaaizwaai");
//        Debug.Log("Closing client connection...");
//    }

//    // This is good way to handle different kind of packets
//    // there has to be some way, to detect, what kind of packet/message is incoming.
//    // With this, you can read message in correct order ( ie. Can't read int, if its string etc )

//    // Best thing about this method is, that even if you change the order of the entrys in enum, the system won't break up
//    // Enum can be casted ( converted ) to byte
//    enum PacketTypes
//    {
//        LOGIN,
//        MOVE,
//        ACTIVEPLAYERS,
//        DISCONNECT
//    }

//    //class LoginPacket
//    //{
//    //    public string MyName { get; set; }
//    //    public LoginPacket(string name)
//    //    {
//    //        MyName = name;
//    //    }
//    //}

//    // Movement directions
//    // This way we can just send byte over net and no need to send anything bigger
//    enum MoveDirection
//    {
//        UP,
//        DOWN,
//        LEFT,
//        RIGHT,
//        NONE
//    }
//}
