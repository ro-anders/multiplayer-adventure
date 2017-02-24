
#include "Portcullis.hpp"

#include "Adventure.h"
#include "color.h"
#include "Ball.hpp"
#include "Bat.hpp"
#include "Board.hpp"
#include "Map.hpp"
#include "RemoteAction.hpp"
#include "Room.hpp"

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

const int Portcullis::EXIT_X = 0xA0;
const int Portcullis::EXIT_Y = 0x58;
const int Portcullis::PORT_X = 0x4d;
const int Portcullis::PORT_Y = 0x31;


Portcullis::Portcullis(const char* inLabel, int inOutsideRoom, ROOM* inInsideRoom, OBJECT* inKey) :
  OBJECT(inLabel, objectGfxPort, portStates, 0x0C, COLOR_BLACK, OBJECT::FIXED_LOCATION),
  allowsEntry(false),
  insideRoom(inInsideRoom->index),
  key(inKey),
  allInsideRooms(NULL) {
      
    // Portcullis's unlike other objects, we know the location of before the game level is selected.
    room = inOutsideRoom;
    x = PORT_X;
    y = PORT_Y;
      
    if (inInsideRoom->visibility == ROOM::OPEN) {
        inInsideRoom->visibility = ROOM::IN_CASTLE;
    }
    
}

Portcullis::~Portcullis() {
    if (allInsideRooms != NULL) {
        delete[] allInsideRooms;
    }
}

void Portcullis::setState(int newState, bool newAllowsEntry) {
    state = newState;
    allowsEntry = newAllowsEntry;
}

void Portcullis::moveOneTurn() {
    if (state == OPEN_STATE) {
        allowsEntry = true;
    } else if (state == CLOSED_STATE) {
        allowsEntry = false;
    } else {
        // Raise/lower the gate
        ++state;
        if (state > 22)
        {
            // Port is unlocked
            state = OPEN_STATE;
        }
    }
}

ObjectMoveAction* Portcullis::checkObjectEnters(OBJECT* object) {
    // Gate must be open and someone else must be in the room to witness (objects don't go through gates if no one
    // is in the room).  Object must be in room, touching gate, and not held by anyone.
    
    // For efficiency, we've computed whether someone is in the room to witness before we ever make this call, so we don't
    // need to check it again.
    
    ObjectMoveAction* newAction = NULL;
    if ((object->room == this->room) && this->allowsEntry) {
        bool held = false;
        int numPlayers = this->board->getNumPlayers();
        for(int ctr=0; !held && (ctr<numPlayers); ++ctr) {
            held = (this->board->getPlayer(ctr)->linkedObject == object->getPKey());
        }
    
        if (!held && board->CollisionCheckObjectObject(this, object)) {
            object->room = this->insideRoom;
            object->y = ENTER_AT_BOTTOM;
            // We only generate an event if we are in the room.
            newAction = new ObjectMoveAction(object->getPKey(), object->room, object->x, object->y);
        }
    }
    
    return newAction;
}

PortcullisStateAction*  Portcullis::checkKeyInteraction() {
    PortcullisStateAction* gateAction = NULL;
    if ((state == OPEN_STATE || state == CLOSED_STATE) && checkKeyTouch(key)) {
        // Toggle the port state
        state++;
        allowsEntry = true; // Either the gate is now opening and active or now closing but still active
        
        // We need to know who, if anyone, is holding the key
        int heldBy = -1;
        int numPlayers = board->getNumPlayers();
        int keyPkey = key->getPKey();
        for(int ctr=0; (ctr < numPlayers) && (heldBy < 0); ++ctr) {
            BALL* nextPlayer = board->getPlayer(ctr);
            // Have to check if player is holding key or player is holding bat holding key
            if ((nextPlayer->linkedObject == keyPkey) ||
                ((nextPlayer->linkedObject == OBJECT_BAT) && (((Bat*)board->getObject(OBJECT_BAT))->linkedObject == keyPkey))) {

                heldBy = ctr;
            }
        }
        
        // Unless someone else is unlocking the castle we broadcast the event
        // So broadcast if we are holding the key or if no one is holding the key and we are in the same room
        BALL* thisPlayer = board->getCurrentPlayer();
        if ((heldBy == thisPlayer->playerNum) ||
            ((heldBy < 0) && (thisPlayer->room == room))) {
            
            gateAction = new PortcullisStateAction(getPKey(), state, allowsEntry);
        }
    }
    return gateAction;
}

/**
 * Returns whether the key is touching the portcullis.  Does not care about other things like
 * whether the gate is in a state where it cares or whether anyone is in the room
 */
bool Portcullis::checkKeyTouch(OBJECT* keyToCheck) {
    bool touched = ((keyToCheck->room == this->room) && (board->CollisionCheckObjectObject(this, keyToCheck)));
    return touched;
}

void Portcullis::openFromInside() {
    ++state;
}

void Portcullis::forceOpen() {
    state = OPEN_STATE;
    allowsEntry = true;
}

void Portcullis::addRoom(ROOM* room) {
    if (allInsideRooms == NULL) {
        int arraySize = Map::getNumRooms(); // Overkill, but easier than authoring a List<int> class.
        allInsideRooms = new int[arraySize];
        allInsideRooms[0] = insideRoom;
        numInsideRooms = 1;
    }
    allInsideRooms[numInsideRooms] = room->index;
    numInsideRooms++;
    if (room->visibility == ROOM::OPEN) {
        room->visibility = ROOM::IN_CASTLE;
    }
}



bool Portcullis::containsRoom(int room) {
    bool found = false;
    if (allInsideRooms == NULL) {
        found = (room == insideRoom);
    } else {
        for (int ctr=0; !found && (ctr<numInsideRooms); ++ctr) {
            found = found || (allInsideRooms[ctr] == room);
        }
    }
    return found;
}

