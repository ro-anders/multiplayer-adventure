using System;

namespace GameEngine.Ai
{
    public class Abort : Exception
    {
        public Abort(string message = "") :
            base(message)
        { }
    }

    abstract public class AiObjective
    {
        public const int CARRY_NO_OBJECT = -10; // We specifically don't want to carry or bump into anything
        public const int DONT_CARE_OBJECT = -20; // We don't care if we bump into an object or not

        /**
         * This is thrown when an objective can't be completed anymore.
         * If things change that make an objective impossible (e.g. a go to command
         * when the gate just closed) then the objective is aborted.*/

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
         * Following this object, do we need blindly move in a direction
         * (this ignores walls and objects and is in contrast to getBDestination()
         * which smartly figures out the directions needed to get to a place)
         * An objective should not return a blind direction AND a destination
         * at the same time.
         * 
         * @param the x direction to move
         * @param the y direction to move
         * @return whether a direction is being returned
         */
        public virtual bool shouldMoveDirection(ref int velbx, ref int velby)
        {
            // Do nothing
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
            if (this.completed)
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
         * Initialize the stategy.  This is called not when the strategy is
         * created but when it's about to be computed.  It computes any
         * state required by isCompleted() and isStillValid().
         */
        protected virtual void initialize()
        {}

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

            initialize();
            completed = computeIsCompleted();
            if (!completed)
            {
                doComputeStrategy();
            }
            computed = true;
        }

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

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            behindPortcullis = strategy.isBehindPortcullis(gotoRoom);
        }

        protected override void doComputeStrategy()
        {
            // Figure out what point in the room is closest.
            AiPathNode path = nav.ComputePathToRoom(aiPlayer.room, aiPlayer.midX, aiPlayer.midY, gotoRoom);
            if (path == null)
            {
                // No way to get out of room
                UnityEngine.Debug.Log("Couldn't compute path for AI player #" + aiPlayerNum + " for objective \"" + this +
                    "\" to get to room " + board.map.roomDefs[gotoRoom].label);
                throw new Abort();
            }
            targetPlot = path.End.ThisPlot.BRect;
            addChild(new GoTo(targetPlot, carrying));
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

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            behindPortcullis = strategy.isBehindPortcullis(gotoRoom);

            objectToBring = board.getObject(toBring);
        }

        protected override void doComputeStrategy()
        {
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
            addChild(new GoTo(target, toBring));
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

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            key = board.getObject(keyId);
        }

        protected override void doComputeStrategy()
        {
            if (aiPlayer.linkedObject != keyId)
            {
                throw new Abort();
            }
            else
            {
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
                    this.addChild(new GoExactlyTo(aiPlayer.room, xLeftToDropAt, yTopToDropAt, keyId));
                    this.addChild(new DropObjective(keyId));

                    // Pick a point under the key and let the tactical algorithms get around the key
                    int yTopToPickupAt = yTopToDropAt + aiPlayer.linkedObjectBY - key.BHeight;
                    int xLeftToPickupAt = Portcullis.EXIT_X;
                    aiPlayer.adjustDestination(ref xLeftToPickupAt, ref yTopToPickupAt, BALL.Adjust.BELOW);
                    this.addChild(new GoExactlyTo(aiPlayer.room, xLeftToPickupAt, yTopToPickupAt, CARRY_NO_OBJECT));
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

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            otherGate = board.getPlayer(otherPlayerNum).homeGate;
            otherKeyId = otherGate.key.getPKey();
        }

        protected override void doComputeStrategy()
        {
            if (otherGate.allowsEntry)
            {
                this.addChild(new ObtainObject(otherKeyId));
                this.addChild(new GoTo(otherGate.room, Portcullis.EXIT_X, 0x30, otherKeyId));
                this.addChild(new RepositionKey(otherKeyId));
                this.addChild(new GoTo(otherGate.insideRoom, Portcullis.EXIT_X, Map.WALL_HEIGHT, otherKeyId));
                this.addChild(new GoTo(otherGate.room, Portcullis.EXIT_X, Map.WALL_HEIGHT, otherKeyId));
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

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            objectToPickup = board.getObject(toPickup);
        }

        protected override void doComputeStrategy()
        {
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


        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            objectToPickup = board.getObject(toPickup);
            magnet = (Magnet)board.getObject(Board.OBJECT_MAGNET);
        }

        protected override void doComputeStrategy()
        {
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
                addChild(new GoTo(plotToStash));
                // Make sure the object is all the way in the room
                addChild(new BringObjectToRoomObjective(plotToStash.room, attracted));
                addChild(new DropObjective(attracted));
                // Go back to the magnet room
                addChild(new GoTo(hidePath.ThisPlot.BRect, CARRY_NO_OBJECT));
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