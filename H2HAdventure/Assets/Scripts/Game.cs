using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Game : NetworkBehaviour
{
    public Button actionButton;
    public Text text;
    public Text playerText;

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

    /** When enough players have joined, we notify each player to start the game
     * but wait for an ack before disconnecting anything.  This is the number 
     * of players that have acked. */
    private int numPlayersReady = 0;

    private LobbyPlayer localPlayer;
    private LobbyPlayer LocalPlayer
    {
        get
        {
            if (localPlayer == null)
            {
                GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
                LobbyController lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
                localPlayer = lobbyController.LocalLobbyPlayer;
            }
            return localPlayer;
        }
    }

    public bool IsInGame(uint player)
    {
        return ((player != NO_PLAYER) &&
                ((playerOne == player) || (playerTwo == player) || (playerThree == player)));
    }

    void Start()
    {
        gameId = this.GetComponent<NetworkIdentity>().netId.Value;

        GameObject GameList = GameObject.FindGameObjectWithTag("GameParent");
        gameObject.transform.SetParent(GameList.transform);
        RefreshGraphic();
    }

    [ClientRpc]
    public void RpcStartGame()
    {
        Debug.Log(localPlayer.playerName + " is starting " + playerOneName + "'s game");
        localPlayer.CmdSignalStartingGame(this.gameId);
        // Regular clients disconnect from the lobby and start playing immediately.
        // But the host of the lobby has to wait until getting an ack from the other
        // players before disconnecting.
        if (!isServer)
        {
            Debug.Log("Not the host so can start game immediately.");
            GameObject lobbyControllerGO = GameObject.FindGameObjectWithTag("LobbyController");
            LobbyController lobbyController = lobbyControllerGO.GetComponent<LobbyController>();
            lobbyController.StartGame(this);
        }
    }

    /**
     * Marks a player as ready to play.  Returns true if all players are now
     * ready to play. 
     */
    public bool readyToPlay(LobbyPlayer player)
    {
        uint playerId = player.Id;
        if ((playerId == playerOne) || (playerId == playerTwo) || 
            ((playerThree != NO_PLAYER) && (playerId == playerThree))) {
            ++numPlayersReady;
        }
        return numPlayersReady >= numPlayers;
    }

    private void RefreshGraphic()
    {
        uint me = (LocalPlayer != null ? LocalPlayer.Id : NO_PLAYER);
        bool amInGame = IsInGame(me);
        string playerList = (playerOne == me ? "you" : playerOneName) + "\n";
        if (playerTwo != NO_PLAYER)
        {
            playerList += (playerTwo == me ? "you" : playerTwoName) + "\n";
            if (playerThree != NO_PLAYER)
            {
                playerList += (playerThree == me ? "you" : playerThreeName) + "\n";
            }

        }
        text.text = "Game " + (gameNumber + 1) + "\n  " + numPlayers + " players";
        playerText.text = playerList;
        if (amInGame)
        {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<Text>().text = "Cancel";
        }
        else if ((playerThree != NO_PLAYER) || ((numPlayers == 2) && (playerTwo != NO_PLAYER)))
        {
            actionButton.gameObject.SetActive(false);
        }
        else
        {
            actionButton.gameObject.SetActive(true);
            actionButton.GetComponentInChildren<Text>().text = "Join";
        }
    }

    public bool Join(uint player, string playerName)
    {
        bool gameReady = false;
        if (playerTwo == NO_PLAYER)
        {
            playerTwo = player;
            playerTwoName = playerName;
            gameReady = (numPlayers == 2);
        }
        else if ((numPlayers > 2) && (playerThree == NO_PLAYER))
        {
            playerThree = player;
            playerThreeName = playerName;
            gameReady = true;
        }
        return gameReady;
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

    public override string ToString()
    {
        return "Game #" + gameNumber + ", " + numPlayers + " players: " + playerOneName + (playerTwo == NO_PLAYER ? "" :
            (playerThree == NO_PLAYER ? "" : ", " + playerThreeName) + " and " + playerTwoName);
        

   

    }

}
