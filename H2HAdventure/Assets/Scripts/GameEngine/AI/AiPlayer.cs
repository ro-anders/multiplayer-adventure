
using System;

namespace GameEngine.Ai
{
    public class AiPlayer
    {
        private const int FRAMES_BETWEEN_STRATEGY_RECOMPUTE = 10 * 60; // Every 10 seconds
        private const int FRAMES_TO_REACT_TO_INVALIDATION = 30;  // Half a second
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
        private RRect desiredLocation = RRect.NOWHERE;
        /** The path we intend on taking to get to the desired location */
        private AiPathNode desiredPath = null;

        public AiPlayer(AiNav inAi, Board inBoard, int inPlayerSlot)
        {
            gameBoard = inBoard;
            aiNav = inAi;
            thisPlayer = inPlayerSlot;
            thisBall = gameBoard.getPlayer(thisPlayer);
            thisBall.ai = this;
            aiTactical = AiTactical.get(thisBall, gameBoard);
            aiStrategy = new AiStrategy(gameBoard, thisPlayer, aiNav);
        }

        public AiObjective CurrentObjective
        {
            get { return currentObjective; }
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
            if ((DEBUG.TRACE_PLAYER == thisPlayer) && (winGameObjective != null))
            {
                UnityEngine.Debug.Log("Player " + thisPlayer + " objective = " + winGameObjective.toFullString());
            }
            if ((currentObjective == null) || (frameNumber >= recomputeStrategyAtFrame))
            {
                UnityEngine.Debug.Log(thisBall + " recomputing whole strategy");
                winGameObjective = new PlayGame(gameBoard, thisPlayer, aiStrategy, aiNav);
                newObjective = winGameObjective.getNextObjective();
                if ((DEBUG.TRACE_PLAYER < 0) || (DEBUG.TRACE_PLAYER == thisPlayer))
                {
                    UnityEngine.Debug.Log("Player " + thisPlayer + " objective = " + winGameObjective.toFullString());
                }
                recomputeStrategyAtFrame = frameNumber + FRAMES_BETWEEN_STRATEGY_RECOMPUTE;
            }
            else
            {
                try
                {
                    // Check to see if we've accomplished anything
                    newObjective = winGameObjective.getNextObjective();
                } catch (Abort)
                {
                    // Things have changed.  Just recompute the whole strategy
                    winGameObjective = new PlayGame(gameBoard, thisPlayer, aiStrategy, aiNav);
                    newObjective = winGameObjective.getNextObjective();
                    recomputeStrategyAtFrame = frameNumber + FRAMES_BETWEEN_STRATEGY_RECOMPUTE;
                    UnityEngine.Debug.Log("New objective = " + newObjective);
                }
            }
            if (newObjective != currentObjective)
            {
                currentObjective = newObjective;
                desiredPath = null;
            }

            // Check to make sure that whatever action we're doing is still valid
            if (!currentObjective.isStillValid())
            {
                int reactFrame = frameNumber + FRAMES_TO_REACT_TO_INVALIDATION;
                if (recomputeStrategyAtFrame > reactFrame)
                {
                    recomputeStrategyAtFrame = reactFrame;
                }
            }
        }

        /**
         * Called once per 3 clicks, this determines which
         * direction the AI player should be going and sets the ball's velocity
         * accordingly
         */
        public void chooseDirection(int frameNumber)
        {
            checkStrategy(frameNumber);

            bool moveBlindly = currentObjective.shouldMoveDirection(ref thisBall.velx, ref thisBall.vely);

            if (moveBlindly)
            {
                return;
            }


            RRect newDesiredLocation = currentObjective.getBDestination();


            if (!newDesiredLocation.IsSomewhere)
            {
                // We have no goal.  Don't do anything.
                doNothing();
                return;
            }

            // We need to recompute the path if where we
            // are going has changed and is no longer at the end of the path.
            if (!newDesiredLocation.equals(desiredLocation)  &&
                (desiredPath != null) &&
                !desiredPath.leadsTo(newDesiredLocation.room, newDesiredLocation.midX, newDesiredLocation.midY)) {

                desiredPath = null;
            }
            desiredLocation = newDesiredLocation;

            if (desiredPath == null)
            {
                // We don't even know where we are going.  Figure it out.

                desiredPath = aiNav.ComputePath(thisBall.room, thisBall.midX, thisBall.midY,
                    desiredLocation.room, desiredLocation.midX, desiredLocation.midY);
                if (desiredPath == null)
                {
                    // No way to get to where we want to go.  Give up
                    UnityEngine.Debug.Log("Couldn't compute path for AI player #" + thisPlayer + " for objective \"" + currentObjective +
                        "\" to get to " + gameBoard.map.roomDefs[desiredLocation.room].label + "("+desiredLocation.midX+","+desiredLocation.midY+")");
                    // ABORT PATH
                    doNothing();
                    return;
                }
            }

            desiredPath = aiNav.checkPathProgress(desiredPath, thisBall.room, thisBall.midX, thisBall.midY);
            if (desiredPath == null)
            {
                // ABORT PATH
                UnityEngine.Debug.LogError("Ball " + thisBall.playerNum + " has fallen off the AI path! Aborting.");
                doNothing();
                return;
            }

            int nextVelx = 0;
            int nextVely = 0;
            bool canGetThere = aiTactical.computeDirectionOnPath(desiredPath, desiredLocation,
                currentObjective.getDesiredObject(), ref nextVelx, ref nextVely);
            if (canGetThere)
            {
                thisBall.velx = nextVelx;
                thisBall.vely = nextVely;
            }
            else
            {
                UnityEngine.Debug.LogError("Ball cannot get where it needs to go.");
                doNothing();
                return;
            }
            //if (DEBUG.TRACE_PLAYER == thisPlayer)
            //{
            //    UnityEngine.Debug.Log("Player " + thisPlayer + " @" + thisBall.room + "(" + thisBall.x + "," + thisBall.y + ")" +
            //        " trying to " + currentObjective + " going (" + nextVelx + "," + nextVely + ") to get to " + desiredLocation);
            //}
        }

        /**
         * Sometimes we have to wait.  This sets up the ball to wait, but
         * does make sure that we don't get eaten by a dragon.
         */
        private void doNothing()
        {
            desiredLocation = RRect.NOWHERE;
            thisBall.velx = 0;
            thisBall.vely = 0;
            if (DEBUG.TRACE_PLAYER == thisPlayer)
            {
                UnityEngine.Debug.Log("Player " + thisPlayer + " @" + thisBall.room + "(" + thisBall.x + "," + thisBall.y + ")" +
                    " trying to " + currentObjective + " doing nothing");
            }
            aiTactical.avoidBeingEaten(ref thisBall.velx, ref thisBall.vely, thisBall.x, thisBall.y);
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
