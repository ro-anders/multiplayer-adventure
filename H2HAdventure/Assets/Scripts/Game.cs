using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Game : NetworkBehaviour
{

    public Text text;

    private static readonly uint NO_PLAYER = NetworkInstanceId.Invalid.Value;

    public uint gameId;
    
    [SyncVar(hook = "OnChangePlayerOne")]
    public uint playerOne = NO_PLAYER;

    [SyncVar(hook = "OnChangePlayerTwo")]
    public uint playerTwo = NO_PLAYER;

    [SyncVar(hook = "OnChangePlayerThree")]
    public uint playerThree = NO_PLAYER;

    [SyncVar(hook = "OnChangeNumPlayers")]
    public int numPlayers;

    [SyncVar(hook = "OnChangeGameNumber")]
    public int gameNumber;

    private LobbyPlayer localPlayer;

    void Start()
    {
        gameId = this.GetComponent<NetworkIdentity>().netId.Value;

        GameObject GameList = GameObject.FindGameObjectWithTag("GameParent");
        gameObject.transform.SetParent(GameList.transform);
        RefreshGraphic();

        GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
        LobbyController lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
        localPlayer = lobbyController.LocalLobbyPlayer;
    }

    private void RefreshGraphic()
    {
        string playerList = playerOne.ToString();
        if (playerTwo != NO_PLAYER)
        {
            playerList += (playerThree != NO_PLAYER ? ", " : " and ");
            playerList += playerTwo;
            if (playerThree != NO_PLAYER)
            {
                playerList += " and " + playerThree;
            }

        }
        text.text = numPlayers + " player game #" + (gameNumber + 1) + " with " + playerList;
    }

    public void Join(uint player) {
        if (playerTwo == NO_PLAYER) {
            playerTwo = player;
            Debug.Log("Joined game");
        }
        else if ((numPlayers > 2) && (playerThree == NO_PLAYER)) {
            playerThree = player;
            Debug.Log("Joined game");
        } else {
            Debug.Log("Did not join game");
        }

    }

    public void OnActionButtonPressed()
    {
        Debug.Log("Calling CmdJoinGame on server");
        localPlayer.CmdJoinGame(gameId);
    }

    void OnChangePlayerOne(uint newPlayerOne)
    {
        playerOne = newPlayerOne;
        RefreshGraphic();
    }

    void OnChangePlayerTwo(uint newPlayerTwo)
    {
        playerTwo = newPlayerTwo;
        RefreshGraphic();
    }

    void OnChangePlayerThree(uint newPlayerThree)
    {
        playerThree = newPlayerThree;
        RefreshGraphic();
    }

    void OnChangeNumPlayers(int newNumPlayers)
    {
        numPlayers = newNumPlayers;
        RefreshGraphic();
    }

    void OnChangeGameNumber(int newGameNumber)
    {
        gameNumber = newGameNumber;
        RefreshGraphic();
    }


}
