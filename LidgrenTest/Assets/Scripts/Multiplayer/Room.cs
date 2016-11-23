using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class Room
{

    public int Id;
    public string Name;
    public GameObject PlayerPrefab;
    public GameObject localPlayer;
    public List<Player> roomPlayers;

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
