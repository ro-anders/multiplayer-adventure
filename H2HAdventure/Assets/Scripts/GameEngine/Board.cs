﻿using System;
namespace GameEngine
{
    public class Board
    {
        public const int PLAYFIELD_HRES = 20;  // 40 with 2nd half mirrored/repeated


        // The position you appear when you enter at the edge of the screen.
        public const int TOP_EDGE_FOR_BALL = Adv.ADVENTURE_OVERSCAN_BHEIGHT + Adv.ADVENTURE_SCREEN_BHEIGHT - 1;
        public const int BOTTOM_EDGE_FOR_BALL = Adv.ADVENTURE_OVERSCAN_BHEIGHT + BALL.DIAMETER - 1;
        public const int RIGHT_EDGE_FOR_BALL = Adv.ADVENTURE_SCREEN_BWIDTH - BALL.DIAMETER;
        public const int LEFT_EDGE_FOR_BALL = 2; // Why is the ball never allowed to be up against the left edge? -RCA

        // The limit as to how close an object can get to the edge
        public const int TOP_EDGE_FOR_OBJECTS = TOP_EDGE_FOR_BALL / Adv.BALL_SCALE;
        public const int BOTTOM_EDGE_FOR_OBJECTS = BOTTOM_EDGE_FOR_BALL / Adv.BALL_SCALE;
        public const int RIGHT_EDGE_FOR_OBJECTS = RIGHT_EDGE_FOR_BALL / Adv.BALL_SCALE;
        public const int LEFT_EDGE_FOR_OBJECTS = LEFT_EDGE_FOR_BALL / Adv.BALL_SCALE;

        public const int STARTING_X = Adv.ADVENTURE_SCREEN_BWIDTH/2 - BALL.RADIUS;
        public const int STARTING_Y = 2 * Map.WALL_HEIGHT - 3; // 3 pixels below gate

        public const int OBJECT_BALL = -2;
        public const int OBJECT_SURROUND = -5; // Actually, up to 3 surrounds with values -5 to -7

        public const int SURROUND_RADIUS_X = 0x1E;
        public const int SURROUND_RADIUS_Y = 0x18;

        public const int OBJECTWIDTH = 8;

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

        /** useful for iterating through certain types of objects. */
        public const int FIRST_DRAGON = OBJECT_REDDRAGON;
        public const int LAST_DRAGON = OBJECT_GREENDRAGON;
        public const int FIRST_PORT = OBJECT_YELLOW_PORT;
        public const int LAST_PORT = OBJECT_CRYSTAL_PORT;
        public const int FIRST_CARRYABLE = OBJECT_SWORD;

        /**
         * The number of objects on the board.
         * This does not include the "null" object that the old game used to mark the end of the list.
         * This does include all game 2 objects even on game 1 when they are all shoved into the unreachable first room.
         */
        public const int NUM_OBJECTS = OBJECT_MAGNET + 1; 

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
                if ((board == null) || (nextExisting < 0)) return null;

                OBJECT rtn = board.getObject(nextExisting);
                nextExisting = findNext(nextExisting + 1, board);
                return rtn;
            }

