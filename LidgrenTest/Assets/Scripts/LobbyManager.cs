using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Security.Policy;
using UnityEngine.UI;

public class LobbyManager : MonoBehaviour
{

    private static NetworkManager _networkManager;
    public GameObject RoomPrefab;
    public GameObject LobbyPanel;
    private int roomPanelPosition = 30;

    // Use this for initialization
    void Start()
    {
        _networkManager = NetworkManager.Instance;
    }

    // Update is called once per frame
    void Update()
    {

    }

    public void RequestRefreshRooms()
    {
        _networkManager.RefreshLobby();
    }

    public void RefreshRooms()
    {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in LobbyPanel.transform)
            children.Add(child.gameObject);
        children.ForEach(child => Destroy(child));
        roomPanelPosition = 30;

        foreach (int roomId in _networkManager.GetLobby())
        {
            GameObject newRoomGameObject = (GameObject)Instantiate(RoomPrefab, new Vector2(0, 0), Quaternion.identity);
            newRoomGameObject.GetComponentInChildren<Text>().text = "RoomId " + roomId;

            newRoomGameObject.transform.SetParent(LobbyPanel.transform);
            newRoomGameObject.GetComponent<RectTransform>().localScale = new Vector3(1, 1, 1);
            newRoomGameObject.GetComponent<RectTransform>().anchoredPosition = new Vector2(0, roomPanelPosition);
            roomPanelPosition -= 30;

            newRoomGameObject.GetComponentInChildren<Button>().onClick.AddListener(() => { _networkManager.JoinRoom(roomId); });
        }
    }

    public void ClickedEnterRoom(int roomId)
    {
        _networkManager.JoinRoom(roomId);
    }
}
