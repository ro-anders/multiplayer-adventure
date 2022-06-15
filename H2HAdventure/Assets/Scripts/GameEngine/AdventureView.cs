using System;
namespace GameEngine
{

    public interface AdventureView
    {
        /**
         * Paint a box on the screen.  Unlike Adventure coordinates, this paints from y up.
         */
        void Platform_PaintPixel(int r, int g, int b, int x, int y, int width, int height);

        void Platform_ReadJoystick(ref bool joyLeft, ref bool joyUp, ref bool joyRight, ref bool joyDown, ref bool joyFire);

        void Platform_ReadConsoleSwitches(ref bool reset);

        void Platform_MakeSound(SOUND sound, float volume);

        void Platform_ReportToServer(string message);

        void Platform_DisplayStatus(string message, int durationSecs);

        void Platform_PopupHelp(string message, string imageName);

        void Platform_GameChange(GAME_CHANGES change);
    }

    public class AdventureReports
    {
        public const string WON_GAME = "Has won a game";
        public const string FOUND_ROBINETT_ROOM = "Robinett Room entered";
        public const string GLIMPSED_CRYSTAL_CASTLE = "Crystal castle glimsed";
        public const string FOUND_CRYSTAL_CASTLE = "Crystal castle found";
        public const string FOUND_CRYSTAL_KEY = "Crystal key found";
        public const string OPENED_CRYSTAL_GATE = "Crystal gate opened";
        public const string BEAT_CRYSTAL_CHALLENGE = "Crystal challenge beaten";
    }

}
