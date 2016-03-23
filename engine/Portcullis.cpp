
#include "Portcullis.hpp"

#include "color.h"
#include "Map.hpp"
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
const int Portcullis::EXIT_Y = 0x2C * 2;


Portcullis::Portcullis(const char* inLabel, int inOutsideRoom, ROOM* inInsideRoom, OBJECT* inKey) :
  OBJECT(inLabel, objectGfxPort, portStates, 0x0C, COLOR_BLACK, inOutsideRoom, 0x4d, 0x31, OBJECT::FIXED_LOCATION),
  isActive(false),
  insideRoom(inInsideRoom->index),
  key(inKey),
  allInsideRooms(NULL) {
      
    if (inInsideRoom->visibility == ROOM::OPEN) {
        inInsideRoom->visibility = ROOM::IN_CASTLE;
    }
    
}

Portcullis::~Portcullis() {
    if (allInsideRooms != NULL) {
        delete[] allInsideRooms;
    }
}

void Portcullis::setState(int newState, bool newActive) {
    state = newState;
    isActive = newActive;
    printf("%s changed.  %s state = %d(%s)\n", this->label, (isActive ? "Active" : "Inactive"),
                                                    newState, (newState == OPEN_STATE ? "open" :
                                                    (newState < CLOSED_STATE ? "closing" :
                                                     (newState == CLOSED_STATE ? "closed" : "opening"))));
}

void Portcullis::updateState() {
    if (state == OPEN_STATE) {
        isActive = true;
    } else if (state == CLOSED_STATE) {
        isActive = false;
    } else {
        // Raise/lower the gate
        ++state;
        if (state > 22)
        {
            // Port is unlocked
            state = OPEN_STATE;
            printf("%s updated.  %s = %d(%s)\n", this->label, (isActive ? "Active" : "Inactive"), state, "open");
        } else if (state == CLOSED_STATE) {
            printf("%s updated.  %s = %d(%s)\n", this->label, (isActive ? "Active" : "Inactive"), state, "closed");
        }
    }
}

void Portcullis::keyTouch() {
    state++;
    isActive = true; // Either the gate is now opening and active or now closing but still active
    printf("%s touched by key.  %s = %d(%s)\n", this->label, (isActive ? "Active" : "Inactive"),
                                                    state, (state == OPEN_STATE ? "open" :
                                                    (state < CLOSED_STATE ? "closing" :
                                                     (state == CLOSED_STATE ? "closed" : "opening"))));

}

void Portcullis::openFromInside() {
    ++state;
    printf("%s opened from inside.  %s = %d(%s)\n", this->label, (isActive ? "Active" : "Inactive"),
                                                                        state, (state == OPEN_STATE ? "open" :
                                                                        (state < CLOSED_STATE ? "closing" :
                                                                         (state == CLOSED_STATE ? "closed" : "opening"))));
}

void Portcullis::forceOpen() {
    state = OPEN_STATE;
    isActive = true;
    printf("%s entered.  %s = %d(%s)\n", this->label, (isActive ? "Active" : "Inactive"),
                                                                            state, (state == OPEN_STATE ? "open" :
                                                                            (state < CLOSED_STATE ? "closing" :
                                                                             (state == CLOSED_STATE ? "closed" : "opening"))));
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

