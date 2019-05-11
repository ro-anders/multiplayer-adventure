using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UserInfo
{
    public bool userInfoSet = false;
    public bool needsPopupHelp = false;
    public bool needsMazeGuides = false;
}

public class SessionInfo {

    // If true, show some other options only for developers
    public const bool DEV_MODE = false;

    // If doing development while offline, set this.  Will stub out
    // network calls.
    public const bool WORK_OFFLINE = false;

    // The version number.  Every time there is a breaking change, this
    // needs to be updated.  Then the server can determine if the client
    // is talking to needs to be updated.
    public const int VERSION = 3;

    // Which scene to take users to once game is established.
    public const string GAME_SCENE = "AdvGame";
    //public const string GAME_SCENE = "FauxGame";

    // Name of player name preference
    public const string PLAYER_NAME_PREF = "PlayerName";

    public enum Network
    {
        MATCHMAKER,
        DIRECT_CONNECT,
        ALL_LOCAL,
        NONE
    }

    // Depending on why we're entering the lobby, we display a 
    // different message.
    public enum LobbyCause
    {
        FIRSTTIME,
        ONHOSTDROP,
        NORMAL
    }
    public const string DIRECT_CONNECT_HOST_FLAG = "host";

    private static uint thisPlayerId;
    private static string thisPlayerName;
    private static GameInLobby gameToPlay;
    private static Network networkSetup;
    private static string directConnectIp;
    private static UserInfo thisPlayerInfo = new UserInfo();

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

    public static UserInfo ThisPlayerInfo
    {
        get
        {
            return thisPlayerInfo;
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

    private static LobbyCause lobbyCause = LobbyCause.FIRSTTIME;
    public static LobbyCause LobbyEntrance
    {
        get
        {
            return lobbyCause;
        }
        set
        {
            lobbyCause = value;
        }
    }

    private static bool raceCompleted = false;
    public static bool RaceCompleted
    {
        get
        {
            return raceCompleted;
        }
        set
        {
            raceCompleted = value;
        }
    }

}
