﻿using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;

namespace GameEngine.Ai
{
    abstract public class AiObjective
    {
        public const int CARRY_NO_OBJECT = -10; // We specifically don't want to carry or bump into anything
        public const int DONT_CARE_OBJECT = -20; // We don't care if we bump into an object or not

        /**
         * This is thrown when an objective can't be completed anymore.
         * If things change that make an objective impossible (e.g. a go to command
         * when the gate just closed) then the objective is aborted.*/
        public class Abort : Exception { }

        /** The next objective after this to accomplish parent objective */
        protected AiObjective sibling;

        /** The first in a linked list of dependent objectives that must be
         * completed before this objective is completed. */
        protected AiObjective child;

        /** the objective that has this objective as a child or in the sibling chain of its child */
        protected AiObjective parent;

        protected Board board;
        protected int aiPlayerNum;
        protected BALL aiPlayer;
        protected AiStrategy strategy;
        protected AiNav nav;

        /** Whether this objective has been successfully completed */
        protected bool completed = false;

        /** Whether this objective has computed all the steps necessary to
         * be achieved.  This is done once.  If things change after that, this
         * objective must be aborted and a new one created. */
        private bool computed = false;

        /** Useful to keep this info in the objective.  Whether or not 
         * the player should reset. */
        private bool needToReset = false;


        /**
         * Return the next objective that needs to be completed
         */
        public AiObjective getNextObjective()
        {
            // TODO: This could be sped up if we kept a ALL SIBLINGS COMPLETED flag
            AiObjective next = null;
            if (!isCompleted())
            {
                if (child != null)
                {
                    next = child.getNextObjective();
                }
                next = (next != null ? next : this);
            }
            else if (sibling != null)
            {
                next = sibling.getNextObjective();
            }
            return next;
        }

        /**
         * Following this objective, what are the next coordinates the
         * ball should go to.
         * @return the area the ball needs to get to to complete this objective
         * in ball coordinates
         */
        public virtual RRect getBDestination()
        {
            // Default behavior is don't go anywhere
            return RRect.NOWHERE;
        }

        /**
         * Following this objective, do we now need to drop an object
         */
        public virtual bool shouldDropHeldObject()
        {
            // Default is to do nothing.
            return false;
        }

        /**
         * Does this objective still make sense, e.g. if the bat picks 
         * up an object, the PickupObjective is no longer valid.
         */
        public virtual bool isStillValid()
        {
            return true;
        }

        /**
         * Following this objective, what object should we be carrying or
         * trying to pickup.
         * May be a key to an object or may be CARRY_NO_OBJECT or DONT_DESIRE_OBJECT
         */
        public virtual int getDesiredObject()
        {
            // Default is don't care
            return DONT_CARE_OBJECT;
        }

        /**
         * Set that this player should reset.  Gets unset
         * when it actually resets.
         */
        public void markShouldReset()
        {
            needToReset = true;
        }

        /**
         * This checks to see if the reset flag is marked, but 
         * actually clears the reset flag after checking.  Don't
         * call this if you don't plan to make the reset happen.
         */
        public bool collectShouldReset()
        {
            bool rtn = needToReset;
            needToReset = false;
            return rtn;
        }

        /**
         * Recursively print this objective and the current sub-objectives it's working
         * on to acheive this objective.
         */
        public string toFullString()
        {
            // If we're complete, maybe our sibling isn't and that's what we
            // should return
            if (this.isCompleted())
            {
                if (this.sibling != null)
                {
                    return this.sibling.toFullString();
                }
                else return this.ToString() + " -- DONE";
            }
            else
            {
                string fullString = this.ToString();
                if (this.child != null)
                {
                    fullString = child.toFullString() + "\n" + fullString;
                }
                return fullString;
            }
        }

        public override abstract string ToString();

        protected AiObjective()
        { }

        /**
         * Compute a set of objectives to complete this objective
         */
        protected void computeStrategy()
        {
            if (computed)
            {
                // Something went wrong
                throw new Exception("Asking to recompute an already computed strategy");
            }

            doComputeStrategy();
            computed = true;
        }

        /**
         * Compute a set of objectives to complete this objective
         */
        protected abstract void doComputeStrategy();

