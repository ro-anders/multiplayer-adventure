using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour {

	void Start () {
        Text thisText = this.GetComponent<Text>();
        NetworkIdentity id = this.GetComponent<NetworkIdentity>();
        thisText.text = "client-" + id.netId;
        Debug.Log("New client.  Adding to list.");
        GameObject LobbyPlayerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(LobbyPlayerList.transform);
        // The lobby controller needs someway to talk to the server, so it uses
        // the LobbyPlayer representing the local player
        if (isLocalPlayer) {
            GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
            LobbyController lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
            lobbyController.setLocalLobbyPlayer(this);
        }
    }

    [Command]
    public void CmdHostGame(GameObject gamePrefab, int numPlayers, int gameNumber, string hostPlayer) {
        GameObject gameGO = Instantiate(gamePrefab);
        Game game = gameGO.GetComponent<Game>();
        game.numPlayers = numPlayers;
        game.gameNumber = gameNumber;
        game.playerOne = hostPlayer;
    }

}
