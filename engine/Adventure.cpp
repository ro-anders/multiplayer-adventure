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
#include <iostream>


#include "Adventure.h"

#include "adventure_sys.h"
#include "color.h"
#include "Ball.hpp"
#include "Board.hpp"
#include "Bat.hpp"
#include "Dragon.hpp"
#include "GameObject.hpp"
#include "Map.hpp"
#include "Portcullis.hpp"
#include "Room.hpp"
#include "ScriptedSync.hpp"
#include "Sync.hpp"
#include "Sys.hpp"
#include "Transport.hpp"

#ifndef max
#define max(a,b) ((a > b) ? a : b);
#endif


// Types


#define OBJECT_LEFTWALL		(-3)
#define OBJECT_RIGHTWALL	(-4)
#define OBJECT_SURROUND		(-5) // Actually, up to 3 surrounds with values -5 to -7


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
static void Portals();
static void SyncDragons();
static void MoveDragon(Dragon* dragon, const int* matrix, int speed);
static void Magnet();

// My helper functions
void addAllRoomsToPort(Portcullis* port, int firstRoom, int lastRoom);
bool checkWonGame();
static void DrawObjects(int room);
static void DrawObject(const OBJECT* object);
void DrawBall(const BALL* ball, COLOR color);
static bool CrossingBridge(int room, int x, int y, BALL* ball);
static bool CollisionCheckBallWithWalls(int room, int x, int y);
static int CollisionCheckBallWithAllObjects(BALL* ball);
static int CollisionCheckBallWithObjects(BALL* ball, Board::ObjIter& iter);
static bool CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height);
void handleSetupMessages();
void randomizeRoomObjects();
static void ResetPlayers();
static void ResetPlayer(BALL* ball);
static void WinGame(int winCastle);


COLOR GetFlashColor();
void AdvanceFlashColor();

//
// local game state vars
//

static bool joyLeft, joyUp, joyRight, joyDown, joyFire;
static bool switchReset;

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

static int gameMapLayout = 0;                               // The board setup.  Level 1 = 0, Levels 2 & 3 = 1, Gauntlet = 2

/** There are five game modes, the original three (but zero justified so game mode 0 means original level 1) and
 * a new fourth, gameMode 3, which I call The Gauntlet. The fifth is used for generating videos and plays a preplanned script. */
static int gameMode = 0;
static bool joystickDisabled = false;

#define GAMEOPTION_PRIVATE_MAGNETS  1
// This holds all the switches for whether to turn on or off different game options
// It is a bitwise or of each game option
static int gameOptions = 0;

static int displayedRoomIndex = 0;                                   // index of current (displayed) room

static int winFlashTimer=0;
static int winningRoom=-1; // The room number of the castle of the winning player.

static int flashColorHue=0;
static int flashColorLum=0;

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

static BALL* objectBall = 0x0;

static OBJECT** surrounds;
//
// Indexed array of all objects and their properties
//
static Board board(ADVENTURE_SCREEN_WIDTH, ADVENTURE_SCREEN_HEIGHT);
static Board* gameBoard = &board;

//
// Indexed array of all rooms and their properties
//
static ROOM** roomDefs = NULL;
static Map* gameMap = NULL;

// Object locations (room and coordinate) for game 01
//        - object, room, x, y, state, movement(x/y)
static const byte game1Objects [] =
{
    OBJECT_YELLOW_PORT, 0x11, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
    OBJECT_COPPER_PORT, COPPER_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 4
    OBJECT_JADE_PORT, 0x1F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 5
    OBJECT_WHITE_PORT, 0x0F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 2
    OBJECT_BLACK_PORT, 0x10, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_NAME, 0x1E, 0x50, 0x69, 0x00, 0x00, 0x00, // Robinett message
    OBJECT_NUMBER, 0x00, 0x50, 0x40, 0x00, 0x00, 0x00, // Starting number
    OBJECT_REDDRAGON, 0x0E, 0x50, 0x20, 0x00, 0x00, 0x00, // Red Dragon
    OBJECT_YELLOWDRAGON, 0x01, 0x50, 0x20, 0x00, 0x00, 0x00, // Yellow Dragon
    OBJECT_GREENDRAGON, 0x1D, 0x50, 0x20, 0x00, 0x00, 0x00, // Green Dragon
    OBJECT_SWORD, 0x12, 0x20, 0x20, 0x00, 0x00, 0x00, // Sword
    OBJECT_BRIDGE, 0x04, 0x2A, 0x37, 0x00, 0x00, 0x00, // Bridge
    OBJECT_YELLOWKEY, 0x11, 0x20, 0x41, 0x00, 0x00, 0x00, // Yellow Key
    OBJECT_COPPERKEY, COPPER_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00, // Copper Key
    OBJECT_JADEKEY, JADE_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00, // Jade Key
    OBJECT_WHITEKEY, 0x0E, 0x20, 0x40, 0x00, 0x00, 0x00, // White Key
    OBJECT_BLACKKEY, 0x1D, 0x20, 0x40, 0x00, 0x00, 0x00, // Black Key
    OBJECT_BAT, 0x1A, 0x20, 0x20, 0x00, 0x00, 0x00, // Bat
    OBJECT_DOT, 0x15, 0x51, 0x12, 0x00, 0x00, 0x00, // Dot
    OBJECT_CHALISE, 0x02 /*0x1C*/, 0x30, 0x20, 0x00, 0x00, 0x00, // Challise
    OBJECT_MAGNET, 0x1B, 0x80, 0x20, 0x00, 0x00, 0x00, // Magnet
    0xff,0,0,0,0,0,0
};

// Object locations (room and coordinate) for Games 02 and 03
//        - object, room, x, y, state, movement(x/y)
static const byte game2Objects [] =
{
    OBJECT_YELLOW_PORT, 0x11, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
    OBJECT_COPPER_PORT, COPPER_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 4
    OBJECT_JADE_PORT, 0x1F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 5
    OBJECT_WHITE_PORT, 0x0F, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 2
    OBJECT_BLACK_PORT, 0x10, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_NAME, 0x1E, 0x50, 0x69, 0x00, 0x00, 0x00, // Robinett message
    OBJECT_NUMBER, 0x00, 0x50, 0x40, 0x00, 0x00, 0x00, // Starting number
    OBJECT_REDDRAGON, 0x14, 0x50, 0x20, 0x00, 3, 3, // Red Dragon
    OBJECT_YELLOWDRAGON, 0x19, 0x50, 0x20, 0x00, 3, 3, // Yellow Dragon
    OBJECT_GREENDRAGON, 0x04, 0x50, 0x20, 0x00, 3, 3, // Green Dragon
    OBJECT_SWORD, 0x11, 0x20, 0x20, 0x00, 0x00, 0x00, // Sword
    OBJECT_BRIDGE, 0x0B, 0x40, 0x40, 0x00, 0x00, 0x00, // Bridge
    OBJECT_YELLOWKEY, 0x09, 0x20, 0x40, 0x00, 0x00, 0x00, // Yellow Key
    OBJECT_COPPERKEY, COPPER_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00, // Copper Key
    OBJECT_JADEKEY, JADE_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00, // Copper Key
    OBJECT_WHITEKEY, 0x06, 0x20, 0x40, 0x00, 0x00, 0x00, // White Key
    OBJECT_BLACKKEY, 0x19, 0x20, 0x40, 0x00, 0x00, 0x00, // Black Key
    OBJECT_BAT, 0x02, 0x20, 0x20, 0x00, 0, -3, // Bat
    OBJECT_DOT, 0x15, 0x45, 0x12, 0x00, 0x00, 0x00, // Dot
    OBJECT_CHALISE, 0x14, 0x30, 0x20, 0x00, 0x00, 0x00, // Challise
    OBJECT_MAGNET, 0x0E, 0x80, 0x20, 0x00, 0x00, 0x00, // Magnet
    0xff,0,0,0,0,0,0
};

