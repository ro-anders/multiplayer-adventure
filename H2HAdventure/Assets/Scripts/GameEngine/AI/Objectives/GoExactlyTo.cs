namespace GameEngine.Ai
{

    /**
     * Go to these exact coordinates.  Only to be used when you've 
     * calculated that these coordinates can be reached given the balls
     * current coordinates and 6 pixel step.
     * Which means only to be used when the destination is in the same room
     * as the ball
     */
    public class GoExactlyTo : AiObjective
    {
        private RRect btarget;
        private int carrying;

        /**
         * Go to these coordinates.
         * @param inRoom the desired room
         * @param inX the desired X of the ball (meaning the left coordinate)
         * @param inY the desired Y of the ball (meaning the top coordinate)
         * @param inCarrying the object you want to carry or CARRY_NO_OBJECT if you
         * specifically don't want to pick up an object or DONT_CARE_OBJECT if you
         * don't care if you pick up an object or not
         */
        public GoExactlyTo(int inRoom, int inLeftBX, int inTopBY, int inCarrying = DONT_CARE_OBJECT)
        {
            btarget = new RRect(inRoom, inLeftBX, inTopBY, BALL.DIAMETER, BALL.DIAMETER);
            carrying = inCarrying;
        }

        protected override void doComputeStrategy()
        {
            // Objective is target
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
            return btarget.room == aiPlayer.room;
        }


        protected override bool computeIsCompleted()
        {
            return (aiPlayer.x == btarget.left) && (aiPlayer.y == btarget.top);
        }

        public override string ToString()
        {
            string str = "go to exact spot " + btarget.ToStringWithRoom(board.map.roomDefs[btarget.room].label);
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