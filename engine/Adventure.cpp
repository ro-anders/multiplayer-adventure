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

#include <stdlib.h>
#include <stdio.h>
#include <iostream>
#include "sys.h"


#include "Adventure.h"

#include "adventure_sys.h"
#include "color.h"
#include "Ball.hpp"
#include "Board.hpp"
#include "Bat.hpp"
#include "Dragon.hpp"
#include "GameObject.hpp"
#include "Logger.hpp"
#include "Map.hpp"
#include "Portcullis.hpp"
#include "Robot.hpp"
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
void ReactToCollisionX(BALL* ball);
void ReactToCollisionY(BALL* ball);
static void BallMovement(BALL* ball);
static void ThisBallMovement();
static void OtherBallMovement();
static void moveBallIntoCastle();
static void MoveCarriedObjects();
static void MoveGroundObject();
static void PrintDisplay();
static void PickupPutdown();
static void OthersPickupPutdown();
static void Surround();
static void Portals();
static void SyncDragons();
static void Magnet();

// My helper functions
void addAllRoomsToPort(Portcullis* port, int firstRoom, int lastRoom);
void checkPlayers();
bool checkWonGame();
static void DrawObjects(int room);
static void DrawObject(const OBJECT* object);
void DrawBall(const BALL* ball, COLOR color);
static bool CrossingBridge(int room, int x, int y, BALL* ball);
static bool CollisionCheckBallWithWalls(int room, int x, int y);
static int CollisionCheckBallWithAllObjects(BALL* ball);
static int CollisionCheckBallWithObjects(BALL* ball, Board::ObjIter& iter);
static bool CollisionCheckBallWithObject(BALL* ball, const OBJECT* object);
static bool CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height);
static bool CollisionCheckBallWithEverything(BALL* ball, int room, int x, int y, bool allowBridge, int* hitObject);
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
static bool switchReset = false;

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

static int gameMapLayout = 0;                               // The board setup.  Level 1 = 0, Levels 2 & 3 = 1, Gauntlet = 2

/** There are five game modes, the original three (but zero justified so game mode 0 means original level 1) and
 * a new fourth, gameMode 3, which I call The Gauntlet. The fifth is used for generating videos and plays a preplanned script. */
static int gameMode = 0;
static bool joystickDisabled = false;

#define GAMEOPTION_PRIVATE_MAGNETS  1
#define GAMEOPTION_UNLOCK_GATES_FROM_INSIDE 2
#define GAMEOPTION_NO_HIDE_KEY_IN_CASTLE 3
// This holds all the switches for whether to turn on or off different game options
// It is a bitwise or of each game option
static int gameOptions = GAMEOPTION_NO_HIDE_KEY_IN_CASTLE;

static int winFlashTimer=0;
static int winningRoom=-1; // The room number of the castle of the winning player.  -1 if the game is not won yet.
static int displayWinningRoom = false; // At the end of the game we show the player who won.

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
    OBJECT_YELLOW_PORT, GOLD_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
    OBJECT_COPPER_PORT, COPPER_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 4
    OBJECT_JADE_PORT, JADE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 5
    OBJECT_WHITE_PORT, WHITE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 2
    OBJECT_BLACK_PORT, BLACK_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_NAME, ROBINETT_ROOM, 0x50, 0x69, 0x00, 0x00, 0x00, // Robinett message
    OBJECT_NUMBER, NUMBER_ROOM, 0x50, 0x40, 0x00, 0x00, 0x00, // Starting number
    OBJECT_YELLOWDRAGON, MAIN_HALL_LEFT, 0x50, 0x20, 0x00, 0x00, 0x00, // Yellow Dragon
    OBJECT_GREENDRAGON, SOUTHEAST_ROOM, 0x50, 0x20, 0x00, 0x00, 0x00, // Green Dragon
    OBJECT_SWORD, GOLD_FOYER, 0x20, 0x20, 0x00, 0x00, 0x00, // Sword
    OBJECT_BRIDGE, BLUE_MAZE_5, 0x2A, 0x37, 0x00, 0x00, 0x00, // Bridge
    OBJECT_YELLOWKEY, GOLD_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00, // Yellow Key
    OBJECT_COPPERKEY, COPPER_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00, // Copper Key
    OBJECT_JADEKEY, JADE_CASTLE, 0x20, 0x41, 0x00, 0x00, 0x00, // Jade Key
    OBJECT_BLACKKEY, SOUTHEAST_ROOM, 0x20, 0x40, 0x00, 0x00, 0x00, // Black Key
    OBJECT_CHALISE, BLACK_INNERMOST_ROOM, 0x30, 0x20, 0x00, 0x00, 0x00, // Challise
    OBJECT_MAGNET, BLACK_FOYER, 0x80, 0x20, 0x00, 0x00, 0x00, // Magnet
    0xff,0,0,0,0,0,0
};

