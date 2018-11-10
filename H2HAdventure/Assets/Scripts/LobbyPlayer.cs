using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour {

    public LobbyController lobbyController;

	void Start () {
        Text thisText = this.GetComponent<Text>();
        NetworkIdentity id = this.GetComponent<NetworkIdentity>();
        thisText.text = "client-" + id.netId;
        GameObject LobbyPlayerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(LobbyPlayerList.transform);
        // The lobby controller needs someway to talk to the server, so it uses
        // the LobbyPlayer representing the local player
        GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
        lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
        if (isLocalPlayer) {
            lobbyController.setLocalLobbyPlayer(this);
        }
    }

    [Command]
    public void CmdHostGame(int numPlayers, int gameNumber, string hostPlayer) {
        GameObject gameGO = Instantiate(lobbyController.gamePrefab);
        Game game = gameGO.GetComponent<Game>();
        game.numPlayers = numPlayers;
        game.gameNumber = gameNumber;
        game.playerOne = hostPlayer;
        NetworkServer.Spawn(gameGO);
    }

}
