
namespace GameEngine.Ai
{

    /**
     * Place an object at a specific position.  This is a low-level objective 
     * that not only assumes you have the object but assumes you are 
     * very close to the needed place to drop the object and at least in the same room.
     **/
    public class PlaceObjectAt : AiObjective
    {
        private int objectKey;
        private OBJECT objectToPlace;
        private int placeAtRoom;
        private int placeAtBX;
        private int placeAtBY;
        private BALL.Adjust adjust;
        private int desiredBallBX;
        private int desiredBallBY;

        /**
         * Put an object down at a specified position
         * @param inObject the key to the object that needs placing
         * @param inRoom the room to place it
         * @param inBX the x position to place the object (note that placement
         *   may not be exact due to step size of the ball)
         * @param inBY the y position to place the object (note that placement
         *   may not be exact due to step size of the ball)
         * @param if the object cannot be placed at the exact location, how to 
         *   choose the best possible close location.  Default is the closest 
         *   possible location.
         */
        public PlaceObjectAt(int inObject, int inRoom, int inBX, int inBY, BALL.Adjust inAdjust = BALL.Adjust.CLOSEST)
        {
            objectKey = inObject;
            objectToPlace = null; // Set at compute time
            placeAtRoom = inRoom;
            placeAtBX = inBX;
            placeAtBY = inBY;
            adjust = inAdjust;
        }

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            objectToPlace = board.getObject(objectKey);
            desiredBallBX = placeAtBX - aiPlayer.linkedObjectBX;
            desiredBallBY = placeAtBY - aiPlayer.linkedObjectBY;
            // Adjust where we're going if it falls between steps
            aiPlayer.adjustDestination(ref desiredBallBX, ref desiredBallBY, adjust);
            placeAtBX = desiredBallBX + aiPlayer.linkedObjectBX;
            placeAtBY = desiredBallBY + aiPlayer.linkedObjectBY;
        }

        protected override void doComputeStrategy()
        {
            this.addChild(new GoExactlyTo(placeAtRoom, desiredBallBX, desiredBallBY, objectKey));
            this.addChild(new DropObjective(objectKey));
        }

        public override bool isStillValid()
        {
            // Still valid as long as we're in the right room and holding the object
            return (aiPlayer.room == placeAtRoom) && (aiPlayer.linkedObject == objectKey);
        }

        /**
         * Return if the bridge actually has one end in the main or white_castle_1
         * zone and the other end in the white_castle_2 zone.
         */
        protected override bool computeIsCompleted()
        {
            /** This is completed when the object is no longer held and
             * the object is within acceptable range of the target coordinates.
             */
            return (aiPlayer.linkedObject != objectToPlace.getPKey()) &&
                (objectToPlace.bx == placeAtBX) &&
                (objectToPlace.by == placeAtBY);
        }

        public override string ToString()
        {
            return "place  " + objectToPlace.label + " at (" + placeAtBX + "," + placeAtBY + ") in " + board.map.getRoom(placeAtRoom).label;
        }

    }


}