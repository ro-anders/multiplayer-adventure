namespace GameEngine.Ai
{
    /**
     * Go and pick up an object.  This is dumb objective that assumes the 
     * object is in the zone, reachable and not moving.
     */
    public class PickupObject : AiObjective
    {
        /** The index of the object we are picking up */
        private int toPickup;

        /** The object we are picking up */
        private OBJECT objectToPickup;

        /** The position of the object to pickup when this objective
         * was started.  Except for small magnetic shifts, movement
         * causes the objective to be aborted. */
        private RRect initialPosition;

        /**
         * The rectangle we are trying to get to to pickup the object.
         */
        private RRect destination;

        /**
         * The object the AI player needs to pickup
         */
        public PickupObject(int inToPickup)
        {
            toPickup = inToPickup;
        }

        public override string ToString()
        {
            return "go pickup " + board.getObject(toPickup).label;
        }

        protected override void doComputeStrategy()
        {
            objectToPickup = board.getObject(toPickup);
            if (strategy.heldByPlayer(objectToPickup) != null)
            {
                throw new Abort();
            }
            NavZone currentZone = nav.WhichZone(aiPlayer.BRect);
            NavZone desiredZone = nav.WhichZone(objectToPickup.BRect, currentZone);
            if (currentZone != desiredZone)
            {
                throw new Abort();
            }

            // Compute the closest reachable rectangle and then assume the
            // object isn't going to leave that rectangle (if it does then
            // it's been picked up or otherwise requires recalculating).
            initialPosition = objectToPickup.Rect;
            destination = computeDestination();
        }

        /**
         * Still valid if the object hasn't been picked up and hasn't moved, though only check
         * if we can see the object.
         */
        public override bool isStillValid()
        {
            bool stillValid = true;
            // We only check if we can see the object or see where the object should be
            if ((aiPlayer.room == objectToPickup.room) || (aiPlayer.room == initialPosition.room))
            {
                // To speed up computation, we assume that if the object was picked up by another player
                // that the position of the object will have changed slightly, and no change
                // indicates no one picked it up
                if (!initialPosition.equals(objectToPickup.Rect))
                {
                    // See if the object is still touching the same plot.
                    Plot destinationPlot = nav.GetPlots(destination)[0]; // There should be exactly one
                    RRect intersection = destinationPlot.BRect.intersect(objectToPickup.Rect);
                    if (intersection.IsValid)
                    {
                        destination = intersection;
                    }
                    else
                    {
                        stillValid = false;
                    }

                    // Check to make sure no players are carrying the object
                    int numPlayers = board.getNumPlayers();
                    for (int ctr = 0; ctr < numPlayers && stillValid; ++ctr)
                    {
                        stillValid = (board.getPlayer(ctr).linkedObject != toPickup);
                    }
                }
            }
            return stillValid;
        }

        /**
         * Return the part of the target object that overlaps the nearest reachable
         * plot.
         */
        public override RRect getBDestination()
        {
            return destination;
        }


        private RRect computeDestination()
        {
            if (toPickup == Board.OBJECT_BRIDGE)
            {
                // Bridge is tricky.  Aim for one of the two posts
                RRect[] posts = {
                    new RRect(objectToPickup.room, objectToPickup.bx, objectToPickup.by, Bridge.FOOT_BWIDTH, objectToPickup.BHeight),
                    new RRect(objectToPickup.room, objectToPickup.bx+objectToPickup.bwidth-Bridge.FOOT_BWIDTH, objectToPickup.by, Bridge.FOOT_BWIDTH, objectToPickup.BHeight)
                };
                AiPathNode shortestPath = nav.ComputePathToAreas(aiPlayer.room, aiPlayer.midX, aiPlayer.midY, posts);
                if (shortestPath == null)
                {
                    return RRect.INVALID;
                }
                else
                {
                    RRect endOfPath = shortestPath.End.ThisPlot.BRect;
                    RRect intersect = endOfPath.intersect(posts[0]);
                    if (intersect.IsValid) {
                        return intersect;
                    } else
                    {
                        intersect = endOfPath.intersect(posts[1]);
                        return intersect;
                    }
                }
            }
            else
            {
                // Aim for the center of reachable object
                RRect found = strategy.closestReachableRectangle(objectToPickup);
                if (!found.IsValid)
                {
                    // Something went wrong.  Shoudn't get that here
                    UnityEngine.Debug.LogError("Request to pick up object " + objectToPickup.label + " that is at not reachable place (" +
                        objectToPickup.x + "," + objectToPickup.y + ")@" + objectToPickup.room);
                }
                return found;
            }
        }

        protected override bool computeIsCompleted()
        {
            return (aiPlayer.linkedObject == toPickup);
        }

        public override int getDesiredObject()
        {
            return toPickup;
        }

    }


}