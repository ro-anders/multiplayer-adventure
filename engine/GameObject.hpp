

#ifndef GameObject_hpp
#define GameObject_hpp

#include <stdlib.h>
#include <stdio.h>
#include "adventure_sys.h"

class OBJECT {

public:
    const byte* gfxData;        // graphics data for each state
    const byte* states;         // array of indicies for each state
    int state;                  // current state
    color color;                  // color
    int room;                   // room
    int x;                      // x position
    int y;                      // y position
    int movementX;              // horizontal movement
    int movementY;              // vertical movement
    int size;                   // size (used for bridge and surround)
    bool displayed;             // flag indicating object was displayed (when more than maxDisplayableObjects for instance)

    OBJECT(const byte* inGfxData, const byte* inStates, int inState, int inColor, int inRoom, int inX, int inY,
               int size=0);
    
    virtual ~OBJECT();
};


#endif /* GameObject_hpp */
