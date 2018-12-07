﻿using System;
namespace GameEngine
{
    public class Board
    {
        public const int PLAYFIELD_HRES = 20;  // 40 with 2nd half mirrored/repeated
        public const int PLAYFIELD_VRES = 20;
        public const int CLOCKS_HSYNC = 2;
        public const int CLOCKS_VSYNC = 4;

        // The position you appear when you enter at the edge of the screen.
        public const int ENTER_AT_TOP = Adv.ADVENTURE_OVERSCAN + Adv.ADVENTURE_SCREEN_HEIGHT;
        public const int ENTER_AT_BOTTOM = Adv.ADVENTURE_OVERSCAN + Adv.ADVENTURE_OVERSCAN - 2;
        public const int ENTER_AT_RIGHT = Adv.ADVENTURE_SCREEN_WIDTH - 5;
        public const int ENTER_AT_LEFT = 5;


        // The limit as to how close a ball can get to the edge
        public const int TOP_EDGE = Adv.ADVENTURE_OVERSCAN + Adv.ADVENTURE_SCREEN_HEIGHT + 6;
        public const int BOTTOM_EDGE = 0x0D * 2;
        public const int RIGHT_EDGE = Adv.ADVENTURE_SCREEN_WIDTH - 4;
        public const int LEFT_EDGE = 4;

        public const int OBJECT_BALL = -2;
        public const int OBJECT_SURROUND = -5; // Actually, up to 3 surrounds with values -5 to -7

        public enum OBJLIST
        {
            OBJECTLIST_NONE = -1,
            OBJECTLIST_YELLOW_PORT = 0,
            OBJECTLIST_COPPER_PORT,
            OBJECTLIST_JADE_PORT,
            OBJECTLIST_WHITE_PORT,
            OBJECTLIST_BLACK_PORT,
            OBJECTLIST_CRYSTAL_PORT,
            OBJECTLIST_NAME,
            OBJECTLIST_EASTEREGG,
            OBJECTLIST_NUMBER,
            OBJECTLIST_REDDRAGON, // Put all immovable objects before this
            OBJECTLIST_YELLOWDRAGON,
            OBJECTLIST_GREENDRAGON,
            OBJECTLIST_SWORD, // Put all carryable objects after this
            OBJECTLIST_BRIDGE,
            OBJECTLIST_YELLOWKEY,
            OBJECTLIST_COPPERKEY,
            OBJECTLIST_JADEKEY,
            OBJECTLIST_WHITEKEY,
            OBJECTLIST_BLACKKEY,
            OBJECTLIST_CRYSTALKEY1,
            OBJECTLIST_CRYSTALKEY2,
            OBJECTLIST_CRYSTALKEY3,
            OBJECTLIST_BAT,
            OBJECTLIST_DOT,
            OBJECTLIST_CHALISE,
            OBJECTLIST_MAGNET
        };

