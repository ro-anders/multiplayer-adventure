using System;

namespace GameEngine.Ai
{
    public abstract class AiTactical
    {
        /**
         * Get an AiTactical object.  Whichever implementation we're using right now.
         * @param inBall - the AI Player this is providing tactical guidance on
         * @param inBoard - the board
         */
        public static AiTactical get(BALL inBall, Board inBoard)
        {
            if (!AiTacticalTests.tests_run)
            {
                AiTacticalTests tests = new AiTacticalTests();
                tests.testAll();
            }
            AiTactical tactical = new AiTactical1(inBall, inBoard);
            return tactical;
        }

        /**
         * This is the main function of the AiTactical's ability to get your 
         * from one point to another.  Given a path and coordinates at the end 
         * of the path, figure out the direction we should move to get us there.
         * @param path the path to get to where we want to go
         * @param finalTargetB the area we want to get to in the end
         * @param desiredObject the object we are carrying or want to be 
         *   carrying.  We will avoid connecting with other objects if we are 
         *   intentionally carrying an object.  DONT_CARE_OBJECT to indicate
         *   we don't care if we connect with an object along the way or 
         *   CARRY_NO_OBJECT to indicate we don't want to connect with 
         *   any object along the way.
         */
        public abstract bool computeDirectionOnPath(AiPathNode path,
            in RRect finalTargetB, int desiredObject,
            ref int nextVelx, ref int nextVely);

        /**
         * Another form of the main function 
         */
        public abstract bool computeDirectionOnPath(AiPathNode path, int finalBX, int finalBY, AiObjective currentObjective,
            ref int nextVelx, ref int nextVely);

        /**
         * If a dragon is currently biting us, take evasive action.
         */
        public abstract bool avoidBeingEaten(ref int nextVelX, ref int nextVelY, int nextStepX, int nextStepY, int desiredObject = AiObjective.DONT_CARE_OBJECT);

    }
}
