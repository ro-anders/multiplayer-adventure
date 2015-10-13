//
// Adventure: Revisited
// C++ Version Copyright © 2006 Peter Hirschberg
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

// Some types
typedef unsigned long color;
typedef unsigned char byte;

// Game funtions
void Adventure_Run();

// Platform-dependant functions
void Platform_PaintPixel(int r, int g, int b, int x, int y, int width=1, int height=1);
void Platform_ReadJoystick(bool* left, bool* up, bool* right, bool* down, bool* fire);
void Platform_ReadConsoleSwitches(bool* select, bool* reset);
void Platform_ReadDifficultySwitches(int* left, int* right);
void Platform_MakeSound(int sound);
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
