
#include "Portcullis.hpp"

#include "color.h"

// Object #1 States 940FF (Graphic)
static const byte objectGfxPort [] =
{
    // state 1
    4,
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    // state 2
    6,
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    // state 3
    8,
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    // state 4
    10,
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    // state 5
    12,
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    // state 6
    14,
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    // state 7
    16,
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA,                  // X X X X
    0xFE,                  // XXXXXXX
    0xAA                   // X X X X
};



// Portcullis states
static const byte portStates [] =
{
    0,0,1,1,2,2,3,3,4,4,5,5,6,6,5,5,4,4,3,3,2,2,1,1
};

const int Portcullis::OPEN_STATE=0;
const int Portcullis::CLOSED_STATE=12;



Portcullis::Portcullis(int inOutsideRoom, int inInsideRoom, OBJECT* inKey) :
  OBJECT(objectGfxPort, portStates, 0x0C, COLOR_BLACK, inOutsideRoom, 0x4d, 0x31),
  insideRoom(inInsideRoom),
  key(inKey) {}

Portcullis::~Portcullis() {}

