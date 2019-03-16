﻿using Amazon.Lambda;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
class ListScheduleEntry
{
    public string PK;
    public string SK;
    public string Host;
    public long Time;
    public int Duration;
    public string Comments;
    public string[] Others;
    public ListScheduleEntry(string inKey, string inHost, long inTime, int inDuration, string[] inOthers, string inComments)
    {
        PK = "Schedule";
        SK = inKey;
        Host = inHost;
        Time = inTime;
        Duration = inDuration;
        Others = inOthers;
        Comments = inComments;
    }
}

[Serializable]
class DummyArrayHolder
{
    public ListScheduleEntry[] Entries;
}

public class ScheduleController : MonoBehaviour {

    private const string LIST_SCHEDULES_LAMBDA = "ListSchedules";
    private const string SCHEDULE_GAME_LAMBDA = "UpsertSchedule";
    private const string DELETE_SCHEDULE_LAMBDA = "DeleteSchedule";

    public GameObject schedulePrefab;
    public GameObject modalOverlay;
    public GameObject promptNamePanel;
    public InputField promptNameInput;
    public GameObject scheduleGamePanel;
    public ScheduleDetails scheduleDetails;
    public AWS awsUtil;

    private GameObject scheduleContainer;
    public GameObject refreshButton;
    public GameObject loginButton;
    public GameObject newButton;

	// Use this for initialization
	void Start () {
        scheduleContainer = transform.Find("ScheduledGames").gameObject;
        bool loggedIn = (SessionInfo.ThisPlayerName != null) && 
           !SessionInfo.ThisPlayerName.Equals("");
        refreshButton.SetActive(loggedIn);
        newButton.SetActive(loggedIn);
        loginButton.SetActive(!loggedIn);
        RefreshList();
	}

