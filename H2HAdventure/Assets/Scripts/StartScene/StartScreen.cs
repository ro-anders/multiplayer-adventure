using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Amazon.Lambda;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[Serializable]
class StatusMessageEntry
{
    public string PK;
    public string SK;
    public int MinimumVersion;
    public string SystemMessage;
    public int MessageId;
    public StatusMessageEntry(int inMinimumVersion, string inSystemMessage,
        int inMessageId)
    {
        PK = "StatusMessage";
        SK = "singleton";
        MinimumVersion = inMinimumVersion;
        SystemMessage = inSystemMessage;
        MessageId = inMessageId;
    }
}

public class StartScreen : MonoBehaviour {

    private const string GAME_STATUS_LAMBDA = "GameStatus";
    private const string NO_SERVER_MESSAGE = "H2H Adventure server is unreachable.The game cannot be played at this time.  Please check system status at";
    private const string NO_SERVER_LINK = "http://h2hadventure.com/status";
    private const string NEED_DOWNLOAD_MESSAGE = "You need to download the latest H2H Adventure client from";
    private const string NEED_DOWNLOAD_LINK = "http://h2hadventure.com/download";

    public const int DIRECT_CONNECT_PORT = 1980;
    public GameObject directConnectPanel;
    public InputField directConnectIp;
    public GameObject overlay;
    public AbortPopup abortPopup;
    public GameObject systemMessagePanel;
    public Text systemMessageText;
    public AWS awsUtil;


    // Use this for initialization
    void Start () {

        awsUtil.CallOnReady(CheckSystemMessages);

        // Show dev resources when in dev mode
        GameObject[] devObjects = GameObject.FindGameObjectsWithTag("dev_only");
        Debug.Log("Found " + devObjects.Length + " dev objects");
        foreach(GameObject devObj in devObjects)
        {
            devObj.SetActive(SessionInfo.DEV_MODE);
        }

    }

    private void StartGame()
    {
        overlay.SetActive(false);
    }

    public void OnScheduleClicked()
    {
        SceneManager.LoadScene("Schedule");
    }

    public void OnGetNotifiedClicked()
    {
        SceneManager.LoadScene("NotifyMe");
    }



    public void OnPlayClicked() {
        SessionInfo.NetworkSetup = SessionInfo.Network.MATCHMAKER;
        SceneManager.LoadScene("Lobby");
    }

    public void OnTestClicked()
    {
        SessionInfo.NetworkSetup = SessionInfo.Network.ALL_LOCAL;
        SceneManager.LoadScene("Lobby");
    }

    public void OnDirectConnectClicked()
    {
        directConnectPanel.SetActive(true);
    }

    public void OnDirectConnectHostPressed()
    {
        directConnectPanel.SetActive(false);
        SessionInfo.NetworkSetup = SessionInfo.Network.DIRECT_CONNECT;
        SessionInfo.DirectConnectIp = SessionInfo.DIRECT_CONNECT_HOST_FLAG;
        SceneManager.LoadScene("Lobby");
    }

    public void OnDirectConnectConnectToHostPressed()
    {
        string ip = directConnectIp.text;
        if ((ip != null) && (ip.Trim() != "")) {
            directConnectPanel.SetActive(false);
            SessionInfo.NetworkSetup = SessionInfo.Network.DIRECT_CONNECT;
            SessionInfo.DirectConnectIp = ip;
            SceneManager.LoadScene("Lobby");
        }
    }

    public void OnDirectConnectBackPressed()
    {
        directConnectPanel.SetActive(false);
    }

    public void OnTestSingleClicked()
    {
        SessionInfo.NetworkSetup = SessionInfo.Network.NONE;
        SessionInfo.GameToPlay = new GameInLobby();
        SessionInfo.GameToPlay.gameId = 11;
        SessionInfo.GameToPlay.playerOne = 12;
        SessionInfo.GameToPlay.playerOneName = "abbie";
        SessionInfo.GameToPlay.playerTwo = 13;
        SessionInfo.GameToPlay.playerTwoName = "ben";
        SessionInfo.GameToPlay.playerThree = 14;
        SessionInfo.GameToPlay.playerThreeName = "chris";
        SessionInfo.ThisPlayerId = SessionInfo.GameToPlay.playerOne;
        SessionInfo.ThisPlayerName = SessionInfo.GameToPlay.playerOneName;
        SessionInfo.GameToPlay.playerMapping = 0;
        SessionInfo.GameToPlay.numPlayers = 3;
        SessionInfo.GameToPlay.gameNumber = 1;
        SessionInfo.GameToPlay.diff1 = DIFF.A;
        SessionInfo.GameToPlay.diff2 = DIFF.A;
        SceneManager.LoadScene(SessionInfo.GAME_SCENE);
    }

    public void OnSystemMessageContinuePressed()
    {
        systemMessagePanel.SetActive(false);
        StartGame();
    }

    private void CheckSystemMessages()
    {
        awsUtil.CallLambdaAsync(GAME_STATUS_LAMBDA, "", OnGameStatusReturn);
    }

    private void OnGameStatusReturn(bool success, string payload)
    {

        StatusMessageEntry statusMessage = null;
        if (success)
        {
            try
            {
                statusMessage = JsonUtility.FromJson<StatusMessageEntry>(payload);
                success = (statusMessage.MinimumVersion != 0) && (statusMessage.SystemMessage != null);
            } catch (Exception e)
            {
                Debug.LogError("Excpecting StatusMessageEntry fron lambda but received: " + payload);
                success = false;
            }
        }

        if (success) { 
            if (statusMessage.MinimumVersion > SessionInfo.VERSION)
            {
                Debug.LogError("Current version " + SessionInfo.VERSION +
                " is too old.  Need to upgrade to version " + statusMessage.MinimumVersion);
                AbortPopup.Show(abortPopup, NEED_DOWNLOAD_MESSAGE, NEED_DOWNLOAD_LINK);
            }
            else if ((statusMessage.SystemMessage != null) && !statusMessage.Equals("")) {
                // Only show the message once (unless it doesn't have an ID, then
                // show it every time).
                Debug.Log("Message is \"" + statusMessage.SystemMessage + "\"");
                string LAST_SYSTEM_MESSAGE_PREF = "LastSystemMessage";
                int lastMessage = PlayerPrefs.GetInt(LAST_SYSTEM_MESSAGE_PREF, 0);
                if ((statusMessage.MessageId == 0) || (statusMessage.MessageId != lastMessage))
                {
                    systemMessageText.text = statusMessage.SystemMessage;
                    overlay.SetActive(true);
                    systemMessagePanel.SetActive(true);
                    PlayerPrefs.SetInt(LAST_SYSTEM_MESSAGE_PREF, statusMessage.MessageId);
                }
            }
            else
            {
                StartGame();
            }
        }
        else
        {
            Debug.LogError("Cannot get system status.  Need to abort.");
            AbortPopup.Show(abortPopup, NO_SERVER_MESSAGE, NO_SERVER_LINK);
        }
    }
}
