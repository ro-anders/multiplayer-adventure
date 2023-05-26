using System;
namespace GameEngine
{
    public class Adv
    {
        public const int ADVENTURE_SCREEN_BWIDTH = 320; // In ball scale
        public const int ADVENTURE_SCREEN_BHEIGHT = 192; // In ball scale
        public const int ADVENTURE_OVERSCAN_BHEIGHT = 16;
        public const int ADVENTURE_TOTAL_SCREEN_HEIGHT = (ADVENTURE_SCREEN_BHEIGHT + ADVENTURE_OVERSCAN_BHEIGHT + ADVENTURE_OVERSCAN_BHEIGHT);
        public const double ADVENTURE_FRAME_PERIOD = 0.017;
        public const int ADVENTURE_MAX_NAME_LENGTH = 40;

        public const int BALL_SCALE = 2; // The ball's x,y is at twice the resolution as object's x,y

        // The game modes
        public const int GAME_MODE_1 = 0;
        public const int GAME_MODE_2 = 1;
        public const int GAME_MODE_3 = 2;
        public const int GAME_MODE_C_1 = 3;
        public const int GAME_MODE_C_2 = 4;
        public const int GAME_MODE_C_3 = 5;
        public const int GAME_MODE_ROLE_PLAY = 6;
        public const int GAME_MODE_GAUNTLET = 7;
    }

    public enum SOUND
    {
        WON = 0,
        ROAR,
        EATEN,
        DRAGONDIE,
        PUTDOWN,
        PICKUP,
        GLOW
    }

    public enum GAME_CHANGES
    {
        GAME_STARTED,
        GAME_ENDED
    }

    public static class MAX
    {
        public const float VOLUME = 11.0f;
    }

    public static class MOD
    {
        /** 
         * x modulo m, though different from "x % m" because
         * C# does not do modulo of negative numbers correctly.
         * This function will return:
         *   mod(6, 5) = 1
         *   mod(-2, 5) = 3 
         */
        public static int mod(int x, int m)
        {
            return (x % m + m) % m;
        }
    }

    /**
     * A useful class for tailoring how classes log things during
     * intense debugging
     * */
    public static class DEBUG
    {
        public static int TRACE_PLAYER = 0; // Set when you want verbose trace logging of a single player
    }

    /** Rectangle in a room.  Coordinates are ball-scale coordinates
     * where height extends down from y and width extends right from x.
     */
    public readonly struct RRect
    {
        /** Used to represent an area that can't exist (like when asking for
         * the intersection of two disjoint rectangles or for closest rectangle
         * in a room when that room isn't reachable */
        public static readonly RRect INVALID = new RRect(-1, -1, -1, -1, -1);

        /** Used, kind of like null, as a convention when an RRect is a target
         * we check for NOWHERE to see if we even want to go somewhere. */
        public static readonly RRect NOWHERE = new RRect(-1, -1, -1, 0, 0);

        public static RRect fromTRBL(int room, int top, int right, int bottom, int left)
        {
            return new RRect(room, left, top, right - left + 1, top - bottom + 1);
        }

        public RRect(int inRoom, int inX, int inY, int inWidth, int inHeight)
        {
            room = inRoom;
            x = inX;
            y = inY;
            width = inWidth;
            height = inHeight;
        }
        public readonly int room;
        public readonly int x;
        public readonly int y;
        public readonly int width;
        public readonly int height;
        public int top
        {
            get { return y; }
        }
        public int right
        {
            get { return x + width - 1; }
        }
        public int bottom
        {
            get { return y - height + 1; }
        }
        public int left
        {
            get { return x; }
        }
        public int midX
        {
            get { return x + (width / 2); }
        }
        public int midY
        {
            get { return y - (height / 2); }
        }
        public bool equals(RRect other)
        {
            return (room == other.room) && (x == other.x) && (y == other.y) &&
                (width == other.width) && (height == other.height);
        }
        public bool IsValid
        {
            get { return (width >= 0) && (height >= 0); }
        }
        public bool IsSomewhere
        {
            get { return (width >= 0) && (height >= 0) && (room >= 0); }
        }

        /**
         * Returns true if this rectangles is in the same room 
         * and overlap the passed in rectangle.
         */
        public bool overlaps(RRect other)
        {
            if (other.room != this.room)
            {
                return false;
            }
            else
            {
                return
                    (other.left <= this.right) &&
                    (other.right >= this.left) &&
                    (other.top >= this.bottom) &&
                    (other.bottom <= this.top);
            }
        }

        /**
         * Returns true if this rectangles is in the same room 
         * and completely encompasss the passed in rectangle.
         */
        public bool contains(RRect other)
        {
            if (other.room != this.room)
            {
                return false;
            }
            else
            {
                return
                    (other.left >= this.left) &&
                    (other.right <= this.right) &&
                    (other.top <= this.top) &&
                    (other.bottom >= this.bottom);
            }
        }

        /**
         * Returns the rectangle that is formed by the overlap of this
         * rectangle with another or INVALID if the rectangles don't overlap.
         */
        public RRect intersect(RRect other)
        {
            if (!overlaps(other))
            {
                return RRect.INVALID;
            }

            return RRect.fromTRBL(this.room,
                Math.Min(other.top, this.top),
                Math.Min(other.right, this.right),
                Math.Max(other.bottom, this.bottom),
                Math.Max(other.left, this.left));
        }

        public override string ToString()
        {
            return room + dimensionsToString();
        }
        // If someone else is willing to give us the name of the room, we can
        // create a more user friendly string representation
        public string ToStringWithRoom(string room)
        {
            return dimensionsToString() + " in " + room;
        }
        private string dimensionsToString()
        {
            return "(" + (width == 1 ? x.ToString() : left + "-" + right) + "," + (height == 1 ? y.ToString() : bottom + "-" + top) + ")";
        }
    }

}
