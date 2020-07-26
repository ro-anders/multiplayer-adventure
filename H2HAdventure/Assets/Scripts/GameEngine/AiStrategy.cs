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
    public int behindLockedGate(OBJECT obj)
    {
        int gate = -1;
        for (int ctr = Board.OBJECT_YELLOW_PORT; (gate < 0) && (ctr <= Board.OBJECT_CRYSTAL_PORT); ++ctr)
        {
            Portcullis nextPort = (Portcullis)board.getObject(ctr);
            if ((nextPort != null) && (!nextPort.allowsEntry) && (nextPort.containsRoom(obj.room)))
            {
                gate = ctr;
            }

        }
        return gate;
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
            objct.x * Adv.BALL_SCALE, objct.y * Adv.BALL_SCALE,
            objct.Width * Adv.BALL_SCALE, objct.Height * Adv.BALL_SCALE);
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
        int objx = objct.x * Adv.BALL_SCALE;
        int objy = objct.y * Adv.BALL_SCALE;
        int objw = objct.Width * Adv.BALL_SCALE;
        int objh = objct.Height * Adv.BALL_SCALE;
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
