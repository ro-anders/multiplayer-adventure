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

#ifdef WIN32
#include <Windows.h>
#endif

#include <stdlib.h>
#include <stdio.h>

#include "Adventure.h"

#include "adventure_sys.h"
#include "Bat.hpp"
#include "Dragon.hpp"
#include "GameObject.hpp"
#include "Sync.hpp"
#include "Transport.hpp"

#ifndef max
#define max(a,b) ((a > b) ? a : b);
#endif


#define PLAYFIELD_HRES      20  // 40 with 2nd half mirrored/repeated
#define PLAYFIELD_VRES      20
#define CLOCKS_HSYNC        2
#define CLOCKS_VSYNC        4

// Types

typedef struct BALL
{
    int room;                   // room
    int x;                      // x position
    int y;                      // y position
    int previousX;              // previous x position
    int previousY;              // previous y position
	int velx;					// Current horizontal speed (walls notwithstanding).  Positive = right.  Negative = left.
	int vely;					// Current vertical speed (walls notwithstanding).  Positive = right.  Negative = down.
	int linkedObject;           // index of linked (carried) object
    int linkedObjectX;          // X value representing the offset from the ball to the object being carried
    int linkedObjectY;          // Y value representing the offset from the ball to the object being carried
    bool hitX;                  // the ball hit something on the X axis
    bool hitY;                  // the ball hit something on the Y axis
    int hitObject;              // the object that the ball hit
	const byte* gfxData;		// graphics data for ball
}BALL;

typedef struct COLOR
{
    int r,g,b;
}COLOR;

typedef struct ROOM
{
    const byte* graphicsData;   // pointer to room graphics data
    byte flags;                 // room flags - see below
    int color;                  // foreground color
    int roomUp;                 // index of room UP
    int roomRight;              // index of room RIGHT
    int roomDown;               // index of room DOWN
    int roomLeft;               // index of room LEFT
}ROOM;
#define ROOMFLAG_NONE           0x00
#define ROOMFLAG_MIRROR         0x01 // bit 0 - 1 if graphics are mirrored, 0 for reversed
#define ROOMFLAG_LEFTTHINWALL   0x02 // bit 1 - 1 for left thin wall
#define ROOMFLAG_RIGHTTHINWALL  0x04 // bit 2 - 1 for right thin wall

enum
{
    OBJECT_NONE=-1,
    OBJECT_PORT1=0,
	OBJECT_PORT4,
    OBJECT_PORT2,
    OBJECT_PORT3,
    OBJECT_NAME,
    OBJECT_NUMBER,
    OBJECT_REDDRAGON,
    OBJECT_YELLOWDRAGON,
    OBJECT_GREENDRAGON,
    OBJECT_SWORD,
    OBJECT_BRIDGE,
    OBJECT_YELLOWKEY,
    OBJECT_WHITEKEY,
    OBJECT_BLACKKEY,
    OBJECT_BAT,
    OBJECT_DOT,
    OBJECT_CHALISE,
    OBJECT_MAGNET
};

#define OBJECT_BALL			(-2)
#define OBJECT_SURROUND		(-3)
#define OBJECT_LEFTWALL		(-4)
#define OBJECT_RIGHTWALL	(-5)


// Functions from original game
static void SetupRoomObjects();
void ReactToCollision(BALL* ball);
static void BallMovement(BALL* ball);
static void ThisBallMovement();
static void OtherBallMovement();
static void MoveCarriedObjects();
static void MoveGroundObject();
static void PrintDisplay();
static void PickupPutdown();
static void OthersPickupPutdown();
static void Surround();
static void MoveBat();
static void Portals();
static void SyncDragons();
static void MoveDragon(Dragon* dragon, const int* matrix, int speed);
static void Magnet();
static int AdjustRoomLevel(int room);

// My helper functions
static void DrawObjects(int room);
static void DrawObject(const OBJECT* object);
void DrawBall(const BALL* ball, COLOR color);
static bool CrossingBridge(int room, int x, int y, BALL* ball);
static bool CollisionCheckBallWithWalls(int room, int x, int y);
static int CollisionCheckBallWithObjects(BALL* ball, int startIndex);
bool CollisionCheckObjectObject(const OBJECT* object1, const OBJECT* object2);
static bool CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height);
void CalcPlayerSpriteExtents(const OBJECT* object, int* cx, int* cy, int* cw, int* ch);
static bool HitTestRects(int ax, int ay, int awidth, int aheight,
                    int bx, int by, int bwidth, int bheight);
static int distanceFromBall(BALL* ball, int x, int y);

COLOR GetFlashColor();
void AdvanceFlashColor();

//
// local game state vars
//

static bool joyLeft, joyUp, joyRight, joyDown, joyFire;
static bool switchSelect, switchReset;

#define MAX_OBJECTS             16                      // Should be plenty
static bool showObjectFlicker = true;                   // True if accurate object flicker is desired
static const int maxDisplayableObjects = 2;             // The 2600 only has 2 Player (sprite) objects. Accuracy will be compromised if this is changed!
static int displayListIndex = 0;

// finite state machine values
#define GAMESTATE_GAMESELECT    0
#define GAMESTATE_ACTIVE_1      1
#define GAMESTATE_ACTIVE_2      2
#define GAMESTATE_ACTIVE_3      3
#define GAMESTATE_WIN           4
static int gameState = GAMESTATE_GAMESELECT;            // finite state machine
#define ISGAMEACTIVE() ((gameState==GAMESTATE_ACTIVE_1) || (gameState==GAMESTATE_ACTIVE_2) || (gameState==GAMESTATE_ACTIVE_3))

// Difficulty switches
// When the left difficulty switch is in the B position, the Dragons will hesitate before they bite you.
// If the right difficulty switch is in the B position all Dragons will run from the sword.
#define DIFFICULTY_A           0
#define DIFFICULTY_B           1
static int gameDifficultyLeft = DIFFICULTY_B;           // 2600 left difficulty switch
static int gameDifficultyRight = DIFFICULTY_B;          // 2600 right difficulty switch
static int gameLevel = 0;                               // current game level (1,2,3 - zero justified)
static int gameNum; // Which game is being played.  May be different from game level.

static int displayedRoomIndex = 0;                                   // index of current (displayed) room

static int batFedUpTimer = 0xff;

static int winFlashTimer=0;

static int flashColorHue=0;
static int flashColorLum=0;

//
// Color lookup table (RGB)
//
static const COLOR colorTable [] = 
{
    { 0x00,0x00,0x00 }, // black (0x0)
    { 0xcd,0xcd,0xcd }, // light gray (0x08)
    { 0xff,0xff,0xff }, // white (0x0e)
    { 0xFF,0xD8,0x4C }, // yellow (0x1a)
    { 0xff,0x98,0x2c }, // orange (0x28)
    { 0xfa,0x52,0x55 }, // red (0x36)
    { 0xA2,0x51,0xD9 }, // purple (0x66)
    { 0x6b,0x64,0xff }, // blue (0x86)
    { 0x55,0xb6,0xff }, // light cyan  (0x98)
    { 0x61,0xd0,0x70 }, // cyan  (0xa8)
    { 0x21,0xd9,0x1b }, // dark green (0xb8)
    { 0x86,0xd9,0x22 }, // lime green (0xc8)
    { 0xa1,0xb0,0x34 }, // olive green (0xd8)
    { 0xd5,0xb5,0x43 }, // tan  (0xe8)
    { 0xa8,0xfc,0x41 }  // flash (0xcb)
};  
enum { COLOR_BLACK=0, COLOR_LTGRAY, COLOR_WHITE, COLOR_YELLOW, COLOR_ORANGE, COLOR_RED, COLOR_PURPLE, COLOR_BLUE, COLOR_LTCYAN, COLOR_CYAN, COLOR_DKGREEN, COLOR_LIMEGREEN, COLOR_OLIVEGREEN, COLOR_TAN, COLOR_FLASH };

// 
// Room graphics
//

// Left of Name Room                                                                                                 
static const byte roomGfxLeftOfName [] =
{
    0xF0,0xFF,0xFF,     // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRRRRRR                                  
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRRRRRR
};

// Below Yellow Castle                                                                                               
static const byte roomGfxBelowYellowCastle [] =
{
    0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRRRRRR
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
	0x00,0x00,0x00,
	0x00,0x00,0x00,
	0x00,0x00,0x00,
    0xF0,0xFF,0xFF      // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRRRRRR                                  
};
                                                                                                                
                                                                                                                
// Side Corridor                                                                                                     
static const byte roomGfxSideCorridor [] =
{
    0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0x00,0x00,0x00,                                                                                           
    0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
};
                                                                                                                
                                                                                                                
// Number Room Definition                                                                                            
static const byte roomGfxNumberRoom [] =
{
    0xF0,0xFF,0xFF,     // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR                                      
};

// `                                                                                                     
static const byte roomGfxTwoExitRoom [] =
{
    0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0x30,0x00,0x00,     // XX                                    RR                                      
    0xF0,0xFF,0x0F      // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
};

// Top of Blue Maze                                                                                                  
static const byte roomGfxBlueMazeTop[] =
{
    0xF0,0xFF,0x0F,     // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
    0x00,0x0C,0x0C,     //         XX    XX        RR    RR                                              
    0xF0,0x0C,0x3C,     // XXXX    XX    XXXX    RRRR    RR    RRRR                                      
    0xF0,0x0C,0x00,     // XXXX    XX                    RR    RRRR                                      
    0xF0,0xFF,0x3F,     // XXXXXXXXXXXXXXXXXX    RRRRRRRRRRRRRRRRRR                                      
    0x00,0x30,0x30,     //       XX        XX    RR        RR                                            
    0xF0,0x33,0x3F      // XXXX  XX  XXXXXXXX    RRRRRRRR  RR  RRRR                                      
};

// Blue Maze #1                                                                                                      
static const byte roomGfxBlueMaze1 [] =
{
    0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
    0x00,0x00,0x00,          //                                                                               
    0xF0,0xFC,0xFF,          // XXXXXXXXXX  XXXXXXXXRRRRRRRR  RRRRRRRRRR                                      
    0xF0,0x00,0xC0,          // XXXX              XXRR              RRRR                                      
    0xF0,0x3F,0xCF,          // XXXX  XXXXXXXXXX  XXRR  RRRRRRRRRR  RRRR                                      
    0x00,0x30,0xCC,          //       XX      XX  XXRR  RR      RR                                            
    0xF0,0xF3,0xCC           // XXXXXXXX  XX  XX  XXRR  RR  RR  RRRRRRRR                                      
};
                                                                                                                
// Bottom of Blue Maze                                                                                               
static const byte roomGfxBlueMazeBottom [] =
{
    0xF0,0xF3,0x0C,          // XXXXXXXX  XX  XX        RR  RR  RRRRRRRR                                      
    0x00,0x30,0x0C,          //       XX      XX        RR      RR                                           
    0xF0,0x3F,0x0F,          // XXXX  XXXXXXXXXX        RRRRRRRRRR  RRRR                                      
    0xF0,0x00,0x00,          // XXXX                                RRRR                                      
    0xF0,0xF0,0x00,          // XXXXXXXX                        RRRRRRRR                                      
    0x00,0x30,0x00,          //       XX                        RR                                            
    0xF0,0xFF,0xFF           // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR                                      
};
                                                                                                                
// Center of Blue Maze                                                                                               
static const byte roomGfxBlueMazeCenter [] =
{
    0xF0,0x33,0x3F,          // XXXX  XX  XXXXXXXX    RRRRRRRR  RR  RRRR                                      
    0x00,0x30,0x3C,          //       XX      XXXX    RRRR      RR                                            
    0xF0,0xFF,0x3C,          // XXXXXXXXXXXX  XXXX    RRRR  RRRRRRRRRRRR                                      
    0x00,0x03,0x3C,          //           XX  XXXX    RRRR  RR                                                
    0xF0,0x33,0x3C,          // XXXX  XX  XX  XXXX    RRRR  RR  RR  RRRR                                      
    0x00,0x33,0x0C,          //       XX  XX  XX        RR  RR  RR                                            
    0xF0,0xF3,0x0C           // XXXXXXXX  XX  XX        RR  RR  RRRRRRRR                                      
};
                                                                                                                
// Blue Maze Entry                                                                                                   
static const byte roomGfxBlueMazeEntry [] =
{
    0xF0,0xF3,0xCC,          // XXXXXXXX  XX  XX  XXRR  RR  RR  RRRRRRRR                                      
    0x00,0x33,0x0C,          //       XX  XX  XX        RR  RR  RR                                            
    0xF0,0x33,0xFC,          // XXXX  XX  XX  XXXXXXRRRRRR  RR  RR  RRRR                                      
    0x00,0x33,0x00,          //       XX  XX                RR  RR                                            
    0xF0,0xF3,0xFF,          // XXXXXXXX  XXXXXXXXXXRRRRRRRRRR  RRRRRRRR                                      
    0x00,0x00,0x00,          //                                                                               
    0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
};
                                                                                                                
// Maze Middle                                                                                                       
static const byte roomGfxMazeMiddle [] =
{
    0xF0,0xFF,0xCC,          // XXXXXXXXXXXX  XX  XXRR  RR  RRRRRRRRRRRR                                      
    0x00,0x00,0xCC,          //               XX  XXRR  RR                                                    
    0xF0,0x03,0xCF,          // XXXX      XXXXXX  XXRR  RRRRRR      RRRR                                      
    0x00,0x03,0x00,          //           XX                RR                                                
    0xF0,0xF3,0xFC,          // XXXXXXXX  XX  XXXXXXRRRRRR  RR  RRRRRRRR                                     
    0x00,0x33,0x0C,          //       XX  XX  XX        RR  RR  RR                                            
    0xF0,0x33,0xCC           // XXXX  XX  XX  XX  XXRR  RR  RR  RR  RRRR
};

