using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

public class AiTactical
{
    /** The ball that is being moved */
    private BALL thisBall;

    private AiPathNode currentPath;
    private int nextStepX;
    private int nextStepY;

    public AiTactical(BALL inBall)
    {
        thisBall = inBall;
    }

    public bool computeDirectionOnPath(AiPathNode path, int finalX, int finalY,
        ref int nextVelx, ref int nextVely)
    {
        // Go to the nextStep coordinates to get us to the next step on the path
        // or the desired coordinates if we're at the end of the path.
        if (path != currentPath)
        {
            currentPath = path;

            // New path or reached new step in path.
            // Recompute how to get to the next step in the path
            if (currentPath.nextNode != null)
            {
                currentPath.ThisPlot.GetOverlap(currentPath.nextNode.ThisPlot,
                    currentPath.nextDirection, ref nextStepX, ref nextStepY);
                smoothMovement(ref nextStepX, ref nextStepY, currentPath.nextDirection);
            }
        }

        if (currentPath.nextNode == null)
        {
            // We've reached the last plot in the path.  
            // Now go to the desired coordinates
            nextStepX = finalX;
            nextStepY = finalY;
        }

        bool canGetThere = computeDirection(nextStepX, nextStepY, ref nextVelx, ref nextVely);

        return canGetThere;
    }

    /**
     * Figure out the best direction to go to get to a point in the current room.
     * Usually this just makes a straight line towards it, but will choose alternate
     * directions to deal with dragons or impeding obstacles.
     */
    private bool computeDirection(int nextStepX, int nextStepY, ref int nextVelX, ref int nextVelY)
    {
        nextVelX = (nextStepX > thisBall.midX ? BALL.MOVEMENT : (nextStepX == thisBall.midX ? 0 : -BALL.MOVEMENT));
        nextVelY = (nextStepY > thisBall.midY ? BALL.MOVEMENT : (nextStepY == thisBall.midY ? 0 :-BALL.MOVEMENT));
        if ((nextVelX != thisBall.velx) || (nextVelY != thisBall.vely))
        {
            //UnityEngine.Debug.Log("Changing (" + thisBall.velx + "," + thisBall.vely +
            //    ") to (" + nextVelX + ", " + nextVelY +
            //    ") at " + thisBall.room + "-(" + thisBall.midX + "," + thisBall.midY + ")");
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
                int diff = (nextStepX - thisBall.midX) % BALL.MOVEMENT;
                if (diff > BALL.MOVEMENT / 2)
                {
                    diff -= BALL.MOVEMENT;
                }
                else if (diff <= -BALL.MOVEMENT / 2)
                {
                    diff += BALL.MOVEMENT;
                }
                nextStepX -= diff;
                return;
            case Plot.LEFT:
            case Plot.RIGHT:
            default:
                diff = (nextStepY - thisBall.midY) % BALL.MOVEMENT;
                if (diff > BALL.MOVEMENT / 2)
                {
                    diff -= BALL.MOVEMENT;
                }
                else if (diff <= -BALL.MOVEMENT / 2)
                {
                    diff += BALL.MOVEMENT;
                }
                nextStepY -= diff;
                return;
        }


    }
}
