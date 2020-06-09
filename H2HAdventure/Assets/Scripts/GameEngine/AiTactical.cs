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
        nextVelX = (nextStepX > thisBall.midX ? BALL_MOVEMENT : (nextStepX == thisBall.midX ? 0 : -BALL_MOVEMENT));
        nextVelY = (nextStepY > thisBall.midY ? BALL_MOVEMENT : (nextStepY == thisBall.midY ? 0 :-BALL_MOVEMENT));
        if ((nextVelX != thisBall.velx) || (nextVelY != thisBall.vely))
        {
            UnityEngine.Debug.Log("Changing (" + thisBall.velx + "," + thisBall.vely +
                ") to (" + nextVelX + ", " + nextVelY +
                ") at " + thisBall.room + "-(" + thisBall.midX + "," + thisBall.midY + ")");
        }
        return true;
    }

    /**
     * To prevent a "drunken walk" pick a target point that the ball will hit and
     * not overshoot with its 6 movement.
     */
    public void smoothMovement(ref int nextStepX, ref int nextStepY, int direction)
    {
        switch (direction)
        {
            case Plot.UP:
            case Plot.DOWN:
                int diff = (nextStepX - thisBall.midX) % BALL_MOVEMENT;
                if (diff > BALL_MOVEMENT / 2)
                {
                    diff -= BALL_MOVEMENT;
                }
                else if (diff <= -BALL_MOVEMENT / 2)
                {
                    diff += BALL_MOVEMENT;
                }
                nextStepX -= diff;
                return;
            case Plot.LEFT:
            case Plot.RIGHT:
            default:
                diff = (nextStepY - thisBall.midY) % BALL_MOVEMENT;
                if (diff > BALL_MOVEMENT / 2)
                {
                    diff -= BALL_MOVEMENT;
                }
                else if (diff <= -BALL_MOVEMENT / 2)
                {
                    diff += BALL_MOVEMENT;
                }
                nextStepY -= diff;
                return;
        }


    }
}
