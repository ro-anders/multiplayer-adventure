using System;
using UnityEngine;
using System.Collections.Generic;

namespace GameEngine.Ai
{
    public class AiTactical2: AiTactical
    {
        /** This attempts to find a path to where we going with
         * a depth first search, but it must still return in 1/20th of
         * a second, so limit how many iterations of the search
         * can be performed.
         */
        private static int MAX_ATTEMPTS_PER_TURN = 30;

        enum direction
        {
            UP = 0,
            UPRIGHT,
            RIGHT,
            DOWNRIGHT,
            DOWN,
            DOWNLEFT,
            LEFT,
            UPLEFT
        };

        /** Starting with up and moving clockwise the 8 possible movement directions */
        private static int[][] dirvals = new int[][]{
            new int[]{ 0, BALL.MOVEMENT},
            new int[]{BALL.MOVEMENT, BALL.MOVEMENT },
            new int[]{BALL.MOVEMENT, 0 },
            new int[]{BALL.MOVEMENT, -BALL.MOVEMENT },
            new int[]{ 0, -BALL.MOVEMENT },
            new int[]{ -BALL.MOVEMENT, -BALL.MOVEMENT },
            new int[]{ -BALL.MOVEMENT, 0 },
            new int[]{ -BALL.MOVEMENT, BALL.MOVEMENT }
        };

        /** The ball that is being moved */
        private BALL thisBall;

        /** The game board */
        private Board board;

        /** The last path we were trying to traverse */
        private AiPathNode currentPath;

        /** 
         * We figure out several moves at a time.
         */
        private bool recomputeSteps = true;
        private AiPathNode lastComputedPath = null;
        private RRect lastComputedFinalDest = RRect.NOWHERE;


        public AiTactical2(BALL inBall, Board inBoard)
        {
            thisBall = inBall;
            board = inBoard;
        }

        /**
         * Less desired form.  Just calls form asking for final RRect
         * */
        public override bool computeDirectionOnPath(AiPathNode path,
            int finalBX, int finalBY, AiObjective currentObjective,
            ref int nextVelx, ref int nextVely)
        {
            return computeDirectionOnPath(path,
                new RRect(path.End.ThisPlot.Room, finalBX, finalBY, 1, 1),
                currentObjective.getDesiredObject(),
                ref nextVelx, ref nextVely);
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
        public override bool computeDirectionOnPath(AiPathNode path,
                in RRect finalTargetB, int desiredObject,
                ref int nextVelx, ref int nextVely)
        {

            //// We may have already computed the path across the plot.
            //// See if we need to recompute it.
            //if (recomputeSteps) {
            //    RRect nextStepTarget = (path.nextNode == null ? finalTargetB : path.ThisPlot.GetOverlapSegment(path.nextNode.ThisPlot, path.nextDirection));
            //    SortedDictionary<int, int> attemptedSteps = new SortedDictionary<int, int>();
            //    int distance = computeDistance(thisBall.midX, thisBall.midY, nextStepTarget);
            //    int key = computeKey(distance, thisBall.midX, thisBall.midY);
            //    int value = computeValue(0, ???, new direction[0]);
            //    attemptedSteps.Add(key, value);
            //    bool found = distance == 0;
            //    for(int attempt_ctr=0; !found && attempt_ctr < MAX_ATTEMPTS_PER_TURN; ++attempt_ctr)
            //    {
            //        found = attemptNextStep(attemptedSteps);
            //    }
            //    // Take whatever steps we've attempted and turn it into
            //    // a path of steps to follow
            //    direction[] steps_to_follow = getBestSteps(attemptedSteps);
            //}

            return false;
        }

        /**
         * If a dragon is currently biting us, take evasive action.
         */
        public override bool avoidBeingEaten(ref int nextVelX, ref int nextVelY, int nextStepX, int nextStepY, int desiredObject = AiObjective.DONT_CARE_OBJECT)
        {
            return false;
        }

    }
}
