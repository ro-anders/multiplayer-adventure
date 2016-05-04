

#ifndef Board_hpp
#define Board_hpp

#include <stdio.h>

#include "GameObject.hpp"

class BALL;

#define PLAYFIELD_HRES      20  // 40 with 2nd half mirrored/repeated
#define PLAYFIELD_VRES      20
#define CLOCKS_HSYNC        2
#define CLOCKS_VSYNC        4

#define OBJECT_BALL			(-2)
enum
{
    OBJECT_NONE=-1,
    OBJECT_YELLOW_PORT=0,
    OBJECT_COPPER_PORT,
    OBJECT_JADE_PORT,
    OBJECT_WHITE_PORT,
    OBJECT_BLACK_PORT,
    OBJECT_NAME,
    OBJECT_NUMBER,
    OBJECT_REDDRAGON,
    OBJECT_YELLOWDRAGON,
    OBJECT_GREENDRAGON,
    OBJECT_SWORD,
    OBJECT_BRIDGE,
    OBJECT_YELLOWKEY,
    OBJECT_COPPERKEY,
    OBJECT_JADEKEY,
    OBJECT_WHITEKEY,
    OBJECT_BLACKKEY,
    OBJECT_BAT,
    OBJECT_DOT,
    OBJECT_CHALISE,
    OBJECT_MAGNET
};

class Board {
public:
    
    // TODO: We really don't want this public.  Migrate Adventure.cpp to using public methods instead.
    OBJECT** objects;
    
    Board(int screenWidth, int screenHeight);
    
    ~Board();
    
    void addObject(int pkey, OBJECT* object);
    
    OBJECT* getObject(int pkey);
    
    /**
     * Get the number of objects on the board.
     * This does not include the "null" object that the old game used to mark the end of the list.
     * This does include all game 2 objects even on game 1 when they are all shoved into the unreachable first room.
     */
    int getNumObjects();
        
    bool static HitTestRects(int ax, int ay, int awidth, int aheight,
                             int bx, int by, int bwidth, int bheight);
    
    int getNumPlayers();
    
    void addPlayer(BALL* newPlayer, bool isCurrent);
    
    BALL* getPlayer(int playerNum);
    
    BALL* getCurrentPlayer();
    
    // Collision check two objects
    // On the 2600 this is done in hardware by the Player/Missile collision registers
    bool CollisionCheckObjectObject(const OBJECT* object1, const OBJECT* object2);

    // Checks an object for collision against the specified rectangle
    // On the 2600 this is done in hardware by the Player/Missile collision registers
    bool CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height);


private:
    
    int numObjects; // Includes the "null" object which the old game used to mark the end of the list
    int numPlayers;
    BALL** players;
    int currentPlayer;
    
    int screenWidth;
    int screenHeight;

};

#endif /* Board_hpp */