// Maze Side 
static const byte roomGfxMazeSide [] =
{
    0xF0,0x33,0xCC,          // XXXX  XX  XX  XX  XXRR  RR  RR  RR  RRRR
    0x00,0x30,0xCC,          //       XX      XX  XXRR  RR      RR                                            
    0x00,0x3F,0xCF,          //       XXXXXX  XX  XXRR  RR  RRRRRR                                            
    0x00,0x00,0xC0,          //                   XXRR                                                        
    0x00,0x3F,0xC3,          //       XXXXXXXX    XXRR    RRRRRRRR                                            
    0x00,0x30,0xC0,          //       XX          XXRR          RR                                            
    0xF0,0xFF,0xFF           // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR                                      
};
                                                                                                                
// Maze Entry                                                                                                        
static const byte roomGfxMazeEntry [] =
{
    0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
    0x00,0x30,0x00,          //       XX                        RR                                            
    0xF0,0x30,0xFF,          // XXXX  XX    XXXXXXXXRRRRRRRRR   RR  RRRR                                      
    0x00,0x30,0xC0,          //       XX          XXRR          RR                                            
    0xF0,0xF3,0xC0,          // XXXXXXXX  XX      XXRR      RR  RRRRRRRR                                      
    0x00,0x03,0xC0,          //           XX      XXRR      RR                                                
    0xF0,0xFF,0xCC           // XXXXXXXXXXXX  XX  XXRR  RR  RRRRRRRRRRRR                                      
};

// Castle
static const byte roomGfxCastle [] =
{
    0xF0,0xFE,0x15,      // XXXXXXXXXXX X X X      R R R RRRRRRRRRRR                                      
    0x30,0x03,0x1F,      // XX        XXXXXXX      RRRRRRR        RR                                      
    0x30,0x03,0xFF,      // XX        XXXXXXXXXXRRRRRRRRRR        RR                                      
    0x30,0x00,0xFF,      // XX          XXXXXXXXRRRRRRRR          RR                                      
    0x30,0x00,0x3F,      // XX          XXXXXX    RRRRRR          RR                                      
    0x30,0x00,0x00,      // XX                                    RR                                      
    0xF0,0xFF,0x0F       // XXXXXXXXXXXXXX            RRRRRRRRRRRRRR                                      
};

// Red Maze #1                                                                                                       
static const byte roomGfxRedMaze1 [] =
{
     0xF0,0xFF,0xFF,          // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR                                      
     0x00,0x00,0x00,          //                                                                               
     0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
     0x00,0x00,0x0C,          //                   XX        RR                                                
     0xF0,0xFF,0x0C,          // XXXXXXXXXXXX  XX        RR  RRRRRRRRRRRR                                      
     0xF0,0x03,0xCC,          // XXXX      XX  XX  XXRR  RR  RR      RRRR                                      
     0xF0,0x33,0xCF           // XXXX  XX  XXXXXX  XXRR  RRRRRR  RR  RRRR
};

// Bottom of Red Maze                                                                                                
static const byte roomGfxRedMazeBottom [] =
{
     0xF0,0x33,0xCF,          // XXXX  XX  XXXXXX  XXRR  RRRRRR  RR  RRRR
     0xF0,0x30,0x00,          // XXXX  XX                        RR  RRRR                                      
     0xF0,0x33,0xFF,          // XXXX  XX  XXXXXXXXXXRRRRRRRRRR  RR  RRRR                                      
     0x00,0x33,0x00,          //       XX  XX                RR  RR  RRRR                                      
     0xF0,0xFF,0x00,          // XXXXXXXXXXXX                RRRRRRRRRRRR                                      
     0x00,0x00,0x00,          //                                                                               
     0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
};
                                                                                                                
// Top of Red Maze                                                                                                   
static const byte roomGfxRedMazeTop [] =
{
     0xF0,0xFF,0xFF,          // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR                                      
     0x00,0x00,0xC0,          //                   XXRR                                                        
     0xF0,0xFF,0xCF,          // XXXXXXXXXXXXXXXX  XXRR  RRRRRRRRRRRRRRRR                                      
     0x00,0x00,0xCC,          //               XX  XXRR  RR                                                    
     0xF0,0x33,0xFF,          // XXXX  XX  XXXXXXXXXXRRRRRRRRRR  RR  RRRR                                      
     0xF0,0x33,0x00,          // XXXX  XX  XX                RR  RR  RRRR                                      
     0xF0,0x3F,0x0C           // XXXX  XXXXXX  XX        RR  RRRRRR  RRRR
};


// White Castle Entry                                                                                                
static const byte roomGfxWhiteCastleEntry [] =
{
     0xF0,0x3F,0x0C,          // XXXX  XXXXXX  XX        RR  RRRRRR  RRRR
     0xF0,0x00,0x0C,          // XXXX          XX        RR          RRRR                                      
     0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
     0x00,0x30,0x00,          //       XX                        RR                                            
     0xF0,0x30,0x00,          // XXXX  XX                        RR  RRRR                                      
     0x00,0x30,0x00,          //       XX                        RR                                            
     0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
};
                                                                                                                
// Top Entry Room  
static const byte roomGfxTopEntryRoom [] =
{
     0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
     0x30,0x00,0x00,          // XX                                    RR                                      
     0x30,0x00,0x00,          // XX                                    RR                                      
     0x30,0x00,0x00,          // XX                                    RR                                      
     0x30,0x00,0x00,          // XX                                    RR                                      
     0x30,0x00,0x00,          // XX                                    RR                                      
     0xF0,0xFF,0xFF           // XXXXXXXXXXXXXXXXXXXXRRRRRRRRRRRRRRRRRRRR                                      
};
                                                                                                                
// Black Maze #1                                                                                                     
static const byte roomGfxBlackMaze1 [] =
{
     0xF0,0xF0,0xFF,          // XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR                                      
     0x00,0x00,0x03,          //             XX            RR                                                  
     0xF0,0xFF,0x03,          // XXXXXXXXXXXXXX            RRRRRRRRRRRRRR                                      
     0x00,0x00,0x00,          //                                                                               
     0x30,0x3F,0xFF,          // XX    XXXXXXXXXXXXXXRRRRRRRRRRRRRR    RR                                      
     0x00,0x30,0x00,          //       XX                        RR                                            
     0xF0,0xF0,0xFF           // XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR
};

// Black Maze #3                                                                                                     
static const byte roomGfxBlackMaze3 [] =
{
     0xF0,0xF0,0xFF,          // XXXXXXXX    XXXXXXXXRRRRRRRR    RRRRRRRR
     0x30,0x00,0x00,          // XX                  MM                                                        
     0x30,0x3F,0xFF,          // XX    XXXXXXXXXXXXXXMM    MMMMMMMMMMMMMM                                      
     0x00,0x30,0x00,          //       XX                  MM                                                  
     0xF0,0xF0,0xFF,          // XXXXXXXX    XXXXXXXXMMMMMMMM    MMMMMMMM                                      
     0x30,0x00,0x03,          // XX          XX      MM          MM                                            
     0xF0,0xF0,0xFF           // XXXXXXXX    XXXXXXXXMMMMMMMM    MMMMMMMM                                      
};
                                                                                                                
// Black Maze #2                                                                                                     
static const byte roomGfxBlackMaze2 [] =
{
     0xF0,0xFF,0xFF,          // XXXXXXXXXXXXXXXXXXXXMMMMMMMMMMMMMMMMMMMM                                      
     0x00,0x00,0xC0,          //                   XX                  MM                                      
     0xF0,0xFF,0xCF,          // XXXXXXXXXXXXXXXX  XXMMMMMMMMMMMMMMMM  MM                                      
     0x00,0x00,0x0C,          //                   XX                  MM                                      
     0xF0,0x0F,0xFF,          // XXXX    XXXXXXXXXXXXMMMM    MMMMMMMMMMMM                                      
     0x00,0x0F,0xC0,          //         XXXX      XX        MMMM      MM                                      
     0x30,0xCF,0xCC           // XX  XX  XXXX  XX  XXMM  MM  MMMM  MM  MM
};

// Black Maze Entry                                                                                                  
static const byte roomGfxBlackMazeEntry [] =
{
     0x30,0xCF,0xCC,          // XX  XX  XXXX  XX  XXMM  MM  MMMM  MM  MM
     0x00,0xC0,0xCC,          //         XX        XX  XXRR  RR        RR                                      
     0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
     0x00,0x00,0x00,          //                                                                               
     0xF0,0xFF,0x0F,          // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
     0x00,0x00,0x00,          //                                                                               
     0xF0,0xFF,0x0F           // XXXXXXXXXXXXXXXX        RRRRRRRRRRRRRRRR                                      
};


//
// Object definitions - 1st byte is the height
//

static const byte objectGfxNum [] =
{
    // Object #5 State #1 Graphic :'1'
    7,
    0x04,                  //  X                                                                        
    0x0C,                  // XX                                                                        
    0x04,                  //  X                                                                        
    0x04,                  //  X                                                                        
    0x04,                  //  X                                                                        
    0x04,                  //  X                                                                        
    0x0E,                  // XXX                                                                       
    // Object #5 State #2 Grphic : '2'                                                                                   
    7,
    0x0E,                  //  XXX                                                                      
    0x11,                  // X   X                                                                     
    0x01,                  //     X                                                                     
    0x02,                  //    X                                                                      
    0x04,                  //   X                                                                       
    0x08,                  //  X                                                                        
    0x1F,                  // XXXXX                                                                     
    // Object #5 State #3 Graphic :'3'                                                                                   
    7,
    0x0E,                  //  XXX                                                                      
    0x11,                  // X   X                                                                     
    0x01,                  //     X                                                                     
    0x06,                  //   XX                                                                      
    0x01,                  //     X                                                                     
    0x11,                  // X   X                                                                     
    0x0E                   //  XXX                                                                      
};

// Number states
static const byte numberStates [] = 
{
    0,1,2
};

static const byte objectGfxPlayer1[] =
{
	8,
	0xFF,				   // XXXXXXXX
	0xFF,				   // XXXXXXXX
	0xFF,				   // XXXXXXXX
	0xFF,				   // XXXXXXXX
	0xFF,				   // XXXXXXXX
	0xFF,				   // XXXXXXXX
	0xFF,				   // XXXXXXXX
	0xFF 				   // XXXXXXXX
};

static const byte objectGfxPlayer2[] =
{
	8,
	0xFF,				   // XXXXXXXX
	0xBD,				   // X XXXX X
	0xDB,				   // XX XX XX
	0xE7,				   // XXX  XXX
	0xE7,				   // XXX  XXX
	0xDB,				   // XX XX XX
	0xBD,				   // X XXXX X
	0xFF 				   // XXXXXXXX
};

static const byte objectGfxPlayer3[] =
{
	8,
	0xFF,				   // XXXXXXXX
	0xE7,				   // XXX  XXX
	0xE7,				   // XXX  XXX
	0x81,				   // X      X
	0x81,				   // X      X
	0xE7,				   // XXX  XXX
	0xE7,				   // XXX  XXX
	0xFF 				   // XXXXXXXX
};


// Object #0B : State FF : Graphic
static const byte objectGfxKey [] =
{
    3,
    0x07,                  //      XXX
    0xFD,                  // XXXXXX X
    0xA7                   // X X  XXX
};

                                                                                                                   
// Object #1 : Graphic
static const byte objectGfxSurround [] =
{
    32,
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF,                  // XXXXXXXX                                                                  
    0xFF                   // XXXXXXXX                                                                  
};

OBJECT& objectSurround = *new OBJECT(objectGfxSurround, 0, 0, COLOR_ORANGE, -1, 0, 0, 0x07);

// Object #0A : State FF : Graphic                                                                                   
static const byte objectGfxBridge [] =
{
    24,
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0x42,                  //  X    X                                                                   
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3                   // XX    XX                                                                  
};

// Object #9 : State FF : Graphics                                                                                   
static const byte objectGfxSword [] =
{
    5,
    0x20,                  //   X                                                                       
    0x40,                  //  X                                                                        
    0xFF,                  // XXXXXXXX     
    0x40,                  //  X                                                                        
    0x20                   //   X                                                                       
};

// Object #0F : State FF : Graphic                                                                                   
static const byte objectGfxDot [] =
{
    1,
    0x80                   // X                                                                         
};