// Object locations (room and coordinate) for game 01
//        - object, room, x, y, state, movement(x/y)
static const byte gameGauntletObjects [] =
{
    OBJECT_YELLOW_PORT, GOLD_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
    OBJECT_BLACK_PORT, BLACK_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_NAME, ROBINETT_ROOM, 0x50, 0x69, 0x00, 0x00, 0x00, // Robinett message
    OBJECT_NUMBER, 0x00, 0x50, 0x40, 0x00, 0x00, 0x00, // Starting number
    OBJECT_REDDRAGON, BLUE_MAZE_1, 0x50, 0x20, 0x00, 0x00, 0x00, // Red Dragon
    OBJECT_YELLOWDRAGON, MAIN_HALL_CENTER, 0x50, 0x20, 0x00, 0x00, 0x00, // Yellow Dragon
    OBJECT_GREENDRAGON, MAIN_HALL_LEFT, 0x50, 0x20, 0x00, 0x00, 0x00, // Green Dragon
    0xff,0,0,0,0,0,0
};


// Magnet Object Matrix
static const int magnetMatrix[] =
{
       OBJECT_YELLOWKEY,
       OBJECT_JADEKEY,
       OBJECT_COPPERKEY,
       OBJECT_WHITEKEY,
       OBJECT_BLACKKEY,
       OBJECT_SWORD,
       OBJECT_BRIDGE,
       OBJECT_CHALISE,
       0x00
};

// Green Dragon's Object Matrix                                                                                      
static const int greenDragonMatrix[] =
{
    OBJECT_SWORD, OBJECT_GREENDRAGON,       // runs from sword
    OBJECT_JADEKEY, OBJECT_GREENDRAGON,     // runs from Jade key
    OBJECT_GREENDRAGON, OBJECT_BALL,        // goes after any Ball
    OBJECT_GREENDRAGON, OBJECT_CHALISE,     // guards Chalise
    OBJECT_GREENDRAGON, OBJECT_BRIDGE,      // guards Bridge
    OBJECT_GREENDRAGON, OBJECT_MAGNET,      // guards Magnet
    OBJECT_GREENDRAGON, OBJECT_BLACKKEY,    // guards Black Key
    0x00, 0x00
};

// Yellow Dragon's Object Matrix                                                                                      
static const int yellowDragonMatrix[] =
{
    OBJECT_SWORD, OBJECT_YELLOWDRAGON,      // runs from sword
    OBJECT_YELLOWKEY, OBJECT_YELLOWDRAGON,  // runs from Yellow Key
    OBJECT_YELLOWDRAGON, OBJECT_BALL,       // goes after any Ball
    OBJECT_YELLOWDRAGON, OBJECT_CHALISE,    // guards Challise
    0x00, 0x00
};

