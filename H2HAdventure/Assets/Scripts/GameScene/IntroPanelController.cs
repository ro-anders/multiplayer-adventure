using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class IntroPanelController : MonoBehaviour {

    public Text gameDescription;
    public Text p1Description;
    public Text p2Description;
    public Text p3Description;
    public Text helpMessage;

    // Use this for initialization
    void Start () {
        gameDescription.text = "Playing " + (SessionInfo.GameToPlay.gameNumber == 3 ?
            "The Gauntlet" :
            "Game #" + (SessionInfo.GameToPlay.gameNumber + 1));
        if ((SessionInfo.GameToPlay.diff1 == DIFF.A) || (SessionInfo.GameToPlay.diff2 == DIFF.A))
        {
            gameDescription.text += "\nwith ";
            if (SessionInfo.GameToPlay.diff1 == DIFF.A)
            {
                gameDescription.text += "fast dragons";
            }
            if ((SessionInfo.GameToPlay.diff1 == DIFF.A) && (SessionInfo.GameToPlay.diff2 == DIFF.A))
            {
                gameDescription.text += " and ";
            }
            if (SessionInfo.GameToPlay.diff2 == DIFF.A)
            {
                gameDescription.text += "dragons run from sword";
            }
        }
        p1Description.text = SessionInfo.GameToPlay.playerOneName + " is in the gold castle";
        p2Description.text = SessionInfo.GameToPlay.playerTwoName + " is in the copper castle";
        p3Description.text = (SessionInfo.GameToPlay.numPlayers < 3 ?
            "" : SessionInfo.GameToPlay.playerThreeName + " is in the jade castle");
        helpMessage.text = "Arrow keys move.  Space key drops.";
        helpMessage.text += "\nHit Respawn button if you get eaten.";
    }

    // Update is called once per frame
    void Update () {
		
	}

    public void Show()
    {
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }
}
