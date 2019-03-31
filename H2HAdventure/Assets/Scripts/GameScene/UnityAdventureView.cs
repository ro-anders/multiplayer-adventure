﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GameEngine;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class UnityAdventureView : UnityAdventureBase, AdventureView, ChatSubmitter
{
    public IntroPanelController introPanel;
    public ChatPanelController chatPanel;

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
        gameStarted = true;
    }

    // Called only on server
    private void OnNetworkManagerSetup()
    {
        chatPanel.ServerSetup();
    }

    public void AdventureSetup(int inLocalPlayerSlot) {
        Debug.Log("Starting game.");
        GameInLobby game = SessionInfo.GameToPlay;
        bool[] useAi = { false, false, false};
        for(int ctr=0; ctr<game.numPlayers; ++ctr)
        {
            //useAi[ctr] = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE) && (ctr != inLocalPlayerSlot);
            useAi[ctr] = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE) && (ctr == 0);
        }
        UnityTransport xportToUse = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE ? null : xport);
        gameEngine = new AdventureGame(this, game.numPlayers, inLocalPlayerSlot, xportToUse, 
            game.gameNumber, game.diff1 == DIFF.A, game.diff2 == DIFF.A,
            SessionInfo.ThisPlayerInfo.needsPopupHelp, SessionInfo.ThisPlayerInfo.needsMazeGuides, useAi);
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
        if (change == GAME_CHANGES.GAME_ENDED)
        {
            // Change the Respawn button to a Quit button
            respawnButton.GetComponentInChildren<Text>().text = "Quit";
            respawnButton.onClick.AddListener(OnQuitPressed);
        }
    }

}
