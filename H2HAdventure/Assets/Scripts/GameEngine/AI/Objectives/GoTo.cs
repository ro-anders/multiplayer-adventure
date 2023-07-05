using System;

namespace GameEngine.Ai
{


    /**
     * Go to coordinates.  This is the high level objective and can get to
     * coordinates even if they're behind a locked portcullis or
     * in the hidden maze.
     */
    public class GoTo : AiObjective
    {
        private RRect btarget;
        private int carrying;
        private static int[] insideRooms = { }; // We cache the ids of the rooms just inside all portcullises

        /**
         * Go to these coordinates.
         * @param inRoom the desired room
         * @param inX the desired X
         * @param inY the desired Y
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public GoTo(int inRoom, int inBX, int inBY, int inCarrying = DONT_CARE_OBJECT)
        {
            btarget = new RRect(inRoom, inBX, inBY, 1, 1);
            carrying = inCarrying;
        }

        /**
         * Go to somewhere within this area.  If the area is big enough, will put
         * the ball entirely within the area.  If it is not big enough, will be
         * as much in the area as possible.  If the area is a point or smaller than
         * the ball, will attempt to get the balls midpoint as close to the center
         * of the area as possible.
         * @param inTarget desired area in ball coordinates
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public GoTo(in RRect inBTarget, int inCarrying = DONT_CARE_OBJECT)
        {
            btarget = inBTarget;
            carrying = inCarrying;
        }

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            if (insideRooms.Length == 0)
            {
                cacheInsideRooms();
            }
        }


        protected override void doComputeStrategy()
        {
            // Reasons we would need to abort
            // The immediately apparent reasons - will be checked by GoTo's isStillValid()
            //   but handled by both GoTo's and WinGame's computeStrategy()
            // - We're eaten by a dragon
            // - We're stuck in a wall
            // More complicated reasons
            // - We're locked behind a portcullis - This, while not immediately apparent, will also be checked by GoTo's isStillValid()
            //    and handled by both GoTo's and WinGame's computeStrategy()
            // - We're stuck in the white castle's hidden maze without a bridge - This, is checked and handled by TransiteWhiteCastle
            //    objective's isStilValid() and computeStrategy()
            // - Where we want to get to is in a wall - This is handled in GoTo's computeStrategy()
            // Not yet handled
            // - We're stuck in a dragon or portcullis - TBD
            // - Where we want to get to is blocked by a dragon or portcullis - TBD
            // - Anything to do with the dot plot or the robinett room - TBD

            if (strategy.eatenByDragon() || strategy.isBallEmbeddedInWall(true))
            {
                markShouldReset();
                return;
            }

            if (strategy.behindLockedGate(aiPlayer.room) != null)
            {
                // We can't do anything if we are locked inside a castle, so reset.
                // More intelligent code may decide if the object we are carrying
                // needs to be shoved in a wall or if the player with the key is
                // waiting outside and may unlock it shortly
                markShouldReset();
                return;
            }

            // If the object is in a different Zone, this is a lot more complicated.
            NavZone currentZone = nav.WhichZone(aiPlayer.BRect);
            NavZone desiredZone = nav.WhichZone(btarget, currentZone);
            if (currentZone == desiredZone)
            {
                this.addChild(new GoStraightTo(btarget, carrying));
            }
            else
            {
                // Check if the object is stuck in a wall
                // This is bad, shouldn't be asking to go to an invalid place.
                // Nothing to do but abort.
                if (desiredZone == NavZone.NO_ZONE)
                {
                    UnityEngine.Debug.LogError("Being asked to go to invalid place, " + btarget);
                    throw new Abort();
                }

                // Check if the target is behind a locked portcullis
                Portcullis portcullis = strategy.behindLockedGate(btarget.room);
                if (portcullis != null)
                {
                    addChild(new UnlockCastle(portcullis.getPKey()));
                    this.addChild(new GoStraightTo(btarget, carrying));
                    return;
                }

                // Check if the object is in the white castles hidden zone or if
                // we're in the white castle hidden zone
                if ((desiredZone == NavZone.WHITE_CASTLE_2) ||
                    (currentZone == NavZone.WHITE_CASTLE_2))
                {
                    addChild(new TransitWhiteCastle2Zone(desiredZone == NavZone.WHITE_CASTLE_2, carrying));
                    addChild(new GoStraightTo(btarget, carrying));
                    return;
                }

                // Check if we're on the bridge.  We already know we're
                // not embedded in a wall without the bridge, so if
                // we're embedded in the wall we're on the bridge.
                if (strategy.isBallEmbeddedInWall(false))
                {
                    // Figure out if we want to go up or down.
                    // Try to get to the desired zone, but if neither end
                    // leads to the desired zone try to get to the main zone
                    // and if neither leads to the main zone, go to any zone.
                    Bridge bridge = (Bridge)board.getObject(Board.OBJECT_BRIDGE);
                    NavZone upZone = nav.WhichZone(bridge.TopExitBRect);
                    NavZone downZone = nav.WhichZone(bridge.BottomExitBRect);
                    if ((upZone == NavZone.NO_ZONE) && (downZone == NavZone.NO_ZONE))
                    {
                        markShouldReset();
                        return;
                    }
                    else
                    {
                        bool goUp = (upZone == desiredZone ? true :
                            (downZone == desiredZone ? false :
                            (upZone == NavZone.MAIN) ? true :
                            (downZone == NavZone.MAIN ? false :
                            (upZone != NavZone.NO_ZONE ? true : false))));
                        addChild(new CrossBridge(goUp));
                        addChild(new GoStraightTo(btarget, carrying));
                        return;
                    }
                }
            }

        }

        /**
         * Still valid as long as you can move and aren't locked in a castle
         */
        public override bool isStillValid()
        {
            if (strategy.eatenByDragon()) {
                return false;
            }
            if (strategy.isBallEmbeddedInWall(true)) 
            {
                return false;
            }
            if (isJustBehindLockedPortcullis())
            {
                return false;
            }
            return true;
        }


