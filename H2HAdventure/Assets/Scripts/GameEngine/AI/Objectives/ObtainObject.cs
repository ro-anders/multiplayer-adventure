using System;

namespace GameEngine.Ai
{

    /**
     * This is the high-level objective for getting an object.  It can
     * deal with stuff like the object is held by the bat or locked in a 
     * castle.  It relies on the low level PickupObject objective, which 
     * assumes the object is unheld and reachable.
     */
    public class ObtainObject : AiObjective
    {

        private int toPickup;
        private int recursionLevel;
        private OBJECT objectToPickup;

        /**
         * Create an objective to get an object.
         * @param inToPickup - the object to get
         * @param recursionLevel - sometimes an objective will recursively duplicate itself as
         *   a child.  This is the acceptable level of recursion.
         */
        public ObtainObject(int inToPickup, int inRecusrionLevel = 0)
        {
            toPickup = inToPickup;
            recursionLevel = inRecusrionLevel;
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

            if (strategy.eatenByDragon() || strategy.isBallEmbeddedInWall(true))
            {
                markShouldReset();
                return;
            }

            // Check if the object is held by another player
            BALL otherPlayer = strategy.heldByPlayer(objectToPickup);
            if (otherPlayer != null)
            {
                addChild(new GetObjectFromPlayer(toPickup, otherPlayer.playerNum));
                return;
            }

            // If the object is in a different Zone, this is a lot more complicated.
            NavZone currentZone = nav.WhichZone(aiPlayer.BRect);
            NavZone desiredZone = nav.WhichZone(objectToPickup.BRect, currentZone);
            if (currentZone != desiredZone)
            {
                // Check if the object is locked in a castle
                Portcullis portcullis = strategy.behindLockedGate(objectToPickup.room);
                if (portcullis != null)
                {
                    addChild(new UnlockCastle(portcullis.getPKey()));
                    addChild(new ObtainObject(toPickup, 1));
                    return;
                }

                // Check if the object is in the secret room in the white castle
                if (objectToPickup.room == Map.RED_MAZE_4)
                {
                    addChild(new TransitWhiteCastle2Zone(desiredZone == NavZone.WHITE_CASTLE_2));
                    addChild(new ObtainObject(toPickup, 2));
                    return;
                }

                // Check if the object is in an unreachable but visible part of the
                // the red or black maze.
                if ((desiredZone == NavZone.WHITE_CASTLE_2) || (desiredZone == NavZone.DOT_LOCATION))
                {
                    // Only option programmed is using the magnet
                    addChild(new GetObjectWithMagnet(toPickup));
                    return;
                }

                // Check if the object is stuck in a wall
                // MUST_IMPLEMENT: Is this the right way to handle this and the same code segment below?
                if (strategy.IsObjectInWall(objectToPickup))
                {
                    // Only option programmed is using the magnet
                    addChild(new GetObjectWithMagnet(toPickup));
                    return;
                }

                // Check if we're on the bridge.  We already know we're
                // not embedded in a wall without the bridge, so if
                // we're embedded in the wall we're on the bridge.
                if (strategy.isBallEmbeddedInWall(false))
                {
                    // Figure out if we want to go up or down
                    Bridge bridge = (Bridge)board.getObject(Board.OBJECT_BRIDGE);
                    NavZone upZone = nav.WhichZone(bridge.TopExitBRect);
                    addChild(new CrossBridge(upZone == desiredZone));
                    addChild(new ObtainObject(toPickup, 3));
                    return;
                }

                // Check if we're in a zone we don't want to be in.
                if (currentZone != NavZone.MAIN)
                {
                    // MUST_IMPLEMENT: Deal with getting back to main zone
                    throw new Abort("Asked to obtain " + objectToPickup.label +
                        " in zone " + desiredZone + " while ball is in zone " + currentZone);
                }


            }
            else
            {

                // Check if the object is stuck in a wall
                if (strategy.IsObjectInWall(objectToPickup))
                {
                    // Need to get the object out of the wall
                    // Only options supported right now is magnet
                    addChild(new GetObjectWithMagnet(toPickup));
                }
                else
                {
                    addChild(new PickupObject(toPickup));
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

            // An ObtainObject objective may add a duplicate of itself to its list of children - multiple
            // times even.  We use recursion level to make sure we are actually making progress and not stuck
            // in an infinite loop
            for (AiObjective nextParent = this.parent; nextParent != null; nextParent = getParentOf(nextParent))
            {
                Type type = nextParent.GetType();
                if (type.Equals(obtainType))
                {
                    ObtainObject nextObtain = (ObtainObject)nextParent;
                    if ((nextObtain.toPickup == this.toPickup) && (nextObtain.recursionLevel >= this.recursionLevel))
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
}