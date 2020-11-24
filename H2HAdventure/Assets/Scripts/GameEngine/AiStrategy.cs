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
     * Returns the point in the room that is the quickest to get to.
     * Will place the point slightly within the room.
     */
    public bool closestPointInRoom(int toRoom, ref int x, ref int y)
    {
        Plot closestPlot = nav.closestPlotInRoom(thisBall.room, thisBall.x, thisBall.y, toRoom);
        bool found = false;
        if (closestPlot != null)
        {
            // TODO: Right now just picking a point in the middle of the plot
            x = (closestPlot.Left + closestPlot.Right) / 2;
            y = (closestPlot.Top + closestPlot.Bottom) / 2;
            found = true;
        }
        return found;
    }


    /**
     * Compute the rectanlge that represents the area of the object that is
     * actually on the path and reachable.  If the object spans multiple 
     * plots, will return the area that is closest.
     * If the object is embedded in a wall, will return false and leave reference
     * parameters unchanged.
     * If the object touches a path but is unreachable (i.e. behing a locked 
     * castle) will still return true and compute a rectangle.
     */
    public bool closestReachableRectangle(OBJECT objct, ref int x, ref int y, ref int width, ref int height)
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
            Board.intersect(objx, objy, objw, objh, plot.Left, plot.Top, plot.Right - plot.Left, plot.Top - plot.Bottom,
                ref x, ref y, ref width, ref height);
        }
        return (plots.Length > 0);


    }

}
