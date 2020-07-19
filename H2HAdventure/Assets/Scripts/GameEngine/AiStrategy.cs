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
     * If an object is behind a locked gate, returns that gate.  Otherwise
     * returns -1.
     */
    public int behindLockedGate(OBJECT obj)
    {
        int gate = -1;
        for(int ctr=Board.OBJECT_YELLOW_PORT; (gate < 0) && (ctr<=Board.OBJECT_CRYSTAL_PORT); ++ctr)
        {
            Portcullis nextPort = (Portcullis)board.getObject(ctr);
            if ((nextPort != null) && (!nextPort.allowsEntry) && (nextPort.containsRoom(obj.room))) {
                gate = ctr;
            }

        }
        return gate;
    }

}
