using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using Lidgren.Network;

public sealed class NetworkManager : MonoBehaviour
{
    private static volatile NetworkManager _instance;
    private static object syncRoot = new Object();

    private Lobby lobby = new Lobby();
    public Room CurrentRoom;
    public int MyClientId;
    private string hostIp;
    private List<Player> activePlayers;
    private ServerConnection serverConnection;
    private float lastSec = 0f;

    private NetworkManager() { }

    public static NetworkManager Instance
    {
        get
        {
            if (_instance == null)
                lock (syncRoot)
                    if (_instance == null)
                    {
                        GameObject Container = new GameObject();
                        Container.name = "NetworkManager";
                        _instance = Container.AddComponent(typeof(NetworkManager)) as NetworkManager;
                    }
            return _instance;
        }
    }

    void Awake()
    {
        DebugConsole.Log("Initialisation NetworkManager");

        hostIp = "127.0.0.1";
        activePlayers = new List<Player>();

        DebugConsole.Log("Establishing connection to server");
        ServerConnection.Instance.CreateConnection("LidgrenTest", hostIp, 12484, "SecretValue");

        lastSec = Time.time;
    }

    public void Reconnect()
    {
        DebugConsole.Log("Initialisating reconnect");
    }

    void Update()
    {
        ServerConnection.Instance.CheckIncomingMessage();

        //if (Input.GetKeyDown(KeyCode.E))
        //{
        //    GameObject newPlayerGameObject = (GameObject)Instantiate(PlayerPrefab, new Vector2(2,4), Quaternion.identity);
        //    Player newPlayer = newPlayerGameObject.GetComponent<Player>();
        //    newPlayer.Id = 3;
        //    newPlayer.name = "wow";
        //    newPlayerGameObject.name = newPlayer.Id + " - " + newPlayer.name;
        //    activePlayers.Add(newPlayer);
        //}

        //if (Input.GetKeyDown(KeyCode.R))
        //{
        //    DebugConsole.Log("Sending message to server");
        //    NetOutgoingMessage outg = ServerConnection.CreateNetOutgoingMessage();
        //    outg.Write((byte)PackageTypes.Message);
        //    outg.Write("weoa man!");
        //    ServerConnection.SendNetOutgoingMessage(outg, NetDeliveryMethod.ReliableOrdered, 2);
        //}
    }

    void Shutdown()
    {
        ServerConnection.Instance.StopConnection();
        DebugConsole.Log("Closing client connection...");
    }

    void OnApplicationQuit()
    {
        ServerConnection.Instance.StopConnection();
        DebugConsole.Log("Closing client connection...");
    }

    public void AddPlayer(NetIncomingMessage netIncomingMessage)
    {
        //GameObject newPlayerGameObject = (GameObject)Instantiate(PlayerPrefab, new Vector2(0, 0), Quaternion.identity);
        //Player newPlayer = newPlayerGameObject.GetComponent<Player>();
        //newPlayer.Id = netIncomingMessage.ReadInt32();
        //newPlayer.name = netIncomingMessage.ReadString();
        //newPlayer.transform.position = netIncomingMessage.ReadVector2();
        //newPlayerGameObject.name = newPlayer.Id + " - " + newPlayer.name;
        //newPlayerGameObject.GetComponent<SpriteRenderer>().color = Color.red;
        //activePlayers.Add(newPlayer);
    }

    public void AddRoomToLobby(NetIncomingMessage netIncomingMessage)
    {
        lobby.AddRoomId(netIncomingMessage.ReadInt32());
    }

    public List<int> GetLobby()
    {
        return lobby.GetRoomIds();
    }

    public void RefreshLobby()
    {
        lobby.RequestRooms();
    }

    public void JoinRoom(int roomId)
    {
        CurrentRoom = lobby.LocalPlayerJoinsRoom(roomId);
    }

    public void WriteConsoleMessage(string message)
    {
        DebugConsole.Log(message);
    }
}
