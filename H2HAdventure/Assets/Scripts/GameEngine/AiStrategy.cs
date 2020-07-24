using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

public class AiStrategy
{
    /** The ball for which this is developing strategy */
    private BALL thisBall;

    private Board board;

    public AiStrategy(Board inBoard, int inPlayerSlot)
    {
        board = inBoard;
        thisBall = board.getPlayer(inPlayerSlot);
    }

    /**
     * If the ball has been eaten by a dragon
     */
    public bool eatenByDragon()
    {
        bool eaten = false;
        for (int ctr = Board.FIRST_DRAGON; !eaten && (ctr <= Board.LAST_DRAGON); ++ctr)
        {
            Dragon dragon = (Dragon)board.getObject(ctr);
            eaten = (dragon.eaten == thisBall);
        }
        return eaten;
    }


    /**
     * If an object is behind a locked gate, returns that gate.  Otherwise
     * returns -1.
     */
    public int behindLockedGate(OBJECT obj)
    {
        int gate = -1;
        for (int ctr = Board.OBJECT_YELLOW_PORT; (gate < 0) && (ctr <= Board.OBJECT_CRYSTAL_PORT); ++ctr)
        {
            Portcullis nextPort = (Portcullis)board.getObject(ctr);
            if ((nextPort != null) && (!nextPort.allowsEntry) && (nextPort.containsRoom(obj.room)))
            {
                gate = ctr;
            }

        }
        return gate;
    }

    /**
     * If an object is held by another player, returns that player.  Otherwise
     * returns null.
     */
    public BALL heldByOtherPlayer(OBJECT obj)
    {
        int objPkey = obj.getPKey();
        BALL heldBy = null;
        int numPlayers = board.getNumPlayers();
        int thisPlayer = thisBall.playerNum;
        for (int ctr = 0; (heldBy == null) && (ctr < numPlayers); ++ctr)
        {
            if (ctr != thisPlayer)
            {
                BALL nextBall = board.getPlayer(ctr);
                if (nextBall.linkedObject == objPkey)
                {
                    heldBy = nextBall;
                }
            }
        }
        return heldBy;
    }

}
