using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

namespace GameEngine.Ai
{
    public class AiStrategy
    {
        /** The ball for which this is developing strategy */
        private BALL thisBall;

        private readonly Board board;

        private AiNav nav;

        public AiStrategy(Board inBoard, int inPlayerSlot, AiNav inNav)
        {
            board = inBoard;
            nav = inNav;
            thisBall = board.getPlayer(inPlayerSlot);
        }

        /**
         * If we need to block a player, return the player that most needs to be blocked.
         * Or -1 if no one needs to be blocked. 
        */
        public int shouldBlockPlayer()
        {
            if (clearPathToVictory(thisBall.playerNum))
            {
                return -1;
            }
            for (int ctr = 0; ctr < board.getNumPlayers(); ++ctr)
            {
                if ((ctr != thisBall.playerNum) && clearPathToVictory(ctr))
                {
                    return ctr;
                }
            }
            return -1;
        }

        /**
         * Returns whether a player has all they need to win.
         * This means their castle is unlocked and either the
         * chalice is out in the open or the castle is protected
         * but they have the key or bridge or magnet to get the chalice.
         */
        private bool clearPathToVictory(int otherPlayer)
        {
            // If their castle is locked they don't have a clear path to victory
            BALL otherBall = board.getPlayer(otherPlayer);
            bool clearPath = true;
            if (!otherBall.homeGate.allowsEntry)
            {
                clearPath = false;
            }

            // If the castle is locked in a castle and they don't have the key, they
            // don't have a clear path to victory
            OBJECT chalice = board.getObject(Board.OBJECT_CHALISE);
            if (clearPath)
            {
                Portcullis behindGate = behindLockedGate(chalice.room);
                if ((behindGate != null) && (otherBall.linkedObject != behindGate.key.getPKey()))
                {
                    clearPath = false;
                }
            }

            // If the chalice is in the hidden passages of the white castle and they
            // don't have the bridge or have the bridge in the white castle, they don't have a
            // clear path to victory.
            if (clearPath)
            {
                if (chalice.room == Map.RED_MAZE_4)
                {
                    Portcullis whitePort = (Portcullis)board.getObject(Board.OBJECT_WHITE_PORT);
                    OBJECT bridge = board.getObject(Board.OBJECT_BRIDGE);
                    if ((otherBall.linkedObject != Board.OBJECT_BRIDGE) &&
                        (!whitePort.containsRoom(bridge.room)))
                    {

                        clearPath = false;
                    }
                }

            }

            // If the chalice is stuck in a wall and they don't have a magnet or bridge
            if (clearPath)
            {
                if (IsObjectInWall(chalice) &&
                    (otherBall.linkedObject != Board.OBJECT_MAGNET) &&
                    (otherBall.linkedObject != Board.OBJECT_BRIDGE)
                )
                {
                    clearPath = false;
                }
            }
            return clearPath;
        }

        /**
         * If the ball has been eaten by a dragon
         */
        public bool eatenByDragon()
        {
            bool eaten = false;
            for (int ctr = Board.FIRST_DRAGON; !eaten && (ctr <= Board.LAST_DRAGON); ++ctr)
            {
                Dragon dragon = (Dragon)board.getObject(ctr);
                eaten = (dragon.eaten == thisBall);
            }
            return eaten;
        }

        /**
         * Gross check to see if the ball is stuck in a wall.  If you're half
         * in the wall but can still get out by going in the right direction,
         * this method gives indeterminate results.
         * @param allowForBridge if the bridge is present and provides a means
         * to get out this will return false unless allowForBridge is false.
         */
        public bool isBallEmbeddedInWall(bool allowForBridge)
        {
            // Do a quick check is to see if the center of the ball is in a wall.
            // If it is do a longer check to see if the entire ball is in the wall.
            ROOM room = board.map.getRoom(thisBall.room);
            bool stuckInWall = room.isWall(thisBall.midX, thisBall.midY);
            if (stuckInWall)
            {
                stuckInWall = room.embeddedInWall(thisBall.x, thisBall.y, BALL.DIAMETER, BALL.DIAMETER);
            }

            // Even if we're stuck in wall, return false if we're inside the bridge.
            if (stuckInWall && allowForBridge)
            {
                Bridge bridge = (Bridge)board.getObject(Board.OBJECT_BRIDGE);
                bool insideBridge = (bridge.room == thisBall.room) &&
                    (bridge.InsideBRect.overlaps(thisBall.BRect));
                stuckInWall = !insideBridge;
            }
            return stuckInWall;
        }


