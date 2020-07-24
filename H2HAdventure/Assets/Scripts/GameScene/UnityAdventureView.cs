using System;
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

[Serializable]
class EggReport
{
    public string Player;
    public int Stage;
    public EggReport(string inPlayer, int inStage)
    {
        Player = inPlayer;
        Stage = inStage;
    }
}

public class UnityAdventureView : UnityAdventureBase, AdventureView, ChatSubmitter
{
    private const string UPDATE_STANDINGS_LAMBDA= "UpdateStandings";
    private const string UPDATE_EGG_SCOREBOARD_LAMBDA = "UpdateScoreboard";
    private const string PING_LAMBDA = "GamePing";

    List<string> eggMessages = new List<string>(new string[]{ 
        AdventureReports.FOUND_ROBINETT_ROOM,
        AdventureReports.GLIMPSED_CRYSTAL_CASTLE,
        AdventureReports.FOUND_CRYSTAL_CASTLE,
        AdventureReports.FOUND_CRYSTAL_KEY,
        AdventureReports.OPENED_CRYSTAL_GATE,
        AdventureReports.BEAT_CRYSTAL_CHALLENGE
      });

    public IntroPanelController introPanel;
    public ChatPanelController chatPanel;
    public AWS awsUtil;
    public GameObject quitConfirmPanel;

    private UnityTransport xport;

    private PlayerSync localPlayer;

    private int numPlayersReady;

    public override void Start() {
        base.Start();
        InvokeRepeating("SendPing", 0, 60);
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
        // TODOX: Right now we are manually jamming the AI
        bool[] useAi = { false, true, true };
        //// Setup AI whenever there is no network.  Turn it on for everyone but the local player
        //bool[] useAi = { false, false, false};
        //for(int ctr=0; ctr<game.numPlayers; ++ctr)
        //{
        //    useAi[ctr] = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE) && (ctr != inLocalPlayerSlot);
        //}
        UnityTransport xportToUse = (SessionInfo.NetworkSetup == SessionInfo.Network.NONE ? null : xport);
        gameEngine = new AdventureGame(this, game.numPlayers, inLocalPlayerSlot, xportToUse, 
            game.gameNumber, game.diff1 == DIFF.A, game.diff2 == DIFF.A,
            SessionInfo.ThisPlayerInfo.needsPopupHelp, SessionInfo.ThisPlayerInfo.needsMazeGuides,
            SessionInfo.RaceCompleted, useAi);
    }

    public void PostChat(string message)
    {
        localPlayer.CmdPostChat(message);
    }

    public void AnnounceVoiceEnabledByHost()
    {
        localPlayer.voiceEnabledOnHost = true;
    }

    public void OnQuitPressed()
    {
        if (gameRunning)
        {
            quitConfirmPanel.SetActive(true);
        }
        else
        {
            OnQuitConfirmed();
        }
    }

    public void OnQuitConfirmed()
    {
        quitConfirmPanel.SetActive(false);
        xport.Disconnect();
    }

    public void OnQuitCanceled()
    {
        quitConfirmPanel.SetActive(false);
    }

    public override void Platform_ReportToServer(string message)
    {

        base.Platform_ReportToServer(message);
        if (message == AdventureReports.WON_GAME)
        {
            UpdateStandingsWithWin();
        }
        else if (eggMessages.Contains(message))
        {
            int index = eggMessages.IndexOf(message);
            ReportRaceToEgg(index);
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

    private void ReportRaceToEgg(int stage)
    {
        // Records people getting closer to finding the easter egg
        EggReport report = new EggReport(SessionInfo.ThisPlayerName, stage);
        string jsonStr = JsonUtility.ToJson(report);
        awsUtil.CallLambdaAsync(UPDATE_EGG_SCOREBOARD_LAMBDA, jsonStr);
    }

    private void SendPing()
    {
        awsUtil.CallLambdaAsync(PING_LAMBDA, "{}");
    }

}
