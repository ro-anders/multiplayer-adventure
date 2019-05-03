using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Amazon.Lambda;
using UnityEngine;
using UnityEngine.SceneManagement;

[Serializable]
class HiscoreRecord: IComparable<HiscoreRecord>
{
    public string PK;
    public string SK;
    public string Username;
    public int Wins;
    public int Losses;
    public HiscoreRecord(string inName, int inWins, int inLosses)
    {
        PK = "Standing";
        SK = inName;
        Username = inName;
        Wins = inWins;
        Losses = inLosses;
    }

    public int CompareTo(HiscoreRecord other)
    {
        return -Wins;
    }
}

[Serializable]
class StandingsReturn
{
    public bool Status;
    public HiscoreRecord[] Standings;
}


public class HiScoresController : MonoBehaviour
{
    private const string LIST_STANDINGS_LAMBDA = "ListStandings";
    private HiscoreRecord[] hiscores = new HiscoreRecord[0];

    public GameObject hiscorePrefab;
    public AWS awsUtil;

    private GameObject hiscoreContainer;

    // Start is called before the first frame update
    void Start()
    {
        hiscoreContainer = transform.Find("PlayerList").gameObject;
        LoadScores();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnBackPressed()
    {
        SceneManager.LoadScene("Start");
    }

    private void LoadScores()
    {
        awsUtil.CallLambdaAsync(LIST_STANDINGS_LAMBDA, "", OnLoadScoresReturn);
    }

    private void OnLoadScoresReturn(bool success, string payload)
    {

        if (success)
        {
            try
            {
                StandingsReturn response = JsonUtility.FromJson<StandingsReturn>(payload);
                if (response.Status == true)
                {
                    // Now sort the list by decreasing wins
                    List<HiscoreRecord> list = new List<HiscoreRecord>(response.Standings);
                    list.Sort();
                    hiscores = list.ToArray();
                    FillList();
                } 
                else
                {
                    Debug.LogError("Excpecting StandingsReturn fron lambda but received: " + payload);
                }
            }
            catch (Exception e)
            {
                Debug.LogError("Excpecting StandingsReturn fron lambda but received: " + payload +
                    "\nError message was: " + e.Message);
            }
        } 
    }

    private void FillList()
    {
        foreach (HiscoreRecord record in hiscores)
        {
            GameObject nextGameObject = Instantiate(hiscorePrefab);
            nextGameObject.transform.SetParent(hiscoreContainer.transform, false);
            HiscoreEntry nextEntry = nextGameObject.GetComponent<HiscoreEntry>();
            nextEntry.Name = record.Username;
            nextEntry.Wins = record.Wins;
            nextEntry.Losses = record.Losses;
        }
    }
}
