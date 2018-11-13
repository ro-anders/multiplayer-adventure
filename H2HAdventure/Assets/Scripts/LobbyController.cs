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
    public Button hostButton;
    public GameObject gamePrefab;
    public GameObject gameList;

    private const string LOBBY_MATCH_NAME = "h2hlobby";
    private LobbyPlayer localLobbyPlayer;


    public LobbyPlayer LocalLobbyPlayer
    {
        get { return localLobbyPlayer; }
        set { localLobbyPlayer = value; }
    }

    public void CloseNewGameDialog(bool submitted) {
        newGamePanel.SetActive(false);
        if (!submitted) {
            hostButton.interactable = true;
        }
    }

    public void SubmitNewGame(NewGameInfo info) {
        localLobbyPlayer.CmdHostGame(info.numPlayers, info.gameNumber, localLobbyPlayer.GetComponent<NetworkIdentity>().netId.Value);
    }

    public void PlayerJoinGame(LobbyPlayer player, uint gameId) {
        Game[] games = gameList.GetComponentsInChildren<Game>();
        Debug.Log("Searching for game #" + gameId + " in list of " + games.Length + " games");
        Game found = null;
        for (int i = 0; (i < games.Length) && (found == null); ++i) {
            if (games[i].gameId == gameId) {
                found = games[i];
            }
        }
        if (found != null) {
            found.Join(player.Id);
            Debug.Log("Client #" + player.Id + " joined " + found.playerOne + "'s game");
        } else {
            Debug.Log("Could not find game #" + gameId + " in list of " + games.Length + " games");
        }

    }

    public void OnHostPressed() {
        hostButton.interactable = false;
        newGamePanel.SetActive(true);
    }

    public void OnLocalStartPressed() {
        hostButton.interactable = true;
        NetworkClient client = null;
        client = lobbyManager.StartHost();
        if (client == null)
        {
            client = lobbyManager.StartClient();
        }
    }

    public void OnNetworkedStartPressed() {
        if (lobbyManager.matchMaker == null)
        {
            lobbyManager.StartMatchMaker();
        }
        lobbyManager.matchMaker.ListMatches(0, 20, "", true, 0, 0, onMatchList);
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
                    if (match.name == LOBBY_MATCH_NAME)
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
                                    "", "", "", 0, 0, lobbyManager.OnMatchCreate);
            }
            else
            {
                lobbyManager.matchMaker.JoinMatch(found.networkId, "", "", "", 0, 0, lobbyManager.OnMatchJoined);
            }
        }
    }

}
