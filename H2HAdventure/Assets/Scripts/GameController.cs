using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class GameController : MonoBehaviour {

    public GameNetworkManager networkManager;
    public Text gameStartText;

	// Use this for initialization
	void Start () {

        bool isHosting = SessionInfo.GameToPlay.playerOne == SessionInfo.ThisPlayerId;
        if (SessionInfo.NetworkSetup == SessionInfo.Network.ALL_LOCAL) {
            if (isHosting) {
                networkManager.networkPort = int.Parse(SessionInfo.GameToPlay.connectionkey);
                networkManager.serverBindAddress = "127.0.0.1";
                networkManager.serverBindToIP = true;
                networkManager.StartHost();
            } else {
                networkManager.networkPort = int.Parse(SessionInfo.GameToPlay.connectionkey);
                networkManager.StartClient();
            }
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
