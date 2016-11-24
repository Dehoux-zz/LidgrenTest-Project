using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Lidgren.Network;

public class Room
{

    public readonly int Id;
    public string Name;
    public GameObject PlayerPrefab;
    public GameObject localPlayer;
    public List<Player> roomPlayers;

    public Room(int roomId)
    {
        Id = roomId;

        NetOutgoingMessage netOutgoingMessage = ServerConnection.Instance.CreateNetOutgoingMessage();
        netOutgoingMessage.Write((byte)PackageTypes.EnterRoom);
        netOutgoingMessage.Write(Id);
        ServerConnection.Instance.SendNetOutgoingMessage(netOutgoingMessage, NetDeliveryMethod.ReliableOrdered, 10);
    }

    public Player FindPlayer(int playerId)
    {
        return roomPlayers.Find(x => x.Id == playerId);
    }

    public Player GetLocalPlayer()
    {
        return localPlayer.GetComponent<Player>();
    }

    public void LocalPlayerJoinsRoom(int playerId)
    {
        GameObject newPlayerGameObject = (GameObject)GameObject.Instantiate(PlayerPrefab, new Vector2(0, 0), Quaternion.identity);
        Player newPlayer = newPlayerGameObject.GetComponent<Player>();
        newPlayer.Id = playerId;
        newPlayer.name = "Player " + playerId;
        newPlayer.transform.position = Vector2.zero;
        newPlayerGameObject.name = newPlayer.Id + " - " + newPlayer.name;
        newPlayerGameObject.GetComponent<SpriteRenderer>().color = Color.red;
        localPlayer = newPlayerGameObject;
    }
}
