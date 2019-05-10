using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Amazon.Lambda;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
class HiscoreRecord : IComparable<HiscoreRecord>
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
class EggRaceRecord : IComparable<EggRaceRecord>
{
    public string PK;
    public string SK;
    public string Username;
    public int Stage;
    public int Time;

    public int CompareTo(EggRaceRecord other)
    {
        if (this.Stage != other.Stage)
        {
            return other.Stage - this.Stage;
        } else
        {
            return this.Time - other.Time;
        }
    }
}

[Serializable]
class StandingsReturn
{
    public bool Status;
    public HiscoreRecord[] Standings;
    public EggRaceRecord[] RaceToEgg;
    public string RaceStatus;
}


public class HiScoresController : MonoBehaviour
{
    private const string LIST_STANDINGS_LAMBDA = "ListStandings";

    public GameObject hiscorePrefab;
    public GameObject hiscoreContainer;
    public GameObject raceStagePrefab;
    public GameObject raceRecordPrefab;
    public GameObject raceContainer;
    public Text topInRaceTitle;
    public AWS awsUtil;

    private Text titleText;
    private Button switchButton;
    private GameObject leaderTitleBar;
    private GameObject leaderScrollView;
    private GameObject raceTitleBar;
    private GameObject raceScrollView;

    private HiscoreRecord[] hiscores = new HiscoreRecord[0];
    private EggRaceRecord[] eggRaceRecords = new EggRaceRecord[0];
    private bool displayingLeaderBoard = true;

    // Start is called before the first frame update
    void Start()
    {
        GameObject titleTextGameObject = transform.Find("TitleText").gameObject;
        titleText = titleTextGameObject.GetComponent<Text>();
        GameObject switchButtonGameObject = transform.Find("SwitchButton").gameObject;
        switchButton = switchButtonGameObject.GetComponent<Button>();
        leaderTitleBar = transform.Find("LeaderTitleBar").gameObject;
        leaderScrollView = transform.Find("LeaderScrollView").gameObject;
        raceTitleBar = transform.Find("RaceTitleBar").gameObject;
        raceScrollView = transform.Find("RaceScrollView").gameObject;
        LoadScores();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void OnSwitchPressed()
    {
        SwitchDisplayedBoard();
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
                    FillLeaderBoard(response.Standings);

                    // If there are entries in the race to the egg, switch the board to displaying that
                    if (response.RaceToEgg.Length > 0)
                    {
                        FillRaceToEgg(response.RaceToEgg, response.RaceStatus);
                    }
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

    private void FillLeaderBoard(HiscoreRecord[] standings)
    {
        // First sort the standings by number of wins
        List<HiscoreRecord> list = new List<HiscoreRecord>(standings);
        list.Sort();
        hiscores = list.ToArray();
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

    private void FillRaceToEgg(EggRaceRecord[] records, String raceStatus)
    {
        string[] STAGES = new string[] {
            "", "",
            "Found the crystal castle",
            "Found the crystal key",
            "Opened the crystal gate",
            "Beat the crystal challenge"
        };
        // First sort the achievments, first by stage second by time
        List<EggRaceRecord> list = new List<EggRaceRecord>(records);
        list.Sort();
        eggRaceRecords = list.ToArray();
        int currentStage = -1;
        // Fill the board
        foreach (EggRaceRecord record in eggRaceRecords)
        {
            if (record.Stage >= 2) // TODO: Filter out unimportant ones on the server
            {
                if (record.Stage != currentStage)
                {
                    // Put in a header to show the stage
                    GameObject nextStageObject = Instantiate(raceStagePrefab);
                    nextStageObject.transform.SetParent(raceContainer.transform, false);
                    RaceStageEntry nextStage = nextStageObject.GetComponent<RaceStageEntry>();
                    nextStage.Title = STAGES[record.Stage];
                    currentStage = record.Stage;
                }
                GameObject nextGameObject = Instantiate(raceRecordPrefab);
                nextGameObject.transform.SetParent(raceContainer.transform, false);
                RaceEntry nextEntry = nextGameObject.GetComponent<RaceEntry>();
                nextEntry.Name = record.Username;
                nextEntry.Time = record.Time;
            }
        }
        // Set the top title
        topInRaceTitle.text = raceStatus;
    }

    private void SwitchDisplayedBoard()
    {
        string LEADER_OPTION = "Leader Board";
        string RACE_OPTION = "Race to the Egg";
        displayingLeaderBoard = !displayingLeaderBoard;
        titleText.text = (displayingLeaderBoard ? LEADER_OPTION : RACE_OPTION);
        switchButton.GetComponentInChildren<Text>().text = 
            "See " + (displayingLeaderBoard ? RACE_OPTION : LEADER_OPTION) + " >";
        switchButton.gameObject.SetActive(true);
        leaderTitleBar.SetActive(displayingLeaderBoard);
        leaderScrollView.SetActive(displayingLeaderBoard);
        raceTitleBar.SetActive(!displayingLeaderBoard);
        raceScrollView.SetActive(!displayingLeaderBoard);
    }
}