            /**
             * Return true if this iterator is at the same position as
             * the passed in iterator.  Return false, otherwise.
             */
            public bool at(in ObjIter other)
            {
                return this.nextExisting == other.nextExisting;
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
                    int maxCtr = Board.NUM_OBJECTS;
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


        private OBJECT[] objects;

        private int numPlayers;
        private BALL[] players;
        private int currentPlayer;

        public Map map;
        private AdventureView view;

        public Board(Map inMap, AdventureView inView)
        {
            map = inMap;
            view = inView;

            objects = new OBJECT[NUM_OBJECTS + 1]; // Includes the "null" object which the old game used to mark the end of the list
            for (int ctr = 0; ctr < NUM_OBJECTS; ++ctr)
            {
                objects[ctr] = null;
            }
            objects[NUM_OBJECTS] = new OBJECT("", new byte[0][], new byte[0], 0, 0);  // #12 Null

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
            ObjIter iter = new ObjIter(this, FIRST_CARRYABLE);
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

        /**
         * There are three types of players, the local player, a remote player
         * and an AI player.  Will return true if this is a remote player
         * @param player_index the number of the player, 0-2
         * @returns true if player is a remote player
         */
        public bool isPlayerRemote(int player_index)
        {
            // Returns if this is not the local player and not an ai player
            return ((player_index < numPlayers) && (player_index != currentPlayer) && (!players[player_index].isAi));
        }

        /**
         * Lots of things don't happen unless the ball is in the room at
         * the time (e.g. sword killing dragon).  Return whether any ball is
         * in this room.
         * @room the room to see if anyone is in
         * @mustBeLocal true if we don't count remote people watching and only 
         * count if the local player or an ai sees it.
         */
        public bool isWitnessed(int room, bool mustBeLocal=false)
        {
            bool witnessed = false;
            for (int ctr = 0; !witnessed && ctr < numPlayers; ++ctr)
            {
                witnessed = (players[ctr].room == room) &&
                    (!mustBeLocal || (ctr == currentPlayer) || (players[ctr].isAi));
            }
            return witnessed;
        }

        /**
         * Return if two rectangles intersect
         */
        public static bool HitTestRects(int bx1, int by1, int bwidth1, int bheight1,
                      int bx2, int by2, int bwidth2, int bheight2)
        {
            return !(((by1 - bheight1) >= by2) || (by1 <= (by2 - bheight2)) ||
                ((bx1 + bwidth1) <= bx2) || (bx1 >= (bx2 + bwidth2)));
        }

        /**
         * Computes the intersection of two rectangles
         * Returns false if they don't intersect
         */
        public static bool intersect(int x1, int y1, int w1, int h1,
                                int x2, int y2, int w2, int h2,
                                ref int xi, ref int yi, ref int wi, ref int hi)
        {
            if (!HitTestRects(x1, y1, w1, h1, x2, y2, w2, h2))
            {
                return false;
            }
            else
            {
                xi = (x1 > x2 ? x1 : x2);
                yi = (y1 < y2 ? y1 : y2);
                wi = (x1 + w1 < x2 + w2 ? (x1 + w1) - xi : (x2 + w2) - xi);
                hi = (y1 - h1 > y2 - h2 ? yi - (y1 - h1) : yi - (y2 - h2));
                return true;
            }
        }

        // Collision check two objects
        // On the 2600 this is done in hardware by the Player/Missile collision registers
        public bool CollisionCheckObjectObject(OBJECT object1, OBJECT object2)
        {
            // Before we do pixel by pixel collision checking, do some trivial rejection
            // and return early if the object extents do not even overlap or are not in the same room

            if (object1.room != object2.room)
                return false;

            // We don't convert the coordinates to ball scale, because everything
            // is in object scale.
            if (!HitTestRects(object1.x, object1.y, object1.width, object1.Height,
                object2.x, object2.y, object2.width, object2.Height))
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
                                    if (wrappedX1 >= Adv.ADVENTURE_SCREEN_BWIDTH)
                                        wrappedX1 -= Adv.ADVENTURE_SCREEN_BWIDTH;

                                    int wrappedX2 = objectX2 + (bit2 * 2 * objectSize2);
                                    if (wrappedX2 >= Adv.ADVENTURE_SCREEN_BWIDTH)
                                        wrappedX2 -= Adv.ADVENTURE_SCREEN_BWIDTH;

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
            int objectY = objct.by;
            int objectSize = (objct.size / 2) + 1;

            // Look up the index to the current state for this object
            int stateIndex = objct.states.Length > 0 ? objct.states[objct.state] : 0;

            // Get the height, then the data
            byte[] dataP = objct.gfxData[stateIndex];
            int objHeight = dataP.Length;

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

                        int wrappedX = objct.bx + (bit * 2 * objectSize);
                        if (wrappedX >= Adv.ADVENTURE_SCREEN_BWIDTH)
                            wrappedX -= Adv.ADVENTURE_SCREEN_BWIDTH;

                        if (HitTestRects(x, y, width, height, wrappedX, objectY, 2 * objectSize, 2))
                        {
                            return true;
                        }
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
                // Have to check if player is holding object or player is holding bat holding object
                if ((nextPlayer.linkedObject == objectPkey) ||
                    ((nextPlayer.linkedObject == OBJECT_BAT) && (((Bat)objects[OBJECT_BAT]).linkedObject == objectPkey)))
                {

                    heldBy = ctr;
                }
            }
            return heldBy;
        }

        public void makeSound(SOUND sound, int fromRoom)
        {
            view.Platform_MakeSound(sound, volumeAtDistance(fromRoom));
        }

        private float volumeAtDistance(int room)
        {
            float NEAR_VOLUME = MAX.VOLUME / 3;
            float FAR_VOLUME = MAX.VOLUME / 9;

            int currentPlayerRoom = players[currentPlayer].room;
            int distance = map.distance(room, currentPlayerRoom);

            float volume = 0.0f;
            switch (distance)
            {
                case 0:
                    volume = MAX.VOLUME;
                    break;
                case 1:
                    volume = NEAR_VOLUME;
                    break;
                case 2:
                    volume = FAR_VOLUME;
                    break;
                default:
                    volume = 0;
                    break;
            }
            return volume;
        }

        public string ballToString(BALL ball)
        {
            ROOM room = map.getRoom(ball.room);
            OBJECT holding = (ball.linkedObject > Board.OBJECT_NONE ? getObject(ball.linkedObject) : null);
                return "player " + ball.playerNum + " at (" + ball.x + "," + ball.y + ")@" + room.label +
                (holding != null ? " with " + holding.label : "");
        }

    }

}
