using System;
using System.Collections;
using System.Collections.Generic;
using Amazon.Lambda;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.Networking.Types;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class NewGameInfo {
    public int numPlayers;
    public int gameNumber;
    public bool fastDragons;
    public bool dragonsRunFromSword;
}

public class LobbyController : MonoBehaviour, ChatSubmitter
{
    public NetworkManager lobbyManager;
    public ChatPanelController chatPanel;
    public GameObject newGamePanel;
    public Button hostButton;
    public GameObject gamePrefab;
    public GameObject gameList;
    public GameObject overlay;
    public GameObject sendCallConf;
    public GameObject noOneElsePanel;
    public AWS awsUtil;

    private const string LOBBY_MATCH_NAME = "h2hlobby";
    private LobbyPlayer localLobbyPlayer;
    private ulong matchNetwork;
    private NodeID matchNode;
    /** When we leave the scene we store the next scene because
     * we have to disconnect first in an asynchronous callback. */
    private String nextSceneName;
    private bool shuttingDown = false;


    public LobbyPlayer LocalLobbyPlayer
    {
        get { return localLobbyPlayer; }
    }

    public string ThisPlayerName 
    {
        get { return SessionInfo.ThisPlayerName; }
        set 
        { 
            SessionInfo.ThisPlayerName = value;
        }
    }

    public ChatPanelController GetChatPanelController()
    {
        return chatPanel;
    }

    public void Start()
    {
        chatPanel.ChatSubmitter = this;
        ConnectToLobby();
    }

    public void OnConnectedToLobby(LobbyPlayer inLocalLobbyPlayer) {
        localLobbyPlayer = inLocalLobbyPlayer;
        SessionInfo.ThisPlayerId = localLobbyPlayer.GetComponent<NetworkIdentity>().netId.Value;
        if (localLobbyPlayer.isServer)
        {
            chatPanel.ServerSetup();
            // If you're hosting the lobby then you're the only one right now
            noOneElsePanel.SetActive(true);
        }
        else
        {
            overlay.SetActive(false);
        }
    }

    public void ConnectToLobby() {
        if (SessionInfo.NetworkSetup == SessionInfo.Network.ALL_LOCAL)
        {
            NetworkClient client = null;
            client = lobbyManager.StartHost();
            if (client == null)
            {
                client = lobbyManager.StartClient();
            }
            hostButton.interactable = true;
        }
        else if (SessionInfo.NetworkSetup == SessionInfo.Network.DIRECT_CONNECT)
        {
            if (SessionInfo.DirectConnectIp == SessionInfo.DIRECT_CONNECT_HOST_FLAG)
            {
                lobbyManager.networkAddress = "127.0.0.1";
                lobbyManager.networkPort = 1980;
                lobbyManager.StartHost();
            } else {
                lobbyManager.networkAddress = SessionInfo.DirectConnectIp;
                lobbyManager.networkPort = 1980;
                lobbyManager.StartClient();
            }
            hostButton.interactable = true;
        }
        else if (SessionInfo.NetworkSetup == SessionInfo.Network.MATCHMAKER) {
            if (lobbyManager.matchMaker == null)
            {
                lobbyManager.StartMatchMaker();
            }
            lobbyManager.matchMaker.ListMatches(0, 20, "", true, 0, 2, onMatchList);
        }
    }

    public void CloseNewGameDialog(bool submitted) {
        newGamePanel.SetActive(false);
        overlay.SetActive(false);
        if (!submitted) {
            hostButton.interactable = true;
        }
    }

    public void SubmitNewGame(NewGameInfo info) {
        localLobbyPlayer.CmdHostGame(info.numPlayers, info.gameNumber, 
            info.fastDragons, info.dragonsRunFromSword,
            localLobbyPlayer.GetComponent<NetworkIdentity>().netId.Value, localLobbyPlayer.playerName);
    }

    public void PostChat(string message)
    {
        localLobbyPlayer.CmdPostChat(message);
    }

    /**
     * Add a player to a game.
     * This method is only executed on the lobby host.
     */    
    public void PlayerJoinGame(LobbyPlayer player, uint gameId) {
        GameInLobby[] games = gameList.GetComponentsInChildren<GameInLobby>();
        GameInLobby found = null;
        for (int i = 0; (i < games.Length) && (found == null); ++i) {
            if (games[i].gameId == gameId) {
                found = games[i];
            }
        }
        if (found != null) {
            bool gameReady = found.Join(player.Id, player.playerName);
            if (gameReady) {
                Debug.Log("Starting " + found.playerOneName + "'s game");
                found.markReadyToPlay();
            }
        } 
    }

