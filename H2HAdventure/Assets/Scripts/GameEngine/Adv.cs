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

        // The game modes
        public const int GAME_MODE_1 = 0;
        public const int GAME_MODE_2 = 1;
        public const int GAME_MODE_3 = 2;
        public const int GAME_MODE_GAUNTLET = 3;
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

    public static class MAX
    {
        public const float VOLUME = 11.0f;
    }


}
