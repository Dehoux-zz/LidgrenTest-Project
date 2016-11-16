using UnityEngine;
using System.Collections;
using System.Security.Policy;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour {

    private static NetworkManager _networkManager;
    public GameObject RoomPrefab;
    public GameObject LobbyPanel;
    private int roomPanelPosition = 30;

    // Use this for initialization
    void Start () {
        _networkManager = NetworkManager.Instance;
    }
	
	// Update is called once per frame
	void Update () {

    }

    public void RefreshRooms()
    {
        Lobby lobby = _networkManager.GetLobby();
        foreach (Room room in lobby.GetRooms())
        {
            GameObject newRoomGameObject = (GameObject)Instantiate(RoomPrefab, new Vector2(0, 0), Quaternion.identity);
            newRoomGameObject.GetComponentInChildren<Text>().text = room.Name;

            newRoomGameObject.transform.SetParent(LobbyPanel.transform);
            newRoomGameObject.GetComponent<RectTransform>().localScale = new Vector3(1,1,1);
            newRoomGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, roomPanelPosition);
            roomPanelPosition -= 30;

            newRoomGameObject.GetComponent<Button>().onClick.AddListener(() => { _networkManager.JoinRoom(room); });
        }
    }

    public void ClickedEnterRoom(int roomId)
    {
        _networkManager.EnterRoom(roomId);
    }
}
