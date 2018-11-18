using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Networking.Match;
using UnityEngine.UI;

public class NewGameInfo {
    public int numPlayers;
    public int gameNumber;
    public bool fastDragons;
    public bool dragonsRunFromSword;
}

public class LobbyController : MonoBehaviour
{

    public NetworkManager lobbyManager;
    public GameObject newGamePanel;
    public GameObject promptNamePanel;
    public Button hostButton;
    public GameObject gamePrefab;
    public GameObject gameList;

    private const string LOBBY_MATCH_NAME = "h2hlobby";
    private string thisPlayerName = "";
    private LobbyPlayer localLobbyPlayer;


    public LobbyPlayer LocalLobbyPlayer
    {
        get { return localLobbyPlayer; }
        set 
        { 
            localLobbyPlayer = value; 
            SessionInfo.ThisPlayerId = value.GetComponent<NetworkIdentity>().netId.Value;
        }
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
            lobbyManager.matchMaker.ListMatches(0, 20, "", true, 0, 0, onMatchList);
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

    public void PlayerJoinGame(LobbyPlayer player, uint gameId) {
        Game[] games = gameList.GetComponentsInChildren<Game>();
        Game found = null;
        for (int i = 0; (i < games.Length) && (found == null); ++i) {
            if (games[i].gameId == gameId) {
                found = games[i];
            }
        }
        if (found != null) {
            found.Join(player.Id, player.playerName);
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

    private void onMatchList(bool success, string extendedInfo, List<MatchInfoSnapshot> matchList)
    {
        if (!success)
        {
            Debug.Log("Error looking for default Lobby match.");
            // we are going to refresh it again
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
                                    "", "", "", 0, 0, OnMatchCreate);
            }
            else
            {
                lobbyManager.matchMaker.JoinMatch(found.networkId, "", "", "", 0, 0, lobbyManager.OnMatchJoined);
            }
        }
    }

    public void OnMatchCreate(bool success, string extendedInfo, MatchInfo matchInfo)
    {
        lobbyManager.OnMatchCreate(success, extendedInfo, matchInfo);
        hostButton.interactable = true;
    }

    public virtual void OnMatchJoined(bool success, string extendedInfo, MatchInfo matchInfo) {
        lobbyManager.OnMatchJoined(success, extendedInfo, matchInfo);
        hostButton.interactable = true;
    }

}