        /**
         * If an object is behind a locked gate, returns that gate.  Otherwise
         * returns null.
         * @param room the room the object is in
         */
        public Portcullis behindLockedGate(int room)
        {
            Portcullis behindGate = null;
            for (int ctr = Board.OBJECT_YELLOW_PORT; (behindGate == null) && (ctr <= Board.OBJECT_CRYSTAL_PORT); ++ctr)
            {
                Portcullis nextPort = (Portcullis)board.getObject(ctr);
                if ((nextPort != null) && (!nextPort.allowsEntry) && (nextPort.containsRoom(room)))
                {
                    behindGate = nextPort;
                }

            }
            return behindGate;
        }

        /**
         * Returns what player is holding an object, or null if not held.
         * @param exclThisPlayer is true and the object is held by this player
         *   this will return null
         */
        public BALL heldByPlayer(OBJECT obj, bool exclThisPlayer = true)
        {
            int objPkey = obj.getPKey();
            BALL heldBy = null;
            int numPlayers = board.getNumPlayers();
            int thisPlayer = thisBall.playerNum;
            for (int ctr = 0; (heldBy == null) && (ctr < numPlayers); ++ctr)
            {
                if (!exclThisPlayer || (ctr != thisPlayer))
                {
                    BALL nextBall = board.getPlayer(ctr);
                    if (nextBall.linkedObject == objPkey)
                    {
                        heldBy = nextBall;
                    }
                }
            }
            return heldBy;
        }


        /**
         * Returns whether an object is contained entirely in a wall and has
         * no part touching a path.  Note that object the is entirely in a wall
         * may still be grabbable from path.
         */
        public bool IsObjectInWall(OBJECT objct)
        {
            ROOM room = board.map.getRoom(objct.room);
            return room.embeddedInWall(objct.bx, objct.by, objct.bwidth, objct.BHeight);
        }

        /**
         * Return if a desired room is behind a portcullis.
         * If you are also behind the same portcullis then this returns that it is not
         * behind a portcullis.
         * @param targetRoom the rooom of interest
         * @returns the portcullis that stands between the ball and the room or null
         * if none does
         */
        public Portcullis isBehindPortcullis(int targetRoom)
        {
            Portcullis targetPort = null;
            Portcullis myPort = null;
            // Figure out if the desired room is behind a locked gate
            for (int portNum = Board.FIRST_PORT; portNum <= Board.LAST_PORT; ++portNum)
            {
                Portcullis port = (Portcullis)board.getObject(portNum);
                targetPort = ((targetPort == null) && port.containsRoom(targetRoom) ? port : targetPort);
                myPort = ((myPort == null) && port.containsRoom(thisBall.room) ? port : myPort);
            }
            return (targetPort == myPort ? null : targetPort);
        }

        /**
         * Compute the rectanlge that represents the area of the object that is
         * actually on the path and reachable.  If the object spans multiple 
         * plots, will return the area that is closest.
         * If the object is embedded in a wall, will return an invalid rectangle.
         * If the object touches a path but is unreachable (i.e. behing a locked 
         * castle) will still return a valid rectangle.
         */
        public RRect closestReachableRectangle(OBJECT objct)
        {
            RRect objBRect = objct.BRect;
            AiPathNode shortestPath = nav.ComputePathToArea(thisBall.room, thisBall.midX, thisBall.midY, objBRect);
            if (shortestPath == null)
            {
                return RRect.INVALID;
            }
            else
            {
                return shortestPath.End.ThisPlot.BRect.intersect(objBRect);
            }

        }

    }
}