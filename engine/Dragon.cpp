
#include "Dragon.hpp"

const int Dragon::STALKING = 0;
const int Dragon::DEAD = 1;
const int Dragon::EATEN = 2;
const int Dragon::ROAR = 3;

// Dragon states
static const byte dragonStates [] =
{
    0,2,0,1
};

// Dragon Difficulty
static const byte dragonDiff [] =
{
    0xD0, 0xE8,           // Level 1 : Am, Pro
    0xF0, 0xF6,           // Level 2 : Am, Pro
    0xF0, 0xF6            // Level 3 : Am, Pro
};

static const byte objectGfxDrag [] =
{
    // Object #6 : State #00 : Graphic
    20,
    0x06,                  //      XX
    0x0F,                  //     XXXX
    0xF3,                  // XXXX  XX
    0xFE,                  // XXXXXXX
    0x0E,                  //     XXX
    0x04,                  //      X
    0x04,                  //      X
    0x1E,                  //    XXXX
    0x3F,                  //   XXXXXX
    0x7F,                  //  XXXXXXX
    0xE3,                  // XXX   XX
    0xC3,                  // XX    XX
    0xC3,                  // XX    XX
    0xC7,                  // XX   XXX
    0xFF,                  // XXXXXXXX
    0x3C,                  //   XXXX
    0x08,                  //     X
    0x8F,                  // X   XXXX
    0xE1,                  // XXX    X
    0x3F,                  //   XXXXXX
    // Object 6 : State 01 : Graphic
    22,
    0x80,                  // X
    0x40,                  //  X
    0x26,                  //   X  XX
    0x1F,                  //    XXXXX
    0x0B,                  //     X XX
    0x0E,                  //     XXX
    0x1E,                  //    XXXX
    0x24,                  //   X  X
    0x44,                  //  X   X
    0x8E,                  // X   XXX
    0x1E,                  //    XXXX
    0x3F,                  //   XXXXXX
    0x7F,                  //  XXXXXXX
    0x7F,                  //  XXXXXXX
    0x7F,                  //  XXXXXXX
    0x7F,                  //  XXXXXXX
    0x3E,                  //   XXXXX
    0x1C,                  //    XXX
    0x08,                  //     X
    0xF8,                  // XXXXX
    0x80,                  // X
    0xE0,                   // XXX
    // Object 6 : State 02 : Graphic
    17,
    0x0C,                  //     XX
    0x0C,                  //     XX
    0x0C,                  //     XX
    0x0E,                  //     XXX
    0x1B,                  //    XX X
    0x7F,                  //  XXXXXXX
    0xCE,                  // XX  XXX
    0x80,                  // X
    0xFC,                  // XXXXXX
    0xFE,                  // XXXXXXX
    0xFE,                  // XXXXXXX
    0x7E,                  //  XXXXXX
    0x78,                  //  XXXX
    0x20,                  //   X
    0x6E,                  //  XX XXX
    0x42,                  //  X    X
    0x7E                   //  XXXXXX
};



Dragon::Dragon(const char* label, int inNumber, int inState, int inColor, int inRoom, int inX, int inY):
    OBJECT(label, objectGfxDrag, dragonStates, inState, inColor, inRoom, inX, inY),
    dragonNumber(inNumber),
    timer(0),
    eaten(NULL),
    eatenX(0),
    eatenY(0)
{}

Dragon::~Dragon() {
    
}

void Dragon::resetTimer(int gameLevel, int dragonDifficulty) {
    timer = 0xFC - dragonDiff[(gameLevel*2) + dragonDifficulty];
}

void Dragon::decrementTimer() {
    --timer;
}

int Dragon::timerExpired() {
    return (timer <= 0);
}

void Dragon::roar(int atX, int atY, int gameLevel, int dragonDifficulty) {
    state = ROAR;
    
    resetTimer(gameLevel, dragonDifficulty);
    
    // Set the dragon's position to the same as the ball
    x = atX+1;
    y = atY;
    
    movementX = 0;
    movementY = 0;
    
}
