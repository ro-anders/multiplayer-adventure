
#include "Ball.hpp"
#include "Portcullis.hpp"

static const byte ballGfxSolid[] =
{
    8,
    0xFF,				   // XXXXXXXX
    0xFF,				   // XXXXXXXX
    0xFF,				   // XXXXXXXX
    0xFF,				   // XXXXXXXX
    0xFF,				   // XXXXXXXX
    0xFF,				   // XXXXXXXX
    0xFF,				   // XXXXXXXX
    0xFF 				   // XXXXXXXX
};

static const byte ballGfxOne[] =
{
    8,
    0xFF,				   // XXXXXXXX
    0xC3,				   // XX    XX
    0xC3,				   // XX    XX
    0xC3,				   // XX    XX
    0xC3,				   // XX    XX
    0xC3,				   // XX    XX
    0xC3,				   // XX    XX
    0xFF 				   // XXXXXXXX
};

static const byte ballGfxTwo[] =
{
    8,
    0xFF,				   // XXXXXXXX
    0xFF,				   // XXXXXXXX
    0x18,				   //    XX
    0x18,				   //    XX
    0x18,				   //    XX
    0x18,				   //    XX
    0xFF,				   // XXXXXXXX
    0xFF 				   // XXXXXXXX
};

static const byte ballGfxX[] =
{
    8,
    0xFF,				   // XXXXXXXX
    0xBD,				   // X XXXX X
    0xDB,				   // XX XX XX
    0xE7,				   // XXX  XXX
    0xE7,				   // XXX  XXX
    0xDB,				   // XX XX XX
    0xBD,				   // X XXXX X
    0xFF 				   // XXXXXXXX
};

static const byte ballGfxPlus[] =
{
    8,
    0xFF,				   // XXXXXXXX
    0xE7,				   // XXX  XXX
    0xE7,				   // XXX  XXX
    0x81,				   // X      X
    0x81,				   // X      X
    0xE7,				   // XXX  XXX
    0xE7,				   // XXX  XXX
    0xFF 				   // XXXXXXXX
};


BALL::BALL(int inPlayerNum, Portcullis* inHomeGate) :
    playerNum(inPlayerNum),
    room(0),
    x(0),
    y(0),
    previousX(0),
    previousY(0),
    velx(0),
    vely(0),
    linkedObject(-1), // TODO: This should be OBJECT_NONE
    linkedObjectX(0),
    linkedObjectY(0),
    hitX(false),
    hitY(false),
    hitObject(-1), // TODO: This should be OBJECT_NONE
    gfxData((playerNum == 5 ? ballGfxSolid : (playerNum == 1 ? ballGfxOne : ballGfxTwo))),
    homeGate(inHomeGate) {}
