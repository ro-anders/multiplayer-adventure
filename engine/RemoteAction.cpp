


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

const char* PlayerPickupAction::CODE = "PP";

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
// PlayerResetAction
//

const char* PlayerResetAction::CODE = "PR";

PlayerResetAction::PlayerResetAction() :
RemoteAction(CODE) {}

PlayerResetAction::PlayerResetAction(int inSender) :
RemoteAction(CODE, inSender) {}

PlayerResetAction::~PlayerResetAction() {}


int PlayerResetAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "PR %d", sender);
    return numChars;
}

void PlayerResetAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d", type, &sender);
}


////////////////////////////////////////////////////////////////////////////////////////////
//
// PlayerWinAction
//

const char* PlayerWinAction::CODE = "PW";

PlayerWinAction::PlayerWinAction() :
RemoteAction(CODE) {}

PlayerWinAction::PlayerWinAction(int inSender, int inWinInRoom) :
RemoteAction(CODE, inSender),
winInRoom(inWinInRoom){}

PlayerWinAction::~PlayerWinAction() {}


int PlayerWinAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "PW %d %d", sender, winInRoom);
    return numChars;
}

void PlayerWinAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d", type, &sender, &winInRoom);
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

////////////////////////////////////////////////////////////////////////////////////////////
//
// PortcullisStateAction
//

const char* PortcullisStateAction::CODE = "GS";

PortcullisStateAction::PortcullisStateAction() :
RemoteAction(CODE) {}

PortcullisStateAction::PortcullisStateAction(int inSender, int inPortNumber, int inNewSate, bool inActive) :
RemoteAction(CODE, inSender),
portNumber(inPortNumber),
newState(inNewSate),
isActive(inActive) {}

PortcullisStateAction::~PortcullisStateAction() {}

int PortcullisStateAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "GS %d %d %d %d",
                           sender, portNumber, newState, (int)isActive);
    return numChars;
}

void PortcullisStateAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d", type, &sender, &portNumber, &newState, &isActive);
}


////////////////////////////////////////////////////////////////////////////////////////////
//
// BatMoveAction
//

const char* BatMoveAction::CODE = "BM";

BatMoveAction::BatMoveAction() :
MoveAction(CODE) {}

BatMoveAction::BatMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
MoveAction(CODE, inSender, inRoom, inPosx, inPosy, inVelx, inVely)
{}

BatMoveAction::~BatMoveAction() {}

int BatMoveAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    int numChars = sprintf(buffer, "BM %d %d %d %d %d %d", sender, room, posx, posy, velx, vely);
    return numChars;
}

void BatMoveAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d", type, &sender, &room, &posx, &posy, &velx, &vely);
}


////////////////////////////////////////////////////////////////////////////////////////////
//
// BatPickupAction
//

const char* BatPickupAction::CODE = "BP";

BatPickupAction::BatPickupAction() :
RemoteAction(CODE) {}

BatPickupAction::BatPickupAction(int inSender, int inPickupObject, int inPickupX, int inPickupY,
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

BatPickupAction::~BatPickupAction() {}


void BatPickupAction::setPickup(int inPickupObject, int inPickupX, int inPickupY) {
    pickupObject = inPickupObject;
    pickupX = inPickupX;
    pickupY = inPickupY;
    
}

void BatPickupAction::setDrop(int inDropObject, int inDropRoom, int inDropX, int inDropY) {
    dropObject = inDropObject;
    dropRoom = inDropRoom;
    dropX = inDropX;
    dropY = inDropY;
}


int BatPickupAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "BP %d %d %d %d %d %d %d %d",
                           sender, pickupObject, pickupX, pickupY, dropObject, dropRoom, dropX, dropY);
    return numChars;
}

void BatPickupAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d %d %d",
           type, &sender, &pickupObject, &pickupX, &pickupY, &dropObject, &dropRoom, &dropX, &dropY);
}



