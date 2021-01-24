using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

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
        for (int ctr=0; ctr<board.getNumPlayers(); ++ctr)
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
        BALL otherBall = board.getPlayer(otherPlayer);
        bool clearPath = true;
        if (!otherBall.homeGate.allowsEntry)
        {
            clearPath = false;
        }

        OBJECT chalice = board.getObject(Board.OBJECT_CHALISE);
        if (clearPath)
        {
            Portcullis behindGate = behindLockedGate(chalice);
            if ((behindGate != null) && (otherBall.linkedObject != behindGate.key.getPKey())) {
                clearPath = false;
            }
        }

        if (clearPath)
        {
            if (!isObjectReachable(chalice) && (otherBall.linkedObject != Board.OBJECT_MAGNET))
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
     * If an object is behind a locked gate, returns that gate.  Otherwise
     * returns -1.
     */
    public Portcullis behindLockedGate(OBJECT obj)
    {
        Portcullis behindGate = null;
        for (int ctr = Board.OBJECT_YELLOW_PORT; (behindGate == null) && (ctr <= Board.OBJECT_CRYSTAL_PORT); ++ctr)
        {
            Portcullis nextPort = (Portcullis)board.getObject(ctr);
            if ((nextPort != null) && (!nextPort.allowsEntry) && (nextPort.containsRoom(obj.room)))
            {
                behindGate = nextPort;
            }

        }
        return behindGate;
    }

    /**
     * If an object is held by another player, returns that player.  Otherwise
     * returns null.
     */
    public BALL heldByOtherPlayer(OBJECT obj)
    {
        int objPkey = obj.getPKey();
        BALL heldBy = null;
        int numPlayers = board.getNumPlayers();
        int thisPlayer = thisBall.playerNum;
        for (int ctr = 0; (heldBy == null) && (ctr < numPlayers); ++ctr)
        {
            if (ctr != thisPlayer)
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

    public bool isObjectReachable(OBJECT objct)
    {
        return nav.IsReachable(objct.room,
            objct.bx, objct.by, objct.bwidth, objct.BHeight);
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
        int objx = objct.bx;
        int objy = objct.by;
        int objw = objct.bwidth;
        int objh = objct.BHeight;
        Plot[] plots = nav.GetPlots(objct.room, objx, objy, objw, objh);
        if (plots.Length > 0)
        {
            // TODO: Right now we don't compute closest.  We return the area
            // overlapping the first plot we find.
            Plot plot = plots[0];
            int x = -1;
            int y = -1;
            int width = -1;
            int height = -1;
            Board.intersect(objx, objy, objw, objh, plot.Left, plot.Top, plot.Right - plot.Left, plot.Top - plot.Bottom,
                ref x, ref y, ref width, ref height);
            return new RRect(objct.room, x, y, width, height);
        }
        else
        {
            return RRect.INVALID;
        }


    }

}
