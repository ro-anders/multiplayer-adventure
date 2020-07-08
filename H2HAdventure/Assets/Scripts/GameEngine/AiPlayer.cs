
using System;

namespace GameEngine
{
    public class AiPlayer
    {
        private Board gameBoard;
        private AiNav aiNav;
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

        public AiPlayer(AiNav inAi, Board inBoard, int inPlayerSlot)
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
                UnityEngine.Debug.Log("New objective = " + currentObjective);
            }
            // Check to see if we've accomplished anything
            AiObjective newObjective = winGameObjective.getNextObjective();
            if (newObjective != currentObjective)
            {
                currentObjective = newObjective;
                desiredPath = null;
                UnityEngine.Debug.Log("New objective = " + currentObjective);
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
            int desiredX = thisBall.midX;
            int desiredY = thisBall.midY;
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
            }

            desiredPath = aiNav.checkPathProgress(desiredPath, thisBall.room, thisBall.midX, thisBall.midY);
            if (desiredPath == null)
            {
                // ABORT PATH
                UnityEngine.Debug.LogError("Ball has fallen off the AI path! Aborting.");
                desiredRoom = -1;
                thisBall.velx = 0;
                thisBall.vely = 0;
                return;
            }

            int nextVelx = 0;
            int nextVely = 0;
            bool canGetThere = aiTactical.computeDirectionOnPath(desiredPath, desiredX, desiredY, ref nextVelx, ref nextVely);
            if (canGetThere)
            {
                thisBall.velx = nextVelx;
                thisBall.vely = nextVely;
            }
            else
            {
                UnityEngine.Debug.LogError("Ball cannot get where it needs to go.");
                desiredRoom = -1;
                thisBall.velx = 0;
                thisBall.vely = 0;
                return;
            }
        }

        /**
         * Called during the pickup/putdown click, this will determine if the AI player
         * wants to drop the held object, and drop it.
         */
        public bool shouldDropHeldObject()
        {
            return currentObjective.shouldDropHeldObject();
        }

    }

}
