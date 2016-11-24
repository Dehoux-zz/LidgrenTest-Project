using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using Lidgren.Network;

public class Lobby {

    private List<Player> lobbyPlayers;
    private List<int> roomIds; 

    public Lobby()
    {
        roomIds = new List<int>();
    }

    public List<int> GetRoomIds()
    {
        return roomIds;
    }

    public void AddPlayer(NetIncomingMessage netIncomingMessage)
    {

    }

    public void AddRoomId(int roomId)
    {
        roomIds.Add(roomId);
    }

    public Player FindPlayer(int playerId)
    {
        return lobbyPlayers.Find(x => x.Id == playerId);
    }

    public void RequestRooms()
    {
        roomIds = new List<int>();

        NetOutgoingMessage netOutgoingMessage = ServerConnection.Instance.CreateNetOutgoingMessage();
        netOutgoingMessage.Write((byte)PackageTypes.RefreshRooms);
        ServerConnection.Instance.SendNetOutgoingMessage(netOutgoingMessage, NetDeliveryMethod.ReliableOrdered, 4);
    }

    public Room LocalPlayerJoinsRoom(int roomId)
    {
        Room room = new Room(roomId);
        return room;
    }
}