        public const int OBJECT_NONE = (int)OBJLIST.OBJECTLIST_NONE;
        public const int OBJECT_YELLOW_PORT = (int)OBJLIST.OBJECTLIST_YELLOW_PORT;
        public const int OBJECT_COPPER_PORT = (int)OBJLIST.OBJECTLIST_COPPER_PORT;
        public const int OBJECT_JADE_PORT = (int)OBJLIST.OBJECTLIST_JADE_PORT;
        public const int OBJECT_WHITE_PORT = (int)OBJLIST.OBJECTLIST_WHITE_PORT;
        public const int OBJECT_BLACK_PORT = (int)OBJLIST.OBJECTLIST_BLACK_PORT;
        public const int OBJECT_CRYSTAL_PORT = (int)OBJLIST.OBJECTLIST_CRYSTAL_PORT;
        public const int OBJECT_NAME = (int)OBJLIST.OBJECTLIST_NAME;
        public const int OBJECT_EASTEREGG = (int)OBJLIST.OBJECTLIST_EASTEREGG;
        public const int OBJECT_NUMBER = (int)OBJLIST.OBJECTLIST_NUMBER;
        public const int OBJECT_REDDRAGON = (int)OBJLIST.OBJECTLIST_REDDRAGON;
        public const int OBJECT_YELLOWDRAGON = (int)OBJLIST.OBJECTLIST_YELLOWDRAGON;
        public const int OBJECT_GREENDRAGON = (int)OBJLIST.OBJECTLIST_GREENDRAGON;
        public const int OBJECT_SWORD = (int)OBJLIST.OBJECTLIST_SWORD;
        public const int OBJECT_BRIDGE = (int)OBJLIST.OBJECTLIST_BRIDGE;
        public const int OBJECT_YELLOWKEY = (int)OBJLIST.OBJECTLIST_YELLOWKEY;
        public const int OBJECT_COPPERKEY = (int)OBJLIST.OBJECTLIST_COPPERKEY;
        public const int OBJECT_JADEKEY = (int)OBJLIST.OBJECTLIST_JADEKEY;
        public const int OBJECT_WHITEKEY = (int)OBJLIST.OBJECTLIST_WHITEKEY;
        public const int OBJECT_BLACKKEY = (int)OBJLIST.OBJECTLIST_BLACKKEY;
        public const int OBJECT_CRYSTALKEY1 = (int)OBJLIST.OBJECTLIST_CRYSTALKEY1;
        public const int OBJECT_CRYSTALKEY2 = (int)OBJLIST.OBJECTLIST_CRYSTALKEY2;
        public const int OBJECT_CRYSTALKEY3 = (int)OBJLIST.OBJECTLIST_CRYSTALKEY3;
        public const int OBJECT_BAT = (int)OBJLIST.OBJECTLIST_BAT;
        public const int OBJECT_DOT = (int)OBJLIST.OBJECTLIST_DOT;
        public const int OBJECT_CHALISE = (int)OBJLIST.OBJECTLIST_CHALISE;
        public const int OBJECT_MAGNET = (int)OBJLIST.OBJECTLIST_MAGNET;

        public class ObjIter
        {
            private Board board;
            private int nextExisting;

            public ObjIter()
            {
                board = null;
                nextExisting = 0;
            }

            public ObjIter(Board inBoard, int startingIndex)
            {
                board = inBoard;
                nextExisting = findNext(startingIndex, inBoard);
            }

            public ObjIter(ObjIter other)
            {
                board = other.board;
                nextExisting = other.nextExisting;
            }

            public bool hasNext()
            {
                return (nextExisting >= 0);
            }

            public OBJECT next()
            {
                OBJECT rtn = null;
                if ((board == null) || (nextExisting < 0)) return rtn;
                rtn = board.getObject(nextExisting);
                nextExisting = findNext(nextExisting + 1, board);
                return rtn;
            }

            /**
             * This is static because it is called before the body of the constructor is called so
             * is safer to not access state.
             */
            private static int findNext(int startAt, Board inBoard)
            {
                int nextAt = -1;
                if (inBoard != null)
                {
                    int maxCtr = inBoard.getNumObjects();
                    for (int nextCtr = startAt; (nextAt < 0) && (nextCtr < maxCtr); ++nextCtr)
                    {
                        OBJECT nextOnBoard = inBoard.getObject(nextCtr);
                        if ((nextOnBoard != null) && (nextOnBoard.exists()))
                        {
                            nextAt = nextCtr;
                        }
                    }
                }
                return nextAt;
            }
        }


        private int numObjects; // Includes the "null" object which the old game used to mark the end of the list
        private OBJECT[] objects;

        private int numPlayers;
        private BALL[] players;
        private int currentPlayer;

        public int screenWidth;
        public int screenHeight;
        public Map map;
        private AdventureView view;

