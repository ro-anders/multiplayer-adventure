using System;

namespace GameEngine.Ai
{
    /**
     * Take an object currently carried by another player.
     */
    public class StealObjectFromPlayer : AiObjective
    {
        private int toSteal;
        private OBJECT objectToSteal;
        private int toStealFrom;
        private BALL ballToStealFrom;
        private static System.Random genRandom = new System.Random(0);

        /**
         * The object the AI player needs to pickup
         */
        public StealObjectFromPlayer(int inToSteal, int inToStealFrom)
        {
            toSteal = inToSteal;
            toStealFrom = inToStealFrom;
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
        }

        protected override void doComputeStrategy()
        {
            if (ballToStealFrom.linkedObject != toSteal)
            {
                throw new Abort();
            }
        }

        /**
         * Still valid unless you see that the object is not held by the player.
         */
        public override bool isStillValid()
        {
            bool stillValid = true;
            if ((aiPlayer.room == objectToSteal.room) || (aiPlayer.room == ballToStealFrom.room))
            {
                stillValid = (ballToStealFrom.linkedObject == toSteal);
            }
            return stillValid;
        }

        public override RRect getBDestination()
        {
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