using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

abstract public class AiObjective
{
    /** The next objective after this to accomplish parent objective */
    protected AiObjective sibling;

    /** The first in a linked list of dependent objectives that must be
     * completed before this objective is completed. */
    protected AiObjective child;

    protected Board board;
    protected int aiPlayerNum;
    protected BALL aiPlayer;

    protected bool completed = false;

    protected AiObjective()
    {}

    /**
     * Compute a set of objectives to complete this objective
     */
    public abstract void computeStrategy();

    /**
     * Whether an objective has been fulfilled
     */
    public bool isCompleted()
    {
        if (!completed)
        {
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
    public void addChild(AiObjective nextChild)
    {
        // The root of the objective tree provides the board and the player num
        nextChild.board = this.board;
        nextChild.aiPlayerNum = this.aiPlayerNum;
        nextChild.aiPlayer = this.aiPlayer;

        if (child == null)
        {
            child = nextChild;
        } else
        {
            child.addSibling(nextChild);
        }
    }

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

    protected abstract bool computeIsCompleted();

    public virtual void getDestination(ref int room, ref int x, ref int y)
    {
        // Default is to do nothing.  Objectives that are composed of
        // other objectives will often leave this unimplemented
    }

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
        } else
        {
            sibling.addSibling(nextObjective);
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


}




//-------------------------------------------------------------------------

public class WinGameObjective: AiObjective
{
    public WinGameObjective(Board inBoard, int inAiPlayerNum)
    {
        base.board = inBoard;
        base.aiPlayerNum = inAiPlayerNum;
        base.aiPlayer = board.getPlayer(inAiPlayerNum);
    }

    public override void computeStrategy()
    {
        // Temporarily trying simple strategy
        //child = new UnlockCastle(board, (Portcullis)board.getObject(Board.OBJECT_YELLOW_PORT));
        //child.computeStrategy();
        //AiObjective nextChild = new PutObject();
        //setSiblingOf(child, nextChild);
        AiObjective unlockCastle = new UnlockCastle(Board.OBJECT_YELLOW_PORT);
        this.addChild(unlockCastle);
        unlockCastle.computeStrategy();
        AiObjective step3 = new UnlockCastle(Board.OBJECT_BLACK_PORT);
        this.addChild(step3);
        step3.computeStrategy();
    }

    protected override bool computeIsCompleted()
    {
        // This is never completed, or by the time it is we don't
        // ever call this anymore
        return false;
    }
}

//-------------------------------------------------------------------------

public class GoToObjective : AiObjective
{
    private int gotoRoom;
    private int gotoX;
    private int gotoY;

    public GoToObjective(int inRoom, int inX, int inY)
    {
        gotoRoom = inRoom;
        gotoX = inX;
        gotoY = inY;
    }

    public override void computeStrategy()
    { }

    public override void getDestination(ref int room, ref int x, ref int y)
    {
        room = gotoRoom;
        x = gotoX;
        y = gotoY;
    }

    protected override bool computeIsCompleted()
    {
        return (aiPlayer.room == gotoRoom) &&
            (Math.Abs(aiPlayer.x - gotoX) <= 3) &&
            (Math.Abs(aiPlayer.y - gotoY) <= 3);
    }
}

//-------------------------------------------------------------------------

/**
 * Go and pick up an object.
 */
public class PickupObjective : AiObjective
{
    private int toPickup;
    private OBJECT objectToPickup;

    /**
     * The object the AI player needs to pickup
     */
    public PickupObjective(int inToPickup)
    {
        toPickup = inToPickup;
    }

    public override void computeStrategy()
    {
        objectToPickup = board.getObject(toPickup);
    }

    public override void getDestination(ref int room, ref int x, ref int y)
    {
        if (toPickup == Board.OBJECT_BRIDGE)
        {
            // Bridge is tricky.  Aim for the corner for now.
            room = objectToPickup.room;
            x = 2 * objectToPickup.x;
            y = 2 * objectToPickup.y;
        }
        else
        {
            // Aim for the center
            room = objectToPickup.room;
            int midWidth = 4; // Except for the bridge, everything is 8 pixels width
            int midHeight = objectToPickup.gfxData[0].Length / 2;
            x = 2 * objectToPickup.x + midWidth;
            y = 2 * objectToPickup.y + midHeight;
        }
    }

    protected override bool computeIsCompleted()
    {
        return (aiPlayer.linkedObject == toPickup);
    }
}


//-------------------------------------------------------------------------

/**
 * Unlock a castle.
 */
public class UnlockCastle : AiObjective
{
    int portId;
    Portcullis port;

    public UnlockCastle(int inPortId)
    {
        portId = inPortId;
    }

    public override void computeStrategy()
    {
        port = (Portcullis)board.getObject(portId);
        // To unlock the castle we first need the key
        AiObjective step1 = new PickupObjective(port.key.getPKey());
        this.addChild(step1);
        step1.computeStrategy();
    }

    public override void getDestination(ref int room, ref int x, ref int y)
    {
        // TODO: Handle when the key is being held beneath us.
        room = port.room;
        x = 2 * port.x + 4;
        y = 2 * port.y + 6;
    }

    protected override bool computeIsCompleted()
    {
        return port.allowsEntry;
    }
}

