using System;
namespace GameEngine
{
    public class ROOM
    {
        public const int FLAG_NONE = 0x00;
        public const int FLAG_MIRROR = 0x01; // 1 for playfield's right side is mirror image of left,
                                             // 0 for right is translated copy of left
        public const int FLAG_LEFTTHINWALL = 0x02; // bit 1 - 1 for left thin wall
        public const int FLAG_RIGHTTHINWALL = 0x04; // bit 2 - 1 for right thin wall
        public const int GRAPHICS_LENGTH = 20; // (ADVENTURE_SCREEN_WIDTH / WALL_WIDTH) / 2

        // An attribute indicating whether objects should be placed in a room when randomly placing objects.
        public enum RandomVisibility
        {
            OPEN, // A room freely accessible without use of a key (e.g. blue labyrinth)
            IN_CASTLE, // a room behind a castle portcullis (e.g. red labyrinth)
            HIDDEN // a room that shouldn't have objects randomly put in it (e.g. the easter egg room)
        };

        public int index;                  // index into the map
        public bool[,] walls;             // 2D array of the walls in the room
        public byte flags;                 // room flags - see below
        public int color;                  // foreground color
        public int roomUp;                 // index of room UP
        public int roomRight;              // index of room RIGHT
        public int roomDown;               // index of room DOWN
        public int roomLeft;               // index of room LEFT
        public String label;                // a short, unique name for the object
        public RandomVisibility visibility; // attribute indicating whether objects can be randomly placed in this room.


        public ROOM(byte[] inGraphicsData, byte inFlags, int inColor,
                    int inRoomUp, int inRoomRight, int inRoomDown, int inRoomLeft, String inLabel, RandomVisibility inVis = RandomVisibility.OPEN)
        {
            flags = inFlags;
            color = (int)inColor;
            roomUp = inRoomUp;
            roomRight = inRoomRight;
            roomDown = inRoomDown;
            roomLeft = inRoomLeft;
            label = inLabel;
            visibility = inVis;
            walls = decodeGraphicsData(inGraphicsData);
        }

        public ROOM(string[] inWalls, byte[] inGraphicsData, byte inFlags, int inColor,
                    int inRoomUp, int inRoomRight, int inRoomDown, int inRoomLeft, String inLabel, RandomVisibility inVis = RandomVisibility.OPEN)
        {
            flags = inFlags;
            color = (int)inColor;
            roomUp = inRoomUp;
            roomRight = inRoomRight;
            roomDown = inRoomDown;
            roomLeft = inRoomLeft;
            label = inLabel;
            visibility = inVis;
            walls = readWalls(inWalls, inGraphicsData);
        }

        public int roomNext(int direction)
        {
            switch (direction)
            {
                case Plot.UP:
                    return roomUp;
                case Plot.RIGHT:
                    return roomRight;
                case Plot.DOWN:
                    return roomDown;
                case Plot.LEFT:
                    return roomLeft;
                default:
                    return -1;
            }
        }

        public void setIndex(int inIndex)
        {
            index = inIndex;
        }

        public void setGraphicsData(byte[] newGraphicsData)
        {
            walls = decodeGraphicsData(newGraphicsData);
        }

        bool isNextTo(ROOM otherRoom)
        {
            int index2 = otherRoom.index;
            return (((this.roomUp == index2) && (otherRoom.roomDown == index)) ||
            ((this.roomRight == index2) && (otherRoom.roomLeft == index)) ||
            ((this.roomDown == index2) && (otherRoom.roomUp == index)) ||
            ((this.roomLeft == index2) && (otherRoom.roomRight == index)));
        }

