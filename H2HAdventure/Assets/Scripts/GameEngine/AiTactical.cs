using System;
using System.Collections;
using System.Collections.Generic;
using GameEngine;
using UnityEngine;

public class AiTactical
{
    /** The ball that is being moved */
    private BALL thisBall;

    /** The game board */
    private Board board;

    private AiPathNode currentPath;
    private int nextStepX;
    private int nextStepY;

    private static int[][] directions = new int[][]{
            new int[]{ 0, BALL.MOVEMENT},
            new int[]{-BALL.MOVEMENT, BALL.MOVEMENT },
            new int[]{-BALL.MOVEMENT, 0 },
            new int[]{-BALL.MOVEMENT, -BALL.MOVEMENT },
            new int[]{ 0, -BALL.MOVEMENT },
            new int[]{ BALL.MOVEMENT, -BALL.MOVEMENT },
            new int[]{ BALL.MOVEMENT, 0 },
            new int[]{ BALL.MOVEMENT, BALL.MOVEMENT }
        };


    public AiTactical(BALL inBall, Board inBoard)
    {
        thisBall = inBall;
        board = inBoard;
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
                currentPath.ThisPlot.GetOverlap(currentPath.nextNode.ThisPlot,
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

        bool canGetThere = computeDirection(nextStepX, nextStepY, currentObjective.getDesiredObject(), ref nextVelx, ref nextVely);

        return canGetThere;
    }

    /**
     * Figure out the best direction to go to get to a point in the current room.
     * Usually this just makes a straight line towards it, but will choose alternate
     * directions to deal with dragons or impeding obstacles.
     */
    private bool computeDirection(int nextStepX, int nextStepY, int desiredObject, ref int nextVelX, ref int nextVelY)
    {
        nextVelX = (nextStepX > thisBall.midX ? BALL.MOVEMENT : (nextStepX == thisBall.midX ? 0 : -BALL.MOVEMENT));
        nextVelY = (nextStepY > thisBall.midY ? BALL.MOVEMENT : (nextStepY == thisBall.midY ? 0 :-BALL.MOVEMENT));

        avoidScrapingWalls(ref nextVelX, ref nextVelY);

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
                    ((desiredObject >= 0) && (desiredObject != thisBall.linkedObject));
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
          objct.x * 2, objct.y * 2, Board.OBJECTWIDTH * 2, objectHt * 2);
    }

    /**
     * Desired movement would cause ball to hit object.  Figure out a direction to 
     * avoid collision and still get to destination.
     */
    private void avoidObject(OBJECT objct, ref int nextVelX, ref int nextVelY)
    {
        if (objct.getPKey() == Board.OBJECT_CHALISE)
        {
            UnityEngine.Debug.Log("Avoiding chalice");
        }
        // Simple algorithm.  Move around the object clockwise until we are passed it.
        // TODO: Doesn't handle hitting a wall or hitting another object
        // Find which direction we are currently supposed to go
        int start = -1;
        for (int ctr = 0; (start == -1) && (ctr < directions.Length); ++ctr)
        {
            if ((directions[ctr][0] == nextVelX) && (directions[ctr][1] == nextVelY)) {
                start = ctr;
            }
        }

        bool foundClearDirection = false;
        for (int ctr = 1; !foundClearDirection && (ctr < directions.Length); ++ctr)
        {
            int check = (start + ctr) % directions.Length;
            if (!quickCheckCollision(thisBall.x + directions[check][0], thisBall.y + directions[check][1], objct))
            {
                nextVelX = directions[check][0];
                nextVelY = directions[check][1];
                foundClearDirection = true;
            }
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
