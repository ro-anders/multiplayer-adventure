
namespace GameEngine.Ai
{

    /**
     * Change where you are holding the bridge so you are under the left foot.
     **/
    public class RepositionBridge : AiObjective
    {
        private OBJECT bridge;

        /**
         * Change where you are holding the bridge so you are under the left foot.
         */
        public RepositionBridge()
        {
        }

        protected override void doComputeStrategy()
        {
            RRect playerBRect = aiPlayer.BRect;
            bridge = board.getObject(Board.OBJECT_BRIDGE);
            RRect bridgeBRect = bridge.BRect;
            NavZone playerZone = nav.WhichZone(playerBRect);
            bool inOrOut = (playerZone == NavZone.MAIN) ||
                (playerZone == NavZone.WHITE_CASTLE_1);

            // Couple possibilities.
            // If we are not holding the bridge below its left foot, but
            // the left foot is in our plot and there's room to maneuver, just
            // drop the bridge and move to under the left foot and pick it up again.
            RRect bSpaceNeeded = bSpaceNeededToReposition(playerBRect, bridgeBRect);
            if (!bSpaceNeeded.IsValid)
            {
                throw new Abort();
            }
            // See if the space needed is all in one plot
            Plot[] plots = nav.GetPlots(bSpaceNeeded);
            if ((plots.Length == 1) && plots[0].Contains(bSpaceNeeded)) {
                this.addChild(new DropObjective(Board.OBJECT_BRIDGE));
                RRect underLeftFoot = new RRect(bridge.room,
                    bridgeBRect.left - BALL.DIAMETER + 1,
                    bridgeBRect.bottom - 1,
                    Bridge.FOOT_BWIDTH + 2 * BALL.DIAMETER - 2,
                    2 * BALL.DIAMETER - 1);
                this.addChild(new GoTo(underLeftFoot, CARRY_NO_OBJECT));
                this.addChild(new PickupObject(Board.OBJECT_BRIDGE));
                return;
            }

            // If the left foot isn't in our plot but if we moved a little
            // it could be, move a little, then drop the bridge and get under
            // it.
            Plot[] ballPlots = nav.GetPlots(playerBRect);
            for(int ctr=0; ctr<ballPlots.Length; ++ctr)
            {
                Plot plot = ballPlots[ctr];
                if ((plot.BWidth >= bSpaceNeeded.width) && (plot.BHeight >= bSpaceNeeded.height))
                {
                    int goto_bleft = plot.BLeft + (playerBRect.left - bSpaceNeeded.left);
                    int goto_bright = plot.BRight - (bSpaceNeeded.right - playerBRect.right);
                    int goto_btop = plot.BTop - (bSpaceNeeded.top - playerBRect.top);
                    int goto_bbottom = plot.BBottom + (playerBRect.bottom - bSpaceNeeded.bottom);
                    RRect goto_brect = RRect.fromTRBL(playerBRect.room, goto_btop, goto_bright, goto_bbottom, goto_bleft);
                    this.addChild(new GoTo(goto_brect, Board.OBJECT_BRIDGE));
                    this.addChild(new RepositionBridge());
                    return;
                }
            }

            // Otherwise, we need a bigger plot.
            // Right now this is only used when getting into or out of the hidden
            // room.  For into, we use the plot just inside the white portcullis,
            // For out of, we use the large plot at the bottom of the hidden room.
            if (inOrOut)
            {
                this.addChild(new GoTo(Map.RED_MAZE_1, Map.WALL_WIDTH * 20, Map.WALL_HEIGHT * 2, Board.OBJECT_BRIDGE));
            }
            else
            {
                this.addChild(new GoTo(Map.RED_MAZE_4, Map.WALL_WIDTH * 20, Map.WALL_HEIGHT * 2, Board.OBJECT_BRIDGE));
            }
            // Once we're in the room, recompute repositioning
            this.addChild(new RepositionBridge());

        }