    // Requery the database for a list of games
    private void RefreshList()
    {
        scheduleDetails.gameObject.SetActive(false);
        DestroyList();
        AmazonLambdaClient lambdaClient = awsUtil.LambdaClient;
        lambdaClient.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest()
        {
            FunctionName = LIST_SCHEDULES_LAMBDA,
            Payload = ""
        },
        (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                try
                {
                    string payloadStr = Encoding.ASCII.GetString(responseObject.Response.Payload.ToArray());
                    LambdaPayload payload = JsonUtility.FromJson<LambdaPayload>(payloadStr);
                    string newPayload = "{\"Entries\":" + payload.body + "}";
                    DummyArrayHolder result = JsonUtility.FromJson<DummyArrayHolder>(newPayload);
                    ListScheduleEntry[] entries = result.Entries;
                    foreach(ListScheduleEntry entry in entries)
                    {
                        // Convert the time to current time
                        DateTime localTime = new DateTime(entry.Time).ToLocalTime();
                        DateTime endTime = localTime.AddMinutes(entry.Duration);
                        // Don't display games scheduled in the past
                        if ((endTime - DateTime.Now).TotalHours >= -1)
                        {
                            GameObject nextGameObject = Instantiate(schedulePrefab);
                            nextGameObject.transform.SetParent(scheduleContainer.transform, false);
                            ScheduledGame nextEvent = nextGameObject.GetComponent<ScheduledGame>();
                            nextEvent.Key = entry.SK;
                            nextEvent.Host = entry.Host;
                            nextEvent.Timestamp = localTime.Ticks;
                            nextEvent.Comments = entry.Comments;
                            nextEvent.Duration = entry.Duration;
                            nextEvent.AddOthers(entry.Others);
                            nextEvent.Controller = this;
                        }
                    }
                } catch (Exception e)
                {
                    Debug.LogError("Error calling lambda:" + e);
                }
            }
            else
            {
                Debug.LogError(responseObject.Exception.ToString());
            }
        }
        );
    }

    public void DeleteGame(ScheduledGame game)
    {
        AmazonLambdaClient lambdaClient = awsUtil.LambdaClient;
        ListScheduleEntry newEntry = new ListScheduleEntry(game.Key, game.Host,
             game.Timestamp, game.Duration, game.Others, game.Comments);
        string jsonStr = JsonUtility.ToJson(newEntry);
        lambdaClient.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest()
        {
            FunctionName = DELETE_SCHEDULE_LAMBDA,
            Payload = jsonStr
        },
        (responseObject) =>
        {
            if (responseObject.Exception == null)
            {
                try
                {
                    if (responseObject.Response.StatusCode != 200)
                    {
                        Debug.LogError("Error calling " + DELETE_SCHEDULE_LAMBDA +
                        " lambda returned status code " + responseObject.Response.StatusCode);
                    }
                    else
                    {
                        RefreshList();
                    }
                }
                catch (Exception e)
                {
                    Debug.LogError("Error calling lambda:" + e);
                }
            }
            else
            {
                Debug.LogError(responseObject.Exception.ToString());
            }
        }
        );
    }

    // Clear out all schedules currently in the schedule list
    private void DestroyList()
    {
        foreach(Transform child in scheduleContainer.transform)
        {
            if (child.gameObject.GetComponent<ScheduledGame>() != null)
            {
                Destroy(child.gameObject);
            }
        }
    }

    public string ScheduleNewGame(string host, DateTime gameStart, int duration, string comments)
    {
        UpsertGame("", host, gameStart, duration, new string[0], comments);
        return null;
    }

    public void UpdateGame(ScheduledGame game)
    {
        UpsertGame(game.Key, game.Host, new DateTime(game.Timestamp),
                game.Duration, game.Others, game.Comments);
    }

    private void UpsertGame(string key, string host, DateTime gameStart, int duration, 
        string[] others, string comments)
    { 
        long startTime = gameStart.ToUniversalTime().Ticks;
        ListScheduleEntry newEntry = new ListScheduleEntry(key, host, startTime, duration, others, comments);
        string jsonStr = JsonUtility.ToJson(newEntry);
        if (key == "")
        {
            awsUtil.CallLambdaAsync(SCHEDULE_GAME_LAMBDA, jsonStr, OnNewGameReturn);
        }
        else
        {
            awsUtil.CallLambdaAsync(SCHEDULE_GAME_LAMBDA, jsonStr, OnUpdateGameReturn);
        }
    }

    private void OnNewGameReturn(bool success, string response)
    {
        RefreshList();
        string subject = NOTIFY_GAME_SUBJECT.Replace("{{name}}", SessionInfo.ThisPlayerName);
        string message = NOTIFY_GAME_MESSAGE.Replace("{{name}}", SessionInfo.ThisPlayerName);
        EmailSubscriptionRequest newRequest = new EmailSubscriptionRequest(subject, message);
        string jsonStr = JsonUtility.ToJson(newRequest);
        awsUtil.CallLambdaAsync(NotifyMeController.EMAIL_SUBSCRIPTION_LAMBDA, jsonStr);
    }

    private void OnUpdateGameReturn(bool success, string response)
    {
        RefreshList();
    }

    public void OnLoginPressed()
    {
        modalOverlay.SetActive(true);
        promptNamePanel.SetActive(true);
        string prevName = PlayerPrefs.GetString(SessionInfo.PLAYER_NAME_PREF, "");
        if (!prevName.Equals(""))
        {
            promptNameInput.text = prevName;
        }
    }

    public void OnRefreshPressed()
    {
        RefreshList();
    }

    public void OnNewPressed()
    {
        modalOverlay.SetActive(true);
        scheduleGamePanel.SetActive(true);
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Start");
    }

    public void OnPromptNameOkPressed()
    {
        SessionInfo.ThisPlayerName = promptNameInput.text.Trim();
        bool loggedIn = (SessionInfo.ThisPlayerName != null) &&
           !SessionInfo.ThisPlayerName.Equals("");
        if (loggedIn)
        {
            PlayerPrefs.SetString(SessionInfo.PLAYER_NAME_PREF, promptNameInput.text.Trim());
        }
        refreshButton.SetActive(loggedIn);
        newButton.SetActive(loggedIn);
        loginButton.SetActive(!loggedIn);
        promptNamePanel.SetActive(false);
        modalOverlay.SetActive(false);
        scheduleDetails.gameObject.SetActive(false);
    }

    public void OnPromptNameCancelPressed()
    {
        promptNamePanel.SetActive(false);
        modalOverlay.SetActive(false);
    }

    public void DismissNewSchedulePanel()
    {
        scheduleGamePanel.SetActive(false);
        modalOverlay.SetActive(false);
    }

    public void DisplayScheduledGame(ScheduledGame schedule)
    {
        scheduleDetails.DisplaySchedule(schedule);
        scheduleDetails.gameObject.SetActive(true);
    }

    private const string NOTIFY_GAME_SUBJECT = "{{name}} has scheduled a new h2hadventure game";
    private const string NOTIFY_GAME_MESSAGE = "You had requested to be emailed whenever someone " +
        "schedules a new H2H Atari Adventure game.  Well {{name}} has just scheduled one.  " +
        "You can check it out in the \"Schedule a Game\" option in H2HAdventure." +
        "\n\n" +
        "Please note that you are emailed whenever someone schedules a new game.  H2HAdventure does not " +
        "notify when games are modified or deleted, even games you have said you will join." +
        "\n\n" +
        "If you wish to no longer receive these events you can unsubscribe through the H2HAdventure " +
        "interface by clicking \"Notify Me\".";

}
