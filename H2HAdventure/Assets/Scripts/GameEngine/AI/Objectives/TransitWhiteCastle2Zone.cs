
namespace GameEngine.Ai
{

    /**
     * Get into or out of the hidden room in the white castle.
     * This assumes the White Castle has been unlocked.
     **/
    public class TransitWhiteCastle2Zone : AiObjective
    {
        private bool inOut; // True if getting into hidden room, False if getting out
        private int carrying;
        private Portcullis whitePort;
        private Bridge bridge = null;
        private RRect bridgePlacedBLoc = RRect.NOWHERE;
        private PlaceBridgeToHiddenRoom placeBridgeObjective;

        /**
         * Go from the main zone to the hidden room zone or vice versa
         * @param inInOut True if we want to get into the hidden room, false if 
         * we want to get out of the hidden room
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public TransitWhiteCastle2Zone(bool inInOut, int inCarrying = DONT_CARE_OBJECT)
        {
            inOut = inInOut;
            carrying = inCarrying;
        }

        protected override void doComputeStrategy()
        {
            whitePort = (Portcullis)board.getObject(Board.OBJECT_WHITE_PORT);
            bridge = (Bridge)board.getObject(Board.OBJECT_BRIDGE);

            placeBridgeObjective = new PlaceBridgeToHiddenRoom(inOut);
            addChild(placeBridgeObjective);
            if (carrying >= 0)
            {
                addChild(new ObtainObject(carrying));
            }
            addChild(new CrossBridge(inOut, carrying));
        }

        /**
         * If we see that the white castle has been locked it is no longer valid.
         * Once we place the bridge, if we see that the bridge has been moved
         * this is no longer valid.
         */
        public override bool isStillValid()
        {
            // If we can see that the white castle is closed from the outside
            // or, from the inside, we are in close enough the exit and it is
            // closed we abort.
            if ((aiPlayer.room == Map.WHITE_CASTLE) &&
                !whitePort.allowsEntry)
            {
                return false;
            }
            if ((aiPlayer.room == Map.RED_MAZE_1) &&
                (aiPlayer.y < Map.WALL_HEIGHT) &&
                !whitePort.allowsEntry)
            {
                return false;
            }

            if ((placeBridgeObjective != null) && (placeBridgeObjective.isCompleted()))
            {
                if (bridgePlacedBLoc.IsSomewhere)
                {
                    return bridgePlacedBLoc.equals(bridge.BRect);
                } else
                {
                    bridgePlacedBLoc = bridge.BRect;
                }
            }

            return true;
        }

        protected override bool computeIsCompleted()
        {
            NavZone playerZone = nav.WhichZone(aiPlayer.BRect);
            if (inOut)
            {
                return playerZone == NavZone.WHITE_CASTLE_2;
            }
            else
            {
                return (playerZone == NavZone.MAIN) || (playerZone == NavZone.WHITE_CASTLE_1);
            }
        }

        public override string ToString()
        {
            string str = (inOut ? "get into " : "leave ") + "hidden white castle room";
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

    }


}