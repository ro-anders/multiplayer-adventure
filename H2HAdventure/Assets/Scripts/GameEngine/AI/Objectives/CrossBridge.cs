
namespace GameEngine.Ai
{

    /**
     * Walk across the bridge.  This assumes the bridge is very close (at most
     * a couple of plots away and definitely in the same room)
     **/
    public class CrossBridge : AiObjective
    {
        private bool upOrDown; // True if going up across the bridge.  False is going down across the bridge.
        private int carrying;
        private Bridge bridge;
        private RRect startingBLocation;
        private GoTo goToStart = null;

        /**
         * Walk over the bridge.
         * @param inInOut True if we want to get into the hidden room, false if 
         * we want to get out of the hidden room
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public CrossBridge(bool inUpOrDown, int inCarrying = DONT_CARE_OBJECT)
        {
            upOrDown = inUpOrDown;
            carrying = inCarrying;
            bridge = null;
            startingBLocation = RRect.NOWHERE;
        }

        protected override void doComputeStrategy()
        {
            bridge = (Bridge)board.getObject(Board.OBJECT_BRIDGE);
            startingBLocation = bridge.BRect;

            // This may be called while we're actually in the middle of the bridge.
            // Don't bother going to the entrance of the bridge if we're already on it.
            if (!bridge.InsideBRect.overlaps(aiPlayer.BRect))
            {
                // Put the ball directly under/over the bridge
                int desiredBX = bridge.BLeft + (bridge.bwidth) / 2;
                int desiredBY = (upOrDown ? bridge.BBottom - BALL.RADIUS - 1 : bridge.BTop + BALL.RADIUS + 1);
                // To avoid hitting the bridge we don't pick up any new objects while
                // executing this objective
                int goToCarrying = carrying == DONT_CARE_OBJECT ? CARRY_NO_OBJECT : carrying;
                goToStart = new GoTo(bridge.room, desiredBX, desiredBY, goToCarrying);
                this.addChild(goToStart);
            }
            // Once the child is satisfied, the shouldMoveDirection kicks in.
        }

        /**
         * If we see that the bridge moves at all while carrying this out we abort 
         * or we drop what we are supposed to be carrying
         */
        public override bool isStillValid()
        {
            RRect currentBLocation = bridge.BRect;
            bool still_carrying =
                (carrying == DONT_CARE_OBJECT) ||
                ((carrying == CARRY_NO_OBJECT) && (aiPlayer.linkedObject == Board.OBJECT_NONE)) ||
                (aiPlayer.linkedObject == carrying);
            return currentBLocation.equals(startingBLocation) && still_carrying;
        }

        protected override bool computeIsCompleted()
        {
            RRect playerBrect = aiPlayer.BRect;
            bool still_carrying =
                (carrying == DONT_CARE_OBJECT) ||
                ((carrying == CARRY_NO_OBJECT) && (aiPlayer.linkedObject == Board.OBJECT_NONE)) ||
                (aiPlayer.linkedObject == carrying);
            // Only completed if we're within the column of the bridge
            bool withinColumn = playerBrect.room == bridge.room &&
                    playerBrect.left >= bridge.InsideBLeft &&
                    playerBrect.right <= bridge.InsideBRight;
            if (upOrDown)
            {
                // Completed if we are directly above the bridge.
                return playerBrect.bottom > bridge.BTop && withinColumn && still_carrying;
            }
            else
            {
                // Completed if we are directly below the bridge.
                return playerBrect.top < bridge.BBottom && withinColumn && still_carrying;
            }
        }

        public override bool shouldMoveDirection(ref int velbx, ref int velby)
        {
            if ((goToStart != null) && goToStart.isCompleted()) {
                velbx = 0;
                velby = (upOrDown ? BALL.MOVEMENT : -BALL.MOVEMENT);
                return true;
            } else
            {
                return false;
            }
        }

        public override string ToString()
        {
            string str = "walk " + (upOrDown ? "up " : "down ") + "the bridge";
            if (carrying == CARRY_NO_OBJECT)
            {
                str += " carrying nothing";
            }
            else
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