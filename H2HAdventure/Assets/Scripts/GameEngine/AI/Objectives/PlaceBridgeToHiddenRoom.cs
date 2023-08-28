
namespace GameEngine.Ai
{

    /**
     * Place the bridge so that you can get into or out of the hidden room in the white castle.
     * This is the high level objective that doesn't assume you are in the white castle 
     * or even have the bridge.
     **/
    public class PlaceBridgeToHiddenRoom : AiObjective
    {
        private bool inOut; // True if getting into hidden room, False if getting out
        private Bridge bridge;

        /** The coordinates where we want to place the bridge to get to
         * the hidden room */
        private const int RED_MAZE_PLACEMENT_BX = 19 * Map.WALL_WIDTH;
        private const int RED_MAZE_PLACEMENT_BY = 3 * Map.WALL_HEIGHT + Bridge.FOOT_BHEIGHT;

        /**
         * Put the bridge down where we can travel to and from the hidden
         * room in the red maze.
         * @param inInOut True if we want to get into the hidden room, false if 
         * we want to get out of the hidden room
         */
        public PlaceBridgeToHiddenRoom(bool inInOut)
        {
            inOut = inInOut;
        }

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            bridge = (Bridge)board.getObject(Board.OBJECT_BRIDGE);
        }

        protected override void doComputeStrategy()
        {
            this.addChild(new ObtainObject(Board.OBJECT_BRIDGE));
            this.addChild(new RepositionBridge());
            if (inOut) {
                // Reposition will always place you under the left foot of the bridge
                // So go to the right place in RedMaze2 - the bottom area where
                // you first enter from RedMaze1.
                this.addChild(new GoTo(new RRect(Map.RED_MAZE_2, Map.WALL_WIDTH * 16, Map.WALL_HEIGHT * 3 - 1, Map.WALL_WIDTH * 8, Map.WALL_HEIGHT * 3), Board.OBJECT_BRIDGE));
                this.addChild(new PlaceObjectAt(Board.OBJECT_BRIDGE, Map.RED_MAZE_2, RED_MAZE_PLACEMENT_BX, RED_MAZE_PLACEMENT_BY, PlaceObjectAt.Adjust.BELOW));
            } else
            {
                // MUST_IMPLEMENT
            }
        }

        public override bool isStillValid()
        {
            // We are invalid if the white castle is locked,
            // but TransitWhiteCastle2Zone objective checks that and this
            // is always a child of that.
            return true;
        }

        /**
         * Return if the bridge actually has one end in the main or white_castle_1
         * zone and the other end in the white_castle_2 zone.
         */
        protected override bool computeIsCompleted()
        {
            // Don't bother checking unless the bridge is in the white castle
            // and not being carried.
            bool completed = false;
            if ((bridge.room >= Map.RED_MAZE_3) && (bridge.room <= Map.RED_MAZE_1) &&
                (strategy.heldByPlayer(bridge, false) == null))
            {
                // Figure out the zones just above and below the bridge.
                RRect bridgeRect = bridge.BRect;
                NavZone bottomZone = nav.WhichZone(bridge.BottomEffectiveExitBRect);
                NavZone topZone = nav.WhichZone(bridge.TopEffectiveExitBRect);
                // One has to be WHITE_CASTLE_2 while the other must be MAIN or WHITE_CASTLE_1
                completed = ((topZone == NavZone.WHITE_CASTLE_2) && ((bottomZone == NavZone.MAIN) || (bottomZone == NavZone.WHITE_CASTLE_1)) ||
                    (bottomZone == NavZone.WHITE_CASTLE_2) && ((topZone == NavZone.MAIN) || (topZone == NavZone.WHITE_CASTLE_1)));

            }
            return completed;
        }

        public override string ToString()
        {
            return "place bridge to hidden room in red maze";
        }

    }


}