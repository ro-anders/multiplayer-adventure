﻿using System;
namespace GameEngine
{
    public class Adv
    {
        public const int ADVENTURE_SCREEN_WIDTH = 320;
        public const int ADVENTURE_SCREEN_HEIGHT = 192;
        public const int ADVENTURE_OVERSCAN = 16;
        public const int ADVENTURE_TOTAL_SCREEN_HEIGHT = (ADVENTURE_SCREEN_HEIGHT + ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN);
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
        public static int TRACE_PLAYER = -1; // Set when you want verbose trace logging of a single player
    }

    /** Rectangle in a room.  Coordinates are ball-scale coordinates
     * where height extends down from y and width extends right from x.
     */
    public readonly struct RRect
    {
        public static readonly RRect INVALID = new RRect(-1, -1, -1, -1, -1);
        public static readonly RRect NOWHERE = new RRect(-1, -1, -1, 0, 0);

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
        public override string ToString()
        {
            return room + "(" + (width==1?x.ToString():left+"-"+right) + "," + (height==1?y.ToString():bottom + "-" + top) + ")";
        }
    }

}