// Object #4 : State FF : Graphic                                                                                    
static const byte objectGfxAuthor [] =
{
    95,
    0xF0,                  // XXXX                                                                      
    0x80,                  // X                                                                         
    0x80,                  // X                                                                         
    0x80,                  // X                                                                         
    0xF4,                  // XXXX X                                                                    
    0x04,                  //      X                                                                    
    0x87,                  // X    XXX                                                                  
    0xE5,                  // XXX  X X                                                                  
    0x87,                  // X    XXX                                                                  
    0x80,                  // X                                                                         
    0x05,                  //      X X                                                                  
    0xE5,                  // XXX  X X                                                                 
    0xA7,                  // X X  XXX                                                                  
    0xE1,                  // XXX    X                                                                  
    0x87,                  // X    XXX                                                                  
    0xE0,                  // XXX                                                                       
    0x01,                  //        X                                                                  
    0xE0,                  // XXX                                                                       
    0xA0,                  // X X                                                                       
    0xF0,                  // XXXX                                                                      
    0x01,                  //        X                                                                  
    0x40,                  //  X                                                                        
    0xE0,                  // XXX                                                                       
    0x40,                  //  X                                                                       
    0x40,                  //  X                                                                        
    0x40,                  //  X                                                                        
    0x01,                  //        X                                                                  
    0xE0,                  // XXX                                                                       
    0xA0,                  // X X                                                                       
    0xE0,                  // XXX                                                                       
    0x80,                  // X                                                                         
    0xE0,                  // XXX                                                                       
    0x01,                  //        X                                                                  
    0x20,                  //   X                                                                       
    0x20,                  //   X                                                                       
    0xE0,                  // XXX                                                                       
    0xA0,                  // X X                                                                       
    0xE0,                  // XXX                                                                       
    0x01,                  //        X                                                                  
    0x01,                  //        X                                                                  
    0x01,                  //        X                                                                  
    0x88,                  //    X   X                                                                  
    0xA8,                  // X X X                                                                     
    0xA8,                  // X X X                                                                     
    0xA8,                  // X X X                                                                     
    0xF8,                  // XXXXX                                                                     
    0x01,                  //        X                                                                  
    0xE0,                  // XXX                                                                       
    0xA0,                  // X X                                                                       
    0xF0,                  // XXXX                                                                      
    0x01,                  //        X                                                                  
    0x80,                  // X                                                                         
    0xE0,                  // XXX                                                                       
    0x8F,                  // X   XXXX                                                                 
    0x89,                  // X   X  X                                                                  
    0x0F,                  //     XXXX                                                                  
    0x8A,                  // X   X X                                                                   
    0xE9,                  // XXX X  X                                                                  
    0x80,                  // X                                                                         
    0x8E,                  // X   XXX                                                                   
    0x0A,                  //     X X                                                                   
    0xEE,                  // XXX XXX                                                                   
    0xA0,                  // X X                                                                      
    0xE8,                  // XXX X                                                                     
    0x88,                  // X   X                                                                     
    0xEE,                  // XXX XXX                                                                   
    0x0A,                  //     X X                                                                   
    0x8E,                  // X   XXX                                                                   
    0xE0,                  // XXX                                                                       
    0xA4,                  // X X  X                                                                    
    0xA4,                  // X X  X                                                                    
    0x04,                  //      X                                                                    
    0x80,                  // X                                                                         
    0x08,                  //     X                                                                     
    0x0E,                  //     XXX                                                                   
    0x0A,                  //     X X                                                                   
    0x0A,                  //     X X                                                                   
    0x80,                  // X                                                                         
    0x0E,                  //     XXX                                                                   
    0x0A,                  //     X X                                                                   
    0x0E,                  //     XXX                                                                   
    0x08,                  //     X                                                                     
    0x0E,                  //     XXX                                                                   
    0x80,                  // X                                                                         
    0x04,                  //      X                                                                    
    0x0E,                  //     XXX                                                                   
    0x04,                  //      X                                                                    
    0x04,                  //      X                                                                    
    0x04,                  //      X                                                                    
    0x80,                  // X                                                                         
    0x04,                  //      X                                                                    
    0x0E,                  //     XXX                                                                   
    0x04,                  //      X                                                                    
    0x04,                  //      X                                                                    
    0x04                   //      X                                                                    
};

// Object #10 : State FF : Graphic                                                                                   
static const byte objectGfxChallise [] =
{
    9,
    0x81,                  // X      X                                                                  
    0x81,                  // X      X                                                                  
    0xC3,                  // XX    XX                                                                  
    0x7E,                  //  XXXXXX                                                                   
    0x7E,                  //  XXXXXX                                                                  
    0x3C,                  //   XXXX                                                                    
    0x18,                  //    XX                                                                     
    0x18,                  //    XX                                                                     
    0x7E                   //  XXXXXX                                                                   
};

// Object #11 : State FF : Graphic                                                                                   
static const byte objectGfxMagnet [] =
{
    8,
    0x3C,                  //   XXXX                                                                    
    0x7E,                  //  XXXXXX                                                                   
    0xE7,                  // XXX  XXX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3,                  // XX    XX                                                                  
    0xC3                   // XX    XX                                                                  
};

// Object #1 States 940FF (Graphic)                                                                                  
static const byte objectGfxPort [] = 
{
    // state 1
    4,
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    // state 2
    6,
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    // state 3
    8,
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    // state 4
    10,
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    // state 5
    12,
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    // state 6
    14,
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    // state 7
    16,
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA,                  // X X X X                                                                   
    0xFE,                  // XXXXXXX                                                                   
    0xAA                   // X X X X                                                                   
};



// Portcullis states
static const byte portStates [] = 
{
    0,0,1,1,2,2,3,3,4,4,5,5,6,6,5,5,4,4,3,3,2,2,1,1
};


static int MAX_PLAYERS = 3;
static BALL balls[] = {
    {
        0/*room*/, 0/*x*/, 0/*y*/, 0/*previousx*/, 0/*previousy*/, 0/*velx*/, 0/*vely*/,
        OBJECT_NONE/*linkedObject*/, 0/*linkedObjectX*/, 0/*linkedObjectY*/,
        false/*hitX*/, false/*hitY*/, OBJECT_NONE/*hitObject*/,	objectGfxPlayer1/*gfxData*/
    }, {
        0/*room*/, 0/*x*/, 0/*y*/, 0/*previousx*/, 0/*previousy*/, 0/*velx*/, 0/*vely*/,
        OBJECT_NONE/*linkedObject*/, 0/*linkedObjectX*/, 0/*linkedObjectY*/,
        false/*hitX*/, false/*hitY*/, OBJECT_NONE/*hitObject*/,	objectGfxPlayer2/*gfxData*/
    }, {
		0/*room*/, 0/*x*/, 0/*y*/, 0/*previousx*/, 0/*previousy*/, 0/*velx*/, 0/*vely*/,
		OBJECT_NONE/*linkedObject*/, 0/*linkedObjectX*/, 0/*linkedObjectY*/, 
		false/*hitX*/, false/*hitY*/, OBJECT_NONE/*hitObject*/,	objectGfxPlayer3/*gfxData*/
	}
};
static BALL* objectBall = 0x0;

//
// Indexed array of all objects and their properties
//
static OBJECT** objectDefs = 0x0;

// Object locations (room and coordinate) for game 01
//        - object, room, x, y, state, movement(x/y)
static const byte game1Objects [] =
{
	OBJECT_PORT1, 0x11, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
	OBJECT_PORT4, 0x1F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 4
	OBJECT_PORT2, 0x0F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 2
    OBJECT_PORT3, 0x10, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_REDDRAGON, 0x0E, 0x50, 0x20, 0x00, 0x00, 0x00, // Red Dragon
    OBJECT_YELLOWDRAGON, 0x01, 0x50, 0x20, 0x00, 0x00, 0x00, // Yellow Dragon
    OBJECT_GREENDRAGON, 0x1D, 0x50, 0x20, 0x00, 0x00, 0x00, // Green Dragon
    OBJECT_SWORD, 0x12, 0x20, 0x20, 0x00, 0x00, 0x00, // Sword
    OBJECT_BRIDGE, 0x04, 0x2A, 0x37, 0x00, 0x00, 0x00, // Bridge
    OBJECT_YELLOWKEY, 0x11, 0x20, 0x41, 0x00, 0x00, 0x00, // Yellow Key
    OBJECT_WHITEKEY, 0x0E, 0x20, 0x40, 0x00, 0x00, 0x00, // White Key
    OBJECT_BLACKKEY, 0x1D, 0x20, 0x40, 0x00, 0x00, 0x00, // Black Key
    OBJECT_BAT, 0x1A, 0x20, 0x20, 0x00, 0x00, 0x00, // Bat
    OBJECT_DOT, 0x15, 0x51, 0x12, 0x00, 0x00, 0x00, // Dot
    OBJECT_CHALISE, 0x1C, 0x30, 0x20, 0x00, 0x00, 0x00, // Challise
    OBJECT_MAGNET, 0x1B, 0x80, 0x20, 0x00, 0x00, 0x00, // Magnet
    0xff,0,0,0,0,0,0
};

// Object locations (room and coordinate) for Games 02 and 03
//        - object, room, x, y, state, movement(x/y)
static const byte game2Objects [] =
{
    OBJECT_PORT1, 0x11, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
	OBJECT_PORT4, 0x1F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 4
	OBJECT_PORT2, 0x0F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 2
    OBJECT_PORT3, 0x10, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_REDDRAGON, 0x14, 0x50, 0x20, 0x00, 3, 3, // Red Dragon
    OBJECT_YELLOWDRAGON, 0x19, 0x50, 0x20, 0x00, 3, 3, // Yellow Dragon
    OBJECT_GREENDRAGON, 0x04, 0x50, 0x20, 0x00, 3, 3, // Green Dragon
    OBJECT_SWORD, 0x11, 0x20, 0x20, 0x00, 0x00, 0x00, // Sword
    OBJECT_BRIDGE, 0x0B, 0x40, 0x40, 0x00, 0x00, 0x00, // Bridge
    OBJECT_YELLOWKEY, 0x09, 0x20, 0x40, 0x00, 0x00, 0x00, // Yellow Key
    OBJECT_WHITEKEY, 0x06, 0x20, 0x40, 0x00, 0x00, 0x00, // White Key
    OBJECT_BLACKKEY, 0x19, 0x20, 0x40, 0x00, 0x00, 0x00, // Black Key
    OBJECT_BAT, 0x02, 0x20, 0x20, 0x00, 0, -3, // Bat
    OBJECT_DOT, 0x15, 0x45, 0x12, 0x00, 0x00, 0x00, // Dot
    OBJECT_CHALISE, 0x14, 0x30, 0x20, 0x00, 0x00, 0x00, // Challise
    OBJECT_MAGNET, 0x0E, 0x80, 0x20, 0x00, 0x00, 0x00, // Magnet
    0xff,0,0,0,0,0,0
};

// Room bounds data for game level 3
// Ex. the chalise can only exist in rooms 13-1A
static const int roomBoundsData [] =
{
   OBJECT_CHALISE, 0x13, 0x1A,
   OBJECT_REDDRAGON, 0x01, 0x1D,
   OBJECT_YELLOWDRAGON, 0x01, 0x1D,
   OBJECT_GREENDRAGON, 0x01, 0x1D,
   OBJECT_SWORD, 0x01, 0x1D,
   OBJECT_BRIDGE, 0x01, 0x1D,
   OBJECT_YELLOWKEY, 0x01, 0x1D,
   OBJECT_WHITEKEY, 0x01, 0x16,
   OBJECT_BLACKKEY, 0x01, 0x12,
   OBJECT_BAT, 0x01, 0x1D,
   OBJECT_MAGNET, 0x01, 0x1D,
   OBJECT_NONE, 0, 0
};

//
// Indexed array of all rooms and their properties
//
static ROOM roomDefs [] =
{
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_PURPLE, 0x00,0x00,0x00,0x00 },       // 0 - Number Room
    { roomGfxBelowYellowCastle, ROOMFLAG_LEFTTHINWALL, COLOR_OLIVEGREEN, 0x08,0x02,0x80,0x03 },       // 1 - Top Access
    { roomGfxBelowYellowCastle, ROOMFLAG_NONE, COLOR_LIMEGREEN, 0x11,0x03,0x83,0x01 },       // 2 - Top Access
    { roomGfxLeftOfName, ROOMFLAG_RIGHTTHINWALL, COLOR_TAN, 0x06,0x01,0x86,0x02 },       // 3 - Left of Name
    { roomGfxBlueMazeTop, ROOMFLAG_NONE, COLOR_BLUE, 0x10,0x05,0x07,0x06 },       // 4 - Top of Blue Maze
    { roomGfxBlueMaze1, ROOMFLAG_NONE, COLOR_BLUE, 0x1F,0x06,0x08,0x04 },       // 5 - Blue Maze #1
    { roomGfxBlueMazeBottom, ROOMFLAG_NONE, COLOR_BLUE, 0x07,0x04,0x03,0x05 },       // 6 - Bottom of Blue Maze
    { roomGfxBlueMazeCenter, ROOMFLAG_NONE, COLOR_BLUE, 0x04,0x08,0x06,0x08 },       // 7 - Center of Blue Maze
    { roomGfxBlueMazeEntry, ROOMFLAG_NONE, COLOR_BLUE, 0x05,0x07,0x01,0x07 },       // 8 - Blue Maze Entry
    { roomGfxMazeMiddle, ROOMFLAG_NONE, COLOR_LTGRAY, 0x0A,0x0A,0x0B,0x0A },       // 9 - Maze Middle
    { roomGfxMazeEntry, ROOMFLAG_NONE, COLOR_LTGRAY, 0x03,0x09,0x09,0x09 },       // A - Maze Entry
    { roomGfxMazeSide, ROOMFLAG_NONE, COLOR_LTGRAY, 0x09,0x0C,0x1C,0x0D },       // B - Maze Side
    { roomGfxSideCorridor, ROOMFLAG_RIGHTTHINWALL, COLOR_LTCYAN, 0x1C,0x0D,0x1D,0x0B },       // C - Side Corridor
    { roomGfxSideCorridor, ROOMFLAG_LEFTTHINWALL, COLOR_DKGREEN, 0x0F,0x0B,0x0E,0x0C },       // D - Side Corridor
    { roomGfxTopEntryRoom, ROOMFLAG_NONE, COLOR_CYAN, 0x0D,0x10,0x0F,0x10 },       // E - Top Entry Room
    { roomGfxCastle, ROOMFLAG_NONE, COLOR_WHITE, 0x0E,0x0F,0x0D,0x0F },       // F - White Castle
    { roomGfxCastle, ROOMFLAG_NONE, COLOR_BLACK, 0x01,0x1C,0x04,0x1C },       // 10 - Black Castle
    { roomGfxCastle, ROOMFLAG_NONE, COLOR_YELLOW, 0x06,0x03,0x02,0x01 },            // 11 - Yellow Castle
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_YELLOW, 0x12,0x12,0x12,0x12 },       // 12 - Yellow Castle Entry
    { roomGfxBlackMaze1, ROOMFLAG_NONE, COLOR_LTGRAY, 0x15,0x14,0x15,0x16 },       // 13 - Black Maze #1
    { roomGfxBlackMaze2, ROOMFLAG_MIRROR, COLOR_LTGRAY, 0x16,0x15,0x16,0x13 },       // 14 - Black Maze #2
    { roomGfxBlackMaze3, ROOMFLAG_MIRROR, COLOR_LTGRAY, 0x13,0x16,0x13,0x14 },       // 15 - Black Maze #3
    { roomGfxBlackMazeEntry, ROOMFLAG_NONE, COLOR_LTGRAY, 0x14,0x13,0x1B,0x15 },       // 16 - Black Maze Entry
    { roomGfxRedMaze1, ROOMFLAG_NONE, COLOR_RED, 0x19,0x18,0x19,0x18 },       // 17 - Red Maze #1
    { roomGfxRedMazeTop, ROOMFLAG_NONE, COLOR_RED, 0x1A,0x17,0x1A,0x17 },       // 18 - Top of Red Maze
    { roomGfxRedMazeBottom, ROOMFLAG_NONE, COLOR_RED, 0x17,0x1A,0x17,0x1A },       // 19 - Bottom of Red Maze
    { roomGfxWhiteCastleEntry, ROOMFLAG_NONE, COLOR_RED, 0x18,0x19,0x18,0x19 },       // 1A - White Castle Entry
    { roomGfxTwoExitRoom, ROOMFLAG_NONE, COLOR_RED, 0x89,0x89,0x89,0x89 },       // 1B - Black Castle Entry
    { roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_PURPLE, 0x1D,0x07,0x8C,0x08 },       // 1C - Other Purple Room
    { roomGfxTopEntryRoom, ROOMFLAG_NONE, COLOR_RED, 0x8F,0x01,0x10,0x03 },       // 1D - Top Entry Room
    { roomGfxBelowYellowCastle, ROOMFLAG_NONE, COLOR_PURPLE, 0x06,0x01,0x06,0x03 },        // 1E - Name Room
	{ roomGfxCastle, ROOMFLAG_NONE, COLOR_DKGREEN, 0x05, 0x06, 0x05, 0x04 },            // 1F - Green Castle
	{ roomGfxNumberRoom, ROOMFLAG_NONE, COLOR_DKGREEN, 0x20, 0x20, 0x20, 0x20 },       // 20 - Yellow Castle Entry
};


