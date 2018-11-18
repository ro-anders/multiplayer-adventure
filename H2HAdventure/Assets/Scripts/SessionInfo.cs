using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SessionInfo {
    public enum Network
    {
        MATCHMAKER,
        ALL_LOCAL
    }

    private static uint thisPlayerId;
    private static string thisPlayerName;
    private static Game gameToPlay;
    private static Network networkSetup;


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

    public static Game GameToPlay
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

}
