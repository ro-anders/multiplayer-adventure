
#include "Room.hpp"

#include <stdlib.h>
#include <string.h>

ROOM::ROOM(const byte* inGraphicsData, byte inFlags, int inColor,
           int inRoomUp, int inRoomRight, int inRoomDown, int inRoomLeft, const char* inLabel, RandomVisibility inVis) :
graphicsData(inGraphicsData),
flags(inFlags),
color(inColor),
roomUp(inRoomUp),
roomRight(inRoomRight),
roomDown(inRoomDown),
roomLeft(inRoomLeft),
visibility(inVis) {

    label = new char[strlen(inLabel)+1];
    strcpy(label, inLabel);
}

ROOM::~ROOM() {
    delete[] label;
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