// Room differences for different levels (level 1,2,3)  
static const byte roomLevelDiffs [] = 
{
    0x10,0x0f,0x0f,            // down from room 01                                                             
    0x05,0x11,0x11,            // down from room 02                                                             
    0x1d,0x0a,0x0a,            // down from room 03                                                             
    0x1c,0x16,0x16,            // u/l/r/d from room 1b (black castle room)                                      
    0x1b,0x0c,0x0c,            // down from room 1c                                                             
    0x03,0x0c,0x0c,            // up from room 1d (top entry room)    
};

// Castle Entry Rooms (Yellow, White, Black)
static const byte entryRoomOffsets[] =
{
	 0x12,0x20,0x1A,0x1B
};

// Castle Rooms (Yellow, White, Black)                                                                               
static const byte castleRoomOffsets[] =
{
	 0x11,0x1F,0x0F,0x10
};

// Magnet Object Matrix                                                                             
static const int magnetMatrix[] =
{
       OBJECT_YELLOWKEY,    // Yellow Key
       OBJECT_WHITEKEY,     // White Key
       OBJECT_BLACKKEY,     // Black Key
       OBJECT_SWORD,        // Sword
       OBJECT_BRIDGE,       // Bridge
       OBJECT_CHALISE,      // Challise
       0x00
};

// Green Dragon's Object Matrix                                                                                      
static const int greenDragonMatrix[] =
{
    OBJECT_SWORD, OBJECT_GREENDRAGON,       // Sword, Green Dragon                                                         
    OBJECT_GREENDRAGON, OBJECT_BALL,        // Green Dragon, Ball                                                          
    OBJECT_GREENDRAGON, OBJECT_CHALISE,     // Green Dragon, Chalise                                                        
    OBJECT_GREENDRAGON, OBJECT_BRIDGE,      // Green Dragon, Bridge                                                        
    OBJECT_GREENDRAGON, OBJECT_MAGNET,      // Green Dragon, Magnet                                                        
    OBJECT_GREENDRAGON, OBJECT_BLACKKEY,    // Green Dragon, Black Key                                                     
    0x00, 0x00
};

// Yellow Dragon's Object Matrix                                                                                      
static const int yellowDragonMatrix[] =
{
    OBJECT_SWORD, OBJECT_YELLOWDRAGON,      // Sword, Yellow Dragon                                                    
    OBJECT_YELLOWKEY, OBJECT_YELLOWDRAGON,  // Yellow Key, Yellow Dragon
    OBJECT_YELLOWDRAGON, OBJECT_BALL,       // Yellow Dragon, Ball
    OBJECT_YELLOWDRAGON, OBJECT_CHALISE,    // Yellow Dragon, Challise                                                        
    0x00, 0x00
};

// Red Dragon's Object Matrix                                                                                      
static const int redDragonMatrix[] =
{
    OBJECT_SWORD, OBJECT_REDDRAGON,         // Sword, Red Dragon
    OBJECT_REDDRAGON, OBJECT_BALL,          // Red Dragon, Ball
    OBJECT_REDDRAGON, OBJECT_CHALISE,       // Red Dragon, Chalise
    OBJECT_REDDRAGON, OBJECT_WHITEKEY,      // Red Dragon, White Key
    0x00, 0x00
};

// Bat Object Matrix
static const int batMatrix [] =
{
       OBJECT_CHALISE,          // Chalise                                                                 
       OBJECT_SWORD,            // Sword                                                                   
       OBJECT_BRIDGE,           // Bridge                                                                  
       OBJECT_YELLOWKEY,        // Yellow Key                                                              
       OBJECT_WHITEKEY,         // White Key                                                               
       OBJECT_BLACKKEY,         // Black Key                                                               
       OBJECT_REDDRAGON,        // Red Dragon                                                              
       OBJECT_YELLOWDRAGON,     // Yellow Dragon                                                           
       OBJECT_GREENDRAGON,      // Green Dragon                                                            
       OBJECT_MAGNET,           // Magnet                                                                  
       0x00                                                                                                   
};

static Sync* sync;
static Transport* transport;
static int numPlayers;
static int thisPlayer;

/** We wait a few seconds between when the game comes up connected and when the game actually starts.
 This is the countdown timer. */
static int timeToStartGame;

static int numDragons = 3;
static Dragon** dragons = NULL;
static Bat* bat = NULL;



void Adventure_Setup(int inNumPlayers, int inThisPlayer, Transport* inTransport, int inGameNum,
                     int initialLeftDiff, int initialRightDiff) {
    numPlayers = inNumPlayers;
    thisPlayer = inThisPlayer;
    gameNum = inGameNum;
    gameLevel = gameNum-1;
    timeToStartGame = 60 * 3;
    
    dragons = (Dragon**)malloc(numDragons * sizeof(Dragon*));
    dragons[0]= new Dragon(0, 0, COLOR_YELLOW, -1, 0, 0);
    dragons[1] = new Dragon(1, 0, COLOR_LIMEGREEN, -1, 0, 0);
    dragons[2] = new Dragon(2, 0, COLOR_RED, -1, 0, 0);
    bat = new Bat(COLOR_BLACK, -1, 0, 0);
    
    
    // Setup the structures
    int numObjects = OBJECT_MAGNET+2;
    objectDefs = (OBJECT**)malloc(numObjects*sizeof(OBJECT*));
    objectDefs[OBJECT_PORT1] = new OBJECT(objectGfxPort, portStates, 0, COLOR_BLACK, -1, 0, 0);
    objectDefs[OBJECT_PORT4] = new OBJECT(objectGfxPort, portStates, 0, COLOR_BLACK, -1, 0, 0);
    objectDefs[OBJECT_PORT2] = new OBJECT(objectGfxPort, portStates, 0, COLOR_BLACK, -1, 0, 0);
    objectDefs[OBJECT_PORT3] = new OBJECT(objectGfxPort, portStates, 0, COLOR_BLACK, -1, 0, 0);
    objectDefs[OBJECT_NAME] = new OBJECT(objectGfxAuthor, 0, 0, COLOR_FLASH, 0x1E, 0x50, 0x69);
    objectDefs[OBJECT_NUMBER] = new OBJECT(objectGfxNum, numberStates, 0, COLOR_LIMEGREEN, 0x00, 0x50, 0x40);
    objectDefs[OBJECT_REDDRAGON] = dragons[2];
    objectDefs[OBJECT_YELLOWDRAGON] =dragons[0];
    objectDefs[OBJECT_GREENDRAGON] = dragons[1];
    objectDefs[OBJECT_SWORD] = new OBJECT(objectGfxSword, 0, 0, COLOR_YELLOW, -1, 0, 0);
    objectDefs[OBJECT_BRIDGE] = new OBJECT(objectGfxBridge, 0, 0, COLOR_PURPLE, -1, 0, 0, 0x07);
    objectDefs[OBJECT_YELLOWKEY] = new OBJECT(objectGfxKey, 0, 0, COLOR_YELLOW, -1, 0, 0);
    objectDefs[OBJECT_WHITEKEY] = new OBJECT(objectGfxKey, 0, 0, COLOR_WHITE, -1, 0, 0);
    objectDefs[OBJECT_BLACKKEY] = new OBJECT(objectGfxKey, 0, 0, COLOR_BLACK, -1, 0, 0);
    objectDefs[OBJECT_BAT] = bat;
    objectDefs[OBJECT_DOT] = new OBJECT(objectGfxDot, 0, 0, COLOR_LTGRAY, -1, 0, 0);
    objectDefs[OBJECT_CHALISE] = new OBJECT(objectGfxChallise, 0, 0, COLOR_FLASH, -1, 0, 0);
    objectDefs[OBJECT_MAGNET] = new OBJECT(objectGfxMagnet, 0, 0, COLOR_BLACK, -1, 0, 0);
    objectDefs[numObjects-1] = new OBJECT((const byte*)0, 0, 0, 0, -1, 0, 0);  // #12 Null

    // Setup the transport
    transport = inTransport;
    sync = new Sync(numPlayers, thisPlayer, transport);
    objectBall = &balls[thisPlayer];
    
    printf("Player %d setup.\n", thisPlayer);
}

void ResetPlayer() {
    if (gameState != GAMESTATE_GAMESELECT) {
        objectBall->room = 0x11;                 // Put us in the yellow castle
        objectBall->x = 0x50*2;                  //
        objectBall->y = 0x20*2;                  //
        objectBall->previousX = objectBall->x;
        objectBall->previousY = objectBall->y;
        objectBall->linkedObject = OBJECT_NONE;  // Not carrying anything
        
        displayedRoomIndex = objectBall->room;
        
        // Make the bat want something right away
        batFedUpTimer = 0xff;
        
        // Set up objects, rooms, and positions
        
        // Else we just bring the dragons to life
        for(int ctr=0; ctr<numDragons; ++ctr) {
            dragons[ctr]->state = 0;
            dragons[ctr]->eaten = NULL;
        }
    }
}


void Adventure_Run()
{
	sync->StartFrame();
    sync->PullLatestMessages();

    // read the console switches every frame
    bool select, reset;
    Platform_ReadConsoleSwitches(&select, &reset);
    Platform_ReadDifficultySwitches(&gameDifficultyLeft, &gameDifficultyRight);

    // Reset switch
    if ((gameState != GAMESTATE_WIN) && switchReset && !reset)
    {
        if (gameState != GAMESTATE_GAMESELECT) {
            ResetPlayer();
        }
    }
    else
    {
        // Is the game active?
        if (gameState == GAMESTATE_GAMESELECT)
        {
            --timeToStartGame;
            if (timeToStartGame <= 0) {
                SetupRoomObjects();
                gameState = GAMESTATE_ACTIVE_1;
                ResetPlayer();
            } else {
                int displayNum = timeToStartGame / 60;
                objectDefs[OBJECT_NUMBER]->state = displayNum;

                // Display the room and objects
                displayedRoomIndex = 0;
                objectBall->room = 0;
                objectBall->x = 0;
                objectBall->y = 0;
                PrintDisplay();
            }
        }
        else if (ISGAMEACTIVE())
        {
            // Get the room the chalise is in

            // Is it in the yellow castle?
            if (objectDefs[OBJECT_CHALISE]->room == 0x12)
            {
                // Play end noise

                // Go to won state
                gameState = GAMESTATE_WIN;
                winFlashTimer = 0xff;

                // Play the sound
                Platform_MakeSound(SOUND_WON);
            }
            else if (switchSelect && !select)
            {
                // Go to game level selection screen if select switch hit
                gameState = GAMESTATE_GAMESELECT;
                objectBall->room = 0;
                objectBall->x = 0;
                objectBall->y = 0;
                objectBall->previousX = objectBall->x;
                objectBall->previousY = objectBall->y;

                displayedRoomIndex = objectBall->room;

                // Setup the room and object
                PrintDisplay();
            }
            else
            {
                // Read joystick
                Platform_ReadJoystick(&joyLeft, &joyUp, &joyRight, &joyDown, &joyFire);

                if (gameState == GAMESTATE_ACTIVE_1)
                {
                    // Check ball collisions and move ball
                    ThisBallMovement();
					// Move all the other players
					OtherBallMovement();

                    // Move the carried object
                    MoveCarriedObjects();

                    // Setup the room and object
                    PrintDisplay();

                    ++gameState;
                }
                else if (gameState == GAMESTATE_ACTIVE_2)
                {
                    // Deal with object pickup and putdown
                    PickupPutdown();
                    OthersPickupPutdown();

                    // Check ball collisions
                    if (!objectBall->hitX && !objectBall->hitY)
                    {
                        // Make sure stuff we are carrying stays out of our way
                        int hitObject = CollisionCheckBallWithObjects(objectBall, 0);
                        if ((hitObject > OBJECT_NONE) && (hitObject == objectBall->linkedObject))
                        {
                            int diffX = objectBall->x - objectBall->previousX;
                            objectBall->linkedObjectX += diffX/2;

                            int diffY = objectBall->y - objectBall->previousY;
                            objectBall->linkedObjectY += diffY/2;
                            
                            // Adjusting how we hold an object is broadcast to other players as a pickup action
                            PlayerPickupAction* action = new PlayerPickupAction(thisPlayer,
                                hitObject, objectBall->linkedObjectX, objectBall->linkedObjectY, OBJECT_NONE, 0, 0, 0);
                            sync->BroadcastAction(action);
                            
                        }
                    }
					for (int i = 0; i < MAX_PLAYERS; ++i) {
                        ReactToCollision(balls + i);
					}

                    // Increment the last object drawn
                    ++displayListIndex;

                    // deal with invisible surround moving
                    Surround();

                    // Move and deal with bat
                    MoveBat();

                    // Move and deal with portcullises
                    Portals();

                    // Display the room and objects
                    PrintDisplay();

                    ++gameState;
                }
                else if (gameState == GAMESTATE_ACTIVE_3)
                {
                    // Read remote dragon actions
                    SyncDragons();
                    
                    // Move and deal with the green dragon
                    MoveDragon((Dragon*)objectDefs[OBJECT_GREENDRAGON], greenDragonMatrix, 2);

                    // Move and deal with the yellow dragon
                    MoveDragon((Dragon*)objectDefs[OBJECT_YELLOWDRAGON], yellowDragonMatrix, 2);

                    // Move and deal with the red dragon
                    MoveDragon((Dragon*)objectDefs[OBJECT_REDDRAGON], redDragonMatrix, 3);

                    // Deal with the magnet
                    Magnet();

                    // Display the room and objects
                    PrintDisplay();

                    gameState = GAMESTATE_ACTIVE_1;
                }
            }
        }
        else if (gameState == GAMESTATE_WIN)
        {
            if (winFlashTimer > 0)
                --winFlashTimer;

            // Display the room and objects
            PrintDisplay();

            // Go to game selection screen on select or reset button
            if ((switchReset && !reset) || (switchSelect && !select))
            {
                gameState = GAMESTATE_GAMESELECT;
            }
        }
    }

    switchReset = reset;
    switchSelect = select;

    AdvanceFlashColor();
}