        /**
         * Whether an objective has been fulfilled
         */
        protected bool isCompleted()
        {
            if (!completed)
            {
                if (!computed)
                {
                    computeStrategy();
                }
                completed = computeIsCompleted();
                // Once a task is completed, it doesn't get uncompleted.
                // That would imply bigger things have changed and we should
                // recompute our strategy.
            }
            return completed;
        }

        /**
         * If this objective is achieved by completing sub-objectives
         * add this sub-objective as the next.
         */
        protected void addChild(AiObjective nextChild)
        {
            // The root of the objective tree provides the board and the player num
            nextChild.board = this.board;
            nextChild.aiPlayerNum = this.aiPlayerNum;
            nextChild.aiPlayer = this.aiPlayer;
            nextChild.strategy = this.strategy;
            nextChild.nav = this.nav;
            nextChild.parent = this;

            if (child == null)
            {
                child = nextChild;
            }
            else
            {
                child.addSibling(nextChild);
            }
        }

        /**
         * If this is a sub-objective of the current objective, remove it.
         */
        protected void removeChild(AiObjective childToRemove)
        {
            if (child == childToRemove)
            {
                child = child.sibling;
            }
            else
            {
                child.removeSibling(childToRemove);
            }
        }

        protected abstract bool computeIsCompleted();

        /**
         * Presumably this objective is a sub-objective of a larger objective.
         * Add this next objective as the sub-objective to complete after completing
         * this objective.
         */
        protected void addSibling(AiObjective nextObjective)
        {
            if (sibling == null)
            {
                sibling = nextObjective;
            }
            else
            {
                sibling.addSibling(nextObjective);
            }
        }

        /**
         * If this objective is a sub-objective of the parent objective, remove it.
         */
        protected void removeSibling(AiObjective objectiveToRemove)
        {
            if (sibling == objectiveToRemove)
            {
                sibling = sibling.sibling;
            }
            else if (sibling != null)
            {
                sibling.removeSibling(objectiveToRemove);
            }
        }

        /**
         * Access to sibling objects
         */
        protected AiObjective getSiblingOf(AiObjective otherObjective)
        {
            return otherObjective.sibling;
        }
        protected void setSiblingOf(AiObjective otherObjective, AiObjective newObjective)
        {
            otherObjective.sibling = newObjective;
        }
        protected AiObjective getParentOf(AiObjective otherObjective)
        {
            return otherObjective.parent;
        }

    }




    //-------------------------------------------------------------------------

    /**
     * Take an object currently carried by another player.
     */
    public class GetObjectFromPlayer : AiObjective
    {
        private int toSteal;
        private OBJECT objectToSteal;
        private int toStealFrom;
        private BALL ballToStealFrom;
        private static System.Random genRandom = new System.Random(0);

        /**
         * The object the AI player needs to pickup
         */
        public GetObjectFromPlayer(int inToSteal, int inToStealFrom)
        {
            toSteal = inToSteal;
            toStealFrom = inToStealFrom;
        }

        public override string ToString()
        {
            return "steal " + board.getObject(toSteal).label + " from player #" + toStealFrom;
        }

        protected override void doComputeStrategy()
        {
            objectToSteal = board.getObject(toSteal);
            ballToStealFrom = board.getPlayer(toStealFrom);
            if (ballToStealFrom.linkedObject != toSteal)
            {
                throw new Abort();
            }
        }

        /**
         * Still valid unless you see that the object is not held by the player.
         */
        public override bool isStillValid()
        {
            bool stillValid = true;
            if ((aiPlayer.room == objectToSteal.room) || (aiPlayer.room == ballToStealFrom.room))
            {
                stillValid = (ballToStealFrom.linkedObject == toSteal);
            }
            return stillValid;
        }

