using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Game : NetworkBehaviour {

    public Text text;

    [SyncVar(hook = "OnChangeNumPlayers")]
    public int numPlayers;

    void Start()
    {
        GameObject GameList = GameObject.FindGameObjectWithTag("GameParent");
        gameObject.transform.SetParent(GameList.transform);
    }

    void OnChangeNumPlayers(int newNumPlayers) {
        Debug.Log("Updating game num players from " + numPlayers + " to " + newNumPlayers);
        text.text = newNumPlayers + " player game";
    }

}
