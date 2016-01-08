
#include "Bat.hpp"
#include "Ball.hpp"
#include "Board.hpp"
#include "Sync.hpp"

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

// Bat Object Matrix
static const int batMatrix [] =
{
    OBJECT_CHALISE,
    OBJECT_SWORD,
    OBJECT_BRIDGE,
    OBJECT_COPPERKEY,
    OBJECT_JADEKEY,
    OBJECT_YELLOWKEY,
    OBJECT_WHITEKEY,
    OBJECT_BLACKKEY,
    OBJECT_REDDRAGON,
    OBJECT_YELLOWDRAGON,
    OBJECT_GREENDRAGON,
    OBJECT_MAGNET,
    0x00
};


int Bat::MAX_FEDUP = 0xff;

Bat::Bat(int inColor, int inRoom, int inX, int inY) :
  OBJECT("bat", objectGfxBat, batStates, 0, inColor, inRoom, inX, inY),
  linkedObject(0),
  linkedObjectX(0),
  linkedObjectY(0) {}

Bat::~Bat() {}

void Bat::moveOneTurn(Sync* sync)
{
    static int flapTimer = 0;
    if (++flapTimer >= 0x04)
    {
        state = (state == 0) ? 1 : 0;
        flapTimer = 0;
    }
    
    if ((linkedObject != OBJECT_NONE) && (batFedUpTimer < MAX_FEDUP))
        ++batFedUpTimer;
    
    RemoteAction* batAction = sync->GetNextBatAction();
    while (batAction != NULL) {
        //bat->handleAction();
        delete batAction;
        batAction = sync->GetNextBatAction();
    }
    
    if (batFedUpTimer >= 0xff)
    {
        // Get the bat's current extents
        int batX, batY, batW, batH;
        CalcSpriteExtents(&batX, &batY, &batW, &batH);
        
        // Enlarge the bat extent by 7 pixels for the proximity checks below
        // (doing the bat once is faster than doing each object and the results are the same)
        batX-=7;
        batY-=7;
        batW+=7*2;
        batH+=7*2;
        
        // Go through the bat's object matrix
        const int* matrixP = batMatrix;
        do
        {
            // Get the object it is seeking
            const OBJECT* seekObject = lookupObject(*matrixP);
            if ((seekObject->room == room) && (linkedObject != *matrixP))
            {
                int seekX = seekObject->x;
                int seekY = seekObject->y;
                
                // Set the movement
                
                // horizontal axis
                if (x < seekX)
                {
                    movementX = 3;
                }
                else if (x > seekX)
                {
                    movementX = -3;
                }
                else movementX = 0;
                
                // vertical axis
                if (y < seekY)
                {
                    movementY = 3;
                }
                else if (y > seekY)
                {
                    movementY = -3;
                }
                else movementY = 0;
                
                // If the bat is within 7 pixels of the seek object it can pick the object up
                // The bat extents have already been expanded by 7 pixels above, so a simple
                // rectangle intersection test is good enought here
                
                int objX, objY, objW, objH;
                seekObject->CalcSpriteExtents(&objX, &objY, &objW, &objH);

                printf("Targeting %s at (%d,%d)-(%d,%d) from (%d,%d)-(%d,%d)\n", seekObject->label,
                       objX, objY-objH, objX+objW, objY, batX, batY-batH, batX+batW, batY);

                if (Board::HitTestRects(batX, batY, batW, batH, objX, objY, objW, objH))
                {
                    // Hit something we want
                    
                    // If the bat grabs something that a player is carrying, the bat gets it
                    // This allows the bat to take something being carried
                    for (int ctr=0; ctr<board->getNumPlayers(); ++ctr) {
                        BALL* nextBall = board->getPlayer(ctr);
                        if (*matrixP == nextBall->linkedObject)
                        {
                            // Now player has nothing
                            nextBall->linkedObject = OBJECT_NONE;
                        }
                    }
                    
                    // Pick it up
                    linkedObject = *matrixP;
                    linkedObjectX = 8;
                    linkedObjectY = 0;
                    
                    // Reset the timer
                    batFedUpTimer = 0;
                }
                
                // break since we found something
                break;
            }
        }
        while (*(++matrixP));
        
    }
}

void Bat::lookForNewObject() {
    batFedUpTimer = MAX_FEDUP;
}