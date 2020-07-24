
using System;

namespace GameEngine
{
    public class AiPlayer
    {
        private const int FRAMES_BETWEEN_STRATEGY_RECOMPUTE = 5 * 60; // Every 5 seconds
        private Board gameBoard;
        private AiNav aiNav;
        private AiTactical aiTactical;
        private AiStrategy aiStrategy;
        private int thisPlayer;
        private BALL thisBall;

        // An AI Player has two objectives.  The overall objective,
        // which is actually a whole tree of objectives to get them
        // to win, and the current objective which is the next one in
        // the plan that has to be completed
        private AiObjective winGameObjective = null;
        private AiObjective currentObjective = null;

        /** When the winning strategy should next be recomputed */
        private int recomputeStrategyAtFrame;

        /** The room & coordinates of where we want to get to */
        private int desiredRoom = -1;
        private int desiredX = -1;
        private int desiredY = -1;
        /** The path we intend on taking to get to the desired location */
        private AiPathNode desiredPath = null;

        public AiPlayer(AiNav inAi, Board inBoard, int inPlayerSlot)
        {
            gameBoard = inBoard;
            aiNav = inAi;
            thisPlayer = inPlayerSlot;
            thisBall = gameBoard.getPlayer(thisPlayer);
            aiTactical = new AiTactical(thisBall, gameBoard);
            aiStrategy = new AiStrategy(gameBoard, inPlayerSlot);
        }

        public void resetPlayer()
        {
            winGameObjective = null;
            currentObjective = null;
            desiredPath = null;
        }

        /**
         * This checks the AI player's objectives and plan to win
         * It may recompute the whole strategy or, for efficiency,
         * just do a quick check or even not check but every so often.
         */
        public void checkStrategy(int frameNumber)
        {
            AiObjective newObjective = null;
            // Compute a new strategy every few seconds
            if ((currentObjective == null) || (frameNumber >= recomputeStrategyAtFrame))
            {
                winGameObjective = new WinGameObjective(gameBoard, thisPlayer, aiStrategy);
                newObjective = winGameObjective.getNextObjective();
                recomputeStrategyAtFrame = frameNumber + FRAMES_BETWEEN_STRATEGY_RECOMPUTE;
            }
            else
            {
                try
                {
                    // Check to see if we've accomplished anything
                    newObjective = winGameObjective.getNextObjective();
                    if (newObjective != currentObjective)
                    {
                        UnityEngine.Debug.Log("New player " + thisBall.playerNum + " objective = " + newObjective);
                    }
                } catch (AiObjective.Abort)
                {
                    // Things have changed.  Just recompute the whole strategy
                    winGameObjective = new WinGameObjective(gameBoard, thisPlayer, aiStrategy);
                    newObjective = winGameObjective.getNextObjective();
                    recomputeStrategyAtFrame = frameNumber + FRAMES_BETWEEN_STRATEGY_RECOMPUTE;
                    UnityEngine.Debug.Log("New objective = " + newObjective);
                }
            }
            if ((newObjective != null) && (newObjective != currentObjective))
            {
                currentObjective = newObjective;
                desiredPath = null;
            }
        }

        /**
         * Called once per 3 clicks, this determines determines which
         * direction the AI player should be going and sets the ball's velocity
         * accordingly
         */
        public void chooseDirection(int frameNumber)
        {
            checkStrategy(frameNumber);

            int newDesiredRoom = thisBall.room;
            int newDesiredX = thisBall.midX;
            int newDesiredY = thisBall.midY;
            currentObjective.getDestination(ref newDesiredRoom, ref newDesiredX, ref newDesiredY);


            if (newDesiredRoom < 0)
            {
                // We have no goal.  Don't do anything.
                return;

            }

            // We need to recompute the path if where we
            // are going has changed and is no longer at the end of the path.
            if (((newDesiredRoom != desiredRoom) || (newDesiredX != desiredX) || (newDesiredY != desiredY)) &&
                (desiredPath != null) &&
                !desiredPath.leadsTo(newDesiredRoom, newDesiredX, newDesiredY)) {

                desiredPath = null;
            }
            desiredRoom = newDesiredRoom;
            desiredX = newDesiredX;
            desiredY = newDesiredY;

            if (desiredPath == null)
            {
                // We don't even know where we are going.  Figure it out.

                //UnityEngine.Debug.Log("Get player " + thisPlayer + " from " +
                //    thisBall.room + "-(" + (thisBall.midX) + "," + (thisBall.midY) + ") to " +
                //    desiredRoom + "-(" + desiredX + "," + desiredY + ")");
                desiredPath = aiNav.ComputePath(thisBall.room, thisBall.midX, thisBall.midY, desiredRoom, desiredX, desiredY);
                if (desiredPath == null)
                {
                    // No way to get to where we want to go.  Give up
                    UnityEngine.Debug.Log("Couldn't compute path for AI player #" + thisPlayer + " for objective \"" + currentObjective +
                        "\" to get to ("+desiredX+","+desiredY+")@"+desiredRoom);
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
                UnityEngine.Debug.LogError("Ball " + thisBall.playerNum + " has fallen off the AI path! Aborting.");
                desiredRoom = -1;
                thisBall.velx = 0;
                thisBall.vely = 0;
                return;
            }

            int nextVelx = 0;
            int nextVely = 0;
            bool canGetThere = aiTactical.computeDirectionOnPath(desiredPath, desiredX, desiredY,
                currentObjective, ref nextVelx, ref nextVely);
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
            // We don't recheck the strategy.  We assume that is done
            // at least once every three frames in the chooseDirection() method.
            return currentObjective.shouldDropHeldObject();
        }

        /**
         * Called during the checking reset switch, this will determine if the AI player
         * wants to reset.  This call clears the "wants to reset" flag. so only
         * call when you intend to issue a reset.
         */
        public bool shouldReset()
        {
            // We don't recheck the strategy.
            return (currentObjective == null ? false : currentObjective.collectShouldReset());
        }
    }

}
