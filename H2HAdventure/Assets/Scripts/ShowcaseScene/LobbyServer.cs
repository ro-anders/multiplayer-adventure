using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LobbyServer
{
    private ShowcaseTransport xport;
    private ProposedGame proposedGame;
    private int playersReady;
    private static readonly int[,] permutations = new int[,] {
        {0, 1, 2},
        {1, 0, 2},
        {1, 2, 0},
        {0, 2, 1},
        {2, 0, 1},
        {2, 1, 0}
    };


    public LobbyServer(ShowcaseTransport inXport)
    {
        xport = inXport;
    }

    public void HandleProposeGame(ProposedGame newGame)
    {
        if (isProposingSameGame(newGame)) 
        {
            int playerToAdd = newGame.players[0];
            if (!proposedGame.ContainsPlayer(playerToAdd))
            {
                List<int> playerList = new List<int>(proposedGame.players);
                playerList.Add(playerToAdd);
                proposedGame.players = playerList.ToArray();
                xport.BcstNewProposedGame(proposedGame);
            }
        }
        else
        {
            proposedGame = newGame;
            xport.BcstNewProposedGame(proposedGame);
        }
        playersReady = 0;
    }

    public void HandleAcceptGame(int acceptingPlayerId)
    {
        if (!proposedGame.ContainsPlayer(acceptingPlayerId))
        {
            List<int> playerList = new List<int>(proposedGame.players);
            playerList.Add(acceptingPlayerId);
            proposedGame.players = playerList.ToArray();
            xport.BcstNewProposedGame(proposedGame);
        }
    }

    public void HandleAbortGame(int abortingPlayerId)
    {
        if (proposedGame.ContainsPlayer(abortingPlayerId)) {
            List<int> playerList = new List<int>(proposedGame.players);
            playerList.Remove(abortingPlayerId);
            if (playerList.Count == 0)
            {
                // All players have aborted.  Cancel the proposed game.
                proposedGame = null;
                xport.BcstNoGame();
            }
            else
            {
                proposedGame.players = playerList.ToArray();
                xport.BcstNewProposedGame(proposedGame);
            }
        }
    }

    public void HandleReadyToStart(int readyPlayerId)
    {
        ++playersReady;
        int numPlayers = proposedGame.players.Length;
        if (playersReady == numPlayers)
        {
            // Reorder the players list randomly
            System.Random rand = new System.Random();
            int which = rand.Next(0, (numPlayers == 2 ? 2 : 6));
            int[] ordering = { permutations[which, 0], permutations[which, 1], permutations[which, 2] };
            int[] playersReordered = new int[numPlayers];
            for(int ctr=0; ctr<numPlayers; ++ctr){
                playersReordered[ctr] = proposedGame.players[ordering[ctr]];
            }
            proposedGame.players = playersReordered;

            xport.BcstStartGame(proposedGame);
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