        public override RRect getBDestination()
        {
            // If we're really close to the object, go for the object
            bool goForObject = ((aiPlayer.room == objectToSteal.room) &&
                    (distToObject() <= 1.5 * BALL.MOVEMENT));

            // If we're really close to the other ball, go for the object
            if ((!goForObject) && (aiPlayer.room == ballToStealFrom.room))
            {
                int distanceX = Math.Abs(aiPlayer.midX - ballToStealFrom.midX) - BALL.DIAMETER;
                int distanceY = Math.Abs(aiPlayer.midY - ballToStealFrom.midY) - BALL.DIAMETER;
                int distance = (distanceX > distanceY ? distanceX : distanceY);
                goForObject = (distance <= 1.5 * BALL.MOVEMENT);
            }

            if (goForObject)
            {
                if (toSteal == Board.OBJECT_BRIDGE)
                {
                    // Bridge is tricky.  Aim for the corner for now.
                    return new RRect(objectToSteal.room, objectToSteal.bx, objectToSteal.by, 1, 1);
                }
                else
                {
                    // Aim for the center
                    RRect target = strategy.closestReachableRectangle(objectToSteal);

                    // In the case where two computers are trying to steal from each
                    // other we need to randomly break an impasse
                    if ((!target.IsValid) && ballToStealFrom.isAi)
                    {
                        AiObjective othersObjective = ballToStealFrom.ai.CurrentObjective;
                        if (othersObjective is GetObjectFromPlayer)
                        {
                            GetObjectFromPlayer othersStealObjective = (GetObjectFromPlayer)othersObjective;
                            if (othersStealObjective.toStealFrom == aiPlayer.playerNum)
                            {
                                // We're stealing from them and they're stealing from us.
                                // Add random movements.
                                int randomX = (genRandom.Next(3) - 1) * BALL.MOVEMENT;
                                int randomY = (genRandom.Next(3) - 1) * BALL.MOVEMENT;
                                target = new RRect(aiPlayer.room, aiPlayer.x + randomX, aiPlayer.y + randomY, BALL.DIAMETER, BALL.DIAMETER);
                            }
                        }
                    }
                    return target;
                }
            }
            else
            {
                return new RRect(ballToStealFrom.room, ballToStealFrom.x, ballToStealFrom.y, BALL.DIAMETER, BALL.DIAMETER);
            }
        }

        protected override bool computeIsCompleted()
        {
            return (aiPlayer.linkedObject == toSteal);
        }

        public override int getDesiredObject()
        {
            return toSteal;
        }

        /**
         * Distance to the object - only valid if in the same room as the object
         */
        private int distToObject()
        {
            int objMidBX = objectToSteal.bx + objectToSteal.bwidth / 2;
            int xdist = Math.Abs(objMidBX - aiPlayer.midX) - (objectToSteal.bwidth / 2) - (BALL.RADIUS);
            int objMidBY = objectToSteal.by - objectToSteal.BHeight / 2;
            int ydist = Math.Abs(objMidBY - aiPlayer.midY) - (objectToSteal.BHeight / 2) - (BALL.RADIUS);
            int dist = (xdist > ydist ? xdist : ydist);
            return dist;
        }
    }


    //-------------------------------------------------------------------------

    public class GoToObjective : AiObjective
    {
        private RRect btarget;
        private int carrying;
        private Portcullis behindPortcullis = null; // If the target room is behind a Portcullis

        /**
         * Go to these coordinates.
         * @param inRoom the desired room
         * @param inX the desired X
         * @param inY the desired Y
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public GoToObjective(int inRoom, int inBX, int inBY, int inCarrying = DONT_CARE_OBJECT)
        {
            btarget = new RRect(inRoom, inBX, inBY, 1, 1);
            carrying = inCarrying;
        }

        /**
         * Go to somewhere within this area.  If the area is big enough, will put
         * the ball entirely within the area.  If it is not big enough, will be
         * as much in the area as possible.  If the area is a point or smaller than
         * the ball, will attempt to get the balls midpoint as close to the center
         * of the area as possible.
         * @param inTarget desired area in ball coordinates
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public GoToObjective(RRect inBTarget, int inCarrying = DONT_CARE_OBJECT)
        {
            btarget = inBTarget;
            carrying = inCarrying;
        }

        protected override void doComputeStrategy()
        {
            behindPortcullis = GoToObjective.isBehindPortcullis(board, aiPlayer, btarget.room);
        }

        public override RRect getBDestination()
        {
            return btarget;
        }

        /**
         * Still valid as long as you are carrying the object you are supposed to
         * be carrying and you can still get to where you're supposed to go.
         */
        public override bool isStillValid()
        {
            bool stillHaveObject =
                (carrying == DONT_CARE_OBJECT) ||
                ((carrying == CARRY_NO_OBJECT) && (aiPlayer.linkedObject == Board.OBJECT_NONE)) ||
                (aiPlayer.linkedObject == carrying);
            bool blocked = (behindPortcullis != null) && (aiPlayer.room == behindPortcullis.room) && !behindPortcullis.allowsEntry;
            return stillHaveObject && !blocked;
        }

