using System;

namespace GameEngine.Ai
{
    /**
     * Take an object currently carried by another player.  Includes waiting 
     * for a player to come out of a zone to take the object from them.
     */
    public class StealObjectFromPlayer : AiObjective
    {
        /** The center, bottom of the screen - a good place to wait
         * for things to happen. */
        private static RRect BOTTOM = RRect.fromTRBL(0, Map.WALL_HEIGHT - 1, 24 * Map.WALL_WIDTH - 1, 0, 16 * Map.WALL_WIDTH);
        private int toSteal;
        private OBJECT objectToSteal;
        private int toStealFrom;
        private BALL ballToStealFrom;
        private NavZone zoneToStealFrom;
        private NavZone startingZone;
        private bool zonesChanged = false; // Set to true if zone of either ball changes

        /** If a player is in another zone we may wait for them to come
         * out rather than try to go in and take it. */
        private RRect waitLocation;

        private static Random genRandom = new Random(0);

        /**
         * The object the AI player needs to pickup
         */
        public StealObjectFromPlayer(int inToSteal, int inToStealFrom)
        {
            toSteal = inToSteal;
            toStealFrom = inToStealFrom;
            waitLocation = RRect.NOWHERE;
        }

        public override string ToString()
        {
            return "steal " + board.getObject(toSteal).label + " from player #" + toStealFrom;
        }

        /**
         * Initialize the stategy.
         */
        protected override void initialize()
        {
            objectToSteal = board.getObject(toSteal);
            ballToStealFrom = board.getPlayer(toStealFrom);
            zoneToStealFrom = nav.WhichZone(ballToStealFrom.BRect);
            startingZone = nav.WhichZone(aiPlayer.BRect);
        }

        protected override void doComputeStrategy()
        {
            if (ballToStealFrom.linkedObject != toSteal)
            {
                throw new Abort();
            }

            if (zoneToStealFrom != startingZone)
            {
                // If we're not in main then pretty much just reset
                if (startingZone != NavZone.MAIN)
                {
                    markShouldReset();
                    return;
                }

                // If they're locked in a castle go grab the key but wait outside the castle
                Portcullis portcullis = strategy.behindLockedGate(ballToStealFrom.room);
                if (portcullis != null)
                {
                    addChild(new ObtainObject(portcullis.key.getPKey()));
                    waitLocation = new RRect(portcullis.room, BOTTOM.x, BOTTOM.y, BOTTOM.width, BOTTOM.height);
                    addChild(new GoTo(waitLocation, portcullis.key.getPKey()));
                }
                // If they're in the white castle's hidden section, wait in the foyer
                else if (zoneToStealFrom == NavZone.WHITE_CASTLE_2)
                {
                    waitLocation = new RRect(Map.RED_MAZE_1, BOTTOM.x, BOTTOM.y, BOTTOM.width, BOTTOM.height);
                    addChild(new GoTo(waitLocation));
                }
                // If they're in the dot room, wait in the black foyer
                else if (zoneToStealFrom == NavZone.DOT_LOCATION)
                {
                    waitLocation = new RRect(Map.RED_MAZE_1, BOTTOM.x, BOTTOM.y, BOTTOM.width, BOTTOM.height);
                    addChild(new GoTo(waitLocation));
                }
                // If they're in a wall or someplace weird just wait until they're out
                else
                {
                    waitLocation = aiPlayer.BRect;
                }
            }
            else
            {
                // If we are in the right zone, we don't need to compute anything.
                // Rely on getBDestination() to guide the ai to where it needs to be.
            }
        }

        /**
         * Still valid unless you see that the object is not held by the player.
         */
        public override bool isStillValid()
        {
            bool stillValid = true;
            // Unlike most validity checks which only act on observable state,
            // this objective immediately aborts if the player being stolen
            // from or this player changes zone.
            if ((nav.WhichZone(aiPlayer.BRect) != startingZone) ||
                    (nav.WhichZone(ballToStealFrom.BRect) != zoneToStealFrom))
            {
                return false;
            }

            if ((aiPlayer.room == objectToSteal.room) || (aiPlayer.room == ballToStealFrom.room))
            {
                stillValid = (ballToStealFrom.linkedObject == toSteal);
            }
            return stillValid;
        }

