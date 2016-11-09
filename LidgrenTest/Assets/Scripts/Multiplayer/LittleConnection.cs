using UnityEngine;
using System.Collections;
using Lidgren.Network;

public class LittleConnection : MonoBehaviour {

	// Use this for initialization
	void Start () {
        NetPeerConfiguration connectionConfig = new NetPeerConfiguration("LidgrenTest");
        //additional config options...
        NetClient Client = new NetClient(connectionConfig);
        Client.Start();

        NetOutgoingMessage outgoingMessage = Client.CreateMessage();
        outgoingMessage.Write("SecretValue");
        Client.Connect("127.0.0.1", 12484, outgoingMessage);
    }
	
	// Update is called once per frame
	void Update () {
	
	}
}
