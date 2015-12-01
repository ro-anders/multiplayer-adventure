
#include "GameObject.hpp"

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
    linkedObject(0),
    linkedObjectX(0),
    linkedObjectY(0),
    size(inSize)
{
    
}

OBJECT::~OBJECT() {}
