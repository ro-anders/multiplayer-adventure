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

#include "adventure_sys.h"

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

/**
 * Check that the game is executing fast enough.  If it isn't, display a recommendation to shrink the screen.
 */
void Adventure_CheckTime(float currentScale);

/**
 Run one iteration of the game
 */
void Adventure_Run();

// Platform-dependant functions
void Platform_PaintPixel(int r, int g, int b, int x, int y, int width=1, int height=1);
void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire);
void Platform_ReadConsoleSwitches(bool* reset);
void Platform_MuteSound(bool mute);
void Platform_MakeSound(int sound, float volume);
#define MAX_VOLUME              11.0f
float Platform_Random();
void Platform_DisplayStatus(const char* message, int durationSec);
void Platform_DisplayAnnouncement(const char* message, const char* link);
void Platform_ReportToServer(const char* message);

// Difficulty switches
// When the left difficulty switch is in the B position, the Dragons will hesitate before they bite you.
// If the right difficulty switch is in the B position all Dragons will run from the sword.
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

