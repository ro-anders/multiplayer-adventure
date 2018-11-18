using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class StartScreen : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnPlayClicked() {
        SessionInfo.NetworkSetup = SessionInfo.Network.MATCHMAKER;
        SceneManager.LoadScene("Lobby");
    }

    public void OnTestClicked() {
        SessionInfo.NetworkSetup = SessionInfo.Network.ALL_LOCAL;
        SceneManager.LoadScene("Lobby");
    }
}
