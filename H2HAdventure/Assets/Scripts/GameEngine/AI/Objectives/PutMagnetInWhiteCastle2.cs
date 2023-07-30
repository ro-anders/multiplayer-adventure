namespace GameEngine.Ai
{

    /**
     * We need to use the magnet inside the hidden maze in the White Castle.  It's too hard
     * to try to take the magnet across the bridge, so, instead, shove it through a wall to 
     * the hidden maze area.
     * White castle must be unlocked.
     */
    public class PutMagnetInWhiteCastle2 : AiObjective
    {
        private Portcullis whitePort;
        private Magnet magnet;

        /**
         * Shove the magnet through a wall in the white maze to the hidden maze area
         */
        public PutMagnetInWhiteCastle2()
        {
            whitePort = null;
        }

        protected override void initialize()
        {
            whitePort = (Portcullis)board.getObject(Board.OBJECT_WHITE_PORT);
            magnet = (Magnet)board.getObject(Board.OBJECT_MAGNET);
        }

        /**
         * We're assuming the white castle is unlocked.
         * Grab the magnet, take it just inside the white castle, reposition the
         * magnet on the left and shove it into the right wall so it's reachable from the adjacent hallway.
         */
        protected override void doComputeStrategy()
        {
            this.addChild(new ObtainObject(Board.OBJECT_MAGNET));
            this.addChild(new GoTo(Map.RED_MAZE_1, Adv.ADVENTURE_SCREEN_BWIDTH / 2, (int)(Map.WALL_HEIGHT * 2.5)));
            this.addChild(new RepositionObject(Board.OBJECT_MAGNET, RepositionObject.RelativeToBall.LEFT_OF_BALL,
                // Use the large plot just inside the white castle entrance to reposition the magnet
                new RRect(Map.RED_MAZE_1, 8*Map.WALL_WIDTH, 4*Map.WALL_HEIGHT-1, 24*Map.WALL_WIDTH, 3*Map.WALL_HEIGHT)));
            this.addChild(new GoTo(Map.RED_MAZE_1, Map.WALL_WIDTH * 8 + BALL.RADIUS, (int)(Map.WALL_HEIGHT * 2.5)-BALL.RADIUS));
            this.addChild(new DropObjective(Board.OBJECT_MAGNET));
        }

        /**
         * Still valid as long as the white castle is unlocked.
         */
        public override bool isStillValid()
        {
            if ((aiPlayer.room == Map.WHITE_CASTLE) &&
                !whitePort.allowsEntry)
            {
                return false;
            }
            return true;
        }

        /**
         * Completed if magnet is dropped in the hidden maze
         */
        protected override bool computeIsCompleted()
        {
            return (aiPlayer.linkedObject != Board.OBJECT_MAGNET) &&
                (nav.WhichZone(magnet.BRect, NavZone.WHITE_CASTLE_2) == NavZone.WHITE_CASTLE_2);
        }

        public override string ToString()
        {
            string str = "put magnet in hidden white maze ";
            return str;
        }
    }
}