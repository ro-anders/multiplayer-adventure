using System;
namespace GameEngine
{

    class Bat : OBJECT
    {
        public int linkedObject;           // index of linked (carried) object
        public int linkedObjectX;
        public int linkedObjectY;

        private static int MAX_FEDUP = 0xff;
        private static int BAT_SPEED = 3;
        private static int MIDHEIGHT = 4; // Approximate half way up bat

        private int batFedUpTimer = 0;
        private int flapTimer = 0;


        public Bat(int inColor) :
        base("bat", objectGfxBat, batStates, 0, inColor)
        {
            linkedObject = 0;
            linkedObjectX = 0;
            linkedObjectY = 0;
            flapTimer = 0;
        }
        
        public void moveOneTurn(Sync sync, BALL objectBall)
        {
            if (++flapTimer >= 0x04)
            {
                state = (state == 0) ? 1 : 0;
                flapTimer = 0;
            }

            if ((linkedObject != Board.OBJECT_NONE) && (batFedUpTimer < MAX_FEDUP))
                ++batFedUpTimer;

            if (batFedUpTimer >= 0xff)
            {
                // Get the bat's current extents
                int batX = 0;
                int batY = 0;
                int batW = 0;
                int batH = 0;
                CalcSpriteExtents( ref batX,  ref batY, ref batW, ref batH);

                // Enlarge the bat extent by 7 pixels for the proximity checks below
                // (doing the bat once is faster than doing each object and the results are the same)
                batX -= 7;
                batY += 7;
                batW += 7 * 2;
                batH += 7 * 2;

                // Go through the bat's object matrix
                for (int matrixIndex = 0; matrixIndex < batMatrix.Length; ++matrixIndex)
                {
                    // Get the object it is seeking
                    int seekObjKey = batMatrix[matrixIndex];
                    OBJECT seekObject = lookupObject(seekObjKey);
                    if ((seekObject.room == room) && (linkedObject != seekObjKey) && (seekObject.exists()))
                    {
                        int seekX = seekObject.x;
                        int seekY = seekObject.y;

                        // Set the movement
                        int newMoveX = 0;
                        int newMoveY = 0;

                        // horizontal axis
                        if (x < seekX)
                        {
                            newMoveX = BAT_SPEED;
                        }
                        else if (x > seekX)
                        {
                            newMoveX = -BAT_SPEED;
                        }

                        // vertical axis
                        if (y < seekY)
                        {
                            newMoveY = BAT_SPEED;
                        }
                        else if (y > seekY)
                        {
                            newMoveY = -BAT_SPEED;
                        }

                        bool sendMessage = ((newMoveX != movementX) || (newMoveY != movementY));
                        movementX = newMoveX;
                        movementY = newMoveY;
                        if (sendMessage)
                        {
                            broadcastMoveAction(sync, objectBall);
                        }

                        // If the bat is within 7 pixels of the seek object it can pick the object up
                        // The bat extents have already been expanded by 7 pixels above, so a simple
                        // rectangle intersection test is good enought here

                        int objX = 0;
                        int objY = 0;
                        int objW = 0;
                        int objH = 0;
                        seekObject.CalcSpriteExtents(ref objX, ref objY, ref objW, ref objH);

                        if (Board.HitTestRects(batX, batY, batW, batH, objX, objY, objW, objH))
                        {
                            // Hit something we want
                            pickupObject(seekObjKey, sync);
                        }

                        // break since we found something
                        break;
                    }
                }

            }
        }

