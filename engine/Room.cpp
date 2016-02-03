
#include "Room.hpp"

#include <stdlib.h>
#include <strings.h>

ROOM::ROOM(const byte* inGraphicsData, byte inFlags, int inColor,
           int inRoomUp, int inRoomRight, int inRoomDown, int inRoomLeft, const char* inLabel) :
graphicsData(inGraphicsData),
flags(inFlags),
color(inColor),
roomUp(inRoomUp),
roomRight(inRoomRight),
roomDown(inRoomDown),
roomLeft(inRoomLeft) {

    label = (char*)malloc((strlen(inLabel)+1)*sizeof(char));
    strcpy(label, inLabel);
}

ROOM::~ROOM() {
    free(label);
}

void ROOM::setIndex(int inIndex) {
    index = inIndex;
}

bool ROOM::isNextTo(ROOM* otherRoom) {
    int index2 = otherRoom->index;
    return (((this->roomUp == index2) && (otherRoom->roomDown == index)) ||
            ((this->roomRight == index2) && (otherRoom->roomLeft == index)) ||
            ((this->roomDown == index2) && (otherRoom->roomUp == index)) ||
            ((this->roomLeft == index2) && (otherRoom->roomRight == index)));

}
