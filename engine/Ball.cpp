
#include "Ball.hpp"
#include "Portcullis.hpp"

static const byte objectGfxPlayer1[] =
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

static const byte objectGfxPlayer2[] =
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

static const byte objectGfxPlayer3[] =
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
    gfxData((playerNum == 1 ? objectGfxPlayer1 : (playerNum == 2 ? objectGfxPlayer2 : objectGfxPlayer3))),
    homeGate(inHomeGate) {}
