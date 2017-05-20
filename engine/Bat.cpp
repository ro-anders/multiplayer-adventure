
#include "sys.h"
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

static int BAT_SPEED = 3;

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

Bat::Bat(int inColor) :
  OBJECT("bat", objectGfxBat, batStates, 0, inColor),
  linkedObject(0),
  linkedObjectX(0),
  linkedObjectY(0) {}

Bat::~Bat() {}

void Bat::moveOneTurn(Sync* sync, BALL* objectBall)
{
    static int flapTimer = 0;
    if (++flapTimer >= 0x04)
    {
        state = (state == 0) ? 1 : 0;
        flapTimer = 0;
    }
    
    if ((linkedObject != OBJECT_NONE) && (batFedUpTimer < MAX_FEDUP))
        ++batFedUpTimer;
    
    if (batFedUpTimer >= 0xff)
    {
        // Get the bat's current extents
        int batX, batY, batW, batH;
        CalcSpriteExtents(&batX, &batY, &batW, &batH);
        
        // Enlarge the bat extent by 7 pixels for the proximity checks below
        // (doing the bat once is faster than doing each object and the results are the same)
        batX-=7;
        batY+=7;
        batW+=7*2;
        batH+=7*2;
        
        // Go through the bat's object matrix
        const int* matrixP = batMatrix;
        do
        {
            // Get the object it is seeking
            const OBJECT* seekObject = lookupObject(*matrixP);
            if ((seekObject->room == room) && (linkedObject != *matrixP) && (seekObject->exists()))
            {
                int seekX = seekObject->x;
                int seekY = seekObject->y;
                
                // Set the movement
                int newMoveX = 0;
                int newMoveY = 0;
                
                // horizontal axis
                if (x < seekX)
                {
                    newMoveX = BAT_SPEED;
                }
                else if (x > seekX)
                {
                    newMoveX = -BAT_SPEED;
                }
                
                // vertical axis
                if (y < seekY)
                {
                    newMoveY = BAT_SPEED;
                }
                else if (y > seekY)
                {
                    newMoveY = -BAT_SPEED;
                }
                
                bool sendMessage = ((newMoveX != movementX) || (newMoveY != movementY));
                movementX = newMoveX;
                movementY = newMoveY;
                if (sendMessage) {
                    broadcastMoveAction(sync, objectBall);
                }
                
                // If the bat is within 7 pixels of the seek object it can pick the object up
                // The bat extents have already been expanded by 7 pixels above, so a simple
                // rectangle intersection test is good enought here
                
                int objX, objY, objW, objH;
                seekObject->CalcSpriteExtents(&objX, &objY, &objW, &objH);

                if (Board::HitTestRects(batX, batY, batW, batH, objX, objY, objW, objH))
                {
                    // Hit something we want
                    pickupObject(*matrixP, sync);
                }
                
                // break since we found something
                break;
            }
        }
        while (*(++matrixP));
        
    }
}

void Bat::handleAction(RemoteAction *action, BALL* objectBall) {
    if (action->typeCode == BatMoveAction::CODE) {
        
        // If we are in the same room as the bat and are closer to it than the reporting player,
        // then we ignore reports and trust our internal state.
        // Otherwise, use the reported state.
        BatMoveAction* nextMove = (BatMoveAction*)action;
        if ((room != objectBall->room) ||
             (objectBall->distanceTo(x, y) > nextMove->distance)) {
                
                room = nextMove->room;
                x = nextMove->posx;
                y = nextMove->posy;
                movementX = nextMove->velx;
                movementY = nextMove->vely;
                
            }
    } else if (action->typeCode == BatPickupAction::CODE) {
        BatPickupAction* nextPickup = (BatPickupAction*)action;
        if (nextPickup->dropObject != OBJECT_NONE) {
            OBJECT* droppedObject = lookupObject(nextPickup->dropObject);
            droppedObject->x = nextPickup->dropX;
            droppedObject->y = nextPickup->dropY;
        }
        pickupObject(nextPickup->pickupObject, NULL);
    }
}

void Bat::pickupObject(int newObject, Sync* sync) {
    // If the bat grabs something that a player is carrying, the bat gets it
    // This allows the bat to take something being carried
    for (int ctr=0; ctr<board->getNumPlayers(); ++ctr) {
        BALL* nextBall = board->getPlayer(ctr);
        if (newObject == nextBall->linkedObject)
        {
            // Now player has nothing
            nextBall->linkedObject = OBJECT_NONE;
        }
    }
    
    // A NULL sync indicates this was initiated by a sync message and does not need to be rebroadcast
    if (sync != NULL) {
        if (linkedObject == OBJECT_NONE) {
            BatPickupAction* action = new BatPickupAction(newObject, 8, 0, OBJECT_NONE, 0, 0, 0);
            sync->BroadcastAction(action);
        } else {
            OBJECT* dropObject = lookupObject(linkedObject);
            BatPickupAction* action = new BatPickupAction(newObject, 8, 0, linkedObject, dropObject->room, dropObject->x, dropObject->y);
            sync->BroadcastAction(action);
        }
    }
    
    // Pick it up
    linkedObject = newObject;
    linkedObjectX = 8;
    linkedObjectY = 0;
    
    // Reset the timer
    batFedUpTimer = 0;
}

void Bat::broadcastMoveAction(Sync *sync, BALL* objectBall) {
    int distance = objectBall->distanceTo(x, y);
    BatMoveAction* action = new BatMoveAction(room, x, y, movementX, movementY, distance);
    sync->BroadcastAction(action);
}

void Bat::lookForNewObject() {
    batFedUpTimer = MAX_FEDUP;
}
