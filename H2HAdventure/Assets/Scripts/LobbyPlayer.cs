using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class LobbyPlayer : NetworkBehaviour
{

    public LobbyController lobbyController;

    public uint Id
    {
        get { return this.GetComponent<NetworkIdentity>().netId.Value; }
    }

    void Start()
    {
        Text thisText = this.GetComponent<Text>();
        thisText.text = "client-" + Id;
        GameObject LobbyPlayerList = GameObject.FindGameObjectWithTag("LobbyPlayerParent");
        gameObject.transform.SetParent(LobbyPlayerList.transform);
        // The lobby controller needs someway to talk to the server, so it uses
        // the LobbyPlayer representing the local player
        GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
        lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
        if (isLocalPlayer)
        {
            lobbyController.LocalLobbyPlayer = this;
        }
    }

    [Command]
    public void CmdHostGame(int numPlayers, int gameNumber, uint hostPlayerId)
    {
        GameObject gameGO = Instantiate(lobbyController.gamePrefab);
        Game game = gameGO.GetComponent<Game>();
        game.numPlayers = numPlayers;
        game.gameNumber = gameNumber;
        game.playerOne = hostPlayerId;
        NetworkServer.Spawn(gameGO);
    }

    [Command]
    public void CmdJoinGame(uint gameId) {
        Debug.Log("Client " + Id + " is joining game #" + gameId);
        lobbyController.PlayerJoinGame(this, gameId);
    }

}
