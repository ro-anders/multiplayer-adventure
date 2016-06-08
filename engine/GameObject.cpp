
#include "GameObject.hpp"

#include <string.h>

#include "Board.hpp"

OBJECT::OBJECT(const char* inLabel, const byte* inGfxData, const byte* inStates, int inState, int inColor,
               RandomizedLocations inRandomPlacement, int inSize):
    gfxData(inGfxData),
    states(inStates),
    state(inState),
    color(inColor),
    room(-1),
    movementX(0),
    movementY(0),
    x(0),
    y(0),
    randomPlacement(inRandomPlacement),
    size(inSize),
    objExists(false),
    privateToPlayer(-1)
{
    label = new char[strlen(inLabel)+1];
    strcpy(label, inLabel);
}

OBJECT::~OBJECT() {
    delete[] label;
}

void OBJECT::setBoard(Board* newBoard, int newPKey) {
    board = newBoard;
    pkey = newPKey;
}

int OBJECT::getPKey() const {
    return pkey;
}

void OBJECT::init(int inRoom, int inX, int inY) {
    room = inRoom;
    x = inX;
    y = inY;
    objExists = true;
}

void OBJECT::setPrivateToPlayer(int player) {
    privateToPlayer = player;
}

bool OBJECT::isTangibleTo(int player) const {
    return (privateToPlayer < 0) || (privateToPlayer == player);
}

OBJECT* OBJECT::lookupObject(int objKey) {
    return board->getObject(objKey);
}

void OBJECT::CalcSpriteExtents(int* cx, int* cy, int* cw, int* ch) const
{
    // Calculate the object's size and position
    *cx = x * 2;
    *cy = y * 2;
    
    int adjSize = (size/2) + 1;
    *cw = (8 * 2) * adjSize;
    
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