void SetupRoomObjects()
{
    // Init all objects
    for (int i=0; objectDefs[i]->gfxData; i++)
    {
        OBJECT* object = objectDefs[i];
        object->movementX = 0;
        object->movementY = 0;
    };
    // Set to no carried objects
    for(int ctr=0; ctr<numDragons; ++ctr) {
        dragons[ctr]->eaten = NULL;
    }
    bat->linkedObject = OBJECT_NONE;

    // Read the object initialization table for the current game level
    const byte* p;
    if (gameLevel == 0)
        p = (byte*)game1Objects;
    else
        p = (byte*)game2Objects;

    while ((*p) != 0xff)
    {
        byte object = *(p++);
        byte room = *(p++);
        byte xpos = *(p++);
        byte ypos = *(p++);
        byte state = *(p++);
        signed char movementX = *(p++);
        signed char movementY = *(p++);

        objectDefs[object]->room = room;
        objectDefs[object]->x = xpos;
        objectDefs[object]->y = ypos;
        objectDefs[object]->state = state;
        objectDefs[object]->movementX = movementX;
        objectDefs[object]->movementY = movementY;
    };

    // Put objects in random rooms for level 3
    if (gameLevel == 2)
    {
        const int* boundsData = roomBoundsData;
        
        int object = *(boundsData++);
        int lower = *(boundsData++);
        int upper = *(boundsData++);

        do
        {
            // pick a room between upper and lower bounds (inclusive)
            while (1)
            {
                int room = Platform_Random() * 0x1f;
                if (room >= lower && room <= upper)
                {
                    objectDefs[object]->room = room;
                    break;
                }
            }

            object = *(boundsData++);
            lower = *(boundsData++);
            upper = *(boundsData++);
        }
        while (object > OBJECT_NONE);
    }
}

void ReactToCollision(BALL* ball) {
	if (ball->hitX)
	{
		// TODO: Don't think we're doing this right
		if ((ball->hitObject > OBJECT_NONE) && (ball->hitObject == ball->linkedObject))
		{
			int diffX = ball->x - ball->previousX;
			ball->linkedObjectX += diffX / 2;
		}

		ball->x = ball->previousX;
		ball->hitX = false;
	}
	if (ball->hitY)
	{
		if ((ball->hitObject > OBJECT_NONE) && (ball->hitObject == ball->linkedObject))
		{
			int diffY = ball->y - ball->previousY;
			ball->linkedObjectY += diffY / 2;
		}

		ball->y = ball->previousY;
		ball->hitY = false;
	}
}

void ThisBallMovement()
{
	// Read the joystick and translate into a velocity
	bool velocityChanged = false;
	int newVelY = 0;
	if (joyUp) {
		if (!joyDown) {
			newVelY = 6;
		}
	}
	else if (joyDown) {
		newVelY = -6;
	}
	velocityChanged = (objectBall->vely != newVelY);
	objectBall->vely = newVelY;

	int newVelX = 0;
	if (joyRight) {
		if (!joyLeft) {
			newVelX = 6;
		}
	}
	else if (joyLeft) {
		newVelX = -6;
	}
	velocityChanged = velocityChanged || (objectBall->velx != newVelX);
	objectBall->velx = newVelX;

	BallMovement(objectBall);

	if (velocityChanged) {
        // TODO: Do we want to be constantly allocating space?
        PlayerMoveAction* moveAction = new PlayerMoveAction(thisPlayer, objectBall->room, objectBall->x, objectBall->y, objectBall->velx, objectBall->vely);
        sync->BroadcastAction(moveAction);
	}
}

void BallMovement(BALL* ball) {
	bool isCurrentPlayer = (ball == objectBall);
    // store the existing ball location
    int tempX = ball->x;
    int tempY = ball->y;
    
    bool eaten = false;
    for(int ctr=0; ctr<numDragons && !eaten; ++ctr) {
        eaten = (dragons[ctr]->eaten == ball);
    }

    // mark the existing Y location as the previous Y location
    ball->previousY = ball->y;

    ball->hitObject = OBJECT_NONE;
	if (isCurrentPlayer) {
		displayedRoomIndex = ball->room;
	}

    // Move the ball on the Y axis
	ball->y += ball->vely;

    if (!eaten)
    {
        // Wrap rooms in Y if necessary
        if (ball->y > (ADVENTURE_OVERSCAN + ADVENTURE_SCREEN_HEIGHT) + 6)
        {
            // Wrap the ball to the bottom of the screen
            ball->y = ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN-2;
            ball->previousY = ball->y;

            // Set the new room
            const ROOM* currentRoom = &roomDefs[ball->room];
            ball->room = currentRoom->roomUp;
            ball->room = AdjustRoomLevel(ball->room);
        }
        else if (ball->y < 0x0D*2)
        {
			bool leftCastle = false;
			for (int portalCtr = OBJECT_PORT1; !leftCastle && (portalCtr <= OBJECT_PORT3); ++portalCtr) {
				if (ball->room == entryRoomOffsets[portalCtr])
				{
					ball->x = 0xA0;
					ball->y = 0x2C * 2;

					ball->previousX = ball->x;
					ball->previousY = ball->y;

					ball->room = castleRoomOffsets[portalCtr];
					ball->room = AdjustRoomLevel(ball->room);
					leftCastle = true;
				}
			}

			if (!leftCastle)
            {
                // Just lookup the next room down and switch to that room
                // Wrap the ball to the top of the screen
                int newY = (ADVENTURE_SCREEN_HEIGHT + ADVENTURE_OVERSCAN);

                const ROOM* currentRoom = &roomDefs[ball->room];
                int roomDown = AdjustRoomLevel(currentRoom->roomDown);

                if (CollisionCheckBallWithWalls(roomDown, tempX, newY))
                {
                    // We've hit a wall on the next screen
                    ball->hitY = true;
					if (isCurrentPlayer) {
						displayedRoomIndex = roomDown;
					}
                }
                else
                {
                    // Set the new room
                    ball->y = newY;
                    ball->room = roomDown;
                }
            }
        }
        // Collision check the ball with the new Y coordinate against walls and objects
        // For collisions with objects, we only care about hitting non-carryable objects at this point
        int hitObject = CollisionCheckBallWithObjects(ball, 0);
        bool crossingBridge = CrossingBridge(ball->room, tempX, ball->y, ball);
        bool hitWall = crossingBridge ? false : CollisionCheckBallWithWalls(ball->room, tempX, ball->y);
        if (hitWall || (hitObject > OBJECT_NONE))
        {
            // Hit a wall or non-carryable object
            ball->hitY = true;
            ball->hitObject = hitObject;
        }
    }
    else
    {
        ball->hitY = true;
    }

    // mark the existing X location as the previous X location
    ball->previousX = ball->x;

    // Move the ball on the X axis
	ball->x += ball->velx;

    if (!eaten)
    {
        // Wrap rooms in X if necessary
        if (ball->x >= (ADVENTURE_SCREEN_WIDTH-4))
        {
            // Wrap the ball to the left side of the screen
            ball->x = 5;

            // Is it room #3 (Right to secret room)
            if (ball->room == 0x3)
            {
                // Set room to secret room
                ball->room = 0x1e;
            }
            else
            {
                // Set the new room
                const ROOM* currentRoom = &roomDefs[ball->room];
                ball->room = currentRoom->roomRight;
            }
            ball->room = AdjustRoomLevel(ball->room);
        }
        else if (ball->x < 4)
        {
            // Wrap the ball to the right side of the screen
            ball->x = ADVENTURE_SCREEN_WIDTH-5;

            // Set the new room
            const ROOM* currentRoom = &roomDefs[ball->room];
            ball->room = currentRoom->roomLeft;
            ball->room = AdjustRoomLevel(ball->room);
        }
        // Collision check the ball with the new Y coordinate against walls and objects
        // For collisions with objects, we only care about hitting non-carryable objects at this point
        int hitObject = CollisionCheckBallWithObjects(ball, 0);
        int hitWall = CollisionCheckBallWithWalls(ball->room, ball->x, tempY);
        if (hitWall || (hitObject > OBJECT_NONE))
        {
            // Hit a wall or non-carryable object
            ball->hitX = true;
            ball->hitObject = hitObject;
        }
    }
    else
    {
        ball->hitX = true;
    }

}

void OtherBallMovement() {
	for (int i = 0; i < numPlayers; ++i) {
        if (i != thisPlayer) {
            PlayerMoveAction* movement = sync->GetLatestBallSync(i);
            if (movement != 0x0) {
                balls[i].room = movement->room;
                balls[i].x = movement->posx;
                balls[i].y = movement->posy;
                balls[i].velx = movement->velx;
                balls[i].vely = movement->vely;
            }
            
            BallMovement(balls+i);
		}
	}

}

void SyncDragons() {
    RemoteAction* next = sync->GetNextDragonAction();
    while (next != NULL) {
        if (next->typeCode == DragonStateAction::CODE) {
            DragonStateAction* nextState = (DragonStateAction*)next;
            Dragon* dragon = dragons[nextState->dragonNum];
            if (nextState->newState == Dragon::EATEN) {
                // Set the State to 01 (eaten)
                dragon->eaten = &balls[nextState->sender];
                dragon->state = Dragon::EATEN;
                // Play the sound
                Platform_MakeSound(SOUND_EATEN);
            } else if (nextState->newState == Dragon::ROAR) {
                // We ignore roar actions if we are already in an eaten state or dead state
                if ((dragon->state != Dragon::EATEN) && (dragon->state != Dragon::DEAD)) {
                    dragon->roar(nextState->posx, nextState->posy, gameLevel, gameDifficultyLeft==DIFFICULTY_A);
                    // Play the sound
                    Platform_MakeSound(SOUND_ROAR);
                }
            }
        } else {
            // If we are in the same room as the dragon and are closer to it than the reporting player,
            // then we ignore reports and trust our internal state.
            // If the dragon is not in stalking state we ignore it.
            // Otherwise, you use the reported state.
            DragonMoveAction* nextMove = (DragonMoveAction*)next;
            Dragon* dragon = dragons[nextMove->dragonNum];
            if ((dragon->state == Dragon::STALKING) &&
                ((dragon->room != objectBall->room) ||
                (distanceFromBall(objectBall, dragon->x, dragon->y) > nextMove->distance))) {
                
                dragon->room = nextMove->room;
                dragon->x = nextMove->posx;
                dragon->y = nextMove->posy;
                dragon->movementX = nextMove->velx;
                dragon->movementY = nextMove->vely;
                
            }
        }
        delete next;
        next = sync->GetNextDragonAction();
    }
}

void MoveCarriedObjects()
{
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        BALL* nextBall = &balls[ctr];
        if (nextBall->linkedObject != OBJECT_NONE)
        {
            OBJECT* object = objectDefs[nextBall->linkedObject];
            object->x = (nextBall->x/2) + nextBall->linkedObjectX;
            object->y = (nextBall->y/2) + nextBall->linkedObjectY;
            object->room = nextBall->room;
        }
    }

    // Seems like a weird place to call this but this matches the original game
    MoveGroundObject();
}

