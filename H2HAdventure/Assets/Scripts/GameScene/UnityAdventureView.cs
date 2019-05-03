﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Amazon.Lambda;

[Serializable]
class WonGameReport
{
    public string Won;
    public string[] Lost;
    public WonGameReport(string inWon, string[] inLost)
    {
        Won = inWon;
        Lost = inLost;
    }
}

public class UnityAdventureView : UnityAdventureBase, AdventureView, ChatSubmitter
{
    private const string UPDATE_STANDINGS_LAMBDA= "UpdateStandings";

    public IntroPanelController introPanel;
    public ChatPanelController chatPanel;
    public AWS awsUtil;

    private UnityTransport xport;

    private PlayerSync localPlayer;

    private int numPlayersReady;

    public override void Start() {
        base.Start();
        chatPanel.ChatSubmitter = this;

        xport = this.gameObject.GetComponent<UnityTransport>();
        introPanel.Show();
        if (SessionInfo.NetworkSetup == SessionInfo.Network.NONE) {
            StartGame();
        }
    }

    public override void Update()
    {
        base.Update();

        // If the transport has been gracefully shutdown it means we are trying
        // to return to the lobby and just waiting for the network to be cleanly shutdown.
        if ((xport != null) && (xport.ConnectionState == UnityTransport.ConnectionStates.SHUTDOWN))
        {
            string nextScene = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE ? "Start" : "Lobby");
            SceneManager.LoadScene(nextScene);
        }
    }

    public ChatPanelController GetChatPanelController()
    {
        return chatPanel;
    }


    public UnityTransport RegisterNewPlayer(PlayerSync newPlayer)
    {
        if (newPlayer.isLocalPlayer)
        {
            localPlayer = newPlayer;
            if (newPlayer.isServer)
            {
                OnNetworkManagerSetup();
            }
        }
        xport.registerSync(newPlayer);
        if (newPlayer.isServer)
        {
            ++numPlayersReady;
            if (numPlayersReady >= SessionInfo.GameToPlay.numPlayers)
            {
                StartCoroutine(SignalStartGame());
            }
        }
        return xport;
    }

    private IEnumerator SignalStartGame()
    {
        const float GAME_START_BANNER_TIME = 10f;
        yield return new WaitForSeconds(GAME_START_BANNER_TIME);
        localPlayer.RpcStartGame();
    }

    public void StartGame()
    {
        introPanel.Hide();
        int localPlayerSlot = 0;
        if (localPlayer == null)
        {
            localPlayerSlot = (SessionInfo.ThisPlayerId == SessionInfo.GameToPlay.playerOne ? 0 :
             (SessionInfo.ThisPlayerId == SessionInfo.GameToPlay.playerTwo ? 1 : 2));
        }
        else
        {
            localPlayerSlot = localPlayer.getSlot();
        }
        AdventureSetup(localPlayerSlot);
        gameRenderable = true;
    }

    // Called only on server
    private void OnNetworkManagerSetup()
    {
        chatPanel.ServerSetup();
    }

    public void AdventureSetup(int inLocalPlayerSlot) {
        Debug.Log("Starting game.");
        GameInLobby game = SessionInfo.GameToPlay;
        UnityTransport xportToUse = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE ? null : xport);
        gameEngine = new AdventureGame(this, game.numPlayers, inLocalPlayerSlot, xportToUse, 
            game.gameNumber, game.diff1 == DIFF.A, game.diff2 == DIFF.A,
            SessionInfo.ThisPlayerInfo.needsPopupHelp, SessionInfo.ThisPlayerInfo.needsMazeGuides);
    }

    public void PostChat(string message)
    {
        localPlayer.CmdPostChat(message);
    }

    public void OnQuitPressed()
    {
        xport.Disconnect();
    }

    public override void Platform_GameChange(GAME_CHANGES change)
    {
        base.Platform_GameChange(change);
        if (change == GAME_CHANGES.GAME_ENDED)
        {
            // Change the Respawn button to a Quit button
            respawnButton.GetComponentInChildren<Text>().text = "Quit";
            respawnButton.onClick.AddListener(OnQuitPressed);
        }
    }

    public override void Platform_ReportToServer(string message)
    {
        base.Platform_ReportToServer(message);
        if (message == AdventureReports.WON_GAME)
        {
            UpdateStandingsWithWin();
        }
    }

    private void UpdateStandingsWithWin()
    {
        // Records win and losses to server for leader board
        List<string> losers = new List<string>();
        if (SessionInfo.GameToPlay.playerOne != SessionInfo.ThisPlayerId)
        {
            losers.Add(SessionInfo.GameToPlay.playerOneName);
        }
        if (SessionInfo.GameToPlay.playerTwo != SessionInfo.ThisPlayerId)
        {
            losers.Add(SessionInfo.GameToPlay.playerTwoName);
        }
        if ((SessionInfo.GameToPlay.numPlayers > 2) &&
            (SessionInfo.GameToPlay.playerThree != SessionInfo.ThisPlayerId))
        {
            losers.Add(SessionInfo.GameToPlay.playerThreeName);
        }
        WonGameReport report = new WonGameReport(SessionInfo.ThisPlayerName,
            losers.ToArray());
        string jsonStr = JsonUtility.ToJson(report);
        awsUtil.CallLambdaAsync(UPDATE_STANDINGS_LAMBDA, jsonStr);
    }

}