        /**
         * Whether the rectangle overlaps any walls in this room.
         * @param x the x value of the left side of the rectangle (in ball scale)
         * @param y the y value of the top edge of the rectangle (in ball scale)
         * @param width the width of the rectangle (in ball scale)
         * @param height the height of the rectangle (in ball scale)
         */
        public bool hitsWall(int x, int y, int width, int height)
        {
            // Convert corners of rectangle down to wall coordinates
            const int MIRROR_EDGE = 2 * GRAPHICS_LENGTH - 1;
            int top = y / Map.WALL_HEIGHT;
            int bottom = (y - height + 1) / Map.WALL_HEIGHT;
            bottom = (bottom < 0 ? 0 : bottom);
            int left = x / Map.WALL_WIDTH;
            int right = (x + width - 1) / Map.WALL_WIDTH;
            right = (right > MIRROR_EDGE ? MIRROR_EDGE : right);

            bool hitWall = false;
            for (int yctr = bottom; !hitWall && (yctr <= top); ++yctr)
            {
                for (int xctr = left; !hitWall && (xctr <= right); ++xctr)
                {
                    hitWall = walls[xctr, yctr];
                }
            }

            return hitWall;
        }

        /** 
         * Translate an ascii map of a room to a boolean map.
         * @param asciimap a 40x7 array of characters where spaces are no walls
         * @return a 40x7 array of booleans. If return[12][3] is true then there is a wall
         * at the fourth row from the bottom and 13th column from the left.
         */
        private bool[,] readWalls(string[] asciimap, byte[] graphicsData)
        {
            bool[,] oldway = decodeGraphicsData(graphicsData);
            bool[,] boolmap = new bool[Map.MAX_WALL_X, Map.MAX_WALL_Y];

            string oldmap = "old map:\n";
            string newmap = "new map:\n";
            for (int y = Map.MAX_WALL_Y-1; y >= 0; --y)
            {
                for (int x = 0; x < Map.MAX_WALL_X; ++x)
                {
                    oldmap += (oldway[x, y] ? '■' : '□');
                    newmap += (asciimap[Map.MAX_WALL_Y - y - 1][x] != ' ' ? '■' : '□');
                }
                oldmap += "\n";
                newmap += "\n";
            }
            for (int y = 0; y < Map.MAX_WALL_Y; ++y)
            {
                for (int x = 0; x < Map.MAX_WALL_X; ++x)
                {
                    boolmap[x, y] = (asciimap[Map.MAX_WALL_Y-y-1][x] != ' ');
                    if (boolmap[x,y] != oldway[x,y])
                    {
                        UnityEngine.Debug.Log(oldmap);
                        UnityEngine.Debug.Log(newmap);
                        throw new Exception("Wall constructs at (" + x + "," + y + ") in " + label + " don't match.");
                    }
                }
            }

            return boolmap;
        }

        /** 
         * Translate the array of Atari Graphics bit masks to a 2D boolean map of the room.
         * @param graphicsData the graphics bitmasks
         * @return a 40x7 array of booleans. If return[12][3] is true then there is a wall
         * at the fourth row from the bottom and 13th column from the left.
         */
        private bool[,] decodeGraphicsData(byte[] graphicsData)
        {
            const int MIRROR_EDGE = 2 * GRAPHICS_LENGTH - 1;
            byte[] shiftreg = {
                        0x10,0x20,0x40,0x80,
                        0x80,0x40,0x20,0x10,0x8,0x4,0x2,0x1,
                        0x1,0x2,0x4,0x8,0x10,0x20,0x40,0x80
                    };
            bool mirror = (flags & FLAG_MIRROR) > 0;

            bool[,] decoded = new bool[Map.MAX_WALL_X, Map.MAX_WALL_Y];
            for (int x = 0; x<Map.MAX_WALL_X; ++x)
            {
                for (int y = 0; y<Map.MAX_WALL_Y; ++y)
                {
                    int ypos = 6 - y;
                    byte bit;
                    int xpos = (x < GRAPHICS_LENGTH ? x : (mirror ? MIRROR_EDGE - x : x - GRAPHICS_LENGTH));
                    if (xpos < 4)
                    {
                        byte pf0 = graphicsData[(ypos * 3) + 0];
                        bit = (byte)(pf0 & shiftreg[xpos]);
                    }
                    else if (xpos < 12)
                    {
                        byte pf1 = graphicsData[(ypos * 3) + 1];
                        bit = (byte)(pf1 & shiftreg[xpos]);
                    }
                    else
                    {
                        byte pf2 = graphicsData[(ypos * 3) + 2];
                        bit = (byte)(pf2 & shiftreg[xpos]);
                    }
                    decoded[x, y] = (bit > 0);
                }
            }
            return decoded;
        }
    }
}