// Red Dragon's Object Matrix                                                                                      
static const int redDragonMatrix[] =
{
    OBJECT_SWORD, OBJECT_REDDRAGON,         // runs from sword
    OBJECT_COPPERKEY, OBJECT_REDDRAGON,     // runs from Copper key
    OBJECT_REDDRAGON, OBJECT_BALL,          // goes after any Ball
    OBJECT_REDDRAGON, OBJECT_CHALISE,       // guards Chalise
    OBJECT_REDDRAGON, OBJECT_WHITEKEY,      // guards White Key
    0x00, 0x00
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
static int numPorts = 4;
static Portcullis** ports = NULL;


void Adventure_Setup(int inNumPlayers, int inThisPlayer, Transport* inTransport, int inGameNum,
                     int initialLeftDiff, int initialRightDiff) {
    numPlayers = inNumPlayers;
    thisPlayer = inThisPlayer;
    gameMode = inGameNum;
    joystickDisabled = (gameMode == GAME_MODE_SCRIPTING);
    timeToStartGame = 60 * 3;
    
    // The map for game 3 is the same as 2 and the map for scripting is hard-coded here
    // so it could be easily changed.
    gameMapLayout = (gameMode == GAME_MODE_SCRIPTING ? GAME_MODE_2 :
                     (gameMode == GAME_MODE_3 ? GAME_MODE_2 : gameMode));
    gameMap = new Map(numPlayers, gameMapLayout);
    roomDefs = gameMap->roomDefs;
    
    surrounds = new OBJECT*[numPlayers];
    char surroundName[16];
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        sprintf(surroundName, "surround%d", ctr);
        surrounds[ctr] = new OBJECT(surroundName, objectGfxSurround, 0, 0, COLOR_ORANGE, OBJECT::FIXED_LOCATION, 0x07);
    }
    
    Dragon::Difficulty difficulty = (gameMode == GAME_MODE_1 ? (initialLeftDiff == DIFFICULTY_B ?  Dragon::TRIVIAL : Dragon::EASY) :
                                     (initialLeftDiff == DIFFICULTY_B ? Dragon::MODERATE : Dragon::HARD));
    Dragon::setDifficulty(difficulty);
    dragons = new Dragon*[numDragons];
    dragons[0] = new Dragon( "yorgle", 0, COLOR_YELLOW, 2, yellowDragonMatrix);
    dragons[1] = new Dragon("grindle", 1, COLOR_LIMEGREEN, 2, greenDragonMatrix);
    dragons[2] = new Dragon("rhindle", 2, COLOR_RED, 3, redDragonMatrix);
    bat = new Bat(COLOR_BLACK);

    OBJECT* goldKey = new OBJECT("gold key", objectGfxKey, 0, 0, COLOR_YELLOW, OBJECT::OUT_IN_OPEN);
    OBJECT* copperKey = new OBJECT("coppey key", objectGfxKey, 0, 0, COLOR_COPPER, OBJECT::OUT_IN_OPEN);
    OBJECT* jadeKey = new OBJECT("jade key", objectGfxKey, 0, 0, COLOR_JADE, OBJECT::OUT_IN_OPEN);
    OBJECT* whiteKey = new OBJECT("white key", objectGfxKey, 0, 0, COLOR_WHITE);
    OBJECT* blackKey = new OBJECT("black key", objectGfxKey, 0, 0, COLOR_BLACK);
    
    numPorts = numPlayers + 2;
    ports = new Portcullis*[5]; // We always create 5 even though we might only use 4.
    ports[0] = new Portcullis("gold gate", GOLD_CASTLE, gameMap->getRoom(GOLD_FOYER), goldKey); // Gold
    ports[1] = new Portcullis("white gate", WHITE_CASTLE, gameMap->getRoom(RED_MAZE_1), whiteKey); // White
    addAllRoomsToPort(ports[1], RED_MAZE_3, RED_MAZE_1);
    ports[2] = new Portcullis("black gate", BLACK_CASTLE, gameMap->getRoom(BLACK_FOYER), blackKey); // Black
    addAllRoomsToPort(ports[2], BLACK_MAZE_1, BLACK_MAZE_ENTRY);
    ports[2]->addRoom(gameMap->getRoom(BLACK_FOYER));
    ports[2]->addRoom(gameMap->getRoom(BLACK_INNERMOST_ROOM));
    ports[3] = new Portcullis("copper gate", COPPER_CASTLE, gameMap->getRoom(COPPER_FOYER), copperKey);
    ports[4] = new Portcullis("jade gate", JADE_CASTLE, gameMap->getRoom(JADE_FOYER), jadeKey);
    gameMap->addCastles(numPorts, ports);
    

    // Setup the number.  Unlike other objects we need to position the number immediately.
    OBJECT* number = new OBJECT("number", objectGfxNum, numberStates, 0, COLOR_LIMEGREEN, OBJECT::FIXED_LOCATION);
    gameBoard->addObject(OBJECT_NUMBER, number);
    number->init(0x00, 0x50, 0x40);

    // Setup the rest of the objects
    gameBoard->addObject(OBJECT_YELLOW_PORT, ports[0]);
    gameBoard->addObject(OBJECT_COPPER_PORT, ports[3]);
    gameBoard->addObject(OBJECT_JADE_PORT, ports[4]);
    gameBoard->addObject(OBJECT_WHITE_PORT, ports[1]);
    gameBoard->addObject(OBJECT_BLACK_PORT, ports[2]);
    gameBoard->addObject(OBJECT_NAME, new OBJECT("easter egg message", objectGfxAuthor, 0, 0, COLOR_FLASH, OBJECT::FIXED_LOCATION));
    gameBoard->addObject(OBJECT_REDDRAGON, dragons[2]);
    gameBoard->addObject(OBJECT_YELLOWDRAGON,dragons[0]);
    gameBoard->addObject(OBJECT_GREENDRAGON, dragons[1]);
    gameBoard->addObject(OBJECT_SWORD, new OBJECT("sword", objectGfxSword, 0, 0, COLOR_YELLOW));
    gameBoard->addObject(OBJECT_BRIDGE, new OBJECT("bridge", objectGfxBridge, 0, 0, COLOR_PURPLE,
                                                   OBJECT::OPEN_OR_IN_CASTLE, 0x07));
    gameBoard->addObject(OBJECT_YELLOWKEY, goldKey);
    gameBoard->addObject(OBJECT_COPPERKEY, copperKey);
    gameBoard->addObject(OBJECT_JADEKEY, jadeKey);
    gameBoard->addObject(OBJECT_WHITEKEY, whiteKey);
    gameBoard->addObject(OBJECT_BLACKKEY, blackKey);
    gameBoard->addObject(OBJECT_BAT, bat);
    gameBoard->addObject(OBJECT_DOT, new OBJECT("dot", objectGfxDot, 0, 0, COLOR_LTGRAY, OBJECT::FIXED_LOCATION));
    gameBoard->addObject(OBJECT_CHALISE, new OBJECT("chalise", objectGfxChallise, 0, 0, COLOR_FLASH));
    gameBoard->addObject(OBJECT_MAGNET, new OBJECT("magnet", objectGfxMagnet, 0, 0, COLOR_BLACK));

    // Setup the players
    
    gameBoard->addPlayer(new BALL(0, ports[0]), thisPlayer == 0);
    Portcullis* p2Home = (gameMode == GAME_MODE_GAUNTLET ? ports[0] : ports[3]);
    gameBoard->addPlayer(new BALL(1, p2Home), thisPlayer == 1);
    if (numPlayers > 2) {
        Portcullis* p3Home = (gameMode == GAME_MODE_GAUNTLET ? ports[0] : ports[4]);
        gameBoard->addPlayer(new BALL(2, p3Home), thisPlayer == 2);
    }
    objectBall = gameBoard->getPlayer(thisPlayer);

    // Setup the transport
    transport = inTransport;
    sync = (gameMode == GAME_MODE_SCRIPTING ? new ScriptedSync(numPlayers, thisPlayer) :
                                              new Sync(numPlayers, thisPlayer, transport));
    
    // Need to have the transport setup before we setup the objects,
    // because we may be broadcasting randomized locations to other machines
    SetupRoomObjects();
    
    printf("Player %d setup.\n", thisPlayer);
}

void addAllRoomsToPort(Portcullis* port, int firstRoom, int lastRoom) {
    for(int nextKey=firstRoom; nextKey <= lastRoom; ++nextKey) {
        ROOM* nextRoom = gameMap->getRoom(nextKey);
        port->addRoom(nextRoom);
    }
}

void ResetPlayers() {
    for(int ctr=0; ctr<gameBoard->getNumPlayers(); ++ctr) {
        ResetPlayer(gameBoard->getPlayer(ctr));
    }
}

void ResetPlayer(BALL* ball) {
    ball->room = ball->homeGate->room;                 // Put us at our home castle
    ball->x = 0x50*2;                  //
    ball->y = 0x20*2;                  //
    ball->previousX = ball->x;
    ball->previousY = ball->y;
    ball->linkedObject = OBJECT_NONE;  // Not carrying anything
    ball->setGlowing(false);
    
    displayedRoomIndex = objectBall->room;
    
    // Make the bat want something right away
    // I guess the bat is reset just like the dragons are reset.
    if (bat->exists()) {
        bat->lookForNewObject();
    }
    
    // Bring the dragons back to life
    for(int ctr=0; ctr<numDragons; ++ctr) {
        Dragon* dragon = dragons[ctr];
        if (dragon->state == Dragon::DEAD) {
            dragon->state = Dragon::STALKING;
        } else if (dragon->eaten == ball) {
            dragon->state = Dragon::STALKING;
            dragon->eaten = NULL;
        }
    }
}