void MoveGroundObject()
{
    OBJECT* port1 = objectDefs[OBJECT_PORT1];
    OBJECT* port2 = objectDefs[OBJECT_PORT2];
	OBJECT* port3 = objectDefs[OBJECT_PORT3];
	OBJECT* port4 = objectDefs[OBJECT_PORT4];

    // Handle ball going into the castles
    if (objectBall->room == port1->room && port1->state != 0x0C && CollisionCheckObject(port1, (objectBall->x-4), (objectBall->y-1), 8, 8))
    {
        objectBall->room = entryRoomOffsets[OBJECT_PORT1];
        objectBall->y = ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN-2;
        objectBall->previousY = objectBall->y;
        port1->state = 0; // make sure it stays unlocked in case we are walking in with the key
    }
    else if (objectBall->room == port2->room && port2->state != 0x0C && CollisionCheckObject(port2, (objectBall->x-4), (objectBall->y-1), 8, 8))
    {
        objectBall->room = entryRoomOffsets[OBJECT_PORT2];
        objectBall->y = ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN-2;
        objectBall->previousY = objectBall->y;
        port2->state = 0; // make sure it stays unlocked in case we are walking in with the key
    }
	else if (objectBall->room == port3->room && port3->state != 0x0C && CollisionCheckObject(port3, (objectBall->x - 4), (objectBall->y - 1), 8, 8))
	{
		objectBall->room = entryRoomOffsets[OBJECT_PORT3];
		objectBall->y = ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN - 2;
		objectBall->previousY = objectBall->y;
		port3->state = 0; // make sure it stays unlocked in case we are walking in with the key
	}
	else if (objectBall->room == port4->room && port4->state != 0x0C && CollisionCheckObject(port4, (objectBall->x - 4), (objectBall->y - 1), 8, 8))
	{
		objectBall->room = entryRoomOffsets[OBJECT_PORT4];
		objectBall->y = ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN - 2;
		objectBall->previousY = objectBall->y;
		port4->state = 0; // make sure it stays unlocked in case we are walking in with the key
	}

    // Move any objects that need moving, and wrap objects from room to room
    for (int i=OBJECT_REDDRAGON; objectDefs[i]->gfxData; i++)
    {
        OBJECT* object = objectDefs[i];

        // Apply movement
        object->x += object->movementX;
        object->y += object->movementY;

        // Check and Deal with Up
        if (object->y > 0x6A)
        {
            object->y = 0x0D;
            object->room = AdjustRoomLevel(roomDefs[object->room].roomUp);
        }

        // Check and Deal with Left
        if (object->x < 0x03)
        {
            object->x = 0x9A;
            object->room = AdjustRoomLevel(roomDefs[object->room].roomLeft);
        }

        // Check and Deal with Down
        if (object->y < 0x0D)
        {
            // Handle object leaving the castles
            if (object->room == entryRoomOffsets[OBJECT_PORT1])
            {            
                object->y = 0x5C;
                object->room = AdjustRoomLevel(castleRoomOffsets[OBJECT_PORT1]);
            }
            else if (object->room == entryRoomOffsets[OBJECT_PORT2])
            {            
                object->y = 0x5C;
                object->room = AdjustRoomLevel(castleRoomOffsets[OBJECT_PORT2]);
            }
			else if (object->room == entryRoomOffsets[OBJECT_PORT3])
			{
				object->y = 0x5C;
				object->room = AdjustRoomLevel(castleRoomOffsets[OBJECT_PORT3]);
			}
			else if (object->room == entryRoomOffsets[OBJECT_PORT4])
			{
				object->y = 0x5C;
				object->room = AdjustRoomLevel(castleRoomOffsets[OBJECT_PORT4]);
			}
			else
            {
                object->y = 0x69;
                object->room = AdjustRoomLevel(roomDefs[object->room].roomDown);
            }
        }

        // Check and Deal with Right
        if (object->x > 0x9B)
        {
            object->x = 0x03;
            object->room = AdjustRoomLevel(roomDefs[object->room].roomRight);
        }

        // If the object has a linked object
        if ((object == bat) && (bat->linkedObject != OBJECT_NONE))
        {
            OBJECT* linkedObj = objectDefs[bat->linkedObject];
            linkedObj->x = object->x + bat->linkedObjectX;
            linkedObj->y = object->y + bat->linkedObjectY;
            linkedObj->room = object->room;
        }
        // TODO: Seems awfully inefficient and with C++ multiple inheritance not even sure
        // that pointer equality check will always work.  Think about another way.
        for (int ctr=0; ctr<numDragons; ++ctr) {
            if ((object == dragons[ctr]) && (dragons[ctr]->eaten != NULL))
            {
                Dragon* dragon = dragons[ctr];
                BALL* linkedObj = dragon->eaten;
                linkedObj->x = object->x + dragon->eatenX;
                linkedObj->y = object->y + dragon->eatenY;
                linkedObj->room = object->room;
            }
        }
    }
}

void PrintDisplay()
{
    // get the playfield data
    int displayedRoom = displayedRoomIndex;
    const ROOM* currentRoom = &roomDefs[displayedRoom];
    const byte* roomData = currentRoom->graphicsData;

    // get the playfield color
    COLOR color = ((gameState == GAMESTATE_WIN) && (winFlashTimer > 0)) ? GetFlashColor() : colorTable[currentRoom->color];
    COLOR colorBackground = colorTable[COLOR_LTGRAY];

    // Fill the entire backbuffer with the playfield background color before we draw anything else
    Platform_PaintPixel(colorBackground.r, colorBackground.g, colorBackground.b, 0, 0, ADVENTURE_SCREEN_WIDTH, ADVENTURE_TOTAL_SCREEN_HEIGHT);

    // paint the surround under the playfield layer
    if ((objectSurround.room == objectBall->room) && (objectSurround.state == 0))
       DrawObject(&objectSurround);

    // get the playfield mirror flag
    bool mirror = currentRoom->flags & ROOMFLAG_MIRROR;

    //
    // Extract the playfield register bits and paint the playfield
    // The playfied register is 20 bits wide encoded across 3 bytes
    // as follows:
    //    PF0   |  PF1   |  PF2
    //  xxxx4567|76543210|01234567
    // Each set bit indicates playfield color - else background color -
    // the size of each block is 8 x 32, and the drawing is shifted
    // upwards by 16 pixels
    //

    // mask values for playfield bits
    byte shiftreg [20] = 
    {
        0x10,0x20,0x40,0x80,
        0x80,0x40,0x20,0x10,0x8,0x4,0x2,0x1,
        0x1,0x2,0x4,0x8,0x10,0x20,0x40,0x80
    };

    // each cell is 8 x 32
    const int cell_width = 8;
    const int cell_height = 32;


    // draw the playfield
    for (int cy=0; cy<=6; cy++)
    {
        byte pf0 = roomData[(cy*3) + 0];
        byte pf1 = roomData[(cy*3) + 1];
        byte pf2 = roomData[(cy*3) + 2];

        int ypos = 6-cy;

        for (int cx=0; cx<20; cx++)
        {
            bool bit=false;

            if (cx < 4)
                bit = pf0 & shiftreg[cx];
            else if (cx < 12)
                bit = pf1 & shiftreg[cx];
            else
                bit = pf2 & shiftreg[cx];

            if (bit)
            {
                Platform_PaintPixel(color.r, color.g, color.b, cx*cell_width, ypos*cell_height, cell_width, cell_height);
                if (mirror)
                    Platform_PaintPixel(color.r, color.g, color.b, (cx+20)*cell_width, ypos*cell_height, cell_width, cell_height);
                else
                    Platform_PaintPixel(color.r, color.g, color.b, ((40-(cx+1))*cell_width), ypos*cell_height, cell_width, cell_height);
            }
        }
    }

    //
    // Draw the balls
    //
    color = colorTable[roomDefs[displayedRoomIndex].color];

	for (int i = 0; i < numPlayers; ++i) {
		if (objectBall->room == balls[i].room) {
			DrawBall(&balls[i], color);
		}
	}

    //
    // Draw any objects in the room
    //
    DrawObjects(displayedRoom);

}

void OthersPickupPutdown() {
    PlayerPickupAction* action = sync->GetNextPickupAction();
    while (action != NULL) {
        int actorNum = action->sender;
        BALL* actor = &balls[actorNum];
        if (action->dropObject != OBJECT_NONE) {
            printf("Received drop action for player %d who is carrying %d\n", actorNum, actor->linkedObject);
        }
        if ((action->dropObject != OBJECT_NONE) && (actor->linkedObject == action->dropObject)) {
            printf("Player %d dropped object %d\n", actorNum, action->dropObject);
            actor->linkedObject = OBJECT_NONE;
            OBJECT* dropped = objectDefs[action->dropObject];
            dropped->room = action->dropRoom;
            dropped->x = action->dropX;
            dropped->y = action->dropY;
        }
        if (action->pickupObject != OBJECT_NONE) {
            actor->linkedObject = action->pickupObject;
            actor->linkedObjectX = action->pickupX;
            actor->linkedObjectY = action->pickupY;
            printf("Setting player %d to carrying %d\n", actorNum, actor->linkedObject);
            // If anybody else was carrying this object, take it away.
            for(int ctr=0; ctr<numPlayers; ++ctr) {
                if ((ctr != actorNum) && (balls[ctr].linkedObject==action->pickupObject)) {
                    printf("Player %d took object %d from player %d\n", action->sender, actor->linkedObject, thisPlayer);
                    balls[ctr].linkedObject = OBJECT_NONE;
                }
            }
            // If they are in the same room as you, play the pickup sound
            if (actor->room == objectBall->room) {
                Platform_MakeSound(SOUND_PICKUP);
            }
        }
        delete action;
        action = sync->GetNextPickupAction();
    }
}

void PickupPutdown()
{
    if (joyFire && (objectBall->linkedObject >= 0))
    {
        int dropped = objectBall->linkedObject;
        OBJECT* droppedObject = objectDefs[dropped];
        
        // Put down the current object!
        objectBall->linkedObject = OBJECT_NONE;
        
        // Tell other clients about the drop
        PlayerPickupAction* action = new PlayerPickupAction(thisPlayer, OBJECT_NONE, 0, 0, dropped, droppedObject->room,
                                                           droppedObject->x, droppedObject->y);
        sync->BroadcastAction(action);

        // Play the sound
        Platform_MakeSound(SOUND_PUTDOWN);
    }
    else
    {
        // See if we are touching any carryable objects
        int hitIndex = CollisionCheckBallWithObjects(objectBall, OBJECT_SWORD);
        if (hitIndex > OBJECT_NONE)
        {
            // Ignore the object we are already carrying
            if (hitIndex == objectBall->linkedObject)
            {
                // Check the remainder of the objects
                hitIndex = CollisionCheckBallWithObjects(objectBall, hitIndex + 1);
            }

            if (hitIndex > OBJECT_NONE)
            {
                // TODO: Handle when we are just repositioning the object we are currently holding
                
                // Collect info about whether we are also dropping an object (for when we broadcast the action)
                PlayerPickupAction* action = new PlayerPickupAction(thisPlayer, OBJECT_NONE, 0, 0, OBJECT_NONE, 0, 0, 0);
                int dropIndex = objectBall->linkedObject;
                if (dropIndex > OBJECT_NONE) {
                    OBJECT* dropped = objectDefs[dropIndex];
                    action->setDrop(dropIndex, dropped->room, dropped->x, dropped->y);
                }
                
                // Pick up this object!
                objectBall->linkedObject = hitIndex;

                // calculate the XY offsets from the ball's position
                objectBall->linkedObjectX = objectDefs[hitIndex]->x - (objectBall->x/2);
                objectBall->linkedObjectY = objectDefs[hitIndex]->y - (objectBall->y/2);
                
                // Take it away from anyone else if they were holding it.
                for(int ctr=0; ctr<numPlayers; ++ctr) {
                    if ((ctr != thisPlayer) && (balls[ctr].linkedObject == hitIndex)) {
                        balls[ctr].linkedObject = OBJECT_NONE;
                    }
                }

                // Broadcast that we picked up an object
                action->setPickup(hitIndex, objectBall->linkedObjectX, objectBall->linkedObjectY);
                sync->BroadcastAction(action);
                
                // Play the sound
                Platform_MakeSound(SOUND_PICKUP);
            }
        }
    }
}

void Surround()
{
    // get the playfield data
    const ROOM* currentRoom = &roomDefs[objectBall->room];
    if (currentRoom->color == COLOR_LTGRAY)
    {
        // Put it in the same room as the ball (player) and center it under the ball
        objectSurround.room = objectBall->room;
        objectSurround.x = (objectBall->x-0x1E)/2;
        objectSurround.y = (objectBall->y+0x18)/2;
    }
    else
    {
        objectSurround.room = -1;
    }
}

void MoveBat()
{
    static int flapTimer = 0;
    if (++flapTimer >= 0x04)
    {
        bat->state = (bat->state == 0) ? 1 : 0;
        flapTimer = 0;
    }

    if ((bat->linkedObject != OBJECT_NONE) && (batFedUpTimer < 0xff))
        ++batFedUpTimer;

    if (batFedUpTimer >= 0xff)
    {
        // Get the bat's current extents
        int batX, batY, batW, batH;
        CalcPlayerSpriteExtents(bat, &batX, &batY, &batW, &batH);
        
        // Enlarge the bat extent by 7 pixels for the proximity checks below
        // (doing the bat once is faster than doing each object and the results are the same)
        batX-=7;
        batY-=7;
        batW+=7*2;
        batH+=7*2;
        
        // Go through the bat's object matrix
        const int* matrixP = batMatrix;
        do
        {
            // Get the object it is seeking
            const OBJECT* seekObject = objectDefs[*matrixP];
            if ((seekObject->room == bat->room) && (bat->linkedObject != *matrixP))
            {
                int seekX = seekObject->x;
                int seekY = seekObject->y;

                // Set the movement

                // horizontal axis
                if (bat->x < seekX)
                {
                    bat->movementX = 3;
                }
                else if (bat->x > seekX)
                {
                    bat->movementX = -3;
                }
                else bat->movementX = 0;

                // vertical axis
                if (bat->y < seekY)
                {
                    bat->movementY = 3;
                }
                else if (bat->y > seekY)
                {
                    bat->movementY = -3;
                }
                else bat->movementY = 0;

                // If the bat is within 7 pixels of the seek object it can pick the object up
                // The bat extents have already been expanded by 7 pixels above, so a simple
                // rectangle intersection test is good enought here

                int objX, objY, objW, objH;
                CalcPlayerSpriteExtents(seekObject, &objX, &objY, &objW, &objH);

                if (HitTestRects(batX, batY, batW, batH, objX, objY, objW, objH))
                {
                    // Hit something we want

                    // If the bat grabs something that the ball is carrying, the bat gets it
                    // This allows the bat to take something we are carrying
                    if (*matrixP == objectBall->linkedObject)
                    {
                        // Now we have nothing
                        objectBall->linkedObject = OBJECT_NONE;
                    }

                    // Pick it up
                    bat->linkedObject = *matrixP;
                    bat->linkedObjectX = 8;
                    bat->linkedObjectY = 0;

                    // Reset the timer
                    batFedUpTimer = 0;
                }

                // break since we found something
                break;
            }
        }
        while (*(++matrixP));

    }
}

