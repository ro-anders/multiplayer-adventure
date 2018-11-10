using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class Game : NetworkBehaviour {

    public Text text;

    [SyncVar(hook = "OnChangePlayerOne")]
    public string playerOne;

    [SyncVar(hook = "OnChangePlayerTwo")]
    public string playerTwo;

    [SyncVar(hook = "OnChangePlayerThree")]
    public string playerThree;

    [SyncVar(hook = "OnChangeNumPlayers")]
    public int numPlayers;

    [SyncVar(hook = "OnChangeGameNumber")]
    public int gameNumber;

    void Start()
    {
        GameObject GameList = GameObject.FindGameObjectWithTag("GameParent");
        gameObject.transform.SetParent(GameList.transform);
        RefreshGraphic();
    }

    private void RefreshGraphic()
    {
        string playerList = playerOne;
        if (playerTwo != "")
        {
            playerList += (playerThree != "" ? ", " : " and ");
            playerList += playerTwo;
            if (playerThree != "")
            {
                playerList += " and " + playerThree;
            }

        }
        text.text = numPlayers + " player game #" + (gameNumber + 1) + " with " + playerList;
    }

    void OnChangePlayerOne(string newPlayerOne)
    {
        playerOne = newPlayerOne;
        RefreshGraphic();
    }

    void OnChangePlayerTwo(string newPlayerTwo)
    {
        playerTwo = newPlayerTwo;
        RefreshGraphic();
    }

    void OnChangePlayerThree(string newPlayerThree)
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