void SyncWithOthers() {
    sync->PullLatestMessages();

    // Check for any setup messages first.
    handleSetupMessages();
    
    // Move all the other players
    OtherBallMovement();
    OthersPickupPutdown();

    // move the dragons
    SyncDragons();
    
    // Move the bat
    RemoteAction* batAction = sync->GetNextBatAction();
    while ((batAction != NULL) && bat->exists()) {
        bat->handleAction(batAction, objectBall);
        delete batAction;
        batAction = sync->GetNextBatAction();
    }
    

    
    // Handle any remote changes to the portal.
    PortcullisStateAction* nextAction = sync->GetNextPortcullisAction();
    while (nextAction != NULL) {
        Portcullis* port = (Portcullis*)gameBoard->getObject(nextAction->portPkey);
        port->setState(nextAction->newState, nextAction->allowsEntry);
        delete nextAction;
        nextAction = sync->GetNextPortcullisAction();
    }

    // Do reset after dragon and move actions.
    PlayerResetAction* otherReset = sync->GetNextResetAction();
    while (otherReset != NULL) {
        ResetPlayer(gameBoard->getPlayer(otherReset->sender));
        delete otherReset;
        otherReset = sync->GetNextResetAction();
    }
    
    // Handle won games last.
    PlayerWinAction* lost = sync->GetGameWon();
    if (lost != NULL) {
        WinGame(lost->winInRoom);
        delete lost;
        lost = NULL;
    }


}


