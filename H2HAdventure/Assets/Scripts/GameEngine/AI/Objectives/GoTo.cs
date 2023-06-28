using System;

namespace GameEngine.Ai
{


    public class GoTo : AiObjective
    {
        private RRect btarget;
        private int carrying;
        private Portcullis behindPortcullis = null; // If the target room is behind a Portcullis

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

        protected override void doComputeStrategy()
        {
            behindPortcullis = GoTo.isBehindPortcullis(board, aiPlayer, btarget.room);
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
         * Return if a desired room is behind a portcullis.
         * If you are also behind the same portcullis then this returns that it is not
         * behind a portcullis.
         * @param board the board
         * @param ball the player
         * @param targetRoom the rooom of interest
         * @returns the portcullis that stands between the ball and the room or null
         * if none does
         */
        public static Portcullis isBehindPortcullis(Board board, BALL ball, int targetRoom)
        {
            Portcullis targetPort = null;
            Portcullis myPort = null;
            // Figure out if the desired room is behind a locked gate
            int FIRST_PORT = Board.OBJECT_YELLOW_PORT;
            int LAST_PORT = Board.OBJECT_CRYSTAL_PORT;
            for (int portNum = FIRST_PORT; portNum <= LAST_PORT; ++portNum)
            {
                Portcullis port = (Portcullis)board.getObject(portNum);
                targetPort = ((targetPort == null) && port.containsRoom(targetRoom) ? port : targetPort);
                myPort = ((myPort == null) && port.containsRoom(ball.room) ? port : myPort);
            }
            return (targetPort == myPort ? null : targetPort);
        }
    }
}