        public override bool isStillValid()
        {
            // Right now this is specifically used for getting into and out of
            // the white castle's hidden room, and so is only valid if the
            // white castle is locked, but TransitWhiteCastle2Zone objective
            // checks that and right now this is always a subchild of that.

            // We need to either be holding the bridge or be moving around it.
            return (aiPlayer.linkedObject == Board.OBJECT_BRIDGE) ||
                (aiPlayer.room == bridge.room);
        }

        /**
         * Return if we are carrying bridge by the left foot.
         */
        protected override bool computeIsCompleted()
        {
            RRect ballRect = aiPlayer.BRect;
            RRect bridgeRect = bridge.BRect;
            bool completed =
                (aiPlayer.linkedObject == Board.OBJECT_BRIDGE) &&
                (ballRect.top < bridgeRect.bottom) &&
                (ballRect.right > bridgeRect.left) &&
                (ballRect.left < bridgeRect.left + 8); // Bridge foot is 8 wide
            
            return completed;
        }

        public override string ToString()
        {
            return "reposition bridge";
        }

        /**
         * Compute the rectangle that would encompass the space the ball needs 
         * to move to the left foot of the bridge.
         * @param playerBRect - the area the ball is taking up
         * @param bridgeBRect - the area the bridge is taking up
         */
        private RRect bSpaceNeededToReposition(RRect playerBRect, RRect bridgeBRect)
        {
            if (playerBRect.room != bridgeBRect.room)
            {
                return RRect.INVALID;
            }
            RRect bspace = RRect.NOWHERE;
            if (playerBRect.top < bridgeBRect.bottom)
            {
                // If the player is below the bridge, this is easy
                bspace = RRect.fromTRBL(
                    playerBRect.room,
                    bridgeBRect.bottom - 1,
                    // If any part of ball is right of foot rightmost is ball, else rightmost is foot
                    playerBRect.right > bridgeBRect.left + Bridge.FOOT_BWIDTH - 1 ? playerBRect.right : bridgeBRect.left + Bridge.FOOT_BWIDTH - 1,
                    playerBRect.bottom,
                    // If any part of ball is left of foot leftmost is ball, else leftmost is foot 
                    playerBRect.left < bridgeBRect.left ? playerBRect.left : bridgeBRect.left + Bridge.FOOT_BWIDTH);
            }
            else
            {
                int btop = playerBRect.top;
                int bbottom = bridgeBRect.bottom - BALL.DIAMETER - BALL.MOVEMENT;
                // If entire ball is left of foot leftmost is ball
                int bleft = (playerBRect.right < bridgeBRect.left ? playerBRect.left :
                    // If ball is in left crook of bridge or directly above left bridge post must go to left of entire bridge
                    (playerBRect.left < bridgeBRect.left+ Bridge.FOOT_BWIDTH - 1 ? bridgeBRect.left - BALL.DIAMETER - BALL.MOVEMENT :
                    // Else leftmost is foot
                    bridgeBRect.left ));
                // If entire ball is right of bridge rightmost is ball
                int bright = (playerBRect.left > bridgeBRect.right ? playerBRect.right :
                    // If ball is in right crook of bridge must go to the right of the entire bridge
                    (playerBRect.left < bridgeBRect.right - Bridge.FOOT_EXTENSION_BWIDTH && playerBRect.bottom <= bridgeBRect.top ? bridgeBRect.right + BALL.DIAMETER + BALL.MOVEMENT :
                    // If ball is right of foot rightmost is ball else rightmost is foot
                    (playerBRect.right > bridgeBRect.left + Bridge.FOOT_BWIDTH - 1 ? playerBRect.right : bridgeBRect.left + Bridge.FOOT_BWIDTH - 1)));
                bspace = RRect.fromTRBL(playerBRect.room, btop, bright, bbottom, bleft);
            }
            return bspace;

        }

    }


}