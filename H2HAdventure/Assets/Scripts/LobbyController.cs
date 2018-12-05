using System;
using System.Collections;
using System.Collections.Generic;
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

public class LobbyController : MonoBehaviour
{

    public const string GAME_SCENE = "FauxGame";

    public NetworkManager lobbyManager;
    public GameObject newGamePanel;
    public GameObject promptNamePanel;
    public Button hostButton;
    public InputField chatInput;
    public GameObject gamePrefab;
    public GameObject chatPrefab;
    public GameObject gameList;

    private const string LOBBY_MATCH_NAME = "h2hlobby";
    private string thisPlayerName = "";
    private LobbyPlayer localLobbyPlayer;
    private ChatSync localChatSync;
    private ulong matchNetwork;
    private NodeID matchNode;


    public LobbyPlayer LocalLobbyPlayer
    {
        get { return localLobbyPlayer; }
    }

    public ChatSync ChatSync {
        set { localChatSync = value; }
    }

    public string ThisPlayerName 
    {
        get { return thisPlayerName; }
        set 
        { 
            thisPlayerName = value;
            SessionInfo.ThisPlayerName = value;
        }
    }

    public void Start()
    {
        if (SessionInfo.ThisPlayerName == null) {
            promptNamePanel.SetActive(true);
        } else {
            ConnectToLobby();
        }
    }

    public void OnConnectedToLobby(LobbyPlayer inLocalLobbyPlayer) {
        localLobbyPlayer = inLocalLobbyPlayer;
        SessionInfo.ThisPlayerId = localLobbyPlayer.GetComponent<NetworkIdentity>().netId.Value;
        if (localLobbyPlayer.isServer) {
            // The lobby has just been created on the host.  So setup stuff that the lobby needs
            GameObject chatSyncGO = Instantiate(chatPrefab);
            NetworkServer.Spawn(chatSyncGO);
        }
    }

    public void ConnectToLobby() {
        if (SessionInfo.NetworkSetup == SessionInfo.Network.ALL_LOCAL) {
            NetworkClient client = null;
            client = lobbyManager.StartHost();
            if (client == null)
            {
                client = lobbyManager.StartClient();
            }
            hostButton.interactable = true;
        } else if (SessionInfo.NetworkSetup == SessionInfo.Network.MATCHMAKER) {
            if (lobbyManager.matchMaker == null)
            {
                lobbyManager.StartMatchMaker();
            }
            lobbyManager.matchMaker.ListMatches(0, 20, "", true, 0, 2, onMatchList);
        }
    }

    public void CloseNewGameDialog(bool submitted) {
        newGamePanel.SetActive(false);
        if (!submitted) {
            hostButton.interactable = true;
        }
    }

    public void GotPlayerName(string inPlayerName) {
        ThisPlayerName = inPlayerName;
        ConnectToLobby();
    }


    public void SubmitNewGame(NewGameInfo info) {
        localLobbyPlayer.CmdHostGame(info.numPlayers, info.gameNumber, localLobbyPlayer.GetComponent<NetworkIdentity>().netId.Value, localLobbyPlayer.playerName);
    }

    public void PostChat(LobbyPlayer player, string message) {
        if (localChatSync != null)
        {
            localChatSync.PostToChat(player, message);
        }
    }

    public void PlayerJoinGame(LobbyPlayer player, uint gameId) {
        Game[] games = gameList.GetComponentsInChildren<Game>();
        Game found = null;
        for (int i = 0; (i < games.Length) && (found == null); ++i) {
            if (games[i].gameId == gameId) {
                found = games[i];
            }
        }
        if (found != null) {
            bool gameReady = found.Join(player.Id, player.playerName);
            if (gameReady) {
                Debug.Log("Starting " + found.playerOneName + "'s game");
                found.RpcStartGame();
            }
        } 
    }

    public void PlayerReadyToStartGame(LobbyPlayer player, uint gameId)
    {
        Game[] games = gameList.GetComponentsInChildren<Game>();
        Game found = null;
        for (int i = 0; (i < games.Length) && (found == null); ++i)
        {
            if (games[i].gameId == gameId)
            {
                found = games[i];
            }
        }
        if (found != null)
        {
            bool allPlayersReady = found.readyToPlay(player);
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
        Game[] games = gameList.GetComponentsInChildren<Game>();
        Game found = null;
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
        } else {
            found.Join(player.Id, player.playerName);
        }
    }

    public void OnHostPressed() {
        hostButton.interactable = false;
        newGamePanel.SetActive(true);
    }

    public void OnChatPostPressed() {
        if (chatInput.text != "")
        {
            localLobbyPlayer.CmdPostChat(chatInput.text);
            chatInput.text = "";
        }
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
        SceneManager.LoadScene(GAME_SCENE);
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
        SceneManager.LoadScene(GAME_SCENE);
    }

    private IEnumerator ShutdownLocalNetwork()
    {
        yield return new WaitForSeconds(0.5);
        if (localLobbyPlayer.isServer)
        {
            lobbyManager.StopHost();
        }
        else
        {
            lobbyManager.StopClient();
        }
        ShutdownNetworkManager();
        SceneManager.LoadScene(GAME_SCENE);
    }

    public void StartGame(Game gameToPlay) {
        Debug.Log("Playing game " + gameToPlay);
        SessionInfo.GameToPlay = gameToPlay;
        // Disconnect from the lobby before switching to 
        if (SessionInfo.NetworkSetup == SessionInfo.Network.ALL_LOCAL) {
            StartCoroutine(ShutdownLocalNetwork());
        }
        else {
            if (localLobbyPlayer.isServer)
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

}
