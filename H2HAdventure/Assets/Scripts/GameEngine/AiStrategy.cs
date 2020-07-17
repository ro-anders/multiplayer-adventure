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



}
