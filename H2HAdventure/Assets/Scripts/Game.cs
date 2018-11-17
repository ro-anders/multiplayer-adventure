using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Game : NetworkBehaviour
{

    public Text text;

    private static readonly uint NO_PLAYER = NetworkInstanceId.Invalid.Value;
    private static readonly string UNKNOWN_NAME = "--";

    public uint gameId;

    [SyncVar(hook = "OnChangePlayerOne")]
    public uint playerOne = NO_PLAYER;

    [SyncVar(hook = "OnChangePlayerOneName")]
    public string playerOneName = UNKNOWN_NAME;

    [SyncVar(hook = "OnChangePlayerTwo")]
    public uint playerTwo = NO_PLAYER;

    [SyncVar(hook = "OnChangePlayerTwoName")]
    public string playerTwoName = UNKNOWN_NAME;

    [SyncVar(hook = "OnChangePlayerThree")]
    public uint playerThree = NO_PLAYER;

    [SyncVar(hook = "OnChangePlayerThreeName")]
    public string playerThreeName = UNKNOWN_NAME;

    [SyncVar(hook = "OnChangeNumPlayers")]
    public int numPlayers;

    [SyncVar(hook = "OnChangeGameNumber")]
    public int gameNumber;

    private LobbyPlayer localPlayer;
    private LobbyPlayer LocalPlayer {
        get {
            if (localPlayer == null) {
                GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
                LobbyController lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
                localPlayer = lobbyController.LocalLobbyPlayer;
            }
            return localPlayer;
        }
    }

    void Start()
    {
        gameId = this.GetComponent<NetworkIdentity>().netId.Value;

        GameObject GameList = GameObject.FindGameObjectWithTag("GameParent");
        gameObject.transform.SetParent(GameList.transform);
        RefreshGraphic();
    }

    private void RefreshGraphic()
    {
        uint me = (LocalPlayer != null ? LocalPlayer.Id : NO_PLAYER);
        string playerList = (playerOne == me ? "you" : playerOneName);
        if (playerTwo != NO_PLAYER)
        {
            playerList += (playerThree != NO_PLAYER ? ", " : " and ");
            playerList += (playerTwo == me ? "you" : playerTwoName);
            if (playerThree != NO_PLAYER)
            {
                playerList += " and " + (playerThree == me ? "you" : playerThreeName);
            }

        }
        text.text = numPlayers + " player game #" + (gameNumber + 1) + " with " + playerList;
    }

    public void Join(uint player, string playerName) {
        if (playerTwo == NO_PLAYER) {
            playerTwo = player;
            playerTwoName = playerName;
            Debug.Log("Joined game");
        }
        else if ((numPlayers > 2) && (playerThree == NO_PLAYER)) {
            playerThree = player;
            playerThreeName = playerName;
            Debug.Log("Joined game");
        } else {
            Debug.Log("Did not join game");
        }

    }

    public void OnActionButtonPressed()
    {
        Debug.Log("Calling CmdJoinGame on server");
        LocalPlayer.CmdJoinGame(gameId);
    }

    void OnChangePlayerOne(uint newPlayerOne)
    {
        playerOne = newPlayerOne;
        RefreshGraphic();
    }

    void OnChangePlayerOneName(string newPlayerOneName)
    {
        playerOneName = newPlayerOneName;
        RefreshGraphic();
    }

    void OnChangePlayerTwo(uint newPlayerTwo)
    {
        playerTwo = newPlayerTwo;
        RefreshGraphic();
    }

    void OnChangePlayerTwoName(string newPlayerTwoName)
    {
        playerTwoName = newPlayerTwoName;
        RefreshGraphic();
    }

    void OnChangePlayerThree(uint newPlayerThree)
    {
        playerThree = newPlayerThree;
        RefreshGraphic();
    }

    void OnChangePlayerThreeName(string newPlayerThreeName)
    {
        playerThreeName = newPlayerThreeName;
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