        public override RRect getBDestination()
        {
            // If zones have changed, destination may not be valid, so return
            // no movement until we recompute a strategy
            if (zonesChanged ||
                (nav.WhichZone(aiPlayer.BRect) != startingZone) ||
                (nav.WhichZone(ballToStealFrom.BRect) != zoneToStealFrom))
            {
                zonesChanged = true;
                return RRect.NOWHERE;
            }

            // If we have a wait objective go there
            if (waitLocation.IsSomewhere)
            {
                return waitLocation;
            }

            // If we're really close to the object, go for the object
            bool goForObject = ((aiPlayer.room == objectToSteal.room) &&
                    (distToObject() <= 1.5 * BALL.MOVEMENT));

            // If we're really close to the other ball, go for the object
            if ((!goForObject) && (aiPlayer.room == ballToStealFrom.room))
            {
                int distanceX = Math.Abs(aiPlayer.midX - ballToStealFrom.midX) - BALL.DIAMETER;
                int distanceY = Math.Abs(aiPlayer.midY - ballToStealFrom.midY) - BALL.DIAMETER;
                int distance = (distanceX > distanceY ? distanceX : distanceY);
                goForObject = (distance <= 1.5 * BALL.MOVEMENT);
            }

            if (goForObject)
            {
                if (toSteal == Board.OBJECT_BRIDGE)
                {
                    // Bridge is tricky.  Aim for the corner for now.
                    return new RRect(objectToSteal.room, objectToSteal.bx, objectToSteal.by, 1, 1);
                }
                else
                {
                    // Aim for the center
                    RRect target = strategy.closestReachableRectangle(objectToSteal);

                    // In the case where two computers are trying to steal from each
                    // other we need to randomly break an impasse
                    if ((!target.IsValid) && ballToStealFrom.isAi)
                    {
                        AiObjective othersObjective = ballToStealFrom.ai.CurrentObjective;
                        if (othersObjective is StealObjectFromPlayer)
                        {
                            StealObjectFromPlayer othersStealObjective = (StealObjectFromPlayer)othersObjective;
                            if (othersStealObjective.toStealFrom == aiPlayer.playerNum)
                            {
                                // We're stealing from them and they're stealing from us.
                                // Add random movements.
                                int randomX = (genRandom.Next(3) - 1) * BALL.MOVEMENT;
                                int randomY = (genRandom.Next(3) - 1) * BALL.MOVEMENT;
                                target = new RRect(aiPlayer.room, aiPlayer.x + randomX, aiPlayer.y + randomY, BALL.DIAMETER, BALL.DIAMETER);
                            }
                        }
                    }
                    return target;
                }
            }
            else
            {
                return new RRect(ballToStealFrom.room, ballToStealFrom.x, ballToStealFrom.y, BALL.DIAMETER, BALL.DIAMETER);
            }
        }

        protected override bool computeIsCompleted()
        {
            return (aiPlayer.linkedObject == toSteal);
        }

        public override int getDesiredObject()
        {
            return toSteal;
        }

        /**
         * Distance to the object - only valid if in the same room as the object
         */
        private int distToObject()
        {
            int objMidBX = objectToSteal.bx + objectToSteal.bwidth / 2;
            int xdist = Math.Abs(objMidBX - aiPlayer.midX) - (objectToSteal.bwidth / 2) - (BALL.RADIUS);
            int objMidBY = objectToSteal.by - objectToSteal.BHeight / 2;
            int ydist = Math.Abs(objMidBY - aiPlayer.midY) - (objectToSteal.BHeight / 2) - (BALL.RADIUS);
            int dist = (xdist > ydist ? xdist : ydist);
            return dist;
        }
    }

}