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
        gameDescription.text = "Playing " + 
            (SessionInfo.GameToPlay.gameNumber < 3 ? "Game #" + (SessionInfo.GameToPlay.gameNumber + 1) :
            (SessionInfo.GameToPlay.gameNumber < 6 ? "Cooperative Game #" + (SessionInfo.GameToPlay.gameNumber - 2) :
            (SessionInfo.GameToPlay.gameNumber == 6 ? "role-based cooperative game" : "The Gauntlet"
            )));
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
        string[] names = SessionInfo.GameToPlay.GetPlayerNamesInGameOrder();
        string p1Text = (SessionInfo.GameToPlay.gameNumber < 3 ? "in the gold castle" : " the solid square");
        string p2Text = (SessionInfo.GameToPlay.gameNumber < 3 ? "in the copper castle" : " the donut");
        string p3Text = (SessionInfo.GameToPlay.gameNumber < 3 ? "in the jade castle" : " the 'I'");
        p1Description.text = names[0] + " is " + p1Text;
        p2Description.text = names[1] + " is " + p2Text;
        p3Description.text = (SessionInfo.GameToPlay.numPlayers < 3 ?
            "" : names[2] + " is " + p3Text);
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
