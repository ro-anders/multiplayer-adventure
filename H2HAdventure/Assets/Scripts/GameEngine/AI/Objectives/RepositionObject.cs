namespace GameEngine.Ai
{
    /**
     * Make sure we are holding an object in a certain position (e.g. "above us"
     * or "on our left").  If needed, drop the object, move around it, and pick 
     * it back up.
     */
    public class RepositionObject : AiObjective
    {

        public enum RelativeToBall
        {
            ABOVE_BALL,
            BELOW_BALL,
            LEFT_OF_BALL,
            RIGHT_OF_BALL
        }

        private const int KEY_WIDTH = 8;
        private const int KEY_HEIGHT = 3;
        private int objectId;
        private RelativeToBall relativePosition;
        private RRect repoAreaBrect;
        private OBJECT obj;
        private const int CASTLE_FOOT = 0x40; // The Y coordinate of the bottom of the castle

        /**
         * Make sure we are holding an object in a certain position.
         * @param inObjectId the id of the object we must reposition.  Should be holding the object
         * @param inRelativePosition how we want to be holding the object
         * @param inRepositionArea an open area big enough to drop the object and get around it
         */
        public RepositionObject(int inObjectId, RelativeToBall inRelativePosition, RRect inRepoAreaBrect)
        {
            objectId = inObjectId;
            relativePosition = inRelativePosition;
            repoAreaBrect = inRepoAreaBrect;

        }

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            obj = board.getObject(objectId);
        }


        public override bool isStillValid()
        {
            // Still valid if we are holding the object or the
            // object is still in the room with us
            return ((aiPlayer.linkedObject == objectId) ||
                (aiPlayer.room == obj.room));
        }

        protected override void doComputeStrategy()
        {
            if (aiPlayer.linkedObject != objectId)
            {
                throw new Abort();
            }
            else
            {
                // Drop the object in the center of the reposition area.
                int bxToDropFrom = aiPlayer.getSteppedBX(repoAreaBrect.midX - obj.bwidth / 2 - aiPlayer.linkedObjectBX);
                int byToDropFrom = aiPlayer.getSteppedBY(repoAreaBrect.midY - obj.BHeight / 2 - aiPlayer.linkedObjectBY);
                this.addChild(new GoExactlyTo(aiPlayer.room, bxToDropFrom, byToDropFrom, objectId));
                this.addChild(new DropObjective(objectId));

                // Pick a point on the correct side of the object and let the tactical algorithms get around the object
                int bxToPickupFrom = 0;
                int byToPickupFrom = 0;
                switch (relativePosition)
                {
                    case RelativeToBall.BELOW_BALL:
                        bxToPickupFrom = aiPlayer.getSteppedBX(obj.BRect.midX - BALL.RADIUS);
                        byToPickupFrom = aiPlayer.getSteppedBY(obj.BTop + BALL.DIAMETER, BALL.STEP_ALG.GTE);
                        break;
                    case RelativeToBall.ABOVE_BALL:
                        bxToPickupFrom = aiPlayer.getSteppedBX(obj.BRect.midX - BALL.RADIUS);
                        byToPickupFrom = aiPlayer.getSteppedBY(obj.BBottom + 1, BALL.STEP_ALG.LTE);
                        break;
                    case RelativeToBall.RIGHT_OF_BALL:
                        bxToPickupFrom = aiPlayer.getSteppedBX(obj.BLeft - BALL.DIAMETER, BALL.STEP_ALG.LTE);
                        byToPickupFrom = aiPlayer.getSteppedBY(obj.BRect.midY + BALL.RADIUS);
                        break;
                    case RelativeToBall.LEFT_OF_BALL:
                    default:
                        bxToPickupFrom = aiPlayer.getSteppedBX(obj.BRight + 1, BALL.STEP_ALG.GTE);
                        byToPickupFrom = aiPlayer.getSteppedBY(obj.BRect.midY + BALL.RADIUS);
                        break;
                }
                this.addChild(new GoExactlyTo(aiPlayer.room, bxToPickupFrom, byToPickupFrom, CARRY_NO_OBJECT));
                this.addChild(new PickupObject(objectId));
            }
        }

        protected override bool computeIsCompleted()
        {
            if (aiPlayer.linkedObject != objectId)
            {
                return false;
            }

            bool inPosition;
            switch (relativePosition)
            {
                case RelativeToBall.ABOVE_BALL:
                    inPosition = obj.BBottom > aiPlayer.BTop;
                    break;
                case RelativeToBall.BELOW_BALL:
                    inPosition = obj.BTop < aiPlayer.BBottom;
                    break;
                case RelativeToBall.LEFT_OF_BALL:
                    inPosition = obj.BRight < aiPlayer.BLeft;
                    break;
                case RelativeToBall.RIGHT_OF_BALL:
                default:
                    inPosition = obj.BLeft > aiPlayer.BRight;
                    break;
            }
            return inPosition;
        }

        public override string ToString()
        {
            return "reposition " + (obj != null ? obj.label : board.getObject(aiPlayer.linkedObject).label) + " " + relativePosition + " ball";
        }
    }
}
