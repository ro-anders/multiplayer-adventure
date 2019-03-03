using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionInfo {

    // If true, show some other options only for developers
    public const bool DEV_MODE = true;

    // The version number.  Every time there is a breaking change, this
    // needs to be updated.  Then the server can determine if the client
    // is talking to needs to be updated.
    public const int VERSION = 1;

    // Which scene to take users to once game is established.
    public const string GAME_SCENE = "AdvGame";
    //public const string GAME_SCENE = "FauxGame";

    public enum Network
    {
        MATCHMAKER,
        DIRECT_CONNECT,
        ALL_LOCAL,
        NONE
    }

    public const string DIRECT_CONNECT_HOST_FLAG = "host";

    private static uint thisPlayerId;
    private static string thisPlayerName;
    private static GameInLobby gameToPlay;
    private static Network networkSetup;
    private static string directConnectIp;


    public static uint ThisPlayerId
    {
        get
        {
            return thisPlayerId;
        }
        set
        {
            thisPlayerId = value;
        }
    }


    public static string ThisPlayerName
    {
        get
        {
            return thisPlayerName;
        }
        set
        {
            thisPlayerName = value;
        }
    }

    public static GameInLobby GameToPlay
    {
        get
        {
            return gameToPlay;
        }
        set
        {
            gameToPlay = value;
        }
    }
    public static Network NetworkSetup
    {
        get
        {
            return networkSetup;
        }
        set
        {
            networkSetup = value;
        }
    }

    public static string DirectConnectIp
    {
        get
        {
            return directConnectIp;
        }
        set
        {
            directConnectIp = value;
        }
    }
}