// Object locations (room and coordinate) for Games 02 and 03
//        - object, room, x, y, state, movement(x/y)
static const byte game2Objects [] =
{
    OBJECT_YELLOW_PORT, GOLD_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
    OBJECT_COPPER_PORT, COPPER_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 4
    OBJECT_JADE_PORT, JADE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 5
    OBJECT_WHITE_PORT, WHITE_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 2
    OBJECT_BLACK_PORT, BLACK_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_NAME, ROBINETT_ROOM, 0x50, 0x69, 0x00, 0x00, 0x00, // Robinett message
    OBJECT_NUMBER, NUMBER_ROOM, 0x50, 0x40, 0x00, 0x00, 0x00, // Starting number
    OBJECT_REDDRAGON, BLACK_MAZE_2, 0x50, 0x20, 0x00, 3, 3, // Red Dragon
    OBJECT_YELLOWDRAGON, RED_MAZE_4, 0x50, 0x20, 0x00, 3, 3, // Yellow Dragon
    OBJECT_GREENDRAGON, BLUE_MAZE_3, 0x50, 0x20, 0x00, 3, 3, // Green Dragon
    OBJECT_SWORD, GOLD_CASTLE, 0x20, 0x20, 0x00, 0x00, 0x00, // Sword
    OBJECT_BRIDGE, WHITE_MAZE_3, 0x40, 0x40, 0x00, 0x00, 0x00, // Bridge
    OBJECT_YELLOWKEY, WHITE_MAZE_2, 0x20, 0x40, 0x00, 0x00, 0x00, // Yellow Key
    OBJECT_COPPERKEY, WHITE_MAZE_2, 0x7a, 0x40, 0x00, 0x00, 0x00, // Copper Key
    OBJECT_JADEKEY, BLUE_MAZE_4, 0x7a, 0x40, 0x00, 0x00, 0x00, // Jade Key
    OBJECT_WHITEKEY, BLUE_MAZE_3, 0x20, 0x40, 0x00, 0x00, 0x00, // White Key
    OBJECT_BLACKKEY, RED_MAZE_4, 0x20, 0x40, 0x00, 0x00, 0x00, // Black Key
    OBJECT_BAT, MAIN_HALL_CENTER, 0x20, 0x20, 0x00, 0, -3, // Bat
    OBJECT_DOT, BLACK_MAZE_3, 0x45, 0x12, 0x00, 0x00, 0x00, // Dot
    OBJECT_CHALISE, BLACK_MAZE_2, 0x30, 0x20, 0x00, 0x00, 0x00, // Challise
    OBJECT_MAGNET, SOUTHWEST_ROOM, 0x80, 0x20, 0x00, 0x00, 0x00, // Magnet
    0xff,0,0,0,0,0,0
};

