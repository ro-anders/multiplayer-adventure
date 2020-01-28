
using System;

namespace GameEngine
{
    public class AiPlayer
    {
        private const int BALL_MOVEMENT = 6;

        private Board gameBoard;
        private AI ai;
        private int thisPlayer;
        private BALL thisBall;

        private int desiredRoom = Map.BLACK_CASTLE;
        private int desiredX = 160;  //Portcullis.EXIT_X;
        private int desiredY = 48;

        private AiPathNode desiredPath = null;
        private int nextStepX = int.MinValue;
        private int nextStepY = int.MinValue;

        public AiPlayer(AI inAi, Board inBoard, int inPlayerSlot)
        {
            gameBoard = inBoard;
            ai = inAi;
            thisPlayer = inPlayerSlot;
            thisBall = gameBoard.getPlayer(thisPlayer);
        }

        public void chooseDirection()
        {
            AiPathNode startingPath = desiredPath;
            if (desiredRoom < 0)
            {
                // We have no goal.  Don't do anything.
                return;

            }

            if (desiredPath == null)
            {
                // We don't even know where we are going.  Figure it out.
                UnityEngine.Debug.Log("Get player " + thisPlayer + " from " +
                    thisBall.room + "-(" + thisBall.x + "," + thisBall.y + ") to " +
                    desiredRoom + "-(" + desiredX + "," + desiredY + ")");
                desiredPath = ai.ComputePath(thisBall.room, thisBall.x, thisBall.y, desiredRoom, desiredX, desiredY);
                if (desiredPath == null)
                {
                    // No way to get to where we want to go.  Give up
                    UnityEngine.Debug.Log("Couldn't compute path for AI player " + thisPlayer);
                    // ABORT PATH
                    desiredRoom = -1;
                    thisBall.velx = 0;
                    thisBall.vely = 0;
                    return;
                }
                desiredPath.ThisPlot.GetOverlap(desiredPath.nextNode.ThisPlot,
                    desiredPath.nextDirection, ref nextStepX, ref nextStepY);
            }

            // Make sure we're still on the path
            if (!desiredPath.ThisPlot.Contains(thisBall.room, thisBall.x, thisBall.y)) {
                // Most probable cause is we've gotten to the next step in the path
                if ((desiredPath.nextNode != null) &&
                    (desiredPath.nextNode.ThisPlot.Contains(thisBall.room, thisBall.x, thisBall.y)))
                {
                    desiredPath = desiredPath.nextNode;
                }
                // Next most probable cause is we've missed the path by just a little.
                else if (desiredPath.ThisPlot.RoughlyContains(thisBall.room, thisBall.x, thisBall.y))
                {
                    // We're ok.  Don't need to do anything.
                }
                else
                {
                    // We're off the path.  See if, by any chance, we are now somewhere further on
                    // the path
                    AiPathNode found = null;
                    for (AiPathNode newNode = desiredPath.nextNode;
                        (newNode != null) && (found != null);
                        newNode = newNode.nextNode)
                    {
                        if (newNode.ThisPlot.Contains(thisBall.room, thisBall.x, thisBall.y)) {
                            found = newNode;
                        }
                    }
                    if (found != null) {
                        desiredPath = found;
                    }
                    else
                    {
                        UnityEngine.Debug.LogError(thisBall.room + "(" + thisBall.x + "," +
                            thisBall.y + ")" + " has fallen off the AI path!\nNot in " + 
                            desiredPath.ThisPlot + 
                            (desiredPath.nextNode == null ? "" : " or " + desiredPath.nextNode.ThisPlot));
                        // ABORT PATH
                        desiredRoom = -1;
                        thisBall.velx = 0;
                        thisBall.vely = 0;
                        return;
                    }
                }
            }

            // Go to the nextStep coordinates to get us to the next step on the path
            // or the desired coordinates.
            if (desiredPath != startingPath)
            {
                // Recompute how to get to the next step in the path
                if (desiredPath.nextNode != null)
                {
                    desiredPath.ThisPlot.GetOverlap(desiredPath.nextNode.ThisPlot,
                        desiredPath.nextDirection, ref nextStepX, ref nextStepY);
                    UnityEngine.Debug.Log("Heading for (" + nextStepX + "," + nextStepY +
                        ") in plot " + desiredPath.ThisPlot);
                }
                else
                {
                    // We've reached the last plot in the path.  
                    // Now go to the desired coordinates
                    nextStepX = desiredX;
                    nextStepY = desiredY;
                }
            }
            int nextVelX = (nextStepX > thisBall.x ? BALL_MOVEMENT : -BALL_MOVEMENT);
            int diffX = Math.Abs(thisBall.x - nextStepX);
            int nextVelY = (nextStepY > thisBall.y ? BALL_MOVEMENT : -BALL_MOVEMENT);
            int diffY = Math.Abs(thisBall.y - nextStepY);
            if ((diffX < BALL_MOVEMENT / 2) && (diffY > BALL_MOVEMENT / 2))
            {
                nextVelX = 0;
            }
            else if ((diffY < BALL_MOVEMENT/2) && (diffX > BALL_MOVEMENT/2))
            {
                nextVelY = 0;
            }
            if ((nextVelX != thisBall.velx) || (nextVelY != thisBall.vely))
            {
                UnityEngine.Debug.Log("Changing (" + thisBall.velx + "," + thisBall.vely +
                    ") to (" + nextVelX + ", " + nextVelY +
                    ") at " + thisBall.room + "-(" + thisBall.x + "," + thisBall.y + ")");
                thisBall.velx = nextVelX;
                thisBall.vely = nextVelY;
            }
        }
    }

}