        protected override bool computeIsCompleted()
        {
            return GoStraightTo.hasPlayerGotTo(aiPlayer, btarget);
        }

        public override string ToString()
        {
            string str = "go to " + btarget.ToStringWithRoom(board.map.roomDefs[btarget.room].label);
            if (carrying == CARRY_NO_OBJECT)
            {
                str += " carrying nothing";
            }
            if (carrying != DONT_CARE_OBJECT)
            {
                str += " with " + carrying;
            }
            return str;
        }

        public override int getDesiredObject()
        {
            return carrying;
        }

        /**
         * Checks to see if we are locked behind a portcullis and close
         * enough to the exit that it would soon be apparent that 
         * its locked.
         */
        private bool isJustBehindLockedPortcullis()
        {
            // We are "close enough to the exit that it would soon be apparent" if
            // we're in the wally=0 part of the room.
            if (aiPlayer.x < Map.WALL_HEIGHT) {
                int portIndex = Array.FindIndex(insideRooms, insideRoom => aiPlayer.room == insideRoom);
                if (portIndex >= 0)
                {
                    Portcullis port = (Portcullis)board.getObject(portIndex + Board.FIRST_PORT);
                    if (!port.allowsEntry)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /**
         * Cache the ids of all rooms just inside portcullises so we can do 
         * fast calculation.
         */
        private void cacheInsideRooms()
        {
            int numPorts = Board.LAST_PORT - Board.FIRST_PORT + 1;
            int[] cached = new int[numPorts];
            for (int portNum = 0; portNum < numPorts; ++portNum)
            {
                Portcullis port = (Portcullis)board.getObject(portNum+Board.FIRST_PORT);
                cached[portNum] = port.insideRoom;
            }
            insideRooms = cached;
        }
    }
}