    /**
     * Mark that this player has all the information needed to start this
     * game.
     */    
    public void PlayerReadyToStartGame(LobbyPlayer player, uint gameId)
    {
        GameInLobby[] games = gameList.GetComponentsInChildren<GameInLobby>();
        GameInLobby found = null;
        for (int i = 0; (i < games.Length) && (found == null); ++i)
        {
            if (games[i].gameId == gameId)
            {
                found = games[i];
            }
        }
        if (found != null)
        {
            bool allPlayersReady = found.markReadyToPlay(player);
            if (allPlayersReady)
            {
                // If one of the game's players is the player hosting the
                // lobby, they delayed starting the game until the other
                // players had acked.
                if (found.IsInGame(localLobbyPlayer.Id))
                {
                    Debug.Log("All players are ready to start game.  Shutting down lobby.");
                    StartGame(found);
                } else {
                    Debug.Log("Game started for players other than host.  Don't need to shut down lobby.");
                }
                NetworkServer.Destroy(found.gameObject);
            } else {
                Debug.Log("Still waiting for more players to signal start.");
            }
        } else {
            Debug.Log("Received signal that " + player.playerName + "is ready to start unknown game #" + gameId);
        }
    }

    public void PlayerLeaveGame(LobbyPlayer player, uint gameId) {
        GameInLobby[] games = gameList.GetComponentsInChildren<GameInLobby>();
        GameInLobby found = null;
        for (int i = 0; (i < games.Length) && (found == null); ++i)
        {
            if (games[i].gameId == gameId)
            {
                found = games[i];
            }
        }
        if (found != null)
        {
            if (player.Id == found.playerOne)
            {
                NetworkServer.Destroy(found.gameObject);
            }
            else
            {
                found.Leave(player.Id);
            }
        }
    }

    /** One of the games in the game list has changed state.  See if the scene
     * needs to change any of its display */
    public void OnGameStateUpdated()
    {
        // First determine if the local player is now in a game or not
        uint me = (localLobbyPlayer == null ? GameInLobby.NO_PLAYER : localLobbyPlayer.Id);
        GameInLobby[] games = gameList.GetComponentsInChildren<GameInLobby>();
        bool inAGame = false;
        for (int i=0; i<games.Length && !inAGame; ++i)
        {
            inAGame = inAGame || (games[i].IsInGame(me) && !games[i].HasBeenDestroyed);
        }
        // Have all games update their display based on whether local player is 
        // now in a game.
        for (int i = 0; i < games.Length; ++i)
        {
            games[i].RefreshGraphic(inAGame);
        }
        // Disable the "Host Game" button.
        hostButton.interactable = !inAGame;
    }

    public void OnHostPressed() {
        hostButton.interactable = false;
        overlay.SetActive(true);
        newGamePanel.SetActive(true);
    }

