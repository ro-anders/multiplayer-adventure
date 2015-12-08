

#include "RemoteAction.hpp"

////////////////////////////////////////////////////////////////////////////////////////////
//
// RemoteAction
//

RemoteAction::RemoteAction(const char* inCode) :
  typeCode(inCode) {}

RemoteAction::RemoteAction(const char* inCode, int inSender) :
  typeCode(inCode),
  sender(inSender) {}

RemoteAction::~RemoteAction() {}

////////////////////////////////////////////////////////////////////////////////////////////
//
// MoveAction
//

MoveAction::MoveAction(const char* inCode) :
  RemoteAction(inCode) {}


MoveAction::~MoveAction() {}

MoveAction::MoveAction(const char* inCode, int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
    RemoteAction(inCode, inSender)
{
    room = inRoom;
    posx = inPosx;
    posy = inPosy;
    velx = inVelx;
    vely = inVely;
}

////////////////////////////////////////////////////////////////////////////////////////////
//
// PlayerMoveAction
//

const char* PlayerMoveAction::CODE = "PM";

PlayerMoveAction::PlayerMoveAction() :
  MoveAction(CODE) {}

PlayerMoveAction::PlayerMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
    MoveAction(CODE, inSender, inRoom, inPosx, inPosy, inVelx, inVely)
{}

PlayerMoveAction::~PlayerMoveAction() {}

int PlayerMoveAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    int numChars = sprintf(buffer, "PM %d %d %d %d %d %d", sender, room, posx, posy, velx, vely);
    return numChars;
}

void PlayerMoveAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d", type, &sender, &room, &posx, &posy, &velx, &vely);
}

////////////////////////////////////////////////////////////////////////////////////////////
//
// PlayerPickupAction
//

const char* PlayerPickupAction::CODE = "DM";

PlayerPickupAction::PlayerPickupAction() :
  RemoteAction(CODE) {}

PlayerPickupAction::PlayerPickupAction(int inSender, int inPickupObject, int inPickupX, int inPickupY,
                                       int inDropObject, int inDropRoom, int inDropX, int inDropY) :
  RemoteAction(CODE, inSender),
  pickupObject(inPickupObject),
  pickupX(inPickupX),
  pickupY(inPickupY),
  dropObject(inDropObject),
  dropRoom(inDropRoom),
  dropX(inDropX),
  dropY(inDropY)
{}

PlayerPickupAction::~PlayerPickupAction() {}


void PlayerPickupAction::setPickup(int inPickupObject, int inPickupX, int inPickupY) {
    pickupObject = inPickupObject;
    pickupX = inPickupX;
    pickupY = inPickupY;
    
}

void PlayerPickupAction::setDrop(int inDropObject, int inDropRoom, int inDropX, int inDropY) {
    dropObject = inDropObject;
    dropRoom = inDropRoom;
    dropX = inDropX;
    dropY = inDropY;
}


int PlayerPickupAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "PP %d %d %d %d %d %d %d %d",
                           sender, pickupObject, pickupX, pickupY, dropObject, dropRoom, dropX, dropY);
    return numChars;
}

void PlayerPickupAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d %d %d",
           type, &sender, &pickupObject, &pickupX, &pickupY, &dropObject, &dropRoom, &dropX, &dropY);
}


////////////////////////////////////////////////////////////////////////////////////////////
//
// DragonMoveAction
//

const char* DragonMoveAction::CODE = "DM";

DragonMoveAction::DragonMoveAction() :
  MoveAction(CODE) {}

DragonMoveAction::DragonMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely,
                                   int inDragonNum, int inDistance) :
  MoveAction(CODE, inSender, inRoom, inPosx, inPosy, inVelx, inVely),
  dragonNum(inDragonNum),
  distance(inDistance)
{}

DragonMoveAction::~DragonMoveAction() {}

int DragonMoveAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "DM %d %d %d %d %d %d %d %d",
                           sender, room, posx, posy, velx, vely, dragonNum, distance);
    return numChars;
}

void DragonMoveAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d %d %d",
           type, &sender, &room, &posx, &posy, &velx, &vely, &dragonNum, &distance);
}


////////////////////////////////////////////////////////////////////////////////////////////
//
// DragonStateAction
//

const char* DragonStateAction::CODE = "DS";

DragonStateAction::DragonStateAction() :
  RemoteAction(CODE) {}

DragonStateAction::DragonStateAction(int inSender, int inDragonNum, int inState, int inRoom, int inPosx, int inPosy) :
  RemoteAction(CODE, inSender),
  dragonNum(inDragonNum),
  newState(inState),
  room(inRoom),
  posx(inPosx),
  posy(inPosy)
{}

DragonStateAction::~DragonStateAction() {}

int DragonStateAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "DS %d %d %d %d %d %d",
                           sender, dragonNum, newState, room, posx, posy);
    return numChars;
}

void DragonStateAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d",
           type, &sender, &dragonNum, &newState, &room, &posx, &posy);
}

