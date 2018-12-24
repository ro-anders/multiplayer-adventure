using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{

    public LobbyController lobbyController;

    [SyncVar(hook = "OnChangePlayerName")]
    public string playerName = "";

    public uint Id
    {
        get { return this.GetComponent<NetworkIdentity>().netId.Value; }
    }

    void Start()
    {
        GameObject LobbyPlayerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(LobbyPlayerList.transform, false);
        // The lobby controller needs someway to talk to the server, so it uses
        // the LobbyPlayer representing the local player
        GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
        lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
        if (isLocalPlayer)
        {
            lobbyController.OnConnectedToLobby(this);
            CmdSetPlayerName(lobbyController.ThisPlayerName);
        }
        RefreshDisplay();
    }

    [Command]
    public void CmdSetPlayerName(string name) {
        playerName = name;
    }

    [Command]
    public void CmdHostGame(int numPlayers, int gameNumber, uint hostPlayerId, string hostPlayerName)
    {
        System.Random rand = new System.Random();
        GameObject gameGO = Instantiate(lobbyController.gamePrefab);
        GameInLobby game = gameGO.GetComponent<GameInLobby>();
        game.numPlayers = numPlayers;
        game.gameNumber = gameNumber;
        game.playerOne = hostPlayerId;
        game.playerOneName = hostPlayerName;
        game.playerMapping = rand.Next(0, (numPlayers == 2 ? 2 : 6));
        if (SessionInfo.NetworkSetup == SessionInfo.Network.MATCHMAKER) {
            game.connectionkey = "h2h-" + hostPlayerId + "-" + rand.Next(100);
        } else {
            game.connectionkey = (30000 + rand.Next(100) * 100 + hostPlayerId).ToString();
        }
        NetworkServer.Spawn(gameGO);
    }

    [Command]
    public void CmdJoinGame(uint gameId) {
        lobbyController.PlayerJoinGame(this, gameId);
    }

    [Command]
    public void CmdSignalStartingGame(uint gameId) {
        Debug.Log(this.playerName + " got start game command");
        lobbyController.PlayerReadyToStartGame(this, gameId);
    }

    [Command]
    public void CmdLeaveGame(uint gameId) {
        lobbyController.PlayerLeaveGame(this, gameId);
    }

    [Command]
    public void CmdPostChat(string message) {
        lobbyController.PostChat(this, message);
    }

    private void RefreshDisplay() {
        Text thisText = this.GetComponent<Text>();
        thisText.text = ((playerName != null) && (playerName != "") ? playerName : "unknown-" + Id);
    }

    private void OnChangePlayerName(string newPlayerName) {
        playerName = newPlayerName;
        Debug.Log(newPlayerName + " connected to lobby");
        RefreshDisplay();
    }
}
