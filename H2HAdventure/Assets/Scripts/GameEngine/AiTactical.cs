using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

public class AiTactical
{
    private const int BALL_MOVEMENT = 6;

    /** The ball that is being moved */
    private BALL thisBall;

    public AiTactical(BALL inBall)
    {
        thisBall = inBall;
    }

    /**
     * Figure out the best direction to go to get to a point in the current room.
     * Usually this just makes a straight line towards it, but will choose alternate
     * directions to deal with dragons or impeding obstacles.
     */
    public bool computeDirection(int nextStepX, int nextStepY, ref int nextVelX, ref int nextVelY)
    {
        nextVelX = (nextStepX > thisBall.midX ? BALL_MOVEMENT : -BALL_MOVEMENT);
        int diffX = Math.Abs(thisBall.midX - nextStepX);
        nextVelY = (nextStepY > thisBall.midY ? BALL_MOVEMENT : -BALL_MOVEMENT);
        int diffY = Math.Abs(thisBall.midY - nextStepY);
        if ((diffX < BALL_MOVEMENT / 2) && (diffY > BALL_MOVEMENT / 2))
        {
            nextVelX = 0;
        }
        else if ((diffY < BALL_MOVEMENT / 2) && (diffX > BALL_MOVEMENT / 2))
        {
            nextVelY = 0;
        }
        if ((nextVelX != thisBall.velx) || (nextVelY != thisBall.vely))
        {
            UnityEngine.Debug.Log("Changing (" + thisBall.velx + "," + thisBall.vely +
                ") to (" + nextVelX + ", " + nextVelY +
                ") at " + thisBall.room + "-(" + thisBall.midX + "," + thisBall.midY + ")");
        }
        return true;
    }
}
