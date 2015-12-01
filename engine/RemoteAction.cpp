

#include "RemoteAction.hpp"

////////////////////////////////////////////////////////////////////////////////////////////
//
// RemoteAction
//

////////////////////////////////////////////////////////////////////////////////////////////
//
// MoveAction
//

MoveAction::MoveAction() {}

MoveAction::~MoveAction() {}

MoveAction::MoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) {
    sender = inSender;
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

PlayerMoveAction::PlayerMoveAction() {}

PlayerMoveAction::PlayerMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) :
    MoveAction(inSender, inRoom, inPosx, inPosy, inVelx, inVely)
{}

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
// PlayerMoveAction
//

DragonMoveAction::DragonMoveAction() {}

DragonMoveAction::DragonMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely,
                                   int inDragonNum, int inDistance) :
    MoveAction(inSender, inRoom, inPosx, inPosy, inVelx, inVely),
    dragonNum(inDragonNum),
    distance(inDistance)
{}

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