    private void onMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        if (!success)
        {
            Debug.Log("Error looking for default Lobby match.");
            // TODO: FIX0001
        }
        else
        {
            MatchInfoSnapshot found = null;
            if (matchList.Count > 0)
            {
                // There should be only one match ever - the default one
                // but we check just in case
                foreach (MatchInfoSnapshot match in matchList)
                {
                    if (match.name.Equals(LOBBY_MATCH_NAME))
                    {
                        found = match;
                        break;
                    }
                }
            }
            if (found == null)
            {
                // No one has hosted yet.  Try to host.
                lobbyManager.matchMaker.CreateMatch(LOBBY_MATCH_NAME, (uint)100, true,
                                    "", "", "", 0, 2, OnMatchCreate);
            }
            else
            {
                lobbyManager.matchMaker.JoinMatch(found.networkId, "", "", "", 0, 2, OnMatchJoined);
            }
        }
    }

    public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        if (!success)
        {
            Debug.Log("Error creating lobby's default match.");
            // TODO: FIX0001
        }
        else
        {
            matchNetwork = (ulong)matchInfo.networkId;
            lobbyManager.OnMatchCreate(success, extendedInfo, matchInfo);
            hostButton.interactable = true;
        }
    }

    public virtual void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo) {
        if (!success)
        {
            Debug.Log("Error joining lobby");
            // TODO: FIX0001
        }
        else
        {
            Debug.Log("Joined lobby!");
            matchNetwork = (ulong)matchInfo.networkId;
            matchNode = matchInfo.nodeId;
            lobbyManager.OnMatchJoined(success, extendedInfo, matchInfo);
            hostButton.interactable = true;
        }
    }

    public void OnDropConnection(bool success, string extendedInfo)
    {
        if (!success)
        {
            // Just report and try to continue with joining game.
            // Theoretically possible that lobby has already been brought down because
            // player hosting lobby has moved on to connecting to game
            Debug.Log("Error trying to disconnect from lobby");
        } else {
            Debug.Log("Disconnected from lobby");
        }
        lobbyManager.OnDropConnection(success, extendedInfo);
        ShutdownNetworkManager();
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnDestroyMatch(bool success, string extendedInfo)
    {
        if (!success)
        {
            // Just report and continue with joining game.
            Debug.Log("Error trying to shutdown lobby");
        }
        else
        {
            Debug.Log("Shutdown lobby");
        }
        lobbyManager.OnDestroyMatch(success, extendedInfo);
        ShutdownNetworkManager();
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnPlayerDropped(LobbyPlayer player)
    {
        // Only the server needs to do anything when a player drops
        if (player.isServer)
        {
            // Remove the player from any game they are in.  If they are
            // the host, cancel the whole game.
            GameInLobby[] games = gameList.GetComponentsInChildren<GameInLobby>();
            for (int i = 0; i < games.Length; ++i)
            {
                GameInLobby next = games[i];
                if (next.playerOne == player.Id)
                {
                    NetworkServer.Destroy(next.gameObject);
                }
                else if ((next.playerTwo == player.Id) || (next.playerThree == player.Id)) { 
                    next.Leave(player.Id);
                }
            }
        }
    }

    public void OnHostDropped()
    {
        if (!shuttingDown)
        {
            // We've gotten disconnected from the host of the lobby.
            // Most likely because that person has gone to a game.
            // Only thing to do is to disconnect and create a new lobby
            SwitchToScene("Lobby");
        }
    }

    private IEnumerator ShutdownLocalNetwork()
    {
        yield return new WaitForSeconds(0.5f);
        if ((localLobbyPlayer != null) && (localLobbyPlayer.isServer))
        {
            lobbyManager.StopHost();
        }
        else
        {
            lobbyManager.StopClient();
        }
        ShutdownNetworkManager();
        SceneManager.LoadScene(nextSceneName);
    }

    public void OnSendCallPressed()
    {
        overlay.SetActive(true);
        sendCallConf.SetActive(true);
    }

    public void OnSendCallConfOkPressed()
    {
        sendCallConf.SetActive(false);
        overlay.SetActive(false);
        SendCall();
    }

    public void OnSendCallConfCancelPressed()
    {
        sendCallConf.SetActive(false);
        overlay.SetActive(false);
    }

    public void OnNoOneElseOkPressed()
    {
        noOneElsePanel.SetActive(false);
        overlay.SetActive(false);
    }

    public void OnBackPressed()
    {
        SwitchToScene("Start");
    }

    public void StartGame(GameInLobby gameToPlay) {
        SessionInfo.GameToPlay = gameToPlay;
        Debug.Log(SessionInfo.ThisPlayerName + "(" + SessionInfo.ThisPlayerId + 
        ") is playing game " + SessionInfo.GameToPlay);
        // Disconnect from the lobby before switching to 
        SwitchToScene(SessionInfo.GAME_SCENE);
    }

    private void SwitchToScene(string sceneName)
    {
        shuttingDown = true;
        nextSceneName = sceneName;
        // Disconnect from the lobby
        if ((SessionInfo.NetworkSetup == SessionInfo.Network.ALL_LOCAL) ||
            (SessionInfo.NetworkSetup == SessionInfo.Network.DIRECT_CONNECT))
        {
            StartCoroutine(ShutdownLocalNetwork());
        }
        else
        {
            if ((localLobbyPlayer == null) || (lobbyManager.matchMaker == null))
            {
                // Host has already disconnected.
                ShutdownNetworkManager();
                SceneManager.LoadScene(nextSceneName);
            }
            else if (localLobbyPlayer.isServer)
            {
                lobbyManager.matchMaker.DestroyMatch((NetworkID)matchNetwork, 0, OnDestroyMatch);
            }
            else
            {
                lobbyManager.matchMaker.DropConnection((NetworkID)matchNetwork, matchNode, 0, OnDropConnection);
            }
        }
    }

    private void ShutdownNetworkManager() {
        Destroy(lobbyManager);
        NetworkManager.Shutdown();
    }

    private void SendCall()
    {
        string subject = SEND_CALL_SUBJECT.Replace("{{name}}", SessionInfo.ThisPlayerName);
        string message = SEND_CALL_MESSAGE.Replace("{{name}}", SessionInfo.ThisPlayerName);
        EmailSubscriptionRequest newRequest = new EmailSubscriptionRequest(subject, message);
        string jsonStr = JsonUtility.ToJson(newRequest);
        awsUtil.CallLambdaAsync(NotifyMeController.EMAIL_SUBSCRIPTION_LAMBDA, jsonStr);
    }

    private const string SEND_CALL_SUBJECT= "{{name}} wants to play h2hadventure";
    private const string SEND_CALL_MESSAGE = "You had requested to be emailed whenever someone is " +
        "looking to play H2H Atari Adventure.  Well {{name}} is online and has just sent out a call." +
        "\n\n" +
        "If you wish to no longer receive these events you can unsubscribe through the H2HAdventure " +
        "interface by clicking \"Notify Me\"";


}