        protected override bool computeIsCompleted()
        {
            if (aiPlayer.room == btarget.room)
            {
                int xBuffer = (BALL.DIAMETER + BALL.MOVEMENT - btarget.width + 1) / 2;
                if (xBuffer < 0)
                {
                    xBuffer = 0;
                }
                else if (Math.Abs(aiPlayer.midY - btarget.midY) <= BALL.MOVEMENT / 2)
                {
                    xBuffer += 2; // 2 to increase the buffer from 3 (BALL.MOVEMENT/2) to 5 (BALL.MOVEMENT-1)
                }
                bool xcheck = (aiPlayer.x >= btarget.left - xBuffer) &&
                    (aiPlayer.x + BALL.DIAMETER <= btarget.right + xBuffer);

                int yBuffer = (BALL.DIAMETER + BALL.MOVEMENT - btarget.height + 1) / 2;
                if (yBuffer < 0)
                {
                    yBuffer = 0;
                }
                else if (Math.Abs(aiPlayer.midX - btarget.midX) <= BALL.MOVEMENT / 2)
                {
                    yBuffer += 2; // 2 to increase the buffer from 3 (BALL.MOVEMENT/2) to 5 (BALL.MOVEMENT-1)
                }
                bool ycheck = (aiPlayer.y - BALL.DIAMETER >= btarget.bottom - yBuffer) &&
                    (aiPlayer.y <= btarget.top + yBuffer);
                return xcheck && ycheck;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            string str = "go to " + btarget.ToStringWithRoom(board.map.roomDefs[btarget.room].label);
            if (carrying == CARRY_NO_OBJECT)
            {
                str += " carrying nothing";
            }
            if (carrying != DONT_CARE_OBJECT)
            {
                str += " with " + carrying;
            }
            return str;
        }

        public override int getDesiredObject()
        {
            return carrying;
        }

        /**
         * Return if a desired room is behind a portcullis.
         * If you are also behind the same portcullis then this returns that it is not
         * behind a portcullis.
         * @param board the board
         * @param ball the player
         * @param targetRoom the rooom of interest
         * @returns the portcullis that stands between the ball and the room or null
         * if none does
         */
        public static Portcullis isBehindPortcullis(Board board, BALL ball, int targetRoom)
        {
            Portcullis targetPort = null;
            Portcullis myPort = null;
            // Figure out if the desired room is behind a locked gate
            int FIRST_PORT = Board.OBJECT_YELLOW_PORT;
            int LAST_PORT = Board.OBJECT_CRYSTAL_PORT;
            for (int portNum = FIRST_PORT; portNum <= LAST_PORT; ++portNum)
            {
                Portcullis port = (Portcullis)board.getObject(portNum);
                targetPort = ((targetPort == null) && port.containsRoom(targetRoom) ? port : targetPort);
                myPort = ((myPort == null) && port.containsRoom(ball.room) ? port : myPort);
            }
            return (targetPort == myPort ? null : targetPort);
        }
    }


    //-------------------------------------------------------------------------

    /**
     * Go to these exact coordinates.  Only to be used when you've 
     * calculated that these coordinates can be reached given the balls
     * current coordinates and 6 pixel step.
     * Which means only to be used when the destination is in the same room
     * as the ball
     */
    public class GoExactlyToObjective : AiObjective
    {
        private RRect btarget;
        private int carrying;

        /**
         * Go to these coordinates.
         * @param inRoom the desired room
         * @param inX the desired X of the ball (meaning the left coordinate)
         * @param inY the desired Y of the ball (meaning the top coordinate)
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public GoExactlyToObjective(int inRoom, int inLeftBX, int inTopBY, int inCarrying = DONT_CARE_OBJECT)
        {
            btarget = new RRect(inRoom, inLeftBX, inTopBY, BALL.DIAMETER, BALL.DIAMETER);
            carrying = inCarrying;
        }

        protected override void doComputeStrategy()
        {
            // Objective is target
        }

        public override RRect getBDestination()
        {
            return btarget;
        }

        /**
         * Still valid as long as you are carrying the object you are supposed to
         * be carrying and you can still get to where you're supposed to go.
         */
        public override bool isStillValid()
        {
            return btarget.room == aiPlayer.room;
        }

        protected override bool computeIsCompleted()
        {
            return (aiPlayer.x == btarget.left) && (aiPlayer.y == btarget.top);
        }

