using Amazon.Lambda;
using System.Collections;
using System.Collections.Generic;
using System;
using System.IO;
using System.Text;
using UnityEngine;

[Serializable]
class LambdaPayload
{
    public int statusCode;
    public string body;
}

[Serializable]
class ListScheduleEntry
{
    public string Host;
    public long Time;
    public int Duration;
    public string Comments;
}

[Serializable]
class DummyArrayHolder
{
    public ListScheduleEntry[] Entries;
}

public class ScheduleController : MonoBehaviour {

    private const string LIST_SCHEDULES_LAMBDA = "ListSchedules";
    private const string SCHEDULE_GAME_LAMBDA = "CreateSchedule";

    public GameObject schedulePrefab;
    public GameObject modalOverlay;
    public GameObject scheduleGamePanel;
    public ScheduleDetails scheduleDetails;

    private GameObject scheduleContainer;

	// Use this for initialization
	void Start () {
        scheduleContainer = transform.Find("ScheduledGames").gameObject;
        AWSUtil.InitializeAws(this.gameObject);
        RefreshList();
	}

    // Requery the database for a list of games
    void RefreshList()
    {
        AmazonLambdaClient lambdaClient = AWSUtil.lambdaClient;
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
                        Debug.Log("Read in game scheduled by " + entry.Host);
                        GameObject nextGameObject = Instantiate(schedulePrefab);
                        nextGameObject.transform.SetParent(scheduleContainer.transform);
                        ScheduledGame nextEvent = nextGameObject.GetComponent<ScheduledGame>();
                        // Convert the time to current time
                        long localTimestamp = new DateTime(entry.Time).ToLocalTime().Ticks;
                        nextEvent.Timestamp = localTimestamp;
                        nextEvent.Host = entry.Host;
                        nextEvent.Comments = entry.Comments;
                        nextEvent.Duration = entry.Duration;
                        nextEvent.Controller = this;
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

    public string ScheduleGame(string host, DateTime gameStart, int duration, string comments)
    {
        UpsertGame(host, gameStart, duration, new string[0], comments);
        return null;
    }

    private void UpsertGame(string host, DateTime gameStart, int duration, 
        string[] others, string comments)
    { 
        AmazonLambdaClient lambdaClient = AWSUtil.lambdaClient;
        ListScheduleEntry newEntry = new ListScheduleEntry();
        newEntry.Time = gameStart.ToUniversalTime().Ticks;
        newEntry.Host = host;
        string jsonStr = JsonUtility.ToJson(newEntry);
        Debug.Log("Sending lambda event " + jsonStr);
        lambdaClient.InvokeAsync(new Amazon.Lambda.Model.InvokeRequest()
        {
            FunctionName = SCHEDULE_GAME_LAMBDA,
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
                        Debug.LogError("Error calling " + SCHEDULE_GAME_LAMBDA + 
                        " lambda returned status code " + responseObject.Response.StatusCode);
                    }
                    else
                    {
                        Debug.Log("AWS reported processing lambda.  Refreshing screen.");
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

    public void OnNewPressed()
    {
        modalOverlay.SetActive(true);
        scheduleGamePanel.SetActive(true);
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
}
