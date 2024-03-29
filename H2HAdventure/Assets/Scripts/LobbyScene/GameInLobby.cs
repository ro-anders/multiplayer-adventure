﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum DIFF
{
    NOT_SET,
    A,
    B
}

public class GameInLobby : MonoBehaviour
{
    public Button actionButton;
    public Text text;
    public Text playerText;
 #pragma warning disable CS0618 // Type or member is obsolete

    public static readonly uint NO_PLAYER = 2743;
    private const string UNKNOWN_NAME = "--";
    private static readonly int[,] permutations = new int[,] {
        {0, 1, 2},
        {1, 0, 2},
        {1, 2, 0},
        {0, 2, 1},
        {2, 0, 1},
        {2, 1, 0}
    };


    public uint gameId;

    //[SyncVar(hook = "OnChangePlayerOne")]
    public uint playerOne = NO_PLAYER;

    //[SyncVar(hook = "OnChangePlayerOneName")]
    public string playerOneName = UNKNOWN_NAME;

    //[SyncVar(hook = "OnChangePlayerTwo")]
    public uint playerTwo = NO_PLAYER;

    //[SyncVar(hook = "OnChangePlayerTwoName")]
    public string playerTwoName = UNKNOWN_NAME;

    //[SyncVar(hook = "OnChangePlayerThree")]
    public uint playerThree = NO_PLAYER;

    //[SyncVar(hook = "OnChangePlayerThreeName")]
    public string playerThreeName = UNKNOWN_NAME;

    //[SyncVar(hook = "OnChangeNumPlayers")]
    public int numPlayers = 0;

    //[SyncVar(hook = "OnChangeGameNumber")]
    public int gameNumber = -1;

    //[SyncVar(hook = "OnChangeDiff1")]
    public DIFF diff1 = DIFF.NOT_SET;

    //[SyncVar(hook = "OnChangeDiff2")]
    public DIFF diff2 = DIFF.NOT_SET;

    //[SyncVar(hook = "OnChangeIsReadyToPlay")]
    public bool isReadyToPlay = false;

    //[SyncVar(hook = "OnChangeConnectionKey")]
    public string connectionkey = "";

    //[SyncVar(hook = "OnChangePlayerMapping")]
    public int playerMapping = -1;
#pragma warning restore CS0618 // Type or member is obsolete

    /** When enough players have joined, we notify each player to start the game
     * but wait for an ack before disconnecting anything.  This is the number 
     * of players that have acked. */
    private int numPlayersReady = 0;
    private bool hasBeenDestroyed = false;
    public bool HasBeenDestroyed
    {
        get { return hasBeenDestroyed; }
    }

    private LobbyPlayer localPlayer;
    private LobbyPlayer LocalPlayer
    {
        get
        {
            // RIPPED
            return localPlayer;
        }
    }

    public bool IsInGame(uint player)
    {
        return ((player != NO_PLAYER) &&
                ((playerOne == player) || (playerTwo == player) || (playerThree == player)));
    }

    public uint[] GetPlayersInGameOrder()
    {
        uint[] unordered = (numPlayers == 2 ? new uint[] { playerOne, playerTwo } :
          new uint[] { playerOne, playerTwo, playerThree });
        int[] ordering = { permutations[playerMapping, 0], permutations[playerMapping, 1], permutations[playerMapping, 2]};
        uint[] ordered = (numPlayers == 2 ? 
            new uint[] { 
                unordered[ordering[0]],
                unordered[ordering[1]]
            } :
            new uint[] {
                unordered[ordering[0]],
                unordered[ordering[1]], 
                unordered[ordering[2]]
            });
        return ordered;
    }

    public string[] GetPlayerNamesInGameOrder()
    {
        string[] unordered = (numPlayers == 2 ? new string[] { playerOneName, playerTwoName } :
          new string[] { playerOneName, playerTwoName, playerThreeName });
        int[] ordering = { permutations[playerMapping, 0], permutations[playerMapping, 1], permutations[playerMapping, 2] };
        string[] ordered = (numPlayers == 2 ?
            new string[] {
                unordered[ordering[0]],
                unordered[ordering[1]]
            } :
            new string[] {
                unordered[ordering[0]],
                unordered[ordering[1]],
                unordered[ordering[2]]
            });
        return ordered;
    }

    public int indexOf(string playerName)
    {
        return (playerOneName == playerName ? 0 :
            (playerTwoName == playerName ? 1 :
            (playerThreeName == playerName ? 2 : -1)));
    }

    private static uint lastGameId = 0;
    void Start()
    {
        lastGameId += 1;
        gameId = lastGameId;

        GameObject GameList = GameObject.FindGameObjectWithTag("GameParent");
        gameObject.transform.SetParent(GameList.transform, false);
        GameObject lobbyControllerGameObject = GameObject.FindGameObjectWithTag("LobbyController");

        OnInternalStateUpdated();
    }

    public void markReadyToPlay()
    {
        localPlayer = null;
        isReadyToPlay = true;
    }

    public bool HasInitialSetup()
    {
        bool hasData = 
            (playerOne != NO_PLAYER) &&
            (playerOneName != UNKNOWN_NAME) &&
            (numPlayers != 0) &&
            (gameNumber != -1) &&
            (diff1 != DIFF.NOT_SET) &&
            (diff2 != DIFF.NOT_SET) &&
            (connectionkey != "") &&
            (playerMapping != -1);
        return hasData;
    }