        public override string ToString()
        {
            string str = "go to exact spot " + btarget.ToStringWithRoom(board.map.roomDefs[btarget.room].label);
            if (carrying == CARRY_NO_OBJECT)
            {
                str += " carrying nothing";
            }
            else if (carrying != DONT_CARE_OBJECT)
            {
                str += " with " + carrying;
            }
            return str;
        }

        public override int getDesiredObject()
        {
            return carrying;
        }
    }


    //-------------------------------------------------------------------------


    /**
     * Finds the shortest route to the room
     * @param inRoom the desired room
     * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
     * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
     * don't care if you pick up an object or not
     */
    public class GoToRoomObjective : AiObjective
    {
        private int gotoRoom;
        private int carrying;
        private RRect targetPlot;
        private Portcullis behindPortcullis = null; // If the target room is behind a Portcullis

        public GoToRoomObjective(int inRoom, int inCarrying = DONT_CARE_OBJECT)
        {
            gotoRoom = inRoom;
            carrying = inCarrying;
        }

        /**
         * Still valid as long as you are carrying the object you are supposed to
         * be carrying and you can still get to where you're supposed to go.
         */
        public override bool isStillValid()
        {
            bool stillHaveObject =
                (carrying == DONT_CARE_OBJECT) ||
                ((carrying == CARRY_NO_OBJECT) && (aiPlayer.linkedObject == Board.OBJECT_NONE)) ||
                (aiPlayer.linkedObject == carrying);
            bool blocked = (behindPortcullis != null) && (aiPlayer.room == behindPortcullis.room) && !behindPortcullis.allowsEntry;
            return stillHaveObject && !blocked;
        }

        protected override void doComputeStrategy()
        {
            behindPortcullis = GoToObjective.isBehindPortcullis(board, aiPlayer, gotoRoom);

            // Figure out what point in the room is closest.
            AiPathNode path = nav.ComputePathToClosestExit(aiPlayer.room, aiPlayer.midX, aiPlayer.midY, gotoRoom);
            if (path == null)
            {
                // No way to get out of room
                UnityEngine.Debug.Log("Couldn't compute path for AI player #" + aiPlayerNum + " for objective \"" + this +
                    "\" to get to room " + board.map.roomDefs[gotoRoom].label);
                throw new Abort();
            }
            targetPlot = path.End.ThisPlot.BRect;
            addChild(new GoToObjective(targetPlot, carrying));
        }

        public override RRect getBDestination()
        {
            return targetPlot;
        }

        protected override bool computeIsCompleted()
        {
            return (aiPlayer.room == gotoRoom);
        }

        public override string ToString()
        {
            return "go to room " + board.map.roomDefs[gotoRoom].label;
        }

        public override int getDesiredObject()
        {
            return carrying;
        }
    }

    //-------------------------------------------------------------------------

    /**
     * Finds the shortest route to the room
     */
    public class BringObjectToRoomObjective : AiObjective
    {
        private int gotoRoom;
        private int toBring;
        private OBJECT objectToBring;
        private Portcullis behindPortcullis = null; // If the target room is behind a Portcullis

        public BringObjectToRoomObjective(int inRoom, int inToBring)
        {
            gotoRoom = inRoom;
            toBring = inToBring;
        }

        protected override bool computeIsCompleted()
        {
            return (aiPlayer.room == gotoRoom) &&
                (objectToBring.room == gotoRoom) &&
                (objectToBring.x >= Board.LEFT_EDGE_FOR_OBJECTS) &&
                (objectToBring.y <= Board.TOP_EDGE_FOR_OBJECTS) &&
                (objectToBring.x + objectToBring.width <= Board.RIGHT_EDGE_FOR_OBJECTS) &&
                (objectToBring.y - objectToBring.Height >= Board.BOTTOM_EDGE_FOR_OBJECTS);
        }

        /**
         * No longer valid if you are no longer holding the object or if the room
         * you are going to has been made unreachable and you can see it is unreachable.
         */
        public override bool isStillValid()
        {
            bool blocked = (behindPortcullis != null) && (aiPlayer.room == behindPortcullis.room) && !behindPortcullis.allowsEntry;
            return (aiPlayer.linkedObject == toBring) && !blocked;
        }