void Portals()
{
    OBJECT* port1 = objectDefs[OBJECT_PORT1];
    OBJECT* port2 = objectDefs[OBJECT_PORT2];
	OBJECT* port3 = objectDefs[OBJECT_PORT3];
	OBJECT* port4 = objectDefs[OBJECT_PORT4];

    const OBJECT* yellowKey = objectDefs[OBJECT_YELLOWKEY];
    const OBJECT* whiteKey = objectDefs[OBJECT_WHITEKEY];
    const OBJECT* blackKey = objectDefs[OBJECT_BLACKKEY];

    if ((port1->room == objectBall->room) && (yellowKey->room == objectBall->room) && (port1->state == 0 || port1->state == 12))
    {
        // Toggle the port state
        if (CollisionCheckObjectObject(port1, yellowKey))
            port1->state++;
    }
    if (port1->state != 0 && port1->state != 12)
    {
        // Raise/lower the port
        port1->state++;
    }
    if (port1->state > 22)
    {
        // Port 1 is unlocked
        port1->state = 0;
        roomDefs[entryRoomOffsets[OBJECT_PORT1]].roomDown = castleRoomOffsets[OBJECT_PORT1];
    }
    else if (port1->state == 12)
    {
        // Port 1 is locked
        roomDefs[entryRoomOffsets[OBJECT_PORT1]].roomDown = entryRoomOffsets[OBJECT_PORT1];
    }

    if ((port2->room == objectBall->room) && (whiteKey->room == objectBall->room) && (port2->state == 0 || port2->state == 12))
    {
        // Toggle the port state
        if (CollisionCheckObjectObject(port2, whiteKey))
            port2->state++;
    }
    if (port2->state != 0 && port2->state != 12)
    {
        // Raise/lower the port
        port2->state++;
    }
    if (port2->state > 22)
    {
        // Port 2 is unlocked
        port2->state = 0;
        roomDefs[entryRoomOffsets[OBJECT_PORT2]].roomDown = castleRoomOffsets[OBJECT_PORT2];
    }
    else if (port2->state == 12)
    {
        // Port 2 is locked
        roomDefs[entryRoomOffsets[OBJECT_PORT2]].roomDown = entryRoomOffsets[OBJECT_PORT2];
    }

    if ((port3->room == objectBall->room) && (blackKey->room == objectBall->room) && (port3->state == 0 || port3->state == 12))
    {
        // Toggle the port state
        if (CollisionCheckObjectObject(port3, blackKey))
            port3->state++;
    }
    if (port3->state != 0 && port3->state != 12)
    {
        // Raise/lower the port
        port3->state++;
    }
    if (port3->state > 22)
    {
        // Port 3 is unlocked
        port3->state = 0;
        roomDefs[entryRoomOffsets[OBJECT_PORT3]].roomDown = castleRoomOffsets[OBJECT_PORT3];
    }
    else if (port3->state == 12)
    {
        // Port 3 is locked
        roomDefs[entryRoomOffsets[OBJECT_PORT3]].roomDown = entryRoomOffsets[OBJECT_PORT3];
    }

	if ((port4->room == objectBall->room) && (yellowKey->room == objectBall->room) && (port4->state == 0 || port4->state == 12))
	{
		// Toggle the port state
		if (CollisionCheckObjectObject(port4, yellowKey))
			port4->state++;
	}
	if (port4->state != 0 && port4->state != 12)
	{
		// Raise/lower the port
		port4->state++;
	}
	if (port4->state > 22)
	{
		// Port 4 is unlocked
		port4->state = 0;
		roomDefs[entryRoomOffsets[OBJECT_PORT4]].roomDown = castleRoomOffsets[OBJECT_PORT4];
	}
	else if (port4->state == 12)
	{
		// Port 4 is locked
		roomDefs[entryRoomOffsets[OBJECT_PORT4]].roomDown = entryRoomOffsets[OBJECT_PORT4];
	}


}

int distanceFromBall(BALL* ball, int x, int y) {
    // Figure out the distance (which is really the max difference along one axis)
    int xdist = ball->x/2 - x;
    if (xdist < 0) {
        xdist = -xdist;
    }
    int ydist = ball->y/2 - y;
    if (ydist < 0) {
        ydist = -ydist;
    }
    int dist = (xdist > ydist ? xdist : ydist);
    return dist;
}

/**
 * Returns the ball closest to the point in the adventure.
 */
BALL* closestBall(int room, int x, int y) {
    int shortestDistance = 10000; // Some big number greater than the diagnol of the board
    BALL* found = 0x0;
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        if (balls[ctr].room == room)
        {
            int dist = distanceFromBall(&balls[ctr], x, y);
            if (dist < shortestDistance) {
                shortestDistance = dist;
                found = &balls[ctr];
            }
        }
    }
    return found;
}

void MoveDragon(Dragon* dragon, const int* matrix, int speed)
{
    if (dragon->state == Dragon::STALKING)
    {
        // Has the Ball hit the Dragon?
        if ((objectBall->room == dragon->room) && CollisionCheckObject(dragon, (objectBall->x-4), (objectBall->y-4), 8, 8))
        {
            dragon->roar(objectBall->x/2, objectBall->y/2,gameLevel, gameDifficultyLeft==DIFFICULTY_A);
            
            // Notify others
            DragonStateAction* action = new DragonStateAction(thisPlayer, dragon->dragonNumber, Dragon::ROAR, dragon->room, dragon->x, dragon->y);
            
            sync->BroadcastAction(action);

            // Play the sound
            Platform_MakeSound(SOUND_ROAR);
        }

        // Has the Sword hit the Dragon?
        if (CollisionCheckObjectObject(dragon, objectDefs[OBJECT_SWORD]))
        {
            // Set the State to 01 (Dead)
            dragon->state = Dragon::DEAD;
            dragon->movementX = 0;
            dragon->movementY = 0;
        
            // Play the sound
            Platform_MakeSound(SOUND_DRAGONDIE);
        }

        if (dragon->state == Dragon::STALKING)
        {
            // Go through the dragon's object matrix
            // Difficulty switch determines flee or don't flee from sword
            const int* matrixP = (gameDifficultyRight == DIFFICULTY_A) ? matrix : matrix+2;
            do
            {
                int seekDir = 0; // 1 is seeking, -1 is fleeing
                int seekX=0, seekY=0;

                int fleeObject = *(matrixP+0); 
                int seekObject = *(matrixP+1); 

                // Dragon fleeing an object
                if ((fleeObject > OBJECT_NONE) && objectDefs[fleeObject] != dragon)
                {
                    // get the object it is fleeing
                    const OBJECT* object = objectDefs[fleeObject];
                    if (object->room == dragon->room)
                    {
                        seekDir = -1;
                        seekX = object->x;
                        seekY = object->y;
                    }
                }
                else
                {
                    // Dragon seeking the ball
                    if (seekDir == 0)
                    {
                        if (*(matrixP+1) == OBJECT_BALL)
                        {
                            BALL* closest = closestBall(dragon->room, dragon->x, dragon->y);
                            if (closest != 0x0) {
                                seekDir = 1;
                                seekX = closest->x/2;
                                seekY = closest->y/2;
                            }
                        }
                    }

                    // Dragon seeking an object
                    if ((seekDir == 0) && (seekObject > OBJECT_NONE))
                    {
                        // Get the object it is seeking
                        const OBJECT* object = objectDefs[seekObject];
                        if (object->room == dragon->room)
                        {
                            seekDir = 1;
                            seekX = object->x;
                            seekY = object->y;
                        }
                    }
                }

                // Move the dragon
                if ((seekDir > 0) || (seekDir < 0))
                {
                    int newMovementX = 0;
                    int newMovementY = 0;

                    // horizontal axis
                    if (dragon->x < seekX)
                    {
                        newMovementX = seekDir*speed;
                    }
                    else if (dragon->x > seekX)
                    {
                        newMovementX = -(seekDir*speed);
                    }

                    // vertical axis
                    if (dragon->y < seekY)
                    {
                        newMovementY = seekDir*speed;
                    }
                    else if (dragon->y > seekY)
                    {
                        newMovementY = -(seekDir*speed);
                    }
                    
                    // Notify others if we've changed our direction
                    if ((dragon->room == objectBall->room) && ((newMovementX != dragon->movementX) || (newMovementY != dragon->movementY))) {
                        int distanceToMe = distanceFromBall(&balls[thisPlayer], dragon->x, dragon->y);
                        DragonMoveAction* newAction = new DragonMoveAction(thisPlayer, dragon->room, dragon->x, dragon->y, newMovementX, newMovementY, dragon->dragonNumber, distanceToMe);
                        sync->BroadcastAction(newAction);
                    }
                    dragon->movementX = newMovementX;
                    dragon->movementY = newMovementY;

                    // Found something - we're done
                    return;
                }
            }
            while (*(matrixP+=2));

        }
    }
    else if (dragon->state == Dragon::EATEN)
    {
        // Eaten
        dragon->eaten->room = dragon->room;
        dragon->eaten->x = (dragon->x + 3) * 2;
        dragon->eaten->y = (dragon->y - 10) * 2;
        dragon->movementX = 0;
        dragon->movementY = 0;
        if (objectBall == dragon->eaten) {
            displayedRoomIndex = objectBall->room;
        }
    }
    else if (dragon->state == Dragon::ROAR)
    {
        dragon->decrementTimer();
        if (dragon->timerExpired())
        {
            // Has the Ball hit the Dragon?
            if ((objectBall->room == dragon->room) && CollisionCheckObject(dragon, (objectBall->x-4), (objectBall->y-1), 8, 8))
            {
                // Set the State to 01 (eaten)
                dragon->eaten = objectBall;
                dragon->state = Dragon::EATEN;

                // Notify others
                DragonStateAction* action = new DragonStateAction(thisPlayer, dragon->dragonNumber, Dragon::EATEN, dragon->room, dragon->x, dragon->y);
                
                sync->BroadcastAction(action);
                

                // Play the sound
                Platform_MakeSound(SOUND_EATEN);
            }
            else
            {
                // Go back to stalking
                dragon->state = Dragon::STALKING;
            }
        }
    }
    // else dead!
}

void Magnet()
{
    const OBJECT* magnet = objectDefs[OBJECT_MAGNET];
    
    int i=0;
    while (magnetMatrix[i])
    {
        // Look for items in the magnet matrix that are in the same room as the magnet
        OBJECT* object = objectDefs[magnetMatrix[i]];
        if ((magnetMatrix[i] != objectBall->linkedObject) && (object->room == magnet->room))
        {
            // horizontal axis
            if (object->x < magnet->x)
                object->x++;
            else if (object->x > magnet->x)
                object->x--;

            // vertical axis - offset by the height of the magnet so items stick to the "bottom"
            if (object->y < (magnet->y - magnet->gfxData[0]))
                object->y++;
            else if (object->y > (magnet->y - magnet->gfxData[0]))
                object->y--;

            // Only attract the first item found in the matrix
            break;
        }
        ++i;
    }
}


int AdjustRoomLevel(int room)
{
    // If the the room number is above 0x80 it changes based on the game level
    if (room & 0x80)
    {
        // Remove the 0x80 flag and add the level number to get the offset into the room delta table
        int newRoomIndex = (room & ~0x80) + gameLevel;
        room = roomLevelDiffs[newRoomIndex];
    }

    return room;
}

void DrawObjects(int room)
{
    // Clear out the display list
	int displayList[MAX_OBJECTS];
    for (int i=0; i < MAX_OBJECTS; i++)
        displayList[i] = OBJECT_NONE;


    // Create a list of all the objects that want to be drawn
    int numAdded = 0;

    if (objectSurround.room == room)
        displayList[numAdded++] = OBJECT_SURROUND;

    int colorFirst = -1;
    int colorLast = -1;

    for (int i=0; objectDefs[i]->gfxData; i++)
    {
        // Init it to not displayed
        objectDefs[i]->displayed = false;
        if (objectDefs[i]->room == room)
        {
            // This object is in the current room - add it to the list
            displayList[numAdded++] = i;

            if (colorFirst < 0) colorFirst = objectDefs[i]->color;
            colorLast = objectDefs[i]->color;
        }
    }

    // Now display the objects in the list, up to the max number of objects at a time

    if (numAdded <= maxDisplayableObjects)
        displayListIndex = 0;
    else
    {
        if (displayListIndex > numAdded)
            displayListIndex = 0;
        if (displayListIndex > MAX_OBJECTS)
            displayListIndex = 0;
        if (displayList[displayListIndex] == OBJECT_NONE)
            displayListIndex = 0;
    }

    objectSurround.displayed = false;

    int numDisplayed = 0;
    int i = displayListIndex;
    if (showObjectFlicker)
    {
        //
        // If more than maxDisplayableObjects are needed to be drawn, we multiplex/cycle through them
        // Note that this also (intentionally) effects collision checking, as per the original game!!
        //
        while ((numDisplayed++) < numAdded && (numDisplayed <= maxDisplayableObjects))
        {
            if (displayList[i] > OBJECT_NONE)
            {
                DrawObject(objectDefs[displayList[i]]);
                objectDefs[displayList[i]]->displayed = true;
                colorLast = objectDefs[displayList[i]]->color;
            }
            else if (displayList[i] == OBJECT_SURROUND)
            {
                objectSurround.displayed = true;
            }

            // wrap to the beginning of the list if we've reached the end
            ++i;
            if (i > MAX_OBJECTS)
                i = 0;
            else if (displayList[i] == OBJECT_NONE)
                i = 0;
        }
    }
    else
    {
        //
        // We still need to keep the displayed flags up to date for proper collision checking
        //
        while ((numDisplayed++) < numAdded && (numDisplayed <= maxDisplayableObjects))
        {
            if (displayList[i] > OBJECT_NONE)
            {
                objectDefs[displayList[i]]->displayed = true;
                colorLast = objectDefs[displayList[i]]->color;
            }
            else if (displayList[i] == OBJECT_SURROUND)
            {
                objectSurround.displayed = true;
            }

            // wrap to the beginning of the list if we've reached the end
            ++i;
            if (i > MAX_OBJECTS)
                i = 0;
            else if (displayList[i] == OBJECT_NONE)
                i = 0;
        }

        // Now just paint everything in this room so we bypass the flicker if desired
        for (int i=0; objectDefs[i]->gfxData; i++)
        {
            if (objectDefs[i]->room == room)
                DrawObject(objectDefs[i]);
        }
    }

    if (roomDefs[room].flags & ROOMFLAG_LEFTTHINWALL)
    {
        // Position missile 00 to 0D,00 - left thin wall
        COLOR color = colorTable[(colorFirst > 0) ? colorFirst : COLOR_BLACK];
        Platform_PaintPixel(color.r,color.g,color.b, 0x0D*2, 0x00*2, 4, ADVENTURE_TOTAL_SCREEN_HEIGHT);
    }
    if (roomDefs[room].flags & ROOMFLAG_RIGHTTHINWALL)
    {
        // Position missile 01 to 96,00 - right thin wall
        COLOR color = colorTable[(colorFirst > 0) ? colorLast : COLOR_BLACK];
        Platform_PaintPixel(color.r,color.g,color.b, 0x96*2, 0x00*2, 4, ADVENTURE_TOTAL_SCREEN_HEIGHT);
    }

}

