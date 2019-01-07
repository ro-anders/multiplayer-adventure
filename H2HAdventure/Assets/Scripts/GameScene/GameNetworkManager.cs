using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;

// TODO: Can we get rid of this class?

public class GameNetworkManager : NetworkManager {

	// Use this for initialization
	void Start () {
		
	}
	
    public override void OnClientSceneChanged(NetworkConnection conn) {
        Debug.Log("Is this call causing an error?");
        base.OnClientSceneChanged(conn);
        Debug.Log("Nope");
    }
}