        /**
         * A bat can process BatMoveActions and BatPickupActions and update its internal state accordingly.
         */
        public void handleAction(RemoteAction action, BALL objectBall)
            {
                if (action.typeCode == BatMoveAction.CODE)
                {

                    // If we are in the same room as the bat and are closer to it than the reporting player,
                    // then we ignore reports and trust our internal state.
                    // Otherwise, use the reported state.
                    BatMoveAction nextMove = (BatMoveAction)action;
                    if ((room != objectBall.room) ||
                         (objectBall.distanceTo(x+4, y-MIDHEIGHT) > nextMove.distance))
                    {

                        room = nextMove.room;
                        x = nextMove.posx;
                        y = nextMove.posy;
                        movementX = nextMove.velx;
                        movementY = nextMove.vely;

                    }
                }
                else if (action.typeCode == BatPickupAction.CODE)
                {
                    BatPickupAction nextPickup = (BatPickupAction)action;
                    if (nextPickup.dropObject != Board.OBJECT_NONE)
                    {
                        OBJECT droppedObject = lookupObject(nextPickup.dropObject);
                        droppedObject.x = nextPickup.dropX;
                        droppedObject.y = nextPickup.dropY;
                    }
                    pickupObject(nextPickup.pickupObject, null);
                }
            }

            private void pickupObject(int newObject, Sync sync)
            {
                // If the bat grabs something that a player is carrying, the bat gets it
                // This allows the bat to take something being carried
                for (int ctr = 0; ctr < board.getNumPlayers(); ++ctr)
                {
                    BALL nextBall = board.getPlayer(ctr);
                    if (newObject == nextBall.linkedObject)
                    {
                        // Now player has nothing
                        nextBall.linkedObject = Board.OBJECT_NONE;
                    }
                }

                // A NULL sync indicates this was initiated by a sync message and does not need to be rebroadcast
                if (sync != null)
                {
                    if (linkedObject == Board.OBJECT_NONE)
                    {
                        BatPickupAction action = new BatPickupAction(newObject, 8, 0, Board.OBJECT_NONE, 0, 0, 0);
                        sync.BroadcastAction(action);
                    }
                    else
                    {
                        OBJECT dropObject = lookupObject(linkedObject);
                        BatPickupAction action = new BatPickupAction(newObject, 8, 0, linkedObject, dropObject.room, dropObject.x, dropObject.y);
                        sync.BroadcastAction(action);
                    }
                }

                // Pick it up
                linkedObject = newObject;
                linkedObjectX = 8;
                linkedObjectY = 0;

                // Reset the timer
                batFedUpTimer = 0;
            }

            private void broadcastMoveAction(Sync sync, BALL objectBall)
            {
                int distance = objectBall.distanceTo(x+4, y-MIDHEIGHT);
                BatMoveAction action = new BatMoveAction(room, x, y, movementX, movementY, distance);
                sync.BroadcastAction(action);
            }

            public void lookForNewObject()
            {
                batFedUpTimer = MAX_FEDUP;
            }

        public static byte[][] objectGfxBat = {
                new byte[] {
                // Object #0E : State 03 : Graphic
                    0x81,                  // X      X
                    0x81,                  // X      X
                    0xC3,                  // XX    XX
                    0xC3,                  // XX    XX
                    0xFF,                  // XXXXXXXX
                    0x5A,                  //  X XX X
                    0x66},                 //  XX  XX
                // Object #0E : State FF : Graphic
                new byte[] {
                    0x01,                  //        X
                    0x80,                  // X
                    0x01,                  //        X
                    0x80,                  // X
                    0x3C,                  //   XXXX
                    0x5A,                  //  X XX X
                    0x66,                  //  XX  XX
                    0xC3,                  // XX    XX
                    0x81,                  // X      X
                    0x81,                  // X      X
                    0x81}                  // X      X
            };

        // Bat states
        private static byte[] batStates = {0,1};

        // Bat Object Matrix
        private static int[] batMatrix = {
            Board.OBJECT_CHALISE,
            Board.OBJECT_SWORD,
            Board.OBJECT_BRIDGE,
            Board.OBJECT_COPPERKEY,
            Board.OBJECT_JADEKEY,
            Board.OBJECT_YELLOWKEY,
            Board.OBJECT_WHITEKEY,
            Board.OBJECT_BLACKKEY,
            Board.OBJECT_REDDRAGON,
            Board.OBJECT_YELLOWDRAGON,
            Board.OBJECT_GREENDRAGON,
            Board.OBJECT_MAGNET
        };


    }

}
