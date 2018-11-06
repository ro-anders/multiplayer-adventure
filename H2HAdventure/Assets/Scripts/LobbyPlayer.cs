using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour {

	// Use this for initialization
	void Start () {
        Text thisText = this.GetComponent<Text>();
        NetworkIdentity id = this.GetComponent<NetworkIdentity>();
        thisText.text = "Client: " + id.netId;
        Debug.Log("New client.  Adding to list.");
        GameObject LobbyPlayerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(LobbyPlayerList.transform);
    }

    // Update is called once per frame
    void Update () {
		
	}


}