void Adventure_Run()
{
	sync->StartFrame();
    SyncWithOthers();
    
    // read the console switches every frame
    bool reset;
    Platform_ReadDifficultySwitches(&gameDifficultyLeft, &gameDifficultyRight);
    Platform_ReadConsoleSwitches(&reset);

	Dragon::setRunFromSword(gameDifficultyRight == DIFFICULTY_A);
    // If joystick is disabled and we hit the reset switch we don't treat it as a reset but as
    // a enable the joystick.  The next time you hit the reset switch it will work as a reset.
    if (joystickDisabled && switchReset && !reset) {
        joystickDisabled = false;
        switchReset = false;
    }
    
    // Reset switch
    if ((gameState != GAMESTATE_WIN) && switchReset && !reset)
    {
        if (gameState != GAMESTATE_GAMESELECT) {
            ResetPlayer(objectBall);
            // Broadcast to everyone else
            PlayerResetAction* action = new PlayerResetAction();
            sync->BroadcastAction(action);
            
        }
    }
    else
    {
        // Is the game active?
        if (gameState == GAMESTATE_GAMESELECT)
        {
            --timeToStartGame;
            if (timeToStartGame <= 0) {
				gameState = GAMESTATE_ACTIVE_1;
                ResetPlayers();
            } else {
                int displayNum = timeToStartGame / 60;
                board[OBJECT_NUMBER]->state = displayNum;

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
            // Has someone won the game.
            if (checkWonGame())
            {
                // Once we win the game it doesn't update the room, so make sure we're looking
                // at the inside of the castle
                displayedRoomIndex = objectBall->room;
                
                WinGame(objectBall->room);
                PlayerWinAction* won = new PlayerWinAction(objectBall->room);
                sync->BroadcastAction(won);
            }
            else
            {
                // Read joystick
                Platform_ReadJoystick(&joyLeft, &joyUp, &joyRight, &joyDown, &joyFire);

                if (gameState == GAMESTATE_ACTIVE_1)
                {
                    // Check ball collisions and move balls
                    ThisBallMovement();
                    for(int i=0; i<numPlayers; ++i) {
                        if (i != thisPlayer) {
                            BallMovement(gameBoard->getPlayer(i));
                        }
                    }

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

                    // Check ball collisions
                    if (!objectBall->hitX && !objectBall->hitY)
                    {
                        // Make sure stuff we are carrying stays out of our way
                        // TODO: Why are we checking collision with all objects if all
                        // we care about is the one we are carrying?
                        int hitObject = CollisionCheckBallWithAllObjects(objectBall);
                        if ((hitObject > OBJECT_NONE) && (hitObject == objectBall->linkedObject))
                        {
                            int diffX = objectBall->x - objectBall->previousX;
                            objectBall->linkedObjectX += diffX/2;

                            int diffY = objectBall->y - objectBall->previousY;
                            objectBall->linkedObjectY += diffY/2;
                            
                            // Adjusting how we hold an object is broadcast to other players as a pickup action
                            PlayerPickupAction* action = new PlayerPickupAction(hitObject,
                                objectBall->linkedObjectX, objectBall->linkedObjectY, OBJECT_NONE, 0, 0, 0);
                            sync->BroadcastAction(action);
                            
                        }
                    }
					for (int i = 0; i < numPlayers; ++i) {
                        ReactToCollision(gameBoard->getPlayer(i));
					}

                    // Increment the last object drawn
                    ++displayListIndex;

                    // deal with invisible surround moving
                    Surround();

                    // Move and deal with bat
                    if (bat->exists()) {
                        bat->moveOneTurn(sync, objectBall);
                    }

                    // Move and deal with portcullises
                    Portals();

                    // Display the room and objects
                    PrintDisplay();

                    ++gameState;
                }
                else if (gameState == GAMESTATE_ACTIVE_3)
                {
                    // Move and deal with the dragons
                    for(int dragonCtr=0; dragonCtr<numDragons; ++dragonCtr) {
                        Dragon* dragon = dragons[dragonCtr];
                        RemoteAction* dragonAction = dragon->move(&displayedRoomIndex);
                        if (dragonAction != NULL) {
                            sync->BroadcastAction(dragonAction);
                        }
                        // In gauntlet mode, getting eaten immediately triggers a reset.
                        if ((gameMode == GAME_MODE_GAUNTLET) && (dragon->state == Dragon::EATEN) && (dragon->eaten == objectBall)) {
                            ResetPlayer(objectBall);
                            // Broadcast to everyone else
                            PlayerResetAction* action = new PlayerResetAction();
                            sync->BroadcastAction(action);

                        }
                    }

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
            // We keep the display pointed at your current room while we make the
            // whole board flash, but once the flash is done, we point the display
            // at the winning castle.
            if (winFlashTimer > 0) {
                --winFlashTimer;
            } else {
                displayedRoomIndex = winningRoom;
            }

            // Display the room and objects
            PrintDisplay();

            // Go to game selection screen on select or reset button
            if (switchReset && !reset)
            {
                gameState = GAMESTATE_GAMESELECT;
            }
        }
    }

    switchReset = reset;
    AdvanceFlashColor();
}

void SetupRoomObjects()
{
    // Init all objects
    Board::ObjIter iter = gameBoard->getObjects();
    while (iter.hasNext()) {
        OBJECT* object = iter.next();
        object->movementX = 0;
        object->movementY = 0;
    }
    
    // Set to no carried objects
    for(int ctr=0; ctr<numDragons; ++ctr) {
        dragons[ctr]->eaten = NULL;
    }
    bat->linkedObject = OBJECT_NONE;

    // Read the object initialization table for the current game level
    const byte* p;
    if (gameMode == GAME_MODE_1) {
        p = (byte*)game1Objects;
    }
    else if (gameMode == GAME_MODE_GAUNTLET) {
        p = (byte*)gameGauntletObjects;
    } else {
        p = (byte*)game2Objects;
    }

    while ((*p) != 0xff)
    {
        byte object = *(p++);
        byte room = *(p++);
        byte xpos = *(p++);
        byte ypos = *(p++);
        byte state = *(p++);
        signed char movementX = *(p++);
        signed char movementY = *(p++);

        OBJECT* toInit = board[object];
        toInit->init(room, xpos, ypos);
        toInit->state = state;
        toInit->movementX = movementX;
        toInit->movementY = movementY;
    };
    
    // Hide the jade key if only 2 player
    if (numPlayers <= 2) {
        board[OBJECT_JADEKEY]->setExists(false);
        board[OBJECT_JADEKEY]->randomPlacement = OBJECT::FIXED_LOCATION;
    }

    // Put objects in random rooms for level 3.
    // Only first player does this and then broadcasts to other players.
    if ((gameMode == GAME_MODE_3) && (thisPlayer == 0))
    {
        randomizeRoomObjects();
    }
    
    // Open the gates if running the gauntlet
    if (gameMode == GAME_MODE_GAUNTLET) {
        Portcullis* blackPort = (Portcullis*)board[OBJECT_BLACK_PORT];
        blackPort->setState(Portcullis::OPEN_STATE, true);
        Portcullis* goldPort = (Portcullis*)board[OBJECT_YELLOW_PORT];
        goldPort->setState(Portcullis::OPEN_STATE, true);
    }
}

/**
 * Puts all the objects in random locations.
 * This follows a different algorithm than the original game.
 * We don't use the original algorithm because
 * 1) it had a vulnerability that the gold key could be in the black 
 * castle while the black key was in the gold castle
 * 2) with three times the number of home castles the algorithm was three
 * times more likely to be deadlocked
 */
void randomizeRoomObjects() {
    int numRooms = gameMap->getNumRooms();
    Portcullis* blackCastle = (Portcullis*)board[OBJECT_BLACK_PORT];
    Portcullis* whiteCastle = (Portcullis*)board[OBJECT_WHITE_PORT];
    
    // Run through all the objects in the game.  The ones that shouldn't be
    // randomized will have their random location flag turned off.
    int numObjects = gameBoard->getNumObjects();
    for(int objCtr=0; objCtr < numObjects; ++objCtr) {
        OBJECT* nextObj = gameBoard->getObject(objCtr);
        if (nextObj->randomPlacement != OBJECT::FIXED_LOCATION) {
            bool ok = false;
            while (!ok) {
                int randomKey = Sys::random() * numRooms;
                ROOM* randomRoom = gameMap->getRoom(randomKey);
                
                // Make sure the object isn't put in a hidden room
                ok = randomRoom->visibility != ROOM::HIDDEN;
                
                // if the object can only be in the open, make sure that it's put in the open.
                ok = ok && ((nextObj->randomPlacement != OBJECT::OUT_IN_OPEN) || (randomRoom->visibility == ROOM::OPEN));
                
                // Make sure chalice is in a castle
                if (ok && (objCtr == OBJECT_CHALISE)) {
                    ok = (blackCastle->containsRoom(randomKey) || whiteCastle->containsRoom(randomKey));
                }
                
                // Make sure white key not in white castle.
                if (ok && (objCtr == OBJECT_WHITEKEY)) {
                    ok = ok && !whiteCastle->containsRoom(randomKey);
                }

                // Make sure white and black key not cyclical
                // We happen to know that the white key is placed first, so set the black.
                if (ok && (objCtr == OBJECT_BLACKKEY)) {
                    if (blackCastle->containsRoom(board[OBJECT_WHITEKEY]->room)) {
                        ok = !whiteCastle->containsRoom(randomKey);
                    }
                    // Also make sure black key not in black castle
                    ok = ok && !blackCastle->containsRoom(randomKey);
                }
                
                // There are parts of the white castle not accessible without the bridge, but the bat
                // can get stuff out of there.  So make sure, if the black key is in the white castle
                // that the bat is not in the black castle.
                if (ok && (objCtr == OBJECT_BAT)) {
                    if (whiteCastle->containsRoom(board[OBJECT_BLACKKEY]->room)) {
                        ok = !blackCastle->containsRoom(randomKey);
                    }
                }
                
                if (ok) {
                    nextObj->room = randomKey;
                    MapSetupObjectAction* action = new MapSetupObjectAction(objCtr, randomKey, nextObj->x, nextObj->y);
                    sync->BroadcastAction(action);
                }
            }
        }
    }
    
    for(int objCtr=0; objCtr < numObjects; ++objCtr) {
        OBJECT* nextObj = gameBoard->getObject(objCtr);
        if (nextObj->randomPlacement != OBJECT::FIXED_LOCATION) {
            printf("%s placed in %s.\n", nextObj->label, gameMap->getRoom(nextObj->room)->label);
        }
    }
}

/**
 * If this was a randomized game, look for another game to define where the objects are placed.
 */
void handleSetupMessages() {
    MapSetupObjectAction* nextMsg = sync->GetNextSetupAction();
    while (nextMsg != NULL) {
        OBJECT* toSetup = board[nextMsg->object];
        toSetup->room = nextMsg->room;
        toSetup->x = nextMsg->x;
        toSetup->y = nextMsg->y;
        nextMsg = sync->GetNextSetupAction();
    }
}

float volumeAtDistance(int room) {
    int NEAR_VOLUME = MAX_VOLUME/3;
    int FAR_VOLUME = MAX_VOLUME/9;
    
    int distance = gameMap->distance(room, objectBall->room);

    float volume = 0.0;
    switch (distance) {
        case 0:
            volume = MAX_VOLUME;
            break;
        case 1:
            volume = NEAR_VOLUME;
            break;
        case 2:
            volume = FAR_VOLUME;
            break;
        default:
            volume = 0;
            break;
    }
    return volume;
}

/**
 * Returns true if this player has gotten the chalise to their home castle and won the game, or, if
 * this is the gauntlet, if the player has reached the gold castle.
 */
bool checkWonGame() {
    bool won = false;
    if (gameMode == GAME_MODE_GAUNTLET) {
        won = (objectBall->isGlowing() && (objectBall->room == objectBall->homeGate->insideRoom));
    } else {
        // Player MUST be holding the chalise to win (or holding the bat holding the chalise).
        // Another player can't win for you.
        if ((objectBall->linkedObject == OBJECT_CHALISE) ||
            ((objectBall->linkedObject == OBJECT_BAT) && (bat->linkedObject == OBJECT_CHALISE))) {
            // Player either has to bring the chalise into the castle or touch the chalise to the gate
            if (board[OBJECT_CHALISE]->room == objectBall->homeGate->insideRoom) {
                won = true;
            } else {
                if ((objectBall->room == objectBall->homeGate->room) &&
                    (objectBall->homeGate->state == Portcullis::OPEN_STATE) &&
                    gameBoard->CollisionCheckObjectObject(objectBall->homeGate, board[OBJECT_CHALISE])) {
                    
                    won = true;
                }
            }
        }
    }
    return won;
}

void WinGame(int winRoom) {
    // Go to won state
    gameState = GAMESTATE_WIN;
    winFlashTimer = 0xff;
    winningRoom = winRoom;
    
    // Play the sound
    Platform_MakeSound(SOUND_WON, MAX_VOLUME);
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
    // If we are scripting, we don't ever look at the joystick or change the velocity here.
    if (!joystickDisabled) {
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
    }
    
	BallMovement(objectBall);

	if (velocityChanged) {
        // TODO: Do we want to be constantly allocating space?
        PlayerMoveAction* moveAction = new PlayerMoveAction(objectBall->room, objectBall->x, objectBall->y, objectBall->velx, objectBall->vely);
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
            const ROOM* currentRoom = roomDefs[ball->room];
            ball->room = currentRoom->roomUp;
        }
        else if (ball->y < 0x0D*2)
        {
            // Handle the ball leaving a castle.
			bool leftCastle = false;
			for (int portalCtr = 0; !leftCastle && (portalCtr < numPorts); ++portalCtr) {
                Portcullis* port = ports[portalCtr];
				if (ball->room == port->insideRoom)
				{
					ball->x = 0xA0;
					ball->y = 0x2C * 2;

					ball->previousX = ball->x;
					ball->previousY = ball->y;

					ball->room = port->room;
                    // If we were locked in the castle, open the portcullis.
                    if (port->state == Portcullis::CLOSED_STATE) {
                        port->openFromInside();
                        PortcullisStateAction* gateAction = new PortcullisStateAction(port->getPKey(), port->state, port->allowsEntry);
                        sync->BroadcastAction(gateAction);

                    }
					leftCastle = true;
				}
			}

			if (!leftCastle)
            {
                // Just lookup the next room down and switch to that room
                // Wrap the ball to the top of the screen
                int newY = (ADVENTURE_SCREEN_HEIGHT + ADVENTURE_OVERSCAN);

                const ROOM* currentRoom = roomDefs[ball->room];
                int roomDown = currentRoom->roomDown;

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
        // TODO: Says we only care about non-carryable object but making no attempt to ignore
        // carryable objects.  Is this ok?
        int hitObject = CollisionCheckBallWithAllObjects(ball);
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
                const ROOM* currentRoom = roomDefs[ball->room];
                ball->room = currentRoom->roomRight;
            }
        }
        else if (ball->x < 4)
        {
            // Wrap the ball to the right side of the screen
            ball->x = ADVENTURE_SCREEN_WIDTH-5;

            // Set the new room
            const ROOM* currentRoom = roomDefs[ball->room];
            ball->room = currentRoom->roomLeft;
        }
        // Collision check the ball with the new Y coordinate against walls and objects
        // For collisions with objects, we only care about hitting non-carryable objects at this point
        int hitObject = CollisionCheckBallWithAllObjects(ball);
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
        // Unless we are scripting we ignore messages to move our own player
        if ((gameMode == GAME_MODE_SCRIPTING) || (i != thisPlayer)) {
            BALL* nextPayer = gameBoard->getPlayer(i);
            PlayerMoveAction* movement = sync->GetLatestBallSync(i);
            if (movement != 0x0) {
                nextPayer->room = movement->room;
                nextPayer->x = movement->posx;
                nextPayer->y = movement->posy;
                nextPayer->velx = movement->velx;
                nextPayer->vely = movement->vely;
            }
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
                dragon->eaten = gameBoard->getPlayer(nextState->sender);
                dragon->state = Dragon::EATEN;
                // Play the sound
                Platform_MakeSound(SOUND_EATEN, volumeAtDistance(dragon->room));
            } else if (nextState->newState == Dragon::DEAD) {
                // We ignore die actions if the dragon has already eaten somebody.
                if (dragon->state != Dragon::EATEN) {
                    dragon->state = Dragon::DEAD;
                    dragon->movementX = 0;
                    dragon->movementY = 0;
                    // Play the sound
                    Platform_MakeSound(SOUND_DRAGONDIE, volumeAtDistance(dragon->room));
                }
            }
            else if (nextState->newState == Dragon::ROAR) {
                // We ignore roar actions if we are already in an eaten state or dead state
                if ((dragon->state != Dragon::EATEN) && (dragon->state != Dragon::DEAD)) {
                    dragon->roar(nextState->posx, nextState->posy);
                    // Play the sound
                    Platform_MakeSound(SOUND_ROAR, volumeAtDistance(dragon->room));
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
                (objectBall->distanceTo(dragon->x, dragon->y) > nextMove->distance))) {
                
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
        BALL* nextBall = gameBoard->getPlayer(ctr);
        if (nextBall->linkedObject != OBJECT_NONE)
        {
            OBJECT* object = board[nextBall->linkedObject];
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
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        BALL* nextBall = gameBoard->getPlayer(ctr);
        // Handle balls going into the castles
        for(int portalCtr=0; portalCtr<numPorts; ++portalCtr) {
            Portcullis* nextPort = ports[portalCtr];
            if (nextBall->room == nextPort->room && nextPort->allowsEntry && CollisionCheckObject(nextPort, (nextBall->x-4), (nextBall->y-1), 8, 8))
            {
                nextBall->room = nextPort->insideRoom;
                nextBall->y = ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN-2;
                nextBall->previousY = nextBall->y;
                // make sure it stays unlocked in case we are walking in with the key
                nextPort->forceOpen();
                // Report to all the other players only if its the current player entering
                if (ctr == thisPlayer) {
                    PortcullisStateAction* gateAction =
                    new PortcullisStateAction(nextPort->getPKey(), nextPort->state, nextPort->allowsEntry);
                    sync->BroadcastAction(gateAction);
                    
                    // Report the ball entering the castle
                    PlayerMoveAction* moveAction = new PlayerMoveAction(objectBall->room, objectBall->x, objectBall->y, objectBall->velx, objectBall->vely);
                    sync->BroadcastAction(moveAction);
                }
                // If entering the black castle in the gauntlet, glow.
                if ((gameMode == GAME_MODE_GAUNTLET) && (nextPort == board[OBJECT_BLACK_PORT]) && !nextBall->isGlowing()) {
                    nextBall->setGlowing(true);
                    Platform_MakeSound(SOUND_GLOW, volumeAtDistance(nextBall->room));
                }
                break;
            }
        }
    }
    
    // Move any objects that need moving, and wrap objects from room to room
    Board::ObjIter iter = board.getMovableObjects();
    while (iter.hasNext()) {
        
        OBJECT* object = iter.next();

        // Apply movement
        object->x += object->movementX;
        object->y += object->movementY;

        // Check and Deal with Up
        if (object->y > 0x6A)
        {
            object->y = 0x0D;
            object->room = roomDefs[object->room]->roomUp;
        }

        // Check and Deal with Left
        if (object->x < 0x03)
        {
            object->x = 0x9A;
            object->room = roomDefs[object->room]->roomLeft;
        }

        // Check and Deal with Down
        if (object->y < 0x0D)
        {
            // Handle object leaving the castles
            bool leftCastle = false;
            for (int ctr=0; (ctr < numPorts) && (!leftCastle); ++ctr) {
                if (object->room == ports[ctr]->insideRoom)
                {
                    object->y = 0x5C;
                    object->room = ports[ctr]->room;
                    // TODO: Do we need to broadcast leaving the castle?  Seems there might be quite a jump.
                    leftCastle = true;
                }
            }
			if (!leftCastle)
            {
                object->y = 0x69;
                object->room = roomDefs[object->room]->roomDown;
            }
        }

        // Check and Deal with Right
        if (object->x > 0x9B)
        {
            object->x = 0x03;
            object->room = roomDefs[object->room]->roomRight;
        }

        // If the object has a linked object
        if ((object == bat) && (bat->linkedObject != OBJECT_NONE))
        {
            OBJECT* linkedObj = board[bat->linkedObject];
            linkedObj->x = object->x + bat->linkedObjectX;
            linkedObj->y = object->y + bat->linkedObjectY;
            linkedObj->room = object->room;
        }
    }
}

void PrintDisplay()
{
    // get the playfield data
    int displayedRoom = displayedRoomIndex;
    const ROOM* currentRoom = roomDefs[displayedRoom];
    const byte* roomData = currentRoom->graphicsData;

    // get the playfield color
    COLOR color = ((gameState == GAMESTATE_WIN) && (winFlashTimer > 0)) ? GetFlashColor() : colorTable[currentRoom->color];
    COLOR colorBackground = colorTable[COLOR_LTGRAY];

    // Fill the entire backbuffer with the playfield background color before we draw anything else
    Platform_PaintPixel(colorBackground.r, colorBackground.g, colorBackground.b, 0, 0, ADVENTURE_SCREEN_WIDTH, ADVENTURE_TOTAL_SCREEN_HEIGHT);

    // paint the surround under the playfield layer
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        if ((surrounds[ctr]->room == displayedRoom) && (surrounds[ctr]->state == 0)) {
            DrawObject(surrounds[ctr]);
        }
    }
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
    COLOR defaultColor = colorTable[roomDefs[displayedRoomIndex]->color];

	for (int i = 0; i < numPlayers; ++i) {
        BALL* player = gameBoard->getPlayer(i);
		if (objectBall->room == player->room) {
            COLOR ballColor = (player->isGlowing() ? GetFlashColor() : defaultColor);
			DrawBall(gameBoard->getPlayer(i), ballColor);
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
        BALL* actor = gameBoard->getPlayer(actorNum);
        if (action->dropObject != OBJECT_NONE) {
            printf("Received drop action for player %d who is carrying %d\n", actorNum, actor->linkedObject);
        }
        if ((action->dropObject != OBJECT_NONE) && (actor->linkedObject == action->dropObject)) {
            printf("Player %d dropped object %d\n", actorNum, action->dropObject);
            actor->linkedObject = OBJECT_NONE;
            OBJECT* dropped = board[action->dropObject];
            dropped->room = action->dropRoom;
            dropped->x = action->dropX;
            dropped->y = action->dropY;
            // Only play a sound if the drop isn't caused by picking up a different object.
            if (action->pickupObject == OBJECT_NONE) {
                Platform_MakeSound(SOUND_PUTDOWN, volumeAtDistance(actor->room));
            }
        }
        if (action->pickupObject != OBJECT_NONE) {
            actor->linkedObject = action->pickupObject;
            actor->linkedObjectX = action->pickupX;
            actor->linkedObjectY = action->pickupY;
            printf("Setting player %d to carrying %d\n", actorNum, actor->linkedObject);
            // If anybody else was carrying this object, take it away.
            for(int ctr=0; ctr<numPlayers; ++ctr) {
                if ((ctr != actorNum) && (gameBoard->getPlayer(ctr)->linkedObject==action->pickupObject)) {
                    printf("Player %d took object %d from player %d\n", action->sender, actor->linkedObject, thisPlayer);
                    gameBoard->getPlayer(ctr)->linkedObject = OBJECT_NONE;
                }
            }
            
            // If they are within hearing distance play the pickup sound
            Platform_MakeSound(SOUND_PICKUP, volumeAtDistance(actor->room));
        }
        delete action;
        action = sync->GetNextPickupAction();
    }
}

void PickupPutdown()
{
    if (!joystickDisabled && joyFire && (objectBall->linkedObject >= 0))
    {
        int dropped = objectBall->linkedObject;
        OBJECT* droppedObject = board[dropped];
        
        // Put down the current object!
        objectBall->linkedObject = OBJECT_NONE;
        
        // Tell other clients about the drop
        PlayerPickupAction* action = new PlayerPickupAction(OBJECT_NONE, 0, 0, dropped, droppedObject->room,
                                                           droppedObject->x, droppedObject->y);
        sync->BroadcastAction(action);

        // Play the sound
        Platform_MakeSound(SOUND_PUTDOWN, MAX_VOLUME);
    }
    else
    {
        // See if we are touching any carryable objects
        Board::ObjIter iter = gameBoard->getCarryableObjects();
        int hitIndex = CollisionCheckBallWithObjects(objectBall, iter);
        if (hitIndex > OBJECT_NONE)
        {
            // Ignore the object we are already carrying
            if (hitIndex == objectBall->linkedObject)
            {
                // Check the remainder of the objects
                hitIndex = CollisionCheckBallWithObjects(objectBall, iter);
            }

            if (hitIndex > OBJECT_NONE)
            {
                // Collect info about whether we are also dropping an object (for when we broadcast the action)
                PlayerPickupAction* action = new PlayerPickupAction(OBJECT_NONE, 0, 0, OBJECT_NONE, 0, 0, 0);
                int dropIndex = objectBall->linkedObject;
                if (dropIndex > OBJECT_NONE) {
                    OBJECT* dropped = board[dropIndex];
                    action->setDrop(dropIndex, dropped->room, dropped->x, dropped->y);
                }
                
                // If the bat is holding the object we do some of the pickup things but not all.
                // We drop our current object and play the pickup sound, but we don't actually
                // pick up the object.
                // NOTE: Discrepancy here between C++ port behavior and original Atari behavior so
                // not totally sure what should be done.  As a guess, we just set linkedObject to none and
                // play the sound.
                if (bat->exists() && (bat->linkedObject == hitIndex)) {
                    if (dropIndex > OBJECT_NONE) {
                        // Drop our current object and broadcast it
                        objectBall->linkedObject = OBJECT_NONE;
                        sync->BroadcastAction(action);
                    } else {
                        // Don't need the action.
                        delete action;
                    }
                } else {
                
                    // Pick up this object!
                    objectBall->linkedObject = hitIndex;
                    
                    // calculate the XY offsets from the ball's position
                    objectBall->linkedObjectX = board[hitIndex]->x - (objectBall->x/2);
                    objectBall->linkedObjectY = board[hitIndex]->y - (objectBall->y/2);
                    
                    // Take it away from anyone else if they were holding it.
                    for(int ctr=0; ctr<numPlayers; ++ctr) {
                        if ((ctr != thisPlayer) && (gameBoard->getPlayer(ctr)->linkedObject == hitIndex)) {
                            gameBoard->getPlayer(ctr)->linkedObject = OBJECT_NONE;
                        }
                    }
                    
                    // Broadcast that we picked up an object
                    action->setPickup(hitIndex, objectBall->linkedObjectX, objectBall->linkedObjectY);
                    sync->BroadcastAction(action);

                }
                
                // Play the sound
                Platform_MakeSound(SOUND_PICKUP, MAX_VOLUME);
            }
        }
    }
}

void Surround()
{
    // get the playfield data
    int roomNum = objectBall->room;
    const ROOM* currentRoom = roomDefs[roomNum];
    if (currentRoom->color == COLOR_LTGRAY)
    {
        for(int ctr=0; ctr<numPlayers; ++ctr) {
            BALL* nextBall = gameBoard->getPlayer(ctr);
            if (nextBall->room == roomNum) {
                // Put it in the same room as the ball (player) and center it under the ball
                surrounds[ctr]->room = roomNum;
                surrounds[ctr]->x = (nextBall->x-0x1E)/2;
                surrounds[ctr]->y = (nextBall->y+0x18)/2;
            } else {
                surrounds[ctr]->room = -1;
            }
        }
    }
    else
    {
        for(int ctr=0; ctr<numPlayers; ++ctr) {
            surrounds[ctr]->room = -1;
        }
    }
}

void Portals()
{
    // Handle all the local actions of portals
    for(int portalCtr=0; portalCtr<numPorts; ++portalCtr) {
        Portcullis* port = ports[portalCtr];
        
        // Someone has to be in the room for the key to trigger the gate
        bool seen = false;
        for(int ctr=0; !seen && ctr<numPlayers; ++ctr) {
            seen = (gameBoard->getPlayer(ctr)->room == port->room);
        }
        if (seen) {
            PortcullisStateAction* gateAction = port->checkInteraction();
            if (gateAction != NULL) {
                // If we are in the same room as the portcullis, broadcast any state change
                if (objectBall->room == port->room) {
                    sync->BroadcastAction(gateAction);
                } else {
                    printf("Not broadcasting.  Player in %s.  Gate in %s.\n", gameMap->getRoom(objectBall->room)->label,
                           gameMap->getRoom(port->room)->label);
                    delete gateAction;
                }
            }
        }
        
        // Raise/lower the port
        port->moveOneTurn();
        if (port->allowsEntry)
        {
            // Port is unlocked
            roomDefs[port->insideRoom]->roomDown = port->room;
        }
        else
        {
            // Port is locked
            roomDefs[port->insideRoom]->roomDown = port->insideRoom;
        }

    }
    
}



void Magnet()
{
    const OBJECT* magnet = board[OBJECT_MAGNET];
    
    int i=0;
    while (magnetMatrix[i])
    {
        // Look for items in the magnet matrix that are in the same room as the magnet
        OBJECT* object = board[magnetMatrix[i]];
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

void DrawObjects(int room)
{
    // Clear out the display list
	int displayList[MAX_OBJECTS];
    for (int i=0; i < MAX_OBJECTS; i++)
        displayList[i] = OBJECT_NONE;


    // Create a list of all the objects that want to be drawn
    int numAdded = 0;

    for(int ctr=0; ctr<numPlayers; ++ctr) {
        if (surrounds[ctr]->room == room) {
            displayList[numAdded++] = OBJECT_SURROUND-ctr;
        }
    }

    int colorFirst = -1;
    int colorLast = -1;

    Board::ObjIter iter = board.getObjects();
    while (iter.hasNext()) {
        OBJECT* toDisplay = iter.next();
        // Init it to not displayed
        toDisplay->displayed = false;
        if (toDisplay->room == room)
        {
            // This object is in the current room - add it to the list
            displayList[numAdded++] = toDisplay->getPKey();

            if (colorFirst < 0) colorFirst = toDisplay->color;
            colorLast = toDisplay->color;
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

    for(int ctr=0; ctr<numPlayers; ++ctr) {
        surrounds[ctr]->displayed = false;
    }
    
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
                OBJECT* toDraw = board[displayList[i]];
                DrawObject(toDraw);
                toDraw->displayed = true;
                colorLast = toDraw->color;
            }
            else if (displayList[i] <= OBJECT_SURROUND)
            {
                surrounds[OBJECT_SURROUND - displayList[i]]->displayed = true;
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
                OBJECT* toDraw = board[displayList[i]];
                toDraw->displayed = true;
                colorLast = toDraw->color;
            }
            else if (displayList[i] <= OBJECT_SURROUND)
            {
                surrounds[OBJECT_SURROUND - displayList[i]]->displayed = true;
            }

            // wrap to the beginning of the list if we've reached the end
            ++i;
            if (i > MAX_OBJECTS)
                i = 0;
            else if (displayList[i] == OBJECT_NONE)
                i = 0;
        }

        // Now just paint everything in this room so we bypass the flicker if desired
        Board::ObjIter iter = board.getObjects();
        while(iter.hasNext()) {
            OBJECT* next = iter.next();
            if (next->room == room)
                DrawObject(next);
        }
    }

    if (roomDefs[room]->flags & ROOMFLAG_LEFTTHINWALL)
    {
        // Position missile 00 to 0D,00 - left thin wall
        COLOR color = colorTable[(colorFirst > 0) ? colorFirst : COLOR_BLACK];
        Platform_PaintPixel(color.r,color.g,color.b, 0x0D*2, 0x00*2, 4, ADVENTURE_TOTAL_SCREEN_HEIGHT);
    }
    if (roomDefs[room]->flags & ROOMFLAG_RIGHTTHINWALL)
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
    const ROOM* currentRoom = roomDefs[room];
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
        if (board[OBJECT_DOT]->room != room)
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
                if (Board::HitTestRects(x-4,(y-4),8,8,cx*cell_width,(ypos*cell_height), cell_width, cell_height))
                {
                    hitWall = true;
                    break;
                }

                if (mirror)
                {
                    if (Board::HitTestRects(x-4,(y-4),8,8,(cx+20)*cell_width,(ypos*cell_height), cell_width, cell_height))
                    {
                        hitWall = true;
                        break;
                    }
                }
                else
                {
                    if (Board::HitTestRects(x-4,(y-4),8,8,((40-(cx+1))*cell_width),(ypos*cell_height), cell_width, cell_height))
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
    const OBJECT* bridge = board[OBJECT_BRIDGE];
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

static int CollisionCheckBallWithAllObjects(BALL* ball) {
    Board::ObjIter iter = gameBoard->getObjects();
    return CollisionCheckBallWithObjects(ball, iter);
}

static int CollisionCheckBallWithObjects(BALL* ball, Board::ObjIter& iter)
{
    // Go through all the objects
    while(iter.hasNext())
    {
        // If this object is in the current room and can be picked up, check it against the ball
        const OBJECT* object = iter.next();
        if (object->displayed && object->isTangibleTo(thisPlayer) && (ball->room == object->room))
        {
            if (CollisionCheckObject(object, ball->x-4,(ball->y-1), 8, 8))
            {
                // return the index of the object
                return object->getPKey();
            }
        }
    }

    return OBJECT_NONE;
}

// Checks an object for collision against the specified rectangle
// On the 2600 this is done in hardware by the Player/Missile collision registers
bool CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height)
{
    return gameBoard->CollisionCheckObject(object, x, y, width, height);
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

