using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

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
     */
    public virtual void getDestination(ref int room, ref int x, ref int y)
    {
        // Default is to do nothing.  Objectives that are composed of
        // other objectives will often leave this unimplemented
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
        nextChild.parent = this;

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
    protected AiObjective getParentOf(AiObjective otherObjective)
    {
        return otherObjective.parent;
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
        if (strategy.eatenByDragon())
        {
            markShouldReset();
        }
        else 
        {
            int playerToBlock = strategy.shouldBlockPlayer();
            if (playerToBlock >= 0)
            {
                this.addChild(new LockCastleAndHideKeyObjective(playerToBlock));
                // Once the first player is blocked, we want to recompute whether
                // we need to block the second player or if we can try to win
                // TODO: Do we want to do this through recursion or some other way
                this.addChild(new WinGameObjective(board, aiPlayerNum, strategy));
            }
            else
            {
                Portcullis homeGate = this.aiPlayer.homeGate;
                this.addChild(new UnlockCastle(homeGate.getPKey()));
                this.addChild(new ObtainObjective(Board.OBJECT_CHALISE));
                this.addChild(new GoToObjective(homeGate.insideRoom, 160, 120, Board.OBJECT_CHALISE));
            }
        }
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
        abortIfLooping();
        objectToPickup = board.getObject(toPickup);

        // Check if the object is locked in a castle
        Portcullis portcullis = strategy.behindLockedGate(objectToPickup);
        if (portcullis != null)
        {
            addChild(new UnlockCastle(portcullis.getPKey()));
        }

        // Check if the object is held by another player
        BALL otherPlayer = strategy.heldByOtherPlayer(objectToPickup);
        if (otherPlayer != null)
        {
            addChild(new GetObjectFromPlayer(toPickup, otherPlayer.playerNum));
        } else {

            // Check if the object is stuck in a wall
            if (strategy.isObjectReachable(objectToPickup))
            {
                addChild(new PickupObjective(toPickup));
            } else
            {
                // Need to get the object out of the wall
                // Only options supported right now is magnet
                addChild(new ObtainObjective(Board.OBJECT_MAGNET));
                addChild(new GoToRoomObjective(objectToPickup.room));
                addChild(new PickupObjective(toPickup));
            }
        }
    }

    /** 
     * Look up the chain of parent objectives to see if we've gotten ourselves
     * into an infinite loop.  (e.g. black key stuck in wall with magnet, bridge
     * and bat all locked in black castle).  If we are, abort.
     */
    private void abortIfLooping()
    {
        Type obtainType = this.GetType();
        ;
        for (AiObjective nextParent = this.parent;  nextParent != null; nextParent = getParentOf(nextParent))
        {
            Type type = nextParent.GetType();
            if (type.Equals(obtainType)) {
                ObtainObjective nextObtain = (ObtainObjective)nextParent;
                if (nextObtain.toPickup == this.toPickup)
                {
                    throw new Abort();
                }
            }
        }
    }

    public override int getDesiredObject()
    {
        return toPickup;
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
            x = objectToPickup.x * Adv.BALL_SCALE;
            y = objectToPickup.y * Adv.BALL_SCALE;
        }
        else
        {
            // Aim for the center of reachable object
            room = objectToPickup.room;
            int rx = objectToPickup.x * Adv.BALL_SCALE;
            int ry = objectToPickup.y * Adv.BALL_SCALE;
            int rw = objectToPickup.Width * Adv.BALL_SCALE;
            int rh = objectToPickup.Height * Adv.BALL_SCALE;
            bool found = strategy.closestReachableRectangle(objectToPickup, ref rx, ref ry, ref rw, ref rh);
            if (!found)
            {
                // Something went wrong.  Shoudn't get that here
                UnityEngine.Debug.LogError("Request to pick up object " + objectToPickup.label + " that is at not reachable place (" +
                    objectToPickup.x + "," + objectToPickup.y + ")@" + objectToPickup.room);
            }
            x = rx + (rw / 2);
            y = ry - (rh / 2);
        }
    }

    protected override bool computeIsCompleted()
    {
        return (aiPlayer.linkedObject == toPickup);
    }

    public override int getDesiredObject()
    {
        return toPickup;
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
                    x = objectToSteal.x * Adv.BALL_SCALE;
                    y = objectToSteal.y * Adv.BALL_SCALE;
                }
                else
                {
                    // Aim for the center
                    room = objectToSteal.room;
                    x = Adv.BALL_SCALE * objectToSteal.x + objectToSteal.Width; // 2 * (x + width/2)
                    y = Adv.BALL_SCALE * objectToSteal.y - objectToSteal.Height; // 2 * (y - hegiht/2)
                }
            }
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
}


