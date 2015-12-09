
#include "Bat.hpp"

static const byte objectGfxBat [] =
{
    // Object #0E : State 03 : Graphic
    7,
    0x81,                  // X      X
    0x81,                  // X      X
    0xC3,                  // XX    XX
    0xC3,                  // XX    XX
    0xFF,                  // XXXXXXXX
    0x5A,                  //  X XX X
    0x66,                  //  XX  XX
    // Object #0E : State FF : Graphic
    11,
    0x01,                  //        X
    0x80,                  // X
    0x01,                  //        X
    0x80,                  // X
    0x3C,                  //   XXXX
    0x5A,                  //  X XX X
    0x66,                  //  XX  XX
    0xC3,                  // XX    XX
    0x81,                  // X      X
    0x81,                  // X      X
    0x81                   // X      X
};

// Bat states
static const byte batStates [] =
{
    0,1
};


Bat::Bat(int inColor, int inRoom, int inX, int inY) :
  OBJECT(objectGfxBat, batStates, 0, inColor, inRoom, inX, inY),
  linkedObject(0),
  linkedObjectX(0),
  linkedObjectY(0) {}

Bat::~Bat() {}