void DrawBall(const BALL* ball, COLOR color)
{
	int left = (ball->x - 4) & ~0x00000001;
	int bottom = (ball->y - 10) & ~0x00000001; // Don't know why ball is drawn 2 pixels below y value

	// scan the data
	const byte* rowByte = ball->gfxData;
	++rowByte; // We know the ball is height=8 so skip that entry in the array
	for (int row = bottom+7; row >= bottom; --row, ++rowByte)
	{
		for (int bit = 0; bit < 8; bit++)
		{
			// If there is a bit in the graphics matric at this row and bit, paint a pixel
			if (*rowByte & (1 << (7 - bit))) {
				int x = left + bit;
				if (x < ADVENTURE_SCREEN_WIDTH) {
					Platform_PaintPixel(color.r, color.g, color.b, x, row, 1, 1);
				}
			}
		}
	}
}

void DrawObject(const OBJECT* object)
{
	// Get object color, size, and position
	COLOR color = object->color == COLOR_FLASH ? GetFlashColor() : colorTable[object->color];
	int cx = object->x * 2;
	int cy = object->y * 2;
    int size = (object->size/2) + 1;

	// Look up the index to the current state for this object
	int stateIndex = object->states ? object->states[object->state] : 0;
    
	// Get the height, then the data
	// (the first byte of the data is the height)
	const byte* dataP = object->gfxData;
	int objHeight = *dataP;
	++dataP;

	// Index into the proper state
    for (int x=0; x < stateIndex; x++)
	{
		dataP += objHeight; // skip over the data
		objHeight = *dataP;
		++dataP;
    }

    // Adjust for proper position
    cx -= CLOCKS_HSYNC;
    cy -= CLOCKS_VSYNC;

    // scan the data
    const byte* rowByte = dataP;
    for (int i=0; i<objHeight; i++)
    {
        // Parse the row - each bit is a 2 x 2 block
        for (int bit=0; bit < 8; bit++)
        {
            if (*rowByte & (1 << (7-bit)))
            {
                int x = cx+(bit*2*size);
                if (x >= ADVENTURE_SCREEN_WIDTH)
                    x-=ADVENTURE_SCREEN_WIDTH;
                Platform_PaintPixel(color.r, color.g, color.b, x, cy, 2*size, 2);
            }
        }

        // next byte - next row
        ++rowByte;
        cy-=2;
    }
}

bool CollisionCheckBallWithWalls(int room, int x, int y)
{
    bool hitWall = false;

    // The playfield is drawn partially in the overscan area, so shift that out here
    y-=30;

    // get the playfield data
    const ROOM* currentRoom = &roomDefs[room];
    const byte* roomData = currentRoom->graphicsData;

    // get the playfield mirror flag
    bool mirror = currentRoom->flags & ROOMFLAG_MIRROR;

    // mask values for playfield bits
    byte shiftreg [20] = 
    {
        0x10,0x20,0x40,0x80,
        0x80,0x40,0x20,0x10,0x8,0x4,0x2,0x1,
        0x1,0x2,0x4,0x8,0x10,0x20,0x40,0x80
    };

    // each cell is 8 x 32
    const int cell_width = 8;
    const int cell_height = 32;

    if ((currentRoom->flags & ROOMFLAG_LEFTTHINWALL) && ((x-(4+4)) < 0x0D*2))
    {
        hitWall = true;
    }
    if ((currentRoom->flags & ROOMFLAG_RIGHTTHINWALL) && ((x+4) > 0x96*2))
    {
        // If the dot is in this room, allow passage through the wall into the Easter Egg room
        if (objectDefs[OBJECT_DOT]->room != room)
            hitWall = true;
    }

    // Check each bit of the playfield data to see if they intersect the ball
    for (int cy=0; (cy<=6) & !hitWall; cy++)
    {
        byte pf0 = roomData[(cy*3) + 0];
        byte pf1 = roomData[(cy*3) + 1];
        byte pf2 = roomData[(cy*3) + 2];

        int ypos = 6-cy;

        for (int cx=0; cx<20; cx++)
        {
            bool bit=false;

            if (cx < 4)
                bit = pf0 & shiftreg[cx];
            else if (cx < 12)
                bit = pf1 & shiftreg[cx];
            else
                bit = pf2 & shiftreg[cx];

            if (bit)
            {
                if (HitTestRects(x-4,(y-4),8,8,cx*cell_width,(ypos*cell_height), cell_width, cell_height))
                {
                    hitWall = true;
                    break;
                }

                if (mirror)
                {
                    if (HitTestRects(x-4,(y-4),8,8,(cx+20)*cell_width,(ypos*cell_height), cell_width, cell_height))
                    {
                        hitWall = true;
                        break;
                    }
                }
                else
                {
                    if (HitTestRects(x-4,(y-4),8,8,((40-(cx+1))*cell_width),(ypos*cell_height), cell_width, cell_height))
                    {
                        hitWall = true;
                        break;
                    }
                }

            }

        }
    }

    return hitWall;
}

static bool CrossingBridge(int room, int x, int y, BALL* ball)
{
    // Check going through the bridge
    const OBJECT* bridge = objectDefs[OBJECT_BRIDGE];
    if ((bridge->room == room)
        && (ball->linkedObject != OBJECT_BRIDGE))
    {
        int xDiff = (x/2) - bridge->x;
        if ((xDiff >=0x0A) && (xDiff <= 0x17))
        {
            int yDiff = bridge->y - (y/2);

            if ((yDiff >= -5) && (yDiff <= 0x15))
            {
                return true;
            }
        }
    }
    return false;
}

static int CollisionCheckBallWithObjects(BALL* ball, int startIndex)
{
    // Go through all the objects
    for (int i=startIndex; objectDefs[i]->gfxData; i++)
    {
        // If this object is in the current room, check it against the ball
        const OBJECT* object = objectDefs[i];
        if (object->displayed && (ball->room == object->room))
        {
            if (CollisionCheckObject(object, ball->x-4,(ball->y-1), 8, 8))
            {
                // return the index of the object
                return i;
            }
        }
    }

    return OBJECT_NONE;
}

void CalcPlayerSpriteExtents(const OBJECT* object, int* cx, int* cy, int* cw, int* ch)
{
    // Calculate the object's size and position
    *cx = object->x * 2;
    *cy = object->y * 2;

    int size = (object->size/2) + 1;
    *cw = (8 * 2) * size;

    // Look up the index to the current state for this object
    int stateIndex = object->states ? object->states[object->state] : 0;

    // Get the height, then the data
    // (the first byte of the data is the height)
    const byte* dataP = object->gfxData;
    *ch = *dataP;
    ++dataP;

    // Index into the proper state
    for (int x=0; x < stateIndex; x++)
    {
        dataP += *ch; // skip over the data
        *ch = *dataP;
        ++dataP;
    }

    *ch *= 2;

    // Adjust for proper position
    *cx -= CLOCKS_HSYNC;
}

// Collision check two objects
// On the 2600 this is done in hardware by the Player/Missile collision registers
bool CollisionCheckObjectObject(const OBJECT* object1, const OBJECT* object2)
{
    // Before we do pixel by pixel collision checking, do some trivial rejection
    // and return early if the object extents do not even overlap or are not in the same room

    if (object1->room != object2->room)
        return false;

    int cx1, cy1, cw1, ch1;
    int cx2, cy2, cw2, ch2;
    CalcPlayerSpriteExtents(object1, &cx1, &cy1, &cw1, &ch1);
    CalcPlayerSpriteExtents(object2, &cx2, &cy2, &cw2, &ch2);
    if (!HitTestRects(cx1, cy1, cw1, ch1, cx2, cy2, cw2, ch2))
        return false;

    // Object extents overlap go pixel by pixel

    int objectX1 = object1->x;
    int objectY1 = object1->y;
    int objectSize1 = (object1->size/2) + 1;

    int objectX2 = object2->x;
    int objectY2 = object2->y;
    int objectSize2 = (object2->size/2) + 1;

    // Look up the index to the current state for the objects
    int stateIndex1 = object1->states ? object1->states[object1->state] : 0;
    int stateIndex2 = object2->states ? object2->states[object2->state] : 0;
    
    // Get the height, then the data
    // (the first byte of the data is the height)

    const byte* dataP1 = object1->gfxData;
    int objHeight1 = *dataP1;
    ++dataP1;

    const byte* dataP2 = object2->gfxData;
    int objHeight2 = *dataP2;
    ++dataP2;

    // Index into the proper states
    for (int i=0; i < stateIndex1; i++)
    {
        dataP1 += objHeight1; // skip over the data
        objHeight1 = *dataP1;
        ++dataP1;
    }
    for (int i=0; i < stateIndex2; i++)
    {
        dataP2 += objHeight2; // skip over the data
        objHeight2 = *dataP2;
        ++dataP2;
    }

    // Adjust for proper position
    objectX1 -= CLOCKS_HSYNC;
    objectX2 -= CLOCKS_HSYNC;

    // Scan the the object1 data
    const byte* rowByte1 = dataP1;
    for (int i=0; i < objHeight1; i++)
    {
        // Parse the object1 row - each bit is a 2 x 2 block
        for (int bit1=0; bit1 < 8; bit1++)
        {
            if (*rowByte1 & (1 << (7-bit1)))
            {
                // test this pixel of object1 for intersection against the pixels of object2
                
                // Scan the the object2 data
                objectY2 = object2->y;
                const byte* rowByte2 = dataP2;
                for (int j=0; j < objHeight2; j++)
                {
                    // Parse the object2 row - each bit is a 2 x 2 block
                    for (int bit2=0; bit2 < 8; bit2++)
                    {
                        if (*rowByte2 & (1 << (7-bit2)))
                        {
                            int wrappedX1 = objectX1+(bit1*2*objectSize1);
                            if (wrappedX1 >= ADVENTURE_SCREEN_WIDTH)
                                wrappedX1-=ADVENTURE_SCREEN_WIDTH;

                            int wrappedX2 = objectX2+(bit2*2*objectSize2);
                            if (wrappedX2 >= ADVENTURE_SCREEN_WIDTH)
                                wrappedX2-=ADVENTURE_SCREEN_WIDTH;

                            if (HitTestRects(wrappedX1, objectY1, 2*objectSize1, 2, wrappedX2, objectY2, 2*objectSize2, 2))
                                // The objects are touching
                                return true;
                        }
                    }

                    // Object 2 - next byte and next row
                    ++rowByte2;
                    objectY2-=2;
                }
            }
        }

        // Object 1 - next byte and next row
        ++rowByte1;
        objectY1-=2;
    }

    return false;

}

// Checks an object for collision against the specified rectangle
// On the 2600 this is done in hardware by the Player/Missile collision registers
bool CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height)
{
    int objectX = object->x * 2;
    int objectY = object->y * 2;
    int objectSize = (object->size/2) + 1;

    // Look up the index to the current state for this object
    int stateIndex = object->states ? object->states[object->state] : 0;
    
    // Get the height, then the data
    // (the first byte of the data is the height)
    const byte* dataP = object->gfxData;
    int objHeight = *dataP;
    ++dataP;

    // Index into the proper state
    for (int i=0; i < stateIndex; i++)
    {
        dataP += objHeight; // skip over the data
        objHeight = *dataP;
        ++dataP;
    }

    // Adjust for proper position
    objectX -= CLOCKS_HSYNC;

    // scan the data
    const byte* rowByte = dataP;
    for (int i=0; i < objHeight; i++)
    {
        // Parse the row - each bit is a 2 x 2 block
        for (int bit=0; bit < 8; bit++)
        {
            if (*rowByte & (1 << (7-bit)))
            {
                // test this pixel for intersection
            
                int wrappedX = objectX+(bit*2*objectSize);
                if (wrappedX >= ADVENTURE_SCREEN_WIDTH)
                    wrappedX-=ADVENTURE_SCREEN_WIDTH;
                
                if (HitTestRects(x, y, width, height, wrappedX, objectY, 2*objectSize, 2))
                    return true;
            }
        }

        // next byte - next row
        ++rowByte;
        objectY-=2;
    }

    return false;
}

bool HitTestRects(int ax, int ay, int awidth, int aheight,
                int bx, int by, int bwidth, int bheight)
{
    bool intersects = true;

    if ( ((ay-aheight) >= by) || (ay <= (by-bheight)) || ((ax+awidth) <= bx) || (ax >= (bx+bwidth)) )
    {
        // Does not intersect
        intersects = false;
    }
    // else must intersect

    return intersects;
}

COLOR GetFlashColor()
{
    COLOR color;

    float r=0, g=0, b=0;
    float h = flashColorHue / (360.0/3);
    if (h < 1)
    {
        r = h * 255;
        g = 0;
        b = (1-h) * 255;
    }
    else if (h < 2)
    {
        h -= 1;
        r = (1-h) * 255;
        g = h * 255;
        b = 0;
    }
    else
    {
        h -= 2;
        r = 0;
        g = (1-h) * 255;
        b = h * 255;
    }

    color.r = max(flashColorLum, r);
    color.g = max(flashColorLum, g);
    color.b = max(flashColorLum, b);

    return color;
}

void AdvanceFlashColor()
{
    flashColorHue += 2;
    if (flashColorHue >= 360)
        flashColorHue -= 360;

    flashColorLum += 11;
    if (flashColorLum > 200)
        flashColorLum = 0;

}

