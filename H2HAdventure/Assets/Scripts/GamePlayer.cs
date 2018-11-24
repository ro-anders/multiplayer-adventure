using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

public class GamePlayer : NetworkBehaviour
{

	// Use this for initialization
	void Start () {
        GameObject playerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(playerList.transform);
    }

    // Update is called once per frame
    void Update () {
		
	}
}