        public Board(int inScreenWidth, int inScreenHeight, Map inMap, AdventureView inView)
        {
            screenWidth = inScreenWidth;
            screenHeight = inScreenHeight;
            map = inMap;
            view = inView;

            numObjects = OBJECT_MAGNET + 2;
            objects = new OBJECT[numObjects];
            for (int ctr = 0; ctr < numObjects; ++ctr)
            {
                objects[ctr] = null;
            }
            objects[numObjects - 1] = new OBJECT("", new byte[0][], new byte[0], 0, 0);  // #12 Null

            int MAX_PLAYERS = 3;
            players = new BALL[MAX_PLAYERS];
            numPlayers = 0;
        }

        public OBJECT getObject(int pkey)
        {
            return (pkey > OBJECT_NONE ? objects[pkey] : null);
        }

        public OBJECT this[int pkey]
        {
            get { return (pkey > OBJECT_NONE ? objects[pkey] : null); }
        }

        public int getNumPlayers()
        {
            return numPlayers;
        }

        /**
         * Get the number of objects on the board.
         * This does not include the "null" object that the old game used to mark the end of the list.
         * This does include all game 2 objects even on game 1 when they are all shoved into the unreachable first room.
         */
        public int getNumObjects()
        {
            // Don't include the "null" object.
            return numObjects - 1;
        }

        public ObjIter getObjects()
        {
            ObjIter iter = new ObjIter(this, 0);
            return iter;
        }

        public ObjIter getMovableObjects()
        {
            ObjIter iter = new ObjIter(this, OBJECT_REDDRAGON);
            return iter;
        }

        public ObjIter getCarryableObjects()
        {
            ObjIter iter = new ObjIter(this, OBJECT_SWORD);
            return iter;
        }


        public void addObject(int pkey, OBJECT objct)
        {
            objects[pkey] = objct;
            objct.setBoard(this, pkey);
        }

        public void addPlayer(BALL newPlayer, bool isCurrent)
        {
            players[numPlayers] = newPlayer;
            if (isCurrent)
            {
                currentPlayer = numPlayers;
            }
            ++numPlayers;
        }

        public BALL getPlayer(int playerNum)
        {
            return players[playerNum];
        }

        public BALL getCurrentPlayer()
        {
            return players[currentPlayer];
        }

        public static bool HitTestRects(int ax, int ay, int awidth, int aheight,
                      int bx, int by, int bwidth, int bheight)
        {
            bool intersects = true;

            if (((ay - aheight) >= by) || (ay <= (by - bheight)) || ((ax + awidth) <= bx) || (ax >= (bx + bwidth)))
            {
                // Does not intersect
                intersects = false;
            }
            // else must intersect

            return intersects;
        }

