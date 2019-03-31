using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ProposedGame {
    public int gameNumber;
    public int numPlayers;
    public int diff1;
    public int diff2;
    public int[] players;
    public bool ContainsPlayer(int desiredPlayer)
    {
        foreach(int nextPlayer in players)
        {
            if (nextPlayer == desiredPlayer)
            {
                return true;
            }
        }
        return false;
    }
}
