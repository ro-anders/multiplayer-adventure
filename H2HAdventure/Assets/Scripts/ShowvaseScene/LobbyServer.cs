using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyServer
{
    private ShowcaseTransport xport;
    private ProposedGame proposedGame;

    public LobbyServer(ShowcaseTransport inXport)
    {
        xport = inXport;
    }

    public void HandleProposeGame(ProposedGame newGame)
    {
        if (isProposingSameGame(newGame)) 
        {
            throw new Exception("TBD");
        }
        else
        {
            proposedGame = newGame;
            xport.BcstNewProposedGame(proposedGame);
        }
    }

    private bool isProposingSameGame(ProposedGame newGame)
    {
        return (proposedGame != null) &&
            (proposedGame.gameNumber == newGame.gameNumber) &&
            (proposedGame.numPlayers == newGame.numPlayers) &&
            (proposedGame.diff1 == newGame.diff1) &&
            (proposedGame.diff2 == newGame.diff2);
    }

}
