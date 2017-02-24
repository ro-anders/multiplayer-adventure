


#include "RemoteAction.hpp"

////////////////////////////////////////////////////////////////////////////////////////////
//
// RemoteAction
//

RemoteAction::RemoteAction(const char* inCode) :
typeCode(inCode) {}

RemoteAction::~RemoteAction() {}

void RemoteAction::setSender(int inSender) {
    sender = inSender;
}

////////////////////////////////////////////////////////////////////////////////////////////
//
// MoveAction
//

MoveAction::MoveAction(const char* inCode) :
RemoteAction(inCode) {}


MoveAction::~MoveAction() {}

MoveAction::MoveAction(const char* inCode, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
RemoteAction(inCode)
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

PlayerMoveAction::PlayerMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
MoveAction(CODE, inRoom, inPosx, inPosy, inVelx, inVely)
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

PlayerPickupAction::PlayerPickupAction(int inPickupObject, int inPickupX, int inPickupY,
                                       int inDropObject, int inDropRoom, int inDropX, int inDropY) :
RemoteAction(CODE),
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

PlayerWinAction::PlayerWinAction(int inWinInRoom) :
RemoteAction(CODE),
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

DragonMoveAction::DragonMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely,
                                   int inDragonNum, int inDistance) :
MoveAction(CODE, inRoom, inPosx, inPosy, inVelx, inVely),
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

DragonStateAction::DragonStateAction(int inDragonNum, int inState, int inRoom, int inPosx, int inPosy) :
RemoteAction(CODE),
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

PortcullisStateAction::PortcullisStateAction(int inPortPkey, int inNewSate, bool inAllowsEntry) :
RemoteAction(CODE),
portPkey(inPortPkey),
newState(inNewSate),
allowsEntry(inAllowsEntry) {}

PortcullisStateAction::~PortcullisStateAction() {}

int PortcullisStateAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "GS %d %d %d %d",
                           sender, portPkey, newState, (int)allowsEntry);
    return numChars;
}

void PortcullisStateAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d", type, &sender, &portPkey, &newState, &allowsEntry);
}


////////////////////////////////////////////////////////////////////////////////////////////
//
// BatMoveAction
//

const char* BatMoveAction::CODE = "BM";

BatMoveAction::BatMoveAction() :
MoveAction(CODE) {}

BatMoveAction::BatMoveAction(int inRoom, int inPosx, int inPosy, int inVelx, int inVely, int inDistance) :
MoveAction(CODE, inRoom, inPosx, inPosy, inVelx, inVely),
distance(inDistance)
{}

BatMoveAction::~BatMoveAction() {}

int BatMoveAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    int numChars = sprintf(buffer, "BM %d %d %d %d %d %d %d", sender, room, posx, posy, velx, vely, distance);
    return numChars;
}

void BatMoveAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d %d", type, &sender, &room, &posx, &posy, &velx, &vely, &distance);
}


////////////////////////////////////////////////////////////////////////////////////////////
//
// BatPickupAction
//

const char* BatPickupAction::CODE = "BP";

BatPickupAction::BatPickupAction() :
RemoteAction(CODE) {}

BatPickupAction::BatPickupAction(int inPickupObject, int inPickupX, int inPickupY,
                                 int inDropObject, int inDropRoom, int inDropX, int inDropY) :
RemoteAction(CODE),
pickupObject(inPickupObject),
pickupX(inPickupX),
pickupY(inPickupY),
dropObject(inDropObject),
dropRoom(inDropRoom),
dropX(inDropX),
dropY(inDropY)
{}

BatPickupAction::~BatPickupAction() {}


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

////////////////////////////////////////////////////////////////////////////////////////////
//
// ObjectMoveAction
//

const char* ObjectMoveAction::CODE = "MO";

ObjectMoveAction::ObjectMoveAction() :
RemoteAction(CODE) {}

ObjectMoveAction::ObjectMoveAction(int inObject, int inRoom, int inX, int inY) :
RemoteAction(CODE),
object(inObject),
room(inRoom),
x(inX),
y(inY)
{}

ObjectMoveAction::~ObjectMoveAction() {}


int ObjectMoveAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    // TODO - Reuse base class serialize
    int numChars = sprintf(buffer, "MO %d %d %d %d %d", sender, object, room, x, y);
    return numChars;
}

void ObjectMoveAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d", type, &sender, &object, &room, &x, &y);
}




