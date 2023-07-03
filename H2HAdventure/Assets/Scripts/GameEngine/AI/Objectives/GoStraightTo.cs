using System;

namespace GameEngine.Ai
{


    /**
     * Objective to go to coordinates or an area.  This is a low level objective
     * and should only be called when in the same zone as the coordinates.
     */
    public class GoStraightTo : AiObjective
    {
        private RRect btarget;
        private int carrying;
        private Portcullis behindPortcullis = null; // If the target room is behind a Portcullis

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
        public GoStraightTo(in RRect inBTarget, int inCarrying = DONT_CARE_OBJECT)
        {
            btarget = inBTarget;
            carrying = inCarrying;
        }

        protected override void doComputeStrategy()
        {
            // Make sure we're in the same zone
            NavZone currentZone = nav.WhichZone(aiPlayer.BRect);
            NavZone desiredZone = nav.WhichZone(btarget, currentZone);
            if (currentZone != desiredZone)
            {
                throw new Abort("GoStraightTo objective cannot go to zone " + desiredZone + " from zone " + currentZone);
            }

            behindPortcullis = strategy.isBehindPortcullis(btarget.room);
        }

        public override RRect getBDestination()
        {
            return btarget;
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

        protected override bool computeIsCompleted()
        {
            return hasPlayerGotTo(aiPlayer, btarget);
        }

        public static bool hasPlayerGotTo(BALL aiPlayer, in RRect btarget) { 
            if (aiPlayer.room == btarget.room)
            {
                int xBuffer = (BALL.DIAMETER + BALL.MOVEMENT - btarget.width + 1) / 2;
                if (xBuffer < 0)
                {
                    xBuffer = 0;
                }
                else if (Math.Abs(aiPlayer.midY - btarget.midY) <= BALL.MOVEMENT / 2)
                {
                    xBuffer += 2; // 2 to increase the buffer from 3 (BALL.MOVEMENT/2) to 5 (BALL.MOVEMENT-1)
                }
                bool xcheck = (aiPlayer.x >= btarget.left - xBuffer) &&
                    (aiPlayer.x + BALL.DIAMETER <= btarget.right + xBuffer);

                int yBuffer = (BALL.DIAMETER + BALL.MOVEMENT - btarget.height + 1) / 2;
                if (yBuffer < 0)
                {
                    yBuffer = 0;
                }
                else if (Math.Abs(aiPlayer.midX - btarget.midX) <= BALL.MOVEMENT / 2)
                {
                    yBuffer += 2; // 2 to increase the buffer from 3 (BALL.MOVEMENT/2) to 5 (BALL.MOVEMENT-1)
                }
                bool ycheck = (aiPlayer.y - BALL.DIAMETER >= btarget.bottom - yBuffer) &&
                    (aiPlayer.y <= btarget.top + yBuffer);
                return xcheck && ycheck;
            }
            else
            {
                return false;
            }
        }

        public override string ToString()
        {
            string str = "go straight to " + btarget.ToStringWithRoom(board.map.roomDefs[btarget.room].label);
            if (carrying == CARRY_NO_OBJECT)
            {
                str += " carrying nothing";
            }
            else if (carrying != DONT_CARE_OBJECT)
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
