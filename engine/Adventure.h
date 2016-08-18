//
// Adventure: Revisited
// C++ Version Copyright © 2007 Peter Hirschberg
// peter@peterhirschberg.com
// http://peterhirschberg.com
//
// Big thanks to Joel D. Park and others for annotating the original Adventure decompiled assembly code.
// I relied heavily and deliberately on that commented code.
//
// Original Adventure™ game Copyright © 1980 ATARI, INC.
// Any trademarks referenced herein are the property of their respective holders.
// 
// Original game written by Warren Robinett. Warren, you rock.
//

#define ADVENTURE_SCREEN_WIDTH              320
#define ADVENTURE_SCREEN_HEIGHT             192
#define ADVENTURE_OVERSCAN                  16
#define ADVENTURE_TOTAL_SCREEN_HEIGHT       (ADVENTURE_SCREEN_HEIGHT + ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN)
#define ADVENTURE_FPS                       58

// Game funtions
class Transport;

/**
 * Setup the game.
 * numPlayers - 2 or 3 players
 * thisPlayer - which player this is.  0-2.
 * transport - the transport to use to talk to other games
 * gameNum - which game/map to play (-1 = scripting, 0-2 = atari games 1-3, 3 = gauntlet)
 * initialLeftDiff - initial value of left difficulty switch (0 = A, 1 = B)
 * initialRightDiff - initial value of right difficulty switch (0 = A, 1 = B)
 */
void Adventure_Setup(int numPlayers, int thisPlayer, Transport* transport, int gameNum, int initialLeftDiff, int initialRightDiff);
void Adventure_Run();

// Platform-dependant functions
void Platform_PaintPixel(int r, int g, int b, int x, int y, int width=1, int height=1);
void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire);
void Platform_ReadConsoleSwitches(bool* reset);
void Platform_ReadDifficultySwitches(int* left, int* right);
void Platform_MuteSound(bool mute);
void Platform_MakeSound(int sound, float volume);
#define MAX_VOLUME              11.0
float Platform_Random();

// Difficulty switch values
#define DIFFICULTY_A           0
#define DIFFICULTY_B           1

// The sounds
#define SOUND_WON       0
#define SOUND_ROAR      1
#define SOUND_EATEN     2
#define SOUND_DRAGONDIE 3
#define SOUND_PUTDOWN   4
#define SOUND_PICKUP    5
#define SOUND_GLOW      6

// The game modes
#define GAME_MODE_SCRIPTING  -1
#define GAME_MODE_1  0
#define GAME_MODE_2  1
#define GAME_MODE_3  2
#define GAME_MODE_GAUNTLET  3

