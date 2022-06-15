using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RosterController : MonoBehaviour {

    public Text playerLabel1;
    public Text playerLabel2;
    public Text playerLabel3;

	// Use this for initialization
	void Start () {
        string[] names = SessionInfo.GameToPlay.GetPlayerNamesInGameOrder();
        playerLabel1.text = "P1: " + names[0];
        playerLabel2.text = "P2: " + names[1];
        if (SessionInfo.GameToPlay.numPlayers == 2)
        {
            playerLabel3.gameObject.SetActive(false);
        }
        else
        {
            playerLabel3.gameObject.SetActive(true);
            playerLabel3.text = "P3: " + names[2];
        }

    }
	
	// Update is called once per frame
	void Update () {
		
	}
}
