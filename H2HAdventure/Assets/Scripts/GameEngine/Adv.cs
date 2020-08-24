using System;
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

}
