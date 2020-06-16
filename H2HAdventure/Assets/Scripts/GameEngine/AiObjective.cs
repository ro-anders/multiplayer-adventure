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

    private bool computed = false;

    protected AiObjective()
    {}

    /**
     * Compute a set of objectives to complete this objective
     */
    public void computeStrategy()
    {;
        if (computed)
        {
            // Something went wrong
            throw new Exception("Asking to recompute an already computed strategy");
        }
        doComputeStrategy();
        computed = true;
    }

    public override abstract string ToString();

    /**
     * Compute a set of objectives to complete this objective
     */
    protected abstract void doComputeStrategy();

    /**
     * Whether an objective has been fulfilled
     */
    public bool isCompleted()
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

    public virtual bool shouldDropHeldObject()
    {
        // Default is to do nothing.
        return false;
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

    public override string ToString() 
    {
        return "win game";
    }

    protected override void doComputeStrategy()
    {
        this.addChild(new UnlockCastle(Board.OBJECT_YELLOW_PORT));
        this.addChild(new UnlockCastle(Board.OBJECT_BLACK_PORT));
        this.addChild(new PickupObjective(Board.OBJECT_CHALISE));
        this.addChild(new GoToObjective(Map.GOLD_FOYER, 160, 120));
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

    protected override void doComputeStrategy()
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
            (Math.Abs(aiPlayer.midX - gotoX) <= 3) &&
            (Math.Abs(aiPlayer.midY - gotoY) <= 3);
    }

    public override string ToString()
    {
        return "go to (" + gotoX + "," + gotoY + ") in room " + gotoRoom;
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

    protected override void doComputeStrategy()
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
            int width = 8; // Except for the bridge, everything is 8 pixels width
            int height = objectToPickup.gfxData[0].Length;
            x = 2 * objectToPickup.x + width; // 2 * (x + width/2)
            y = 2 * objectToPickup.y - height; // 2 * (y - hegiht/2)
        }
    }

    protected override bool computeIsCompleted()
    {
        return (aiPlayer.linkedObject == toPickup);
    }

    public override string ToString()
    {
        return "get " + board.getObject(toPickup).label;
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

    protected override void doComputeStrategy()
    {
        port = (Portcullis)board.getObject(portId);
        this.addChild(new PickupObjective(port.key.getPKey()));
        this.addChild(new GoToObjective(port.room, Portcullis.EXIT_X, 0x30));
        this.addChild(new RepositionKey());
    }

    public override void getDestination(ref int room, ref int x, ref int y)
    {
        room = port.room;
        x = Portcullis.EXIT_X - aiPlayer.linkedObjectX;
        y = 0x3D;
    }

    protected override bool computeIsCompleted()
    {
        return port.allowsEntry;
    }

    public override string ToString()
    {
        return "unlock " + board.getObject(portId).label;
    }
}

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
    private int MINIMUM_Y = 1;
    public RepositionKey()
    {}

    protected override void doComputeStrategy()
    {
        keyId = aiPlayer.linkedObject;
        key = board.getObject(keyId);
        if (aiPlayer.linkedObjectY < MINIMUM_Y)
        {
            this.addChild(new DropObjective());

            //If right above the key, move to the closest top corner

            // linkedObjectX = obj.x - ball.x/2
            // linkedObjectY = obj.y - (ball.y-6)/2
            int sideEdge;
            if (aiPlayer.linkedObjectX > -KEY_WIDTH / 4)
            {
                // Move around to the left
                sideEdge = key.x*2 - 4;
                sideEdge -= (6 - (aiPlayer.midX - sideEdge) % 6) % 6;
                if (aiPlayer.midX > sideEdge)
                {
                    this.addChild(new GoToObjective(aiPlayer.room, sideEdge, aiPlayer.midY));
                }
            }
            else
            {
                // Move around to the right
                sideEdge = key.x * 2 + KEY_WIDTH + 4;
                sideEdge += (6 - (sideEdge - aiPlayer.midX) % 6) % 6;
                if (aiPlayer.midX < sideEdge)
                {
                    this.addChild(new GoToObjective(aiPlayer.room, sideEdge, aiPlayer.midY));
                }
            }
            // Then move to the closest bottom corner, then to the bottom middle, then re-pickup the key
            int bottomEdge = key.y * 2 - KEY_HEIGHT - 4;
            bottomEdge -= (6 - (aiPlayer.midY - bottomEdge) % 6) % 6;
            this.addChild(new GoToObjective(aiPlayer.room, sideEdge, bottomEdge));
            this.addChild(new GoToObjective(aiPlayer.room, key.x * 2 + KEY_WIDTH / 2, bottomEdge));
            this.addChild(new GoToObjective(aiPlayer.room, key.x * 2 + KEY_WIDTH / 2, key.y * 2 - KEY_HEIGHT / 2));
            UnityEngine.Debug.Log("REPOSITIONING KEY at (" + key.x * 2 + "," + key.y * 2 + ") with (" + aiPlayer.midX + "," + aiPlayer.midY + ")-(" + sideEdge + "," + aiPlayer.midY +
                ")-(" + sideEdge + "," + bottomEdge + ")-(" + (key.x * 2 + KEY_WIDTH / 2) + "," + bottomEdge + ")");
            UnityEngine.Debug.Log("REPOSITIONING KEY at (" + key.x * 2 + "," + key.y * 2 + ") with (" + aiPlayer.midX + "," + aiPlayer.midY + ")-(" + sideEdge + "," + aiPlayer.midY +
                ")-(" + sideEdge + "," + bottomEdge + ")-(" + (key.x * 2 + KEY_WIDTH / 2) + "," + bottomEdge + ")");
        }
    }

    protected override bool computeIsCompleted()
    {
        return (aiPlayer.linkedObject == keyId) && (aiPlayer.linkedObjectY >= MINIMUM_Y);
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
    public override string ToString()
    {
        return "drop held object";
    }

    protected override bool computeIsCompleted()
    {
        return aiPlayer.linkedObject == Board.OBJECT_NONE;
    }

    protected override void doComputeStrategy()
    {
        // No strategy needed.  Just trigger drop.
    }

    public override bool shouldDropHeldObject()
    {
        return true;
    }

}