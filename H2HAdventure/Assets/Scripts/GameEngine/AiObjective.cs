using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

abstract public class AiObjective
{
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

    protected Board board;
    protected int aiPlayerNum;
    protected BALL aiPlayer;
    protected AiStrategy strategy;

    /** Whether this objective has been successfully completed */
    protected bool completed = false;

    /** Whether this objective has computed all the steps necessary to
     * be achieved.  This is done once.  If things change after that, this
     * objective must be aborted and a new one created. */
    private bool computed = false;


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

    public override abstract string ToString();

    protected AiObjective()
    {}

    /**
     * Compute a set of objectives to complete this objective
     */
    protected void computeStrategy()
    {;
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

        if (child == null)
        {
            child = nextChild;
        } else
        {
            child.addSibling(nextChild);
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
    public WinGameObjective(Board inBoard, int inAiPlayerNum, AiStrategy inAiStrategy)
    {
        base.board = inBoard;
        base.aiPlayerNum = inAiPlayerNum;
        base.aiPlayer = board.getPlayer(inAiPlayerNum);
        base.strategy = inAiStrategy;
    }

    public override string ToString() 
    {
        return "win game";
    }

    protected override void doComputeStrategy()
    {
        Portcullis homeGate = this.aiPlayer.homeGate;
        this.addChild(new UnlockCastle(homeGate.getPKey()));
        this.addChild(new ObtainObjective(Board.OBJECT_CHALISE));
        this.addChild(new GoToObjective(homeGate.insideRoom, 160, 120));
    }

    protected override bool computeIsCompleted()
    {
        // This is never completed, or by the time it is we don't
        // ever call this anymore
        return false;
    }

}

//-------------------------------------------------------------------------

/**
 * This is the high-level objective for getting an object.  It can
 * deal with stuff like the object is held by the bat or locked in a 
 * castle.  It relies on the low level PickupObject objective, which 
 * assumes the object is unheld and reachable.
 */
public class ObtainObjective : AiObjective
{

    private int toPickup;
    private OBJECT objectToPickup;

    public ObtainObjective(int inToPickup)
    {
        toPickup = inToPickup;
    }

    public override string ToString()
    {
        return "obtain  " + board.getObject(toPickup).label;
    }

    protected override bool computeIsCompleted()
    {
        return (aiPlayer.linkedObject == toPickup);
    }

    protected override void doComputeStrategy()
    {
        objectToPickup = board.getObject(toPickup);

        int portcullis = strategy.behindLockedGate(objectToPickup);
        if (portcullis >= 0)
        {
            addChild(new UnlockCastle(portcullis));
        }
        BALL otherPlayer = strategy.heldByOtherPlayer(objectToPickup);
        if (otherPlayer != null)
        {
            addChild(new GetObjectFromPlayer(toPickup, otherPlayer.playerNum));
        } else { 
            addChild(new PickupObjective(toPickup));
        }
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

    public override string ToString()
    {
        return "go pickup " + board.getObject(toPickup).label;
    }

    protected override void doComputeStrategy()
    {
        objectToPickup = board.getObject(toPickup);
        if (strategy.heldByOtherPlayer(objectToPickup) != null)
        {
            throw new Abort();
        }
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

    public override void getDestination(ref int room, ref int x, ref int y)
    {
        // If we're really close, go for the object, otherwise go for the ball
        if (aiPlayer.room != ballToStealFrom.room)
        {
            room = ballToStealFrom.room;
            x = ballToStealFrom.x;
            y = ballToStealFrom.y;
        }
        else
        {
            int distanceX = Math.Abs(aiPlayer.midX - ballToStealFrom.midX);
            int distanceY = Math.Abs(aiPlayer.midY - ballToStealFrom.midY);
            int distance = (distanceX > distanceY ? distanceX : distanceY);
            if (distance > 2 * BALL.MOVEMENT)
            {
                room = ballToStealFrom.room;
                x = ballToStealFrom.x;
                y = ballToStealFrom.y;

            }
            else
            {
                if (toSteal == Board.OBJECT_BRIDGE)
                {
                    // Bridge is tricky.  Aim for the corner for now.
                    room = objectToSteal.room;
                    x = 2 * objectToSteal.x;
                    y = 2 * objectToSteal.y;
                }
                else
                {
                    // Aim for the center
                    room = objectToSteal.room;
                    int width = 8; // Except for the bridge, everything is 8 pixels width
                    int height = objectToSteal.gfxData[0].Length;
                    x = 2 * objectToSteal.x + width; // 2 * (x + width/2)
                    y = 2 * objectToSteal.y - height; // 2 * (y - hegiht/2)
                }
            }
        }
    }

    protected override bool computeIsCompleted()
    {
        return (aiPlayer.linkedObject == toSteal);
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
            (((Math.Abs(aiPlayer.midX - gotoX) <= (BALL.MOVEMENT / 2)) &&
              (Math.Abs(aiPlayer.midY - gotoY) < BALL.MOVEMENT)) ||
            ((Math.Abs(aiPlayer.midY - gotoY) <= (BALL.MOVEMENT / 2)) &&
              (Math.Abs(aiPlayer.midX - gotoX) < BALL.MOVEMENT)));
    }

    public override string ToString()
    {
        return "go to (" + gotoX + "," + gotoY + ") in room " + gotoRoom;
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
        int key = port.key.getPKey();
        this.addChild(new ObtainObjective(key));
        this.addChild(new GoToObjective(port.room, Portcullis.EXIT_X, 0x30));
        this.addChild(new RepositionKey(key));
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
    public RepositionKey(int inKeyId)
    {
        keyId = inKeyId;
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
            if (aiPlayer.linkedObjectY < MINIMUM_Y)
            {
                this.addChild(new DropObjective());

                //If right above the key, move to the closest top corner

                int sideEdge;
                if (aiPlayer.linkedObjectX > -KEY_WIDTH / 4)
                {
                    // Move around to the left
                    sideEdge = key.x * 2 - BALL.RADIUS;
                    sideEdge -= (BALL.MOVEMENT - (aiPlayer.midX - sideEdge) % BALL.MOVEMENT) % BALL.MOVEMENT;
                    if (aiPlayer.midX > sideEdge)
                    {
                        this.addChild(new GoToObjective(aiPlayer.room, sideEdge, aiPlayer.midY));
                    }
                }
                else
                {
                    // Move around to the right
                    sideEdge = key.x * 2 + KEY_WIDTH + BALL.RADIUS;
                    sideEdge += (BALL.MOVEMENT - (sideEdge - aiPlayer.midX) % BALL.MOVEMENT) % BALL.MOVEMENT;
                    if (aiPlayer.midX < sideEdge)
                    {
                        this.addChild(new GoToObjective(aiPlayer.room, sideEdge, aiPlayer.midY));
                    }
                }
                // Then move to the closest bottom corner, then to the bottom middle, then re-pickup the key
                int bottomEdge = key.y * 2 - KEY_HEIGHT - BALL.RADIUS;
                bottomEdge -= (BALL.MOVEMENT - (aiPlayer.midY - bottomEdge) % BALL.MOVEMENT) % BALL.MOVEMENT;
                this.addChild(new GoToObjective(aiPlayer.room, sideEdge, bottomEdge));
                this.addChild(new GoToObjective(aiPlayer.room, key.x * 2 + KEY_WIDTH / 2, bottomEdge));
                this.addChild(new GoToObjective(aiPlayer.room, key.x * 2 + KEY_WIDTH / 2, key.y * 2 - KEY_HEIGHT / 2));
            }
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
