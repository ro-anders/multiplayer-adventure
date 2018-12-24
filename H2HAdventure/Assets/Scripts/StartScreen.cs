﻿using System.Collections;
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

    public void OnTestSingleClicked()
    {
        SessionInfo.NetworkSetup = SessionInfo.Network.NONE;
        SessionInfo.ThisPlayerId = 12;
        SessionInfo.ThisPlayerName = "adam";
        SessionInfo.GameToPlay = new GameInLobby();
        SessionInfo.GameToPlay.gameId = 11;
        SessionInfo.GameToPlay.playerOne = SessionInfo.ThisPlayerId;
        SessionInfo.GameToPlay.playerOneName = SessionInfo.ThisPlayerName;
        SessionInfo.GameToPlay.playerTwo = 13;
        SessionInfo.GameToPlay.playerTwoName = "ben";
        SessionInfo.GameToPlay.playerThree = 14;
        SessionInfo.GameToPlay.playerThreeName = "calvin";
        SessionInfo.GameToPlay.numPlayers = 3;
        SessionInfo.GameToPlay.gameNumber = 0;
        SceneManager.LoadScene(LobbyController.GAME_SCENE);
    }
}
