
using System;

namespace GameEngine
{
    public class AiPlayer
    {
        private Board gameBoard;
        private AINav aiNav;
        private AiTactical aiTactical;
        private int thisPlayer;
        private BALL thisBall;

        // An AI Player has two objectives.  The overall objective,
        // which is actually a whole tree of objectives to get them
        // to win, and the current objective which is the next one in
        // the plan that has to be completed
        private AiObjective winGameObjective;
        private AiObjective currentObjective;

        private AiPathNode desiredPath = null;
        private int nextStepX = int.MinValue;
        private int nextStepY = int.MinValue;

        public AiPlayer(AINav inAi, Board inBoard, int inPlayerSlot)
        {
            gameBoard = inBoard;
            aiNav = inAi;
            thisPlayer = inPlayerSlot;
            thisBall = gameBoard.getPlayer(thisPlayer);
            aiTactical = new AiTactical(thisBall);
            winGameObjective = new WinGameObjective(gameBoard, inPlayerSlot);
        }

        /**
         * This checks the AI player's objectives and plan to win
         * It may recompute the whole strategy or, for efficiency,
         * just do a quick check or even not check but every so often.
         */
        public void checkStrategy()
        {
            // Right now we compute strategy once and never touch it again
            if (currentObjective == null)
            {
                winGameObjective.computeStrategy();
                currentObjective = winGameObjective.getNextObjective();
            }
            if (currentObjective.isCompleted())
            {
                currentObjective = winGameObjective.getNextObjective();
                desiredPath = null;
            }
        }

        /**
         * Called once per 3 clicks, this determines determines which
         * direction the AI player should be going and sets the ball's velocity
         * accordingly
         */
        public void chooseDirection()
        {
            checkStrategy();

            int desiredRoom = thisBall.room;
            int desiredX = thisBall.x;
            int desiredY = thisBall.y;
            currentObjective.getDestination(ref desiredRoom, ref desiredX, ref desiredY);


            if (desiredRoom < 0)
            {
                // We have no goal.  Don't do anything.
                return;

            }

            if (desiredPath == null)
            {
                // We don't even know where we are going.  Figure it out.
                UnityEngine.Debug.Log("Get player " + thisPlayer + " from " +
                    thisBall.room + "-(" + (thisBall.midX) + "," + (thisBall.midY) + ") to " +
                    desiredRoom + "-(" + desiredX + "," + desiredY + ")");
                desiredPath = aiNav.ComputePath(thisBall.room, thisBall.midX, thisBall.midY, desiredRoom, desiredX, desiredY);
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

            AiPathNode nextPath = aiNav.checkPathProgress(desiredPath, thisBall.room, thisBall.midX, thisBall.midY);
            if (nextPath == null)
            {
                // ABORT PATH
                UnityEngine.Debug.LogError("Ball has fallen off the AI path! Aborting.");
                desiredRoom = -1;
                thisBall.velx = 0;
                thisBall.vely = 0;
                return;
            }

            // Go to the nextStep coordinates to get us to the next step on the path
            // or the desired coordinates.
            if (nextPath != desiredPath)
            {
                desiredPath = nextPath;
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

            int nextVelx = 0;
            int nextVely = 0;
            bool canGetThere = aiTactical.computeDirection(nextStepX, nextStepY, ref nextVelx, ref nextVely);
            if (canGetThere)
            {
                thisBall.velx = nextVelx;
                thisBall.vely = nextVely;
            } else
            {
                UnityEngine.Debug.LogError("Ball cannot get where it needs to go.");
                desiredRoom = -1;
                thisBall.velx = 0;
                thisBall.vely = 0;
                return;
            }
        }
    }

}