// Object locations (room and coordinate) for game 01
//        - object, room, x, y, state, movement(x/y)
static const byte gameGauntletObjects [] =
{
    OBJECT_YELLOW_PORT, GOLD_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 1
    OBJECT_BLACK_PORT, BLACK_CASTLE, 0x4d, 0x31, 0x0C, 0x00, 0x00, // Port 3
    OBJECT_NAME, ROBINETT_ROOM, 0x50, 0x69, 0x00, 0x00, 0x00, // Robinett message
    OBJECT_NUMBER, NUMBER_ROOM, 0x50, 0x40, 0x00, 0x00, 0x00, // Starting number
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
    
    Dragon::Difficulty difficulty = (gameMode == GAME_MODE_1 ?
                                     (initialLeftDiff == DIFFICULTY_B ?  Dragon::TRIVIAL : Dragon::EASY) :
                                     (initialLeftDiff == DIFFICULTY_B ? Dragon::MODERATE : Dragon::HARD));
    Dragon::setRunFromSword(initialRightDiff == DIFFICULTY_A);

    if (gameMode == GAME_MODE_SCRIPTING) difficulty = Dragon::EASY;
    Dragon::setDifficulty(difficulty);
    dragons = new Dragon*[numDragons];
    dragons[0] = new Dragon("grindle", 0, COLOR_LIMEGREEN, 2, greenDragonMatrix);
    dragons[1] = new Dragon( "yorgle", 1, COLOR_YELLOW, 2, yellowDragonMatrix);
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
    number->init(NUMBER_ROOM, 0x50, 0x40);

    // Setup the rest of the objects
    gameBoard->addObject(OBJECT_YELLOW_PORT, ports[0]);
    gameBoard->addObject(OBJECT_COPPER_PORT, ports[3]);
    gameBoard->addObject(OBJECT_JADE_PORT, ports[4]);
    gameBoard->addObject(OBJECT_WHITE_PORT, ports[1]);
    gameBoard->addObject(OBJECT_BLACK_PORT, ports[2]);
    gameBoard->addObject(OBJECT_NAME, new OBJECT("easter egg message", objectGfxAuthor, 0, 0, COLOR_FLASH, OBJECT::FIXED_LOCATION));
    gameBoard->addObject(OBJECT_REDDRAGON, dragons[2]);
    gameBoard->addObject(OBJECT_YELLOWDRAGON,dragons[1]);
    gameBoard->addObject(OBJECT_GREENDRAGON, dragons[0]);
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
    
    Logger::log() << "Player " << thisPlayer << " starting game at " << Sys::datetime() << "." << Logger::EOM;
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
    ball->previousRoom = ball->room;
    ball->displayedRoom = ball->room;
    ball->x = 0x50*2;                  //
    ball->y = 0x20*2;                  //
    ball->previousX = ball->x;
    ball->previousY = ball->y;
    ball->linkedObject = OBJECT_NONE;  // Not carrying anything
    ball->setGlowing(false);
    
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

void Adventure_CheckTime(float currentScale) {
    const int FRAMES_PER_SLOT = 60;
    const long TARGET_SLOT_TIME = (long)(FRAMES_PER_SLOT * ADVENTURE_FRAME_PERIOD * 1000);
    const int MAX_SLOTS_MISSED = 5;
    static float lastScale = 0;
    static bool haveWarnedAboutThisScale = false;
    static int framesIntoSlot = 0;
    static long timeAtStartOfSlot = Sys::runTime();
    static int numSlotsMissed = 0;
    
    if (currentScale != lastScale) {
        // Scale changed.  Reset timing history.
        haveWarnedAboutThisScale = false;
        framesIntoSlot = 0;
        timeAtStartOfSlot = Sys::runTime();
        numSlotsMissed = 0;
        lastScale = currentScale;
    } else if (!haveWarnedAboutThisScale) {
        ++framesIntoSlot;
        if (framesIntoSlot >= FRAMES_PER_SLOT) {
            long currentTime = Sys::runTime();
            long elapsed = currentTime - timeAtStartOfSlot;
            if (elapsed > TARGET_SLOT_TIME * 1.1) { // We allow for 10% slowness.
                ++numSlotsMissed;
                if (numSlotsMissed > MAX_SLOTS_MISSED) {
                    Platform_DisplayStatus("Your game is running too slow.\nConsider shrinking the window size.", 4);
                    haveWarnedAboutThisScale = true;
                }
            } else {
                numSlotsMissed = 0;
            }
            framesIntoSlot = 0;
            timeAtStartOfSlot = currentTime;
        }
    }
}


void Adventure_Run()
{
	sync->StartFrame();
    SyncWithOthers();
    checkPlayers();
    
    // read the console switches every frame
    bool reset = false;
    Platform_ReadConsoleSwitches(&reset);
    if (Robot::isOn()) {
        Robot::ControlConsoleSwitches(&reset, dragons, numDragons, objectBall);
    }
    
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
                objectBall->room = 0;
                objectBall->previousRoom = 0;
                objectBall->displayedRoom = 0;
                objectBall->x = 0;
                objectBall->y = 0;
                objectBall->previousX = 0;
                objectBall->previousY = 0;
                PrintDisplay();
            }
        }
        else if (ISGAMEACTIVE())
        {
            // Has someone won the game.
            if (checkWonGame())
            {
                WinGame(objectBall->room);
                PlayerWinAction* won = new PlayerWinAction(objectBall->room);
                sync->BroadcastAction(won);
            }
            else
            {
                // Read joystick
                Platform_ReadJoystick(&joyLeft, &joyUp, &joyRight, &joyDown, &joyFire);
                if (Robot::isOn()) {
                    Robot::ControlJoystick(&joyLeft, &joyUp, &joyRight, &joyDown, &joyFire);
                }

                if (gameState == GAMESTATE_ACTIVE_1)
                {
                    // Move balls
                    ThisBallMovement();
                    for(int i=0; i<numPlayers; ++i) {
                        if (i != thisPlayer) {
                            BallMovement(gameBoard->getPlayer(i));
                        }
                    }

                    // Move the carried object
                    MoveCarriedObjects();

					// Collision check the balls in their new coordinates against walls and objects
					for (int i = 0; i < numPlayers; ++i) {
						BALL* nextBall = gameBoard->getPlayer(i);
						nextBall->hit = CollisionCheckBallWithEverything(nextBall, nextBall->room, nextBall->x, nextBall->y, false, &nextBall->hitObject);
					}

                    // Setup the room and object
                    PrintDisplay();

                    ++gameState;
                }
                else if (gameState == GAMESTATE_ACTIVE_2)
                {
                    // Deal with object pickup and putdown
                    PickupPutdown();

					for (int i = 0; i < numPlayers; ++i) {
                        ReactToCollisionX(gameBoard->getPlayer(i));
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
                        RemoteAction* dragonAction = dragon->move();
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
                    
                    for (int i = 0; i < numPlayers; ++i) {
                        ReactToCollisionY(gameBoard->getPlayer(i));
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
                displayWinningRoom = true;
            }

            // Increment the last object drawn
            if (sync->getFrameNumber() % 3 == 0) {
                ++displayListIndex;
            }

            // Display the room and objects
            PrintDisplay();
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
        object->setMovementX(0);
        object->setMovementY(0);
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
        toInit->init(room, xpos, ypos, state, movementX, movementY);
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
	// TODO: Bug in windows requires we reseed now.
	Sys::randomized = false;
	for(int objCtr=0; objCtr < numObjects; ++objCtr) {
        OBJECT* nextObj = gameBoard->getObject(objCtr);
        if (nextObj->randomPlacement != OBJECT::FIXED_LOCATION) {
            bool ok = false;
            while (!ok) {
                int randomKey = (int)(Sys::random() * numRooms);
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
                    ObjectMoveAction* action = new ObjectMoveAction(objCtr, randomKey, nextObj->x, nextObj->y);
                    sync->BroadcastAction(action);
                }
            }
        }
    }
    
    // Print out where everything was randomized to
    // Only for debugging - keep commented out
#if 0
    for(int objCtr=0; objCtr < numObjects; ++objCtr) {
        OBJECT* nextObj = gameBoard->getObject(objCtr);
        if (nextObj->randomPlacement != OBJECT::FIXED_LOCATION) {
            printf("%s placed in %s.\n", nextObj->label, gameMap->getRoom(nextObj->room)->label);
        }
    }
#endif
    
}

/**
 * If this was a randomized game, look for another game to define where the objects are placed.
 */
void handleSetupMessages() {
    ObjectMoveAction* nextMsg = sync->GetNextSetupAction();
    while (nextMsg != NULL) {
        OBJECT* toSetup = board[nextMsg->object];
        toSetup->room = nextMsg->room;
        toSetup->x = nextMsg->x;
        toSetup->y = nextMsg->y;
        nextMsg = sync->GetNextSetupAction();
    }
}

float volumeAtDistance(int room) {
    float NEAR_VOLUME = MAX_VOLUME/3;
    float FAR_VOLUME = MAX_VOLUME/9;
    
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
    
    // Report back to the server.
    Platform_ReportToServer("Has won a game");

}

void ReactToCollisionX(BALL* ball) {
	if (ball->hit) {
        if (ball->velx != 0) {
            if ((ball->hitObject > OBJECT_NONE) && (ball->hitObject == ball->linkedObject)) {
                ball->linkedObjectX += ball->velx;
                if (ball == objectBall) {
                    // If this is adjusting how the current player holds an object,
                    // we broadcast to other players as a pickup action
                    PlayerPickupAction* action = new PlayerPickupAction(ball->hitObject,
                        objectBall->linkedObjectX, objectBall->linkedObjectY, OBJECT_NONE, 0, 0, 0);
                    sync->BroadcastAction(action);
                }
            }
            
            if ((ball->room != ball->previousRoom) && (ABS(ball->x - ball->previousX) > ABS(ball->velx))) {
                // We switched rooms, kick them back
                ball->room = ball->previousRoom;
            }
            ball->x = ball->previousX;
        }
        // Try recompute hit allowing for the bridge.
        ball->hit = CollisionCheckBallWithEverything(ball, ball->room, ball->x, ball->y, true, &ball->hitObject);
	}
}

void ReactToCollisionY(BALL* ball) {
	if ((ball->hit) && (ball->vely != 0))
	{
		if ((ball->hitObject > OBJECT_NONE) && (ball->hitObject == ball->linkedObject))
		{
			ball->linkedObjectY += ball->vely;
            if (ball == objectBall) {
                // If this is adjusting how the current player holds an object,
                // we broadcast to other players as a pickup action
                PlayerPickupAction* action = new PlayerPickupAction(ball->hitObject,
                    objectBall->linkedObjectX, objectBall->linkedObjectY, OBJECT_NONE, 0, 0, 0);
                sync->BroadcastAction(action);
            }
		}

        // We put y back to the last y, but if we are moving diagonally, we
        // put x back to the new x value which we had reverted last phase and try again.
        // if new x and old y is still a collision we revert at the beginning of the next phase
        if ((ball->room != ball->previousRoom) && (ABS(ball->y - ball->previousY) > ABS(ball->vely))) {
            // We switched rooms, kick them back
            ball->room = ball->previousRoom;
        }
        ball->y = ball->previousY;
        ball->x += ball->velx;
        // Need to check if new X takes us to new room (again)
        if (ball->x >= RIGHT_EDGE) {
            ball->x = ENTER_AT_LEFT;
            ball->room = ball->displayedRoom; // The displayed room hasn't changed
        } else if (ball->x < LEFT_EDGE) {
            ball->x = ENTER_AT_RIGHT;
            ball->room = ball->displayedRoom;
        }

        ball->hit = CollisionCheckBallWithEverything(ball, ball->displayedRoom, ball->x, ball->y, false, &ball->hitObject);
	}
}

void ThisBallMovement()
{
	// Read the joystick and translate into a velocity
    int prevVelX = objectBall->velx;
    int prevVelY = objectBall->vely;
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
        objectBall->velx = newVelX;
    }
    
	BallMovement(objectBall);

	if (!joystickDisabled && ((objectBall->velx != prevVelX) || (objectBall->vely != prevVelY))) {
        // TODO: Do we want to be constantly allocating space?
        PlayerMoveAction* moveAction = new PlayerMoveAction(objectBall->room, objectBall->x, objectBall->y, objectBall->velx, objectBall->vely);
        sync->BroadcastAction(moveAction);
	}
}

void BallMovement(BALL* ball) {

    bool eaten = false;
    for(int ctr=0; ctr<numDragons && !eaten; ++ctr) {
        eaten = (dragons[ctr]->eaten == ball);
    }

    // Save the last, non-colliding position
    if (ball->hit) {
        ball->x = ball->previousX;
        ball->y = ball->previousY;
        ball->room = ball->previousRoom;
    } else {
        ball->previousX = ball->x;
        ball->previousY = ball->y;
        ball->previousRoom = ball->room;
    }
    
    ball->hit = eaten;
    ball->hitObject = OBJECT_NONE;

    // Move the ball
    ball->x += ball->velx;
	ball->y += ball->vely;
    

    // Wrap rooms in Y if necessary
    if (ball->y > TOP_EDGE)
    {
        ball->y = ENTER_AT_BOTTOM;
        ball->room = roomDefs[ball->room]->roomUp;
    }
    else if (ball->y < BOTTOM_EDGE)
    {
        // Handle the ball leaving a castle.
		bool canUnlockFromInside = ((gameOptions & GAMEOPTION_UNLOCK_GATES_FROM_INSIDE) != 0);
        bool leftCastle = false;
        for (int portalCtr = 0; !leftCastle && (portalCtr < numPorts); ++portalCtr) {
            Portcullis* port = ports[portalCtr];
            if ((ball->room == port->insideRoom) &&
                ((port->state != Portcullis::CLOSED_STATE) || canUnlockFromInside))
            {
                ball->x = Portcullis::EXIT_X;
                ball->y = Portcullis::EXIT_Y;
                
                ball->previousX = ball->x;
                ball->previousY = ball->y;
                
                ball->room = port->room;
                ball->previousRoom = ball->room;
                // If we were locked in the castle, open the portcullis.
                if (port->state == Portcullis::CLOSED_STATE && canUnlockFromInside) {
                    port->openFromInside();
                    PortcullisStateAction* gateAction = new PortcullisStateAction(port->getPKey(), port->state, port->allowsEntry);
                    sync->BroadcastAction(gateAction);
                    
                }
                leftCastle = true;
            }
        }
        
        if (!leftCastle)
        {
            // Wrap the ball to the top of the next screen
            ball->y = ENTER_AT_TOP;
            ball->room = roomDefs[ball->room]->roomDown;
        }
    }
    
    if (ball->x >= RIGHT_EDGE) {
        // Can't diagonally switch rooms.  If trying, only allow changing rooms vertically
        if (ball->room != ball->previousRoom) {
            ball->x = ball->previousX;
            ball->velx = 0;
        } else {
            // Wrap the ball to the left side of the next screen
            ball->x = ENTER_AT_LEFT;

            // Figure out the room to the right (which might be the secret room)
            ball->room = (ball->room == MAIN_HALL_RIGHT ? ROBINETT_ROOM :
                             roomDefs[ball->room]->roomRight);
        }
    } else if (ball->x < LEFT_EDGE) {
        // Can't diagonally switch rooms.  If trying, only allow changing rooms vertically
        if (ball->room != ball->previousRoom) {
            ball->x = ball->previousX;
            ball->velx = 0;
        } else {
            ball->x = ENTER_AT_RIGHT;
            ball->room = roomDefs[ball->room]->roomLeft;
        }
    }
    
    ball->displayedRoom = ball->room;

}

// Check if the ball would be hitting anything (wall, object, ...)
// ball - the ball to check
// room - the room in which to check
// x - the x position to check
// y - the y position to check
// allowBridge - if moving vertically, the bridge can allow you to not collide into a wall
// hitObject - if we hit an object, will set this reference to the object we hit.  If NULL, will not try to set it.
//
static bool CollisionCheckBallWithEverything(BALL* ball, int checkRoom, int checkX, int checkY, bool allowBridge, int* hitObjectOut) {
    int hitObject = CollisionCheckBallWithAllObjects(ball);
    bool hitWall = false;
    if (hitObject == OBJECT_NONE) {
        bool crossingBridge = allowBridge && CrossingBridge(checkRoom, checkX, checkY, ball);
        hitWall = !crossingBridge && CollisionCheckBallWithWalls(checkRoom, checkX, checkY);
    }
    if (hitObjectOut != NULL) {
        *hitObjectOut = hitObject;
    }
    return hitWall || (hitObject > OBJECT_NONE);
}


void OtherBallMovement() {
	for (int i = 0; i < numPlayers; ++i) {
        // Unless we are scripting we ignore messages to move our own player
        if ((gameMode == GAME_MODE_SCRIPTING) || (i != thisPlayer)) {
            BALL* nextPayer = gameBoard->getPlayer(i);
            PlayerMoveAction* movement = sync->GetLatestBallSync(i);
            if (movement != 0x0) {
                nextPayer->room = movement->room;
                nextPayer->previousRoom = movement->room;
                nextPayer->displayedRoom = movement->room;
                nextPayer->x = movement->posx;
                nextPayer->previousX = movement->posx-movement->velx;
                nextPayer->y = movement->posy;
                nextPayer->previousY = movement->posy-movement->vely;
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
            // If something causes a sound, we need to know how far away it is.
            float volume = volumeAtDistance(dragon->room);
            dragon->syncAction(nextState, volume);
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
                
                    dragon->syncAction(nextMove);
            }
        }
        delete next;
        next = sync->GetNextDragonAction();
    }
}

void MoveCarriedObjects()
{
    // RCA: moveBallIntoCastle originally was called after we moved the carried objects, but
    // this created too many problems with the ball being in the castle and the key being
    // still outside the castle.  So I moved it to before.
    moveBallIntoCastle();

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

void moveBallIntoCastle() {
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        BALL* nextBall = gameBoard->getPlayer(ctr);
        // Handle balls going into the castles
        for(int portalCtr=0; portalCtr<numPorts; ++portalCtr) {
            Portcullis* nextPort = ports[portalCtr];
            if (nextBall->room == nextPort->room && nextPort->allowsEntry && CollisionCheckObject(nextPort, (nextBall->x-4), (nextBall->y-1), 8, 8))
            {
                nextBall->room = nextPort->insideRoom;
                nextBall->previousRoom = nextBall->room;
                nextBall->displayedRoom = nextBall->room;
                nextBall->y = ENTER_AT_BOTTOM;
                nextBall->previousY = nextBall->y;
				nextBall->vely = 0;
				nextBall->velx = 0;
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
}

void MoveGroundObject()
{
    // Move any objects that need moving, and wrap objects from room to room
    Board::ObjIter iter = board.getMovableObjects();
    while (iter.hasNext()) {
        
        OBJECT* object = iter.next();

        // Apply movement
        if ((object->gfxData != Dragon::gfxData) || (object->state == 0)) {
            object->x += object->getMovementX();
            object->y += object->getMovementY();
        }

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
                if ((object->room == ports[ctr]->insideRoom) && (ports[ctr]->allowsEntry))
                {
                    object->x = Portcullis::EXIT_X/2;
                    object->y = Portcullis::EXIT_Y/2;
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
    int displayedRoom = (displayWinningRoom ? winningRoom : objectBall->displayedRoom);
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
            byte bit=false;

            if (cx < 4)
                bit = pf0 & shiftreg[cx];
            else if (cx < 12)
                bit = pf1 & shiftreg[cx];
            else
                bit = pf2 & shiftreg[cx];

            if (bit != 0)
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
    COLOR defaultColor = colorTable[roomDefs[displayedRoom]->color];

	for (int i = 0; i < numPlayers; ++i) {
        BALL* player = gameBoard->getPlayer(i);
		if (player->displayedRoom == displayedRoom) {
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
        }
        if ((action->dropObject != OBJECT_NONE) && (actor->linkedObject == action->dropObject)) {
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
            // If anybody else was carrying this object, take it away.
            for(int ctr=0; ctr<numPlayers; ++ctr) {
                if ((ctr != actorNum) && (gameBoard->getPlayer(ctr)->linkedObject==action->pickupObject)) {
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

/**
 * To make game play more fun, you can't shove your key inside the walls of your own castle.  If you try, it will
 * stick out the other side.
 */
void unhideKey(OBJECT* droppedObject) {

    int objectPkey = droppedObject->getPKey();
    if ((objectPkey == OBJECT_YELLOWKEY) || (objectPkey == OBJECT_COPPERKEY) || (objectPkey == OBJECT_JADEKEY)) {
        int roomNum = droppedObject->room;
        if ((roomNum == GOLD_FOYER) || (roomNum == COPPER_FOYER) || (roomNum == JADE_FOYER)) {
            if (droppedObject->y < 15) {
                droppedObject->y = 15;
            } else if (droppedObject->y > 99) {
                droppedObject->y = 99;
            }
        }
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
        
        if ((gameOptions & GAMEOPTION_NO_HIDE_KEY_IN_CASTLE) != 0 ) {
            unhideKey(droppedObject);
        }
        
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
            // Check if a key unlocks the gate
            PortcullisStateAction* gateAction = port->checkKeyInteraction();
            if (gateAction != NULL) {
                sync->BroadcastAction(gateAction);
            }
            
            // Check if anything runs into the gate
            Board::ObjIter iter = board.getMovableObjects();
            while (iter.hasNext()) {
                OBJECT* next = iter.next();
                ObjectMoveAction* reaction = port->checkObjectEnters(next);
                if (reaction != NULL) {
                    sync->BroadcastAction(reaction);
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
        if ((magnetMatrix[i] != objectBall->linkedObject) && (object->room == magnet->room) && (object->exists()))
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

    if ((currentRoom->flags & ROOMFLAG_LEFTTHINWALL) && ((x-(4+4)) < 0x0D*2) && ((x+4) > 0x0D*2))
    {
        hitWall = true;
    }
    if ((currentRoom->flags & ROOMFLAG_RIGHTTHINWALL) && ((x+4) > 0x96*2) && ((x-(4+4) < 0x96*2)))
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
            byte bit=false;

            if (cx < 4)
                bit = pf0 & shiftreg[cx];
            else if (cx < 12)
                bit = pf1 & shiftreg[cx];
            else
                bit = pf2 & shiftreg[cx];

            if (bit != 0)
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

/**
 * Checks if ball is colliding with any other object.  Returns first object it finds or OBJECT_NONE
 * if no collisions.
 */
static int CollisionCheckBallWithAllObjects(BALL* ball) {
    Board::ObjIter iter = gameBoard->getObjects();
    return CollisionCheckBallWithObjects(ball, iter);
}

/**
 * Checks if ball is colliding with any of the objects in the iterable collection.
 * Returns first object it finds or OBJECT_NONE if no collisions.
 */
static int CollisionCheckBallWithObjects(BALL* ball, Board::ObjIter& iter)
{
    // Go through all the objects
    while(iter.hasNext())
    {
        // If this object is in the current room and can be picked up, check it against the ball
        const OBJECT* object = iter.next();
        if (CollisionCheckBallWithObject(ball, object))
        {
                return object->getPKey();
        }
    }

    return OBJECT_NONE;
}

/**
 * Checks if ball is colliding with object.
 */
static bool CollisionCheckBallWithObject(BALL* ball, const OBJECT* object)
{
    bool collision = (object->displayed &&
                      object->isTangibleTo(thisPlayer) &&
                      (ball->room == object->room) &&
                      (CollisionCheckObject(object, ball->x-4,(ball->y-1), 8, 8)) ? true : false);
    return collision;
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
    float h = flashColorHue / (360.0f/3);
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

    color.r = (int)max(flashColorLum, r);
    color.g = (int)max(flashColorLum, g);
    color.b = (int)max(flashColorLum, b);

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

/**
 * If the player has become disconnect them, remove them from the game.
 */
void dropPlayer(int player) {
    // Mostly just move the player to the 0 room, but drop any object they
    // are carrying and free any dragon they have been eaten by.
    if (player != thisPlayer) {
        BALL* toDrop = gameBoard->getPlayer(player);
        
        // Drop anything player is carrying
        toDrop->linkedObject = OBJECT_NONE;
        
        // Free the dragon if it has eaten the player
        for(int ctr=0; ctr<numDragons; ++ctr) {
            Dragon* dragon = dragons[ctr];
            if (dragon->eaten == toDrop) {
                dragon->state = Dragon::STALKING;
                dragon->eaten = NULL;
            }
        }

        // Move the player to the 0 room.
        toDrop->room = 0;
    }
}

/**
 * Sends a message that a player has gone offlline or come back online.
 * playerDropped - the player that dropped off, 0 means no one dropped off.  -1 means two players dropped off.
 * playerRejoined - the player that rejoined, 0 means no one rejoined.  -1 means two players rejoined.
 */
void warnOfDropoffRejoin(int playerDroppedOff, int playerRejoined) {
    if ((playerRejoined != 0) || (playerDroppedOff != 0)) {
        char firstMessage[1000];
        char message[2000];
        if (playerRejoined == 0) {
            firstMessage[0] = '\0';
        } else {
            if (playerRejoined < 0) {
                sprintf(firstMessage, "All other players have rejoined the game.\n");
            } else {
                sprintf(firstMessage, "Player %d has rejoined the game.\n", playerRejoined);
            }
        }
        if (playerDroppedOff != 0) {
            if (playerDroppedOff < 0) {
                sprintf(message, "%sAll other players have disconnected.\n", firstMessage);
            } else {
                sprintf(message, "%sPlayer %d has disconnected.\n", firstMessage, playerDroppedOff);
            }
            Platform_DisplayStatus(message, 5);
        } else {
            Platform_DisplayStatus(firstMessage, 5);
        }
    }
}

/**
 * Report whether  we've gotten messages from other players recently.
 * Also send a ping to other players to make sure they see activity from us.
 */
void checkPlayers() {
    // We check for players every 15 seconds (actually every 5 seconds/300 turns we check to see if its
    // been 15 seconds) if we've received anything from the other players.  If they've missed 3 15 second marks
    // in a row we assume they have disconnected and remove them from the game.
    // We also send out a ping every 15 seconds to others so we know they've heard from us.
    const int TURNS_BETWEEN_TIME_CHECKS = 300; // About 5 seconds.
    const int MILLIS_BETWEEN_MESSAGE_CHECKS = 15000; // 15 seconds
    const int MAX_MISSED_CHECKS = 3;
    
    static int turnsSinceTimeCheck = 0;
    static int long timeSinceLastMessageCheck = Sys::runTime();
    static int missedChecks[3] = {0, 0, 0};
    
    ++turnsSinceTimeCheck;
    if (turnsSinceTimeCheck >= TURNS_BETWEEN_TIME_CHECKS) {
        
        // Check the time, and see if it's time for a message check.
        long currentTime = Sys::runTime();
        if (currentTime - timeSinceLastMessageCheck > MILLIS_BETWEEN_MESSAGE_CHECKS) {
            int offline = 0;
            int online = 0;
            for(int ctr=0; ctr<numPlayers; ++ctr) {
                if (ctr != thisPlayer) {
                    if (sync->getMessagesReceived(ctr) == 0) {
                        ++missedChecks[ctr];
                        if (missedChecks[ctr] == MAX_MISSED_CHECKS) {
                            dropPlayer(ctr);
                            offline = (offline == 0 ? ctr+1 : -1);
                        }
                    } else {
                        if (missedChecks[ctr] >= MAX_MISSED_CHECKS) {
                            online = (online == 0 ? ctr+1 : -1);
                        }
                        missedChecks[ctr] = 0;
                    }
                }
            }
            warnOfDropoffRejoin(offline, online);
            PingAction* action = new PingAction();
            sync->BroadcastAction(action);
            sync->resetMessagesReceived();
            timeSinceLastMessageCheck = currentTime;
        }
        
        turnsSinceTimeCheck = 0;
    }
}



