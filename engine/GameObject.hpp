

#ifndef GameObject_hpp
#define GameObject_hpp

#include <stdlib.h>
#include <stdio.h>
#include "adventure_sys.h"

class Board;

class OBJECT {

public:
    
    enum RandomizedLocations {
        OUT_IN_OPEN,
        OPEN_OR_IN_CASTLE,
        FIXED_LOCATION
    };
    

    const byte* gfxData;        // graphics data for each state
    const byte* states;         // array of indicies for each state
    int state;                  // current state
    color color;                  // color
    int room;                   // room
    int x;                      // x position
    int y;                      // y position
    int size;                   // size (used for bridge and surround)
    bool displayed;             // flag indicating object was displayed (when more than maxDisplayableObjects for instance)
    char* label;                // a short, unique name for the object
    RandomizedLocations randomPlacement; // How to randomly place this object in game 3

    
    OBJECT(const char* inLabel, const byte* inGfxData, const byte* inStates, int inState, int inColor,
               RandomizedLocations inRandomPlacement=OPEN_OR_IN_CASTLE, int inSize=0);
    
    virtual ~OBJECT();
    
    int getMovementX() {return movementX;}
    void setMovementX(int moveX) {movementX = moveX;}
    int getMovementY() {return movementY;}
    void setMovementY(int moveY) {movementY = moveY;}
    
    void setBoard(Board* newBoard, int newPKey);
    
    /** 
     * Only one player can pickup this object.  All other players pass through it.  Used for private magnets.
     * player - player it is private to.  A negative number means obect is not private.
     */
    void setPrivateToPlayer(int player);

    /**
     * Returns true if this object is solid or grabbable by the player.  If object is private (e.g. private magnet)
     * will return false and player will pass right through it without picking it up.
     */
    bool isTangibleTo(int player) const;
    
    void CalcSpriteExtents(int* cx, int* cy, int* cw, int* ch) const;
    
    int getPKey() const;
    
    inline bool exists() const {return objExists;}
    inline void setExists(bool inExists) {objExists = inExists;}
    
    /**
     * Sets up the object in the room it will start off in.
     */
    virtual void init(int room, int x, int y, int state=0, int movementX=0, int movementY=0);
    
protected:

    bool objExists;             // Whether the object is active in this game.  Starts out false until init() called.
    Board* board;               // The board on which this object has been placed.
    OBJECT* lookupObject(int objectKey);

    int movementX;              // horizontal movement
    int movementY;              // vertical movement

    /** For private magnets. */
    int privateToPlayer = -1;
    
private:
    
    int pkey;                   // The "primary key" or index to this object on the board
    

    
};


#endif /* GameObject_hpp */
