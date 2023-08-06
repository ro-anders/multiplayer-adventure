using System;

namespace GameEngine.Ai
{
    public abstract class AiTactical
    {
        static private bool testsRun = false;

        /**
         * Get an AiTactical object.  Whichever implementation we're using right now.
         * @param inBall - the AI Player this is providing tactical guidance on
         * @param inBoard - the board
         */
        public static AiTactical get(BALL inBall, Board inBoard)
        {
            if (!testsRun)
            {
                testsRun = true;
                AiTacticalTests tests = new AiTacticalTests();
                tests.testAll();
            }
            return new AiTactical1(inBall, inBoard);
        }

        /**
         * This is the main function of the AiTactical's ability to get your 
         * from one point to another.  Given a path and coordinates at the end 
         * of the path, figure out the direction we should move to get us there.
         */
        public abstract bool computeDirectionOnPath(AiPathNode path, int finalX, int finalY, AiObjective currentObjective,
                    ref int nextVelx, ref int nextVely);
    }
}
