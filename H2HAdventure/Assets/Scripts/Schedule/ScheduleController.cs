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
    public int Time;
    public int Duration;
}

[Serializable]
class DummyArrayHolder
{
    public ListScheduleEntry[] Entries;
}

public class ScheduleController : MonoBehaviour {

    private const string LIST_SCHEDULES_LAMBDA = "ListSchedules";

	// Use this for initialization
	void Start () {
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
                    Debug.Log("Read in " + result.Entries.Length + " scheduled games");
                    Debug.Log("First one is from " + result.Entries[0].Host);
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
}
