using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Game : NetworkBehaviour
{
    public Button actionButton;
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

    [SyncVar]
    public string connectionkey;

    [SyncVar]
    public int playerMapping;


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
        bool isInGame = (me != NO_PLAYER) && ((me == playerOne) || (me == playerTwo) || (me == playerThree));
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
        if (isInGame) {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<Text>().text = "Cancel";
        } else if ((playerThree != NO_PLAYER) || ((numPlayers == 2) && (playerTwo != NO_PLAYER))) {
            actionButton.gameObject.SetActive(false);
        } else {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<Text>().text = "Join";
        }
    }

    public void Join(uint player, string playerName) {
        if (playerTwo == NO_PLAYER) {
            playerTwo = player;
            playerTwoName = playerName;
        }
        else if ((numPlayers > 2) && (playerThree == NO_PLAYER)) {
            playerThree = player;
            playerThreeName = playerName;
        }
    }

    public void Leave(uint player)
    {
        if (playerTwo == player)
        {
            playerTwo = playerThree;
            playerThree = NO_PLAYER;
            playerTwoName = playerThreeName;
            playerThreeName = UNKNOWN_NAME;
        }
        else if (playerThree == player)
        {
            playerThree = NO_PLAYER;
            playerThreeName = UNKNOWN_NAME;
        }
    }


    public void OnActionButtonPressed()
    {
        uint me = LocalPlayer.Id;
        bool isInGame = (me != NO_PLAYER) && ((me == playerOne) || (me == playerTwo) || (me == playerThree));
        if (isInGame)
        {
            LocalPlayer.CmdLeaveGame(gameId);
        }
        else
        {
            LocalPlayer.CmdJoinGame(gameId);
        }
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
