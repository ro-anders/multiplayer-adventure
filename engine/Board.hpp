

#ifndef Board_hpp
#define Board_hpp

#include <stdio.h>

#include "adventure_sys.h"
#include "GameObject.hpp"

class BALL;

#define PLAYFIELD_HRES      20  // 40 with 2nd half mirrored/repeated
#define PLAYFIELD_VRES      20
#define CLOCKS_HSYNC        2
#define CLOCKS_VSYNC        4

// The position you appear when you enter at the edge of the screen.
#define ENTER_AT_TOP      ADVENTURE_OVERSCAN + ADVENTURE_SCREEN_HEIGHT
#define ENTER_AT_BOTTOM    ADVENTURE_OVERSCAN + ADVENTURE_OVERSCAN-2
#define ENTER_AT_RIGHT      ADVENTURE_SCREEN_WIDTH-5
#define ENTER_AT_LEFT       5


// The limit as to how close a ball can get to the edge
#define TOP_EDGE        ADVENTURE_OVERSCAN + ADVENTURE_SCREEN_HEIGHT + 6
#define BOTTOM_EDGE     0x0D*2
#define RIGHT_EDGE      ADVENTURE_SCREEN_WIDTH-4
#define LEFT_EDGE       4

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
    OBJECT_REDDRAGON, // Put all immovable objects before this
    OBJECT_YELLOWDRAGON,
    OBJECT_GREENDRAGON,
    OBJECT_SWORD, // Put all carryable objects after this
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
    
    class ObjIter {
    public:
        ObjIter();
        ObjIter(Board* board, int startingIndex);
        ObjIter(const ObjIter& other);
        ObjIter& operator=(const ObjIter& other);
        bool hasNext();
        OBJECT* next();
    private:
        Board* board;
        int nextExisting;
        static int findNext(int startAt, Board* board);
    };
    
    
    Board(int screenWidth, int screenHeight);
    
    ~Board();
    
    void addObject(int pkey, OBJECT* object);
    
    inline OBJECT* getObject(int pkey) {return (pkey > OBJECT_NONE ? objects[pkey] : NULL);}
    
    inline OBJECT* operator[](int pkey) {return (pkey > OBJECT_NONE ? objects[pkey] : NULL);}
    
    /**
     * Get the number of objects on the board.
     * This does not include the "null" object that the old game used to mark the end of the list.
     * This does include all game 2 objects even on game 1 when they are all shoved into the unreachable first room.
     */
    int getNumObjects();
    
    ObjIter getObjects();
    
    ObjIter getMovableObjects();
    
    ObjIter getCarryableObjects();

    bool static HitTestRects(int ax, int ay, int awidth, int aheight,
                             int bx, int by, int bwidth, int bheight);
    
    inline int getNumPlayers() {return numPlayers;}
    
    void addPlayer(BALL* newPlayer, bool isCurrent);
    
    BALL* getPlayer(int playerNum);
    
    BALL* getCurrentPlayer();
    
    // Collision check two objects
    // On the 2600 this is done in hardware by the Player/Missile collision registers
    bool CollisionCheckObjectObject(const OBJECT* object1, const OBJECT* object2);

    // Checks an object for collision against the specified rectangle
    // On the 2600 this is done in hardware by the Player/Missile collision registers
    bool CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height);

    // Returns the player number of whoever is holding an object (holding the bat holding the
    // object counts).  Returns -1 if no one is holding the object.
    int getPlayerHoldingObject(OBJECT* object);

private:
    
    int numObjects; // Includes the "null" object which the old game used to mark the end of the list
    OBJECT** objects;

    int numPlayers;
    BALL** players;
    int currentPlayer;
    
    int screenWidth;
    int screenHeight;

};

#endif /* Board_hpp */
