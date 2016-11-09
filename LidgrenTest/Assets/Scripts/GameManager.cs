using UnityEngine;
using System.Collections;

public class GameManager : MonoBehaviour {

    private static NetworkManager networkManager;
    public GameObject PlayerPrefab;
    public Player localPlayer;

    // Use this for initialization
    void Start () {
        DontDestroyOnLoad(this);
        networkManager = NetworkManager.Instance;
        networkManager.PlayerPrefab = PlayerPrefab;
        networkManager.localPlayer = localPlayer;
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}
