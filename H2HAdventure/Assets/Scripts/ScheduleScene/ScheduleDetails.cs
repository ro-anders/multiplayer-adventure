using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScheduleDetails : MonoBehaviour {

    public Text dateText;
    public Text hostText;
    public Text commentsText;
    public Text othersText;

    public void DisplaySchedule(ScheduledGame game)
    {
        string host = game.Host;
        if ((host == null) && host.Equals("")) {
            hostText.text = "Original host has canceled but others are still playing";
        } else
        {
            hostText.text = "<b>Hosted by:</b> " + host;
        }
    }
}
