
#include "GameObject.hpp"

#include "Board.hpp"

OBJECT::OBJECT(const byte* inGfxData, const byte* inStates, int inState, int inColor, int inRoom, int inX, int inY,
                       int inSize):
    gfxData(inGfxData),
    states(inStates),
    state(inState),
    color(inColor),
    room(inRoom),
    movementX(0),
    movementY(0),
    x(inX),
    y(inY),
    size(inSize)
{
    
}

OBJECT::~OBJECT() {}

void OBJECT::setBoard(Board* newBoard, int newPKey) {
    board = newBoard;
    pkey = newPKey;
}

OBJECT* OBJECT::lookupObject(int objKey) {
    return board->getObject(objKey);
}

void OBJECT::CalcSpriteExtents(int* cx, int* cy, int* cw, int* ch) const
{
    // Calculate the object's size and position
    *cx = x * 2;
    *cy = y * 2;
    
    int size = (size/2) + 1;
    *cw = (8 * 2) * size;
    
    // Look up the index to the current state for this object
    int stateIndex = states ? states[state] : 0;
    
    // Get the height, then the data
    // (the first byte of the data is the height)
    const byte* dataP = gfxData;
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

