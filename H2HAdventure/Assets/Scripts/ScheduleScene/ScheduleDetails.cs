using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScheduleDetails : MonoBehaviour {

    public Text dateText;
    public Text hostText;
    public Text commentsText;
    public Text othersText;
    public Button joinButton;
    public ScheduleController controller;

    private bool inGame = false;
    private ScheduledGame currentGame;

    public void DisplaySchedule(ScheduledGame game)
    {
        currentGame = game;
        inGame = false;
        string host = game.Host;
        if ((host == null) || host.Equals("")) {
            hostText.text = "Original host has canceled but others are still playing";
        } else
        {
            hostText.text = "<b>Hosted by:</b> " + host;
            if (host.Equals(SessionInfo.ThisPlayerName))
            {
                hostText.text += " ← <b>you!</b>";
                inGame = true;
            }
        }
        DateTime start = new DateTime(game.Timestamp);
        DateTime end = start.AddMinutes(game.Duration);
        dateText.text = start.ToString("ddd MMM d,yyyy\nh:mm tt") + " - " +
            end.ToString("h:mm");
        commentsText.text = ((game.Comments != null) && !game.Comments.Equals("") ?
            "<b>Comments:</b>\n" + game.Comments : "");
        if (game.Others.Length == 0)
        {
            othersText.text = "";
        }
        else
        {
            othersText.text = "<b>Joined by:</b>";
            foreach (string other in game.Others)
            {
                othersText.text += "\n" + other;
                if (other.Equals(SessionInfo.ThisPlayerName))
                {
                    othersText.text += " ← <b>you!</b>";
                    inGame = true;
                }
            }
        }
        joinButton.GetComponentInChildren<Text>().text = (inGame ? "Leave" : "Join");
        bool makeActive = (SessionInfo.ThisPlayerName != null) && !SessionInfo.ThisPlayerName.Equals("");
        joinButton.gameObject.SetActive(makeActive);
    }

    public void OnJoinPressed()
    {
        if (!inGame)
        {
            // Add the user to the others list
            currentGame.AddOther(SessionInfo.ThisPlayerName);
            controller.UpdateGame(currentGame);
        }
        else
        {
            if (currentGame.Host.Equals(SessionInfo.ThisPlayerName))
            {
                currentGame.Host = "";
            }
            else
            {
                currentGame.RemoveOther(SessionInfo.ThisPlayerName);
            }
            // If no one is still playing the game we delete it.
            // Otherwise we update it.
            if (((currentGame.Host == null) || currentGame.Host.Equals("")) &&
                (currentGame.Others.Length == 0))
            {
                controller.DeleteGame(currentGame);
            }
            else
            {
                controller.UpdateGame(currentGame);
            }
        }
    }
}