        // Collision check two objects
        // On the 2600 this is done in hardware by the Player/Missile collision registers
        public bool CollisionCheckObjectObject(OBJECT object1, OBJECT object2)
        {
            // Before we do pixel by pixel collision checking, do some trivial rejection
            // and return early if the object extents do not even overlap or are not in the same room

            if (object1.room != object2.room)
                return false;

            int cx1 = 0, cy1 = 0, cw1 = 0, ch1 = 0;
            int cx2 = 0, cy2 = 0, cw2 = 0, ch2 = 0;
            object1.CalcSpriteExtents(ref cx1, ref cy1, ref cw1, ref ch1);
            object2.CalcSpriteExtents(ref cx2, ref cy2, ref cw2, ref ch2);
            if (!HitTestRects(cx1, cy1, cw1, ch1, cx2, cy2, cw2, ch2))
                return false;

            // Object extents overlap go pixel by pixel

            int objectX1 = object1.x;
            int objectY1 = object1.y;
            int objectSize1 = (object1.size / 2) + 1;

            int objectX2 = object2.x;
            int objectY2 = object2.y;
            int objectSize2 = (object2.size / 2) + 1;

            // Look up the index to the current state for the objects
            int stateIndex1 = object1.states.Length > 0 ? object1.states[object1.state] : 0;
            int stateIndex2 = object2.states.Length > 0 ? object2.states[object2.state] : 0;

            // Get the height, then the data
            // (the first byte of the data is the height)

            byte[] dataP1 = object1.gfxData[stateIndex1];
            int objHeight1 = dataP1.Length;
            byte[] dataP2 = object2.gfxData[stateIndex2];
            int objHeight2 = dataP2.Length;

            // Adjust for proper position
            objectX1 -= CLOCKS_HSYNC;
            objectX2 -= CLOCKS_HSYNC;

            // Scan the the object1 data
            for (int i = 0; i < objHeight1; i++)
            {
                byte rowByte1 = dataP1[i];
                // Parse the object1 row - each bit is a 2 x 2 block
                for (int bit1 = 0; bit1 < 8; bit1++)
                {
                    if ((rowByte1 & (1 << (7 - bit1))) > 0)
                    {
                        // test this pixel of object1 for intersection against the pixels of object2

                        // Scan the the object2 data
                        objectY2 = object2.y;
                        for (int j = 0; j < objHeight2; j++)
                        {
                            byte rowByte2 = dataP2[j];
                            // Parse the object2 row - each bit is a 2 x 2 block
                            for (int bit2 = 0; bit2 < 8; bit2++)
                            {
                                if ((rowByte2 & (1 << (7 - bit2))) > 0)
                                {
                                    int wrappedX1 = objectX1 + (bit1 * 2 * objectSize1);
                                    if (wrappedX1 >= screenWidth)
                                        wrappedX1 -= screenWidth;

                                    int wrappedX2 = objectX2 + (bit2 * 2 * objectSize2);
                                    if (wrappedX2 >= screenWidth)
                                        wrappedX2 -= screenWidth;

                                    if (HitTestRects(wrappedX1, objectY1, 2 * objectSize1, 2, wrappedX2, objectY2, 2 * objectSize2, 2))
                                        // The objects are touching
                                        return true;
                                }
                            }

                            objectY2 -= 2;
                        }
                    }
                }

                objectY1 -= 2;
            }

            return false;

        }

        // Checks an object for collision against the specified rectangle
        // On the 2600 this is done in hardware by the Player/Missile collision registers
        public bool CollisionCheckObject(OBJECT objct, int x, int y, int width, int height)
        {
            int objectX = objct.x * 2;
            int objectY = objct.y * 2;
            int objectSize = (objct.size / 2) + 1;

            // Look up the index to the current state for this object
            int stateIndex = objct.states.Length > 0 ? objct.states[objct.state] : 0;

            // Get the height, then the data
            byte[] dataP = objct.gfxData[stateIndex];
            int objHeight = dataP.Length;

            // Adjust for proper position
            objectX -= CLOCKS_HSYNC;

            // scan the data
            for (int i = 0; i < objHeight; i++)
            {
                byte rowByte = dataP[i];
                // Parse the row - each bit is a 2 x 2 block
                for (int bit = 0; bit < 8; bit++)
                {
                    if ((rowByte & (1 << (7 - bit))) > 0)
                    {
                        // test this pixel for intersection

                        int wrappedX = objectX + (bit * 2 * objectSize);
                        if (wrappedX >= screenWidth)
                            wrappedX -= screenWidth;

                        if (HitTestRects(x, y, width, height, wrappedX, objectY, 2 * objectSize, 2))
                            return true;
                    }
                }

                objectY -= 2;
            }

            return false;
        }

        // Returns the player number of whoever is holding an object (holding the bat holding the
        // object counts).  Returns -1 if no one is holding the object.
        public int getPlayerHoldingObject(OBJECT objct)
        {
            int heldBy = -1;
            int objectPkey = objct.getPKey();
            for (int ctr = 0; (ctr < numPlayers) && (heldBy < 0); ++ctr)
            {
                BALL nextPlayer = players[ctr];
                // Have to check if player is holding key or player is holding bat holding key
                if ((nextPlayer.linkedObject == objectPkey) ||
                    ((nextPlayer.linkedObject == OBJECT_BAT) && (((Bat)objects[OBJECT_BAT]).linkedObject == objectPkey)))
                {

                    heldBy = ctr;
                }
            }
            return heldBy;
        }

        public void makeSound(SOUND sound, float volume) {
            view.Platform_MakeSound(sound, volume);
        }


    }
}