    public bool IsForMeAndReadyToPlay()
    {
        bool ready =
            HasInitialSetup() &&
            isReadyToPlay &&
            (playerTwo != NO_PLAYER) &&
            (playerTwoName != UNKNOWN_NAME);
        bool forMe = (localPlayer != null) &&
         ((playerOne == localPlayer.Id) || (playerTwo == localPlayer.Id));
        if (numPlayers > 2)
        {
            ready = ready &&
                (playerThree != NO_PLAYER) &&
                (playerThreeName != UNKNOWN_NAME);
            forMe = forMe ||
                ((localPlayer != null) && (playerThree == localPlayer.Id));
        }
        return ready && forMe;
    }

    public void StartGame()
    {
        localPlayer.CmdSignalStartingGame(this.gameId);
    }

    /**
     * Marks a player as ready to play.  Returns true if all players are now
     * ready to play. 
     */
    public bool markReadyToPlay(LobbyPlayer player)
    {
        uint playerId = player.Id;
        if ((playerId == playerOne) || (playerId == playerTwo) || 
            ((playerThree != NO_PLAYER) && (playerId == playerThree))) {
            ++numPlayersReady;
        }
        return numPlayersReady >= numPlayers;
    }

    private void OnInternalStateUpdated()
    {
        if (HasInitialSetup())
        {
            if (IsForMeAndReadyToPlay() && !HasBeenDestroyed)
            {
                StartGame();
            }
        }
    }

    public void RefreshGraphic(bool localPlayerInAGame)
    {
        Debug.Log("Refreshing game graphics");
        // Don't even try to update display the game object until 
        // the game has its initial data
        if (HasInitialSetup())
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
            string gameTitle = (gameNumber < 3 ? "Game " + (gameNumber + 1) :
                (gameNumber < 6 ? "Coop " + (gameNumber - 2) :
                (gameNumber == 6 ? "Coop X" : "Gauntlet")));
            text.text = gameTitle + "\n  " + numPlayers + " players";
            if (diff1 == DIFF.A)
            {
                text.text += "\n  Fast dragons";
            }
            if (diff2 == DIFF.A)
            {
                text.text += "\n  Run from sword";
            }
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
                if (localPlayerInAGame)
                {
                    actionButton.gameObject.SetActive(false);
                }
                else
                {
                    actionButton.gameObject.SetActive(true);
                    actionButton.GetComponentInChildren<Text>().text = "Join";
                }
            }
        }
    }

    /**
     * Adds a player to a game.
     * Returns whether this join filled the game.  Will returns
     * false if the game still needs players or if the game was
     * already full and this player did not succesfully join.
     */    
    public bool Join(uint player, string playerName)
    {
        bool gameFull = false;
        if (playerTwo == NO_PLAYER)
        {
            playerTwo = player;
            playerTwoName = playerName;
            gameFull = (numPlayers == 2);
        }
        else if ((numPlayers > 2) && (playerThree == NO_PLAYER))
        {
            playerThree = player;
            playerThreeName = playerName;
            gameFull = true;
        }
        return gameFull;
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
        OnInternalStateUpdated();
    }

    void OnChangePlayerOneName(string newPlayerOneName)
    {
        playerOneName = newPlayerOneName;
        OnInternalStateUpdated();
    }

    void OnChangePlayerTwo(uint newPlayerTwo)
    {
        playerTwo = newPlayerTwo;
        OnInternalStateUpdated();
    }

    void OnChangePlayerTwoName(string newPlayerTwoName)
    {
        playerTwoName = newPlayerTwoName;
        OnInternalStateUpdated();
    }

    void OnChangePlayerThree(uint newPlayerThree)
    {
        playerThree = newPlayerThree;
        OnInternalStateUpdated();
    }

    void OnChangePlayerThreeName(string newPlayerThreeName)
    {
        playerThreeName = newPlayerThreeName;
        OnInternalStateUpdated();
    }

    void OnChangeNumPlayers(int newNumPlayers)
    {
        numPlayers = newNumPlayers;
        OnInternalStateUpdated();
    }

    void OnChangeGameNumber(int newGameNumber)
    {
        gameNumber = newGameNumber;
        OnInternalStateUpdated();
    }

    void OnChangeDiff1(DIFF newDiff1)
    {
        diff1 = newDiff1;
        OnInternalStateUpdated();
    }

    void OnChangeDiff2(DIFF newDiff2)
    {
        diff2 = newDiff2;
        OnInternalStateUpdated();
    }

    void OnChangeConnectionKey(string newConnectionKey)
    {
        connectionkey = newConnectionKey;
        OnInternalStateUpdated();
    }

    void OnChangePlayerMapping(int newPlayerMapping)
    {
        playerMapping = newPlayerMapping;
        OnInternalStateUpdated();
    }

    void OnChangeIsReadyToPlay(bool newIsReady)
    {
        isReadyToPlay = newIsReady;
        OnInternalStateUpdated();
    }

    public override string ToString()
    {
        return "Game #" + gameNumber + 
            (diff1 == DIFF.A ? 
                (diff2 == DIFF.A ? " (FD, RFS), " : " (FD), ") :
                (diff2 == DIFF.A ? " (RFS), " : ", ")) +
            numPlayers + " players: " + playerOneName + "(" + playerOne + ") and " +
             playerTwoName + "(" + playerTwo + ")" +
            (numPlayers == 2 ? "" : " and " + playerThreeName + "(" + playerThree + ")");
    }

}