        protected override void doComputeStrategy()
        {
            behindPortcullis = GoToObjective.isBehindPortcullis(board, aiPlayer, gotoRoom);

            objectToBring = board.getObject(toBring);

            // Compute the area of the room where, if the ball were in that area
            // then the object would be all the way in the room.
            RRect ballTargetSpace = RRect.fromTRBL(gotoRoom,
                Board.TOP_EDGE_FOR_BALL - aiPlayer.linkedObjectBY,
                Board.RIGHT_EDGE_FOR_OBJECTS - aiPlayer.linkedObjectBX - objectToBring.bwidth,
                Board.BOTTOM_EDGE_FOR_BALL + objectToBring.BHeight - aiPlayer.linkedObjectBY,
                Board.LEFT_EDGE_FOR_BALL - aiPlayer.linkedObjectBX);
            // but keep it in the room
            ballTargetSpace = RRect.fromTRBL(ballTargetSpace.room,
                Math.Min(ballTargetSpace.top, Board.TOP_EDGE_FOR_BALL),
                Math.Min(ballTargetSpace.right, Board.RIGHT_EDGE_FOR_BALL),
                Math.Max(ballTargetSpace.bottom, Board.BOTTOM_EDGE_FOR_BALL),
                Math.Max(ballTargetSpace.left, Board.LEFT_EDGE_FOR_BALL));

            // Compute an area slightly smaller, that if a plot is touching this
            // area then the ball can find a place in this plot that is in
            // the target area.
            RRect plotTargetSpace = RRect.fromTRBL(gotoRoom,
                ballTargetSpace.top - BALL.DIAMETER - BALL.MOVEMENT,
                ballTargetSpace.right - BALL.MOVEMENT,
                ballTargetSpace.bottom + BALL.MOVEMENT,
                ballTargetSpace.left + BALL.DIAMETER + BALL.MOVEMENT);
            AiPathNode closestPlot = nav.ComputePathToArea(aiPlayer.room, aiPlayer.midX, aiPlayer.midY, plotTargetSpace);
            if (closestPlot == null)
            {
                // No way to get there.  Give up.
                UnityEngine.Debug.Log("Couldn't compute path for AI player #" + aiPlayerNum + " for objective \"" + this +
                    "\" to get to room " + board.map.roomDefs[gotoRoom].label);
                throw new Abort();

            }
            RRect target = closestPlot.End.ThisPlot.BRect.intersect(ballTargetSpace);
            addChild(new GoToObjective(target, toBring));
        }

        public override string ToString()
        {
            return "bring " + board.getObject(toBring).label + " to room " + board.map.roomDefs[gotoRoom].label;
        }

        public override int getDesiredObject()
        {
            return toBring;
        }

    }


    //-------------------------------------------------------------------------

    //-------------------------------------------------------------------------

    /**
     * Reposition a key so you can unlock a castle.
     * Assumes you are holding the key and you are directly below the portcullis where you
     * have room to move around.
     */
    public class RepositionKey : AiObjective
    {
        private const int KEY_WIDTH = 8;
        private const int KEY_HEIGHT = 3;
        private int keyId;
        private OBJECT key;
        private const int CASTLE_FOOT = 0x40; // The Y coordinate of the bottom of the castle

        public RepositionKey(int inKeyId)
        {
            keyId = inKeyId;
        }

        public override bool isStillValid()
        {
            // Still valid if we are holding the key or the
            // key is still in the room with us
            return ((aiPlayer.linkedObject == keyId) ||
                (aiPlayer.room == key.room));
        }

        protected override void doComputeStrategy()
        {
            if (aiPlayer.linkedObject != keyId)
            {
                throw new Abort();
            }
            else
            {
                key = board.getObject(keyId);
                // The key may already be in a good enough position.  Check.
                if ((aiPlayer.linkedObjectBY < 0) ||
                    (aiPlayer.linkedObjectBX < -key.bwidth) ||
                    (aiPlayer.linkedObjectBX > BALL.DIAMETER))
                {
                    // Key is not in a good position.  Drop it and get under it.
                    // 
                    int xLeftToDropAt = Adv.ADVENTURE_SCREEN_BWIDTH / 2 - key.bwidth / 2 - aiPlayer.linkedObjectBX;
                    int yTopToDropAt = CASTLE_FOOT - 1;
                    aiPlayer.adjustDestination(ref xLeftToDropAt, ref yTopToDropAt, BALL.Adjust.BELOW);
                    this.addChild(new GoExactlyToObjective(aiPlayer.room, xLeftToDropAt, yTopToDropAt, keyId));
                    this.addChild(new DropObjective(keyId));

                    // Pick a point under the key and let the tactical algorithms get around the key
                    int yTopToPickupAt = yTopToDropAt + aiPlayer.linkedObjectBY - key.BHeight;
                    int xLeftToPickupAt = Portcullis.EXIT_X;
                    aiPlayer.adjustDestination(ref xLeftToPickupAt, ref yTopToPickupAt, BALL.Adjust.BELOW);
                    this.addChild(new GoExactlyToObjective(aiPlayer.room, xLeftToPickupAt, yTopToPickupAt, CARRY_NO_OBJECT));
                    this.addChild(new PickupObject(keyId));
                }
            }
        }

