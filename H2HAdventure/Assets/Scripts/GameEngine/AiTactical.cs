﻿using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameEngine
{
    public class AiTactical
    {
        static private bool testsRun = false;

        /** The ball that is being moved */
        private BALL thisBall;

        /** The game board */
        private Board board;

        private AiPathNode currentPath;
        private int nextStepX;
        private int nextStepY;

        /** Whether to turn clockwise or counter-clockwise when going around
         * an object. 
         * Note that if you are turning clockwise you tend to go around in a
         * conter-clockwise arc. */
        enum Turn
        {
            CLOCKWISE,
            NONE,
            COUNTERCLOCKWISE
        }

        private static int[][] directions = new int[][]{
            new int[]{ 0, BALL.MOVEMENT},
            new int[]{BALL.MOVEMENT, BALL.MOVEMENT },
            new int[]{BALL.MOVEMENT, 0 },
            new int[]{BALL.MOVEMENT, -BALL.MOVEMENT },
            new int[]{ 0, -BALL.MOVEMENT },
            new int[]{ -BALL.MOVEMENT, -BALL.MOVEMENT },
            new int[]{ -BALL.MOVEMENT, 0 },
            new int[]{ -BALL.MOVEMENT, BALL.MOVEMENT }
        };


        public AiTactical(BALL inBall, Board inBoard)
        {
            thisBall = inBall;
            board = inBoard;
            if (!testsRun) {
                testsRun = true;
                AiTacticalTests tests = new AiTacticalTests();
                tests.testAll();
            }
        }

        public bool computeDirectionOnPath(AiPathNode path, int finalX, int finalY, AiObjective currentObjective,
            ref int nextVelx, ref int nextVely)
        {
            // Go to the nextStep coordinates to get us to the next step on the path
            // or the desired coordinates if we're at the end of the path.
            if (path != currentPath)
            {
                currentPath = path;

                // New path or reached new step in path.
                // Recompute how to get to the next step in the path
                if (currentPath.nextNode != null)
                {
                    currentPath.ThisPlot.GetOverlapMidpoint(currentPath.nextNode.ThisPlot,
                        currentPath.nextDirection, ref nextStepX, ref nextStepY);
                    smoothMovement(ref nextStepX, ref nextStepY, currentPath.nextDirection);
                }
            }

            if (currentPath.nextNode == null)
            {
                // We've reached the last plot in the path.  
                // Now go to the desired coordinates
                nextStepX = finalX;
                nextStepY = finalY;
            }

            bool allowScrapingWalls = (currentPath.nextNode == null); // Don't worry about scraping walls once we're in the right plot
            bool canGetThere = computeDirection(nextStepX, nextStepY, currentObjective.getDesiredObject(), allowScrapingWalls, ref nextVelx, ref nextVely);

            return canGetThere;
        }

        /**
         * Figure out the best direction to go to get to a point in the current room.
         * Usually this just makes a straight line towards it, but will choose alternate
         * directions to deal with dragons or impeding obstacles.
         */
        private bool computeDirection(int nextStepX, int nextStepY, int desiredObject, bool allowScrapingWalls, ref int nextVelX, ref int nextVelY)
        {
            nextVelX = (nextStepX > thisBall.midX ? BALL.MOVEMENT : (nextStepX == thisBall.midX ? 0 : -BALL.MOVEMENT));
            nextVelY = (nextStepY > thisBall.midY ? BALL.MOVEMENT : (nextStepY == thisBall.midY ? 0 : -BALL.MOVEMENT));

            if (!allowScrapingWalls)
            {
                avoidScrapingWalls(ref nextVelX, ref nextVelY);
            }

            avoidAllObjects(desiredObject, ref nextVelX, ref nextVelY);

            if ((nextVelX != thisBall.velx) || (nextVelY != thisBall.vely))
            {
                //UnityEngine.Debug.Log("Changing (" + thisBall.velx + "," + thisBall.vely +
                //    ") to (" + nextVelX + ", " + nextVelY +
                //    ") at " + thisBall.room + "-(" + thisBall.midX + "," + thisBall.midY + ")");
            }
            return true;
        }

        // This handles running up against walls when we actually just want to move
        // along beside them until we can get around them.
        private void avoidScrapingWalls(ref int nextVelX, ref int nextVelY)
        {
            if ((nextVelX != 0) && (nextVelY != 0))
            {
                if (quickCheckWall(thisBall.x + nextVelX, thisBall.y + nextVelY, thisBall.room))
                {
                    if (!quickCheckWall(thisBall.x, thisBall.y + nextVelY, thisBall.room))
                    {
                        nextVelX = 0;
                    }
                    else if (!quickCheckWall(thisBall.x + nextVelX, thisBall.y, thisBall.room))
                    {
                        nextVelY = 0;
                    }
                }
            }
        }

        /**
         * Check to see if our chosen direction will hit any objects, and, if it will,
         * choose a new direction.
         * @param desiredObject the object we would like (and don't mind running into) or
         * AiObjective.DONT_CARE_OBJECT or AiObjective.CARRY_NO_OBJECT
         * @param nextVelX the X velocity of the direction we plan on going.  Will be modified
         * to avoid objects
         * @param nextVelY the X velocity of the direction we plan on going.  Will be modified
         * to avoid objects
         */
        private void avoidAllObjects(int desiredObject, ref int nextVelX, ref int nextVelY)
        {
            // Collect a list of objects that may be in the way
            // TODO: Not sure how to handle portcullis's and the Robinett message,
            // so for right now just worrying about dragons and objects you can
            // pickup
            Board.ObjIter iter = board.getMovableObjects();
            while (iter.hasNext())
            {
                OBJECT objct = iter.next();
                if (objct.room == thisBall.room)
                {
                    int pkey = objct.getPKey();
                    // Ignore stalking dragons.  We deal with them differently.
                    bool ignore = ((pkey >= Board.FIRST_DRAGON) && (pkey <= Board.LAST_DRAGON) &&
                        (objct.state == Dragon.STALKING));
                    // Ignore objects we are currently holding or desiring
                    ignore = ignore || ((pkey == desiredObject) || (pkey == thisBall.linkedObject));
                    // Ignore any objects that can be picked up when we don't care or when we
                    // are looking for some other object (and don't care if we pick up a different one
                    // along the way)
                    bool dontCare = (desiredObject == AiObjective.DONT_CARE_OBJECT) ||
                        ((desiredObject > Board.OBJECT_NONE) && (desiredObject != thisBall.linkedObject));
                    ignore = ignore || ((pkey >= Board.FIRST_CARRYABLE) && dontCare);
                    if (!ignore)
                    {
                        // TODO: This isn't handling bridge correctly
                        if ((objct.room == thisBall.room) &&
                            quickCheckCollision(thisBall.x + nextVelX, thisBall.y + nextVelY, objct))
                        {
                            // Ball would connect with object next turn, figure a different direction
                            avoidObject(objct, ref nextVelX, ref nextVelY);
                            break;
                        }
                    }
                }
            }
        }

        /**
         * Check if a ball at a given position will collide with a wall.
         * @param ballx the x position of the ball (the top left corner)
         * @param bally the y position of the ball (the top left corner)
         * @param roomNum the room in which to check
         */
        private bool quickCheckWall(int ballx, int bally, int roomNum)
        {
            ROOM room = board.map.roomDefs[roomNum];
            return room.hitsWall(ballx, bally, BALL.DIAMETER, BALL.DIAMETER);
        }

        /**
         * Check if a ball at a given position will collide with an object.
         * This just checks the object's bounding box, it doesn't do pixel level
         * collision checking.
         * @param ballx the x position of the ball (the top left corner)
         * @param bally the y position of the ball (the top left corner)
         * @param objct the object to check
         */
        private bool quickCheckCollision(int ballx, int bally, OBJECT objct)
        {
            // TODO: This doesn't handle bridge correctly
            int objectHt = objct.Height;
            return Board.HitTestRects(ballx, bally, BALL.DIAMETER, BALL.DIAMETER,
              objct.bx, objct.by, objct.bwidth, objct.BHeight);
        }

        /**
         * Desired movement would cause ball to hit object.  Figure out a direction to 
         * avoid collision and still get to destination.
         */
        public void avoidObject(OBJECT objct, ref int nextVelX, ref int nextVelY)
        {
            int blockedTop = 0, blockedRight = 0, blockedBottom = 0, blockedLeft = 0;
            computeBlockedArea(objct, ref blockedTop, ref blockedRight, ref blockedBottom, ref blockedLeft);
            Turn toTurn = whichDirection(blockedTop, blockedRight, blockedBottom, blockedLeft);

            if (toTurn != Turn.NONE)
            {

                int startDirection = velocityToDirection(nextVelX, nextVelY);
                int step = (toTurn == Turn.CLOCKWISE ? +1 : -1);
                int nextDir = startDirection;
                bool foundClearDirection = false;
                for (int ctr = 1; !foundClearDirection && (ctr < directions.Length); ++ctr)
                {
                    nextDir = MOD.mod(nextDir + step, directions.Length);
                    if (!quickCheckCollision(thisBall.x + directions[nextDir][0], thisBall.y + directions[nextDir][1], objct))
                    {
                        nextVelX = directions[nextDir][0];
                        nextVelY = directions[nextDir][1];
                        foundClearDirection = true;
                    }
                }
            }
        }

        /**
         * Maps a x,y velocity to a 8-point direction
         * Returns 0=up, 1=up&right, 2=right,... 7=up&left.  -1 means it didn't map.
         */
        private int velocityToDirection(int velX, int velY)
        {
            int start = -1;
            for (int ctr = 0; (start == -1) && (ctr < directions.Length); ++ctr)
            {
                if ((directions[ctr][0] == velX) && (directions[ctr][1] == velY))
                {
                    start = ctr;
                }
            }
            return start;
        }

        /**
         * Compute how much an object actually blocks a ball.  For example, a ball cannot
         * get within 4 pixels of an object because of its own width.  Also takes into 
         * account ball moving in quantum steps and other blocking objects touching this objects.
         */
        private void computeBlockedArea(OBJECT objct, ref int top, ref int right, ref int bottom, ref int left)
        {
            top = objct.by + BALL.DIAMETER;
            right = objct.bx + objct.bwidth - 1;
            bottom = objct.by - objct.BHeight + 1;
            left = objct.bx - BALL.DIAMETER;

            // Now factor in that ball moves in 6 pixel steps
            top += MOD.mod(thisBall.y - top, BALL.MOVEMENT);
            right += MOD.mod(thisBall.x - right + 1, BALL.MOVEMENT);
            bottom -= MOD.mod(bottom - 1 - thisBall.y, BALL.MOVEMENT);
            left -= MOD.mod(left - thisBall.x, BALL.MOVEMENT);
        }

        /**
         * Compute how much of a plot is actually available to a ball.  For example, a ball cannot
         * get within 4 pixels of a wall because of its own width.  Also takes into 
         * account ball moving in quantum steps.
         */
        private void computeAvailablePlot(Plot plot, ref int top, ref int right, ref int bottom, ref int left)
        {
            top = plot.Top;
            right = plot.Right - BALL.DIAMETER + 1;
            bottom = plot.Bottom + BALL.DIAMETER - 1;
            left = plot.Left;

            // Now factor in that ball moves in 6 pixel steps
            top -= MOD.mod(top - thisBall.y, BALL.MOVEMENT);
            right -= MOD.mod(right - thisBall.x, BALL.MOVEMENT);
            bottom += MOD.mod(thisBall.y - bottom, BALL.MOVEMENT);
            left += MOD.mod(thisBall.x - left, BALL.MOVEMENT);
        }

        /**
         * Compute how much of an exit is actually available to a ball.  For example, a ball cannot
         * get within 4 pixels of a wall because of its own width.  Also takes into 
         * account ball moving in quantum steps.
         */
        private void computeAvailableExit(ref int top, ref int right, ref int bottom, ref int left)
        {
            // An exit is either horizontal (top==bottom) or vertical (left==right)
            if (top == bottom) // horizontal
            {
                right = right - BALL.DIAMETER + 1;

                // Now factor in that ball moves in 6 pixel steps
                right -= MOD.mod(right - thisBall.x, BALL.MOVEMENT);
                left += MOD.mod(thisBall.x - left, BALL.MOVEMENT);
            }
            else // vertical
            {
                bottom = bottom + BALL.DIAMETER - 1;

                // Now factor in that ball moves in 6 pixel steps
                top -= MOD.mod(top - thisBall.y, BALL.MOVEMENT);
                bottom += MOD.mod(thisBall.y - bottom, BALL.MOVEMENT);
            }
        }

        /**
         * When the ball has hit an object which direction should the ball turn to go around the object
         * @param upEdge, rightEdge, downEdge, leftEdge the x,y values of the blocked area (will be bigger
         * than the x,y edges of the object and may be an area that is the union of multiple objects)
         * @return clockwise or counterclockwise or none if neither direction will work
         */
        private Turn whichDirection(int upEdge, int rightEdge, int downEdge, int leftEdge)
        {
            int plotTop = 0, plotRight = 0, plotBottom = 0, plotLeft = 0;
            computeAvailablePlot(currentPath.ThisPlot, ref plotTop, ref plotRight, ref plotBottom, ref plotLeft);

            if (currentPath.nextNode != null)
            {
                int exit1x = 0, exit1y = 0, exit2x = 0, exit2y = 0;
                currentPath.ThisPlot.GetOverlapSegment(currentPath.nextNode.ThisPlot, currentPath.nextDirection,
                    ref exit1x, ref exit1y, ref exit2x, ref exit2y);
                computeAvailableExit(ref exit2y, ref exit2x, ref exit1y, ref exit1x);

                // Everything is in terms of where you are going, so to make calculations
                // easier, rotate everything until your destination is down.
                rotate(currentPath.nextDirection, Plot.DOWN, ref upEdge, ref rightEdge, ref downEdge, ref leftEdge);
                rotate(currentPath.nextDirection, Plot.DOWN, ref plotTop, ref plotRight, ref plotBottom, ref plotLeft);
                rotate(currentPath.nextDirection, Plot.DOWN, ref exit2y, ref exit2x, ref exit1y, ref exit1x);
                int ballx = thisBall.midX;
                int bally = thisBall.midY;
                rotate(currentPath.nextDirection, Plot.DOWN, ref ballx, ref bally);
                return whichDirectionToExit(upEdge, rightEdge, downEdge, leftEdge,
                                            plotTop, plotRight, plotBottom, plotLeft,
                                            exit2x, exit1x, ballx, bally);
            }
            else
            {
                // TODOXXX: Handle when the blocking object is in the same room.
                return Turn.COUNTERCLOCKWISE;
            }
        }

        /**
         * When the ball has hit an object which direction should the ball turn to go around the object.
         * This is specifically called in the case when we are in the middle of a path and trying to reach the next step
         * in the path
         * @param upEdge, rightEdge, downEdge, leftEdge the x,y values of the blocked area (will be bigger
         * than the x,y edges of the object and may be an area that is the union of multiple objects)
         * @param plotTop, plotRight, plotDown, plotLeft the plot that we are currently in (rotated
         * so that we are trying to reach the bottom of it)
         * @param exitRight, exitLeft the x values of the exit from the plot on the bottom edge
         * @param ballx, bally the coordinate of the ball
         * @return clockwise or counterclockwise or none if neither direction will work
         */
        private Turn whichDirectionToExit(int upEdge, int rightEdge, int downEdge, int leftEdge,
                int plotTop, int plotRight, int plotBottom, int plotLeft,
                int exitRight, int exitLeft, int ballx, int bally)
        {
            bool cwOkay = true;
            bool ccwOkay = true;

            // Does the blocked area touch a side?
            cwOkay = cwOkay && (leftEdge > plotLeft);
            ccwOkay = ccwOkay && (rightEdge < plotRight);

            // Does the blocked area reach the bottom?  If it does,
            // got a lot to check to see if either turn will work.
            if (downEdge <= plotBottom)
            {
                cwOkay = cwOkay && (leftEdge > exitLeft);
                ccwOkay = ccwOkay && (rightEdge < exitRight);
            }

            // Does the blocked area touch the top?
            if (upEdge >= plotTop)
            {
                int blockMiddle = (rightEdge + leftEdge) / 2;
                cwOkay = cwOkay && (ballx <= blockMiddle);
                ccwOkay = ccwOkay && (ballx >= blockMiddle);
            }

            // If both are okay, pick clockwise.
            return (cwOkay ? Turn.CLOCKWISE : (ccwOkay ? Turn.COUNTERCLOCKWISE : Turn.NONE));
        }

        /** Translate the x,y coordinates of a box as if rotating it around the origin */
        private void rotate(int fromDirection, int toDirection, ref int top, ref int right, ref int bottom, ref int left)
        {
            int angle = MOD.mod(toDirection - fromDirection, 4);
            int tmp;
            switch (angle)
            {
                case 0:
                    break;
                case 1:
                    tmp = top;
                    top = -left;
                    left = bottom;
                    bottom = -right;
                    right = tmp;
                    break;
                case 2:
                    tmp = top;
                    top = -bottom;
                    bottom = -tmp;
                    tmp = right;
                    right = -left;
                    left = -tmp;
                    break;
                case 3:
                    tmp = top;
                    top = right;
                    right = -bottom;
                    bottom = left;
                    left = -top;
                    break;
            }
        }

        /** Translate the x,y coordinates of a point as if rotating it around the origin */
        private void rotate(int fromDirection, int toDirection, ref int x, ref int y)
        {
            int angle = MOD.mod(toDirection - fromDirection, 4);
            int tmp;
            switch (angle)
            {
                case 0:
                    break;
                case 1:
                    tmp = x;
                    x = y;
                    y = -tmp;
                    break;
                case 2:
                    x = -x;
                    y = -y;
                    break;
                case 3:
                    tmp = x;
                    x = -y;
                    y = x;
                    break;
            }
        }

        /**
         * To prevent a "drunken walk" pick a target point that the ball will hit and
         * not overshoot with its 6 movement.
         */
        public void smoothMovement(ref int nextStepX, ref int nextStepY, int direction)
        {
            switch (direction)
            {
                case Plot.UP:
                case Plot.DOWN:
                    int diff = (nextStepX - thisBall.midX) % BALL.MOVEMENT;
                    if (diff > BALL.MOVEMENT / 2)
                    {
                        diff -= BALL.MOVEMENT;
                    }
                    else if (diff <= -BALL.MOVEMENT / 2)
                    {
                        diff += BALL.MOVEMENT;
                    }
                    nextStepX -= diff;
                    return;
                case Plot.LEFT:
                case Plot.RIGHT:
                default:
                    diff = (nextStepY - thisBall.midY) % BALL.MOVEMENT;
                    if (diff > BALL.MOVEMENT / 2)
                    {
                        diff -= BALL.MOVEMENT;
                    }
                    else if (diff <= -BALL.MOVEMENT / 2)
                    {
                        diff += BALL.MOVEMENT;
                    }
                    nextStepY -= diff;
                    return;
            }


        }
    }
}
