using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScheduledGame : MonoBehaviour {

	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

    public void OnJoinClicked()
    {
        Debug.Log("Button clicked");
    }

    public void OnGameClicked()
    {
        Debug.Log("Whole game clicked");
    }
}
