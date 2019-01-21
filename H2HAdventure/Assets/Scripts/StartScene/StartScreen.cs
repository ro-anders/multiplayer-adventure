using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class StartScreen : MonoBehaviour {

    public const int DIRECT_CONNECT_PORT = 1980;
    public GameObject directConnectPanel;
    public InputField directConnectIp;

	// Use this for initialization
	void Start () {
        // Show dev resources when in dev mode
        GameObject[] devObjects = GameObject.FindGameObjectsWithTag("dev_only");
        Debug.Log("Found " + devObjects.Length + " dev objects");
        foreach(GameObject devObj in devObjects)
        {
            devObj.SetActive(SessionInfo.DEV_MODE);
        }

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
}