        protected override bool computeIsCompleted()
        {
            return ((aiPlayer.linkedObject == keyId) &&
                (aiPlayer.linkedObjectY > 0) &&
                (aiPlayer.linkedObjectX >= -KEY_WIDTH) &&
                (aiPlayer.linkedObjectX <= BALL.DIAMETER / Adv.BALL_SCALE));
        }

        public override string ToString()
        {
            return "reposition " + (key != null ? key.label : board.getObject(aiPlayer.linkedObject).label);
        }
    }


    //-------------------------------------------------------------------------

    /**
     * Drop what is currently held
     */
    public class DropObjective : AiObjective
    {
        int toDrop;

        /**
         * Drop the desired object.  If you happen to 
         * not be carrying that object, then this 
         * immediately succeeds.
         * @param inToDrop the object to drop
         */
        public DropObjective(int inToDrop)
        {
            toDrop = inToDrop;
        }

        public override string ToString()
        {
            return "drop " + board.getObject(toDrop).label;
        }

        protected override bool computeIsCompleted()
        {
            return aiPlayer.linkedObject != toDrop;
        }

        protected override void doComputeStrategy()
        {
            // No strategy needed.  Just trigger drop.
        }

        public override bool shouldDropHeldObject()
        {
            return (aiPlayer.linkedObject == toDrop);
        }

    }

    //-------------------------------------------------------------------------

    /**
     * Lock another player's castle and hide the key
     */
    public class LockCastleAndHideKeyObjective : AiObjective
    {
        int otherPlayerNum;
        Portcullis otherGate;
        int otherKeyId;

        public LockCastleAndHideKeyObjective(int inOtherPlayerNum)
        {
            if (inOtherPlayerNum < 0)
            {
                throw new IndexOutOfRangeException();
            }
            otherPlayerNum = inOtherPlayerNum;
        }

        public override string ToString()
        {
            return "lock player #" + otherPlayerNum + "'s castle and hide key";
        }

        protected override bool computeIsCompleted()
        {
            return !otherGate.allowsEntry /* TBD --- && (aiPlayer.linkedObject != otherKeyId)*/;
        }

        protected override void doComputeStrategy()
        {
            otherGate = board.getPlayer(otherPlayerNum).homeGate;
            otherKeyId = otherGate.key.getPKey();

            if (otherGate.allowsEntry)
            {
                this.addChild(new ObtainObject(otherKeyId));
                this.addChild(new GoToObjective(otherGate.room, Portcullis.EXIT_X, 0x30, otherKeyId));
                this.addChild(new RepositionKey(otherKeyId));
                this.addChild(new GoToObjective(otherGate.insideRoom, Portcullis.EXIT_X, Map.WALL_HEIGHT, otherKeyId));
                this.addChild(new GoToObjective(otherGate.room, Portcullis.EXIT_X, Map.WALL_HEIGHT, otherKeyId));
            }
        }


    }

    //-------------------------------------------------------------------------

    /**
     * Use the magnet to get an object out of a wall
     */
    public class GetObjectWithMagnet : AiObjective
    {
        private int toPickup;
        private OBJECT objectToPickup;

        public GetObjectWithMagnet(int inToPickup)
        {
            toPickup = inToPickup;
        }

        public override string ToString()
        {
            return "use magnet to obtain  " + board.getObject(toPickup).label;
        }

        protected override bool computeIsCompleted()
        {
            return (aiPlayer.linkedObject == toPickup);
        }