//-------------------------------------------------------------------------

public class GoToObjective : AiObjective
{
    private int gotoRoom;
    private int gotoX;
    private int gotoY;
    private int carrying;

    public GoToObjective(int inRoom, int inX, int inY, int inCarrying = DONT_CARE_OBJECT)
    {
        gotoRoom = inRoom;
        gotoX = inX;
        gotoY = inY;
        carrying = inCarrying;
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
        return "go to (" + gotoX + "," + gotoY + ") in room " + board.map.roomDefs[gotoRoom].label;
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
public class GoToRoomObjective : AiObjective
{
    private int gotoRoom;
    private int carrying;
    private int gotoX;
    private int gotoY;

    public GoToRoomObjective(int inRoom, int inCarrying = DONT_CARE_OBJECT)
    {
        gotoRoom = inRoom;
        carrying = inCarrying;
    }

    protected override void doComputeStrategy()
    {
        // Figure out what point in the room is closest.
        bool found = strategy.closestPointInRoom(gotoRoom, ref gotoX, ref gotoY); 
    }

    public override void getDestination(ref int room, ref int x, ref int y)
    {
        room = gotoRoom;
        x = gotoX;
        y = gotoY;
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
        this.addChild(new GoToObjective(port.room, Portcullis.EXIT_X, 0x30, key));
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
    private const int KEY_AT_Y = 0x30;

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
            if ((aiPlayer.linkedObjectY < 0)  ||
                (aiPlayer.linkedObjectX < -KEY_WIDTH) ||
                (aiPlayer.linkedObjectX > BALL.DIAMETER/Adv.BALL_SCALE)) 
            {
                this.addChild(new GoToObjective(aiPlayer.room, Portcullis.EXIT_X - 2 * aiPlayer.linkedObjectX, KEY_AT_Y - 2 * aiPlayer.linkedObjectY, keyId));
                this.addChild(new DropObjective());

                // Pick a point under the key and let the tactical algorithms get around the key
                int bottomEdge = KEY_AT_Y - KEY_HEIGHT * Adv.BALL_SCALE - BALL.RADIUS;
                bottomEdge -= (BALL.MOVEMENT - (aiPlayer.midY - bottomEdge) % BALL.MOVEMENT) % BALL.MOVEMENT;
                this.addChild(new GoToObjective(aiPlayer.room, Portcullis.EXIT_X, bottomEdge, CARRY_NO_OBJECT));
                this.addChild(new PickupObjective(keyId));
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
        otherPlayerNum = inOtherPlayerNum;
    }

    public override string ToString()
    {
        return "lock player #"+otherPlayerNum + "'s castle and hide key";
    }

    protected override bool computeIsCompleted()
    {
        return !otherGate.allowsEntry && (aiPlayer.linkedObject != otherKeyId);
    }

    protected override void doComputeStrategy()
    {
        otherGate = board.getPlayer(otherPlayerNum).homeGate;
        otherKeyId = otherGate.key.getPKey();

        if (otherGate.allowsEntry)
        {
            this.addChild(new ObtainObjective(otherKeyId));
            this.addChild(new GoToObjective(otherGate.room, Portcullis.EXIT_X, 0x30, otherKeyId));
            this.addChild(new RepositionKey(otherKeyId));
            this.addChild(new GoToObjective(otherGate.insideRoom, Portcullis.EXIT_X, Map.WALL_HEIGHT, otherKeyId));
            this.addChild(new GoToObjective(otherGate.room, Portcullis.EXIT_X, Map.WALL_HEIGHT, otherKeyId));
        }
    }


}
