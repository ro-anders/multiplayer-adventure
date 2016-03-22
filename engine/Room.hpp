
#ifndef Room_hpp
#define Room_hpp

#include <stdio.h>

#include "adventure_sys.h"

#define ROOMFLAG_NONE           0x00
#define ROOMFLAG_MIRROR         0x01 // bit 0 - 1 if graphics are mirrored, 0 for reversed
#define ROOMFLAG_LEFTTHINWALL   0x02 // bit 1 - 1 for left thin wall
#define ROOMFLAG_RIGHTTHINWALL  0x04 // bit 2 - 1 for right thin wall


class ROOM
{
public:
    
    // An attribute indicating whether objects should be placed in a room when randomly placing objects.
    enum RandomVisibility {
            OPEN, // A room freely accessible without use of a key (e.g. blue labyrinth)
            IN_CASTLE, // a room behind a castle portcullis (e.g. red labyrinth)
            HIDDEN // a room that shouldn't have objects randomly put in it (e.g. the easter egg room)
    };
    
    int index;                  // index into the map
    const byte* graphicsData;   // pointer to room graphics data
    byte flags;                 // room flags - see below
    int color;                  // foreground color
    int roomUp;                 // index of room UP
    int roomRight;              // index of room RIGHT
    int roomDown;               // index of room DOWN
    int roomLeft;               // index of room LEFT
    char* label;                // a short, unique name for the object
    RandomVisibility visibility; // attribute indicating whether objects can be randomly placed in this room.
    
    ROOM(const byte* graphicsData, byte flags, int color,
         int roomUp, int roomRight, int roomDown, int roomLeft, const char* inLabel, RandomVisibility inVis=OPEN);
    
    ~ROOM();
    
    void setIndex(int inIndex);
    
    bool isNextTo(ROOM* otherRoom);
    
};


#endif /* Room_hpp */