        protected override void doComputeStrategy()
        {
            objectToPickup = board.getObject(toPickup);
            addChild(new ObtainObject(Board.OBJECT_MAGNET));
            addChild(new BringObjectToRoomObjective(objectToPickup.room, Board.OBJECT_MAGNET));
            addChild(new DropObjective(Board.OBJECT_MAGNET));
            addChild(new WaitForMagnetObjective(toPickup));
            addChild(new PickupObject(toPickup));
        }

        public override int getDesiredObject()
        {
            return toPickup;
        }

    }

    //-------------------------------------------------------------------------

    /**
     * Wait for an object to pulled by the magnet
     */
    public class WaitForMagnetObjective : AiObjective
    {
        private int toPickup;
        private OBJECT objectToPickup;
        private Magnet magnet;

        public WaitForMagnetObjective(int inToPickup)
        {
            toPickup = inToPickup;
        }

        public override string ToString()
        {
            return "wait for magnet to pull  " + board.getObject(toPickup).label;
        }

        protected override bool computeIsCompleted()
        {
            // We're done either if the object has reached the magnet or collided
            // with us
            bool reachedMagnet = ((objectToPickup.bx == magnet.bx) &&
                (objectToPickup.by == magnet.by - magnet.BHeight));
            return (aiPlayer.linkedObject == toPickup) || reachedMagnet;

        }

        /**
         * Still valid if the object is in the same room as the magnet and
         * no one is holding the object.
         */
        public override bool isStillValid()
        {
            bool held = false;
            for (int ctr = 0; !held && ctr < board.getNumPlayers(); ++ctr)
            {
                held = (ctr != aiPlayerNum) &&
                    (board.getPlayer(ctr).linkedObject == toPickup);
            }
            return (!held && (magnet.room == objectToPickup.room));
        }


        protected override void doComputeStrategy()
        {
            objectToPickup = board.getObject(toPickup);
            magnet = (Magnet)board.getObject(Board.OBJECT_MAGNET);

            // If there is another object in the room that is more attracted
            // to the magnet, we need to remove that from the room

            OBJECT attractedObject = magnet.getAtractedObject();
            if ((attractedObject != null) && (attractedObject.getPKey() != toPickup))
            {
                int attracted = attractedObject.getPKey();
                addChild(new WaitForMagnetObjective(attracted));
                addChild(new PickupObject(attracted));
                AiPathNode hidePath = plotToHideFromMagnet();
                RRect plotToStash = hidePath.End.ThisPlot.BRect;
                addChild(new GoToObjective(plotToStash));
                // Make sure the object is all the way in the room
                addChild(new BringObjectToRoomObjective(plotToStash.room, attracted));
                addChild(new DropObjective(attracted));
                // Go back to the magnet room
                addChild(new GoToObjective(hidePath.ThisPlot.BRect, CARRY_NO_OBJECT));
                // We need to make this recursive in case there are other objects
                // that the magnet attracts more than the desired one.
                addChild(new WaitForMagnetObjective(toPickup));
            }

        }

        /**
         * Compute the closest plot not in the room with the magnet.
         * We actually want to return 2 plots, the closest plot not
         * in the room, and the exit plot in the room that you go through to
         * get to the closest plot not in the room.  We return it as a two step 
         * path
         * @returns two step path with the start being the exit plot and the end 
         * being the closest plot not in the room 
         */
        private AiPathNode plotToHideFromMagnet()
        {
            // Find the path to the closest exit
            AiPathNode path = nav.ComputePathToClosestExit(aiPlayer.room, aiPlayer.midX, aiPlayer.midY, magnet.room);
            // Find the plot on the other side of that closest exit
            AiMapNode exit = path.End.thisNode;
            int foundDirection = Plot.NO_DIRECTION;
            for (int dir = Plot.FIRST_DIRECTION;
                (dir <= Plot.LAST_DIRECTION) && (foundDirection == Plot.NO_DIRECTION);
                ++dir)
            {
                if ((exit.neighbors[dir] != null) &&
                    (exit.neighbors[dir].thisPlot.Room != exit.thisPlot.Room))
                {
                    foundDirection = dir;
                }
            }
            AiPathNode end = new AiPathNode(exit.neighbors[foundDirection]);
            AiPathNode twoStepPath = end.Prepend(exit, foundDirection);
            return twoStepPath;
        }

        public override int getDesiredObject()
        {
            return toPickup;
        }
    }
}