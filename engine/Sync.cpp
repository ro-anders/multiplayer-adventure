         

#ifdef WIN32
#include "stdafx.h"
#endif

#include "Sync.h"
#include "Transport.hpp"


// The existing adventure port doesn't do a lot of dynamic memory allocation, but instead 
// allocates everything up front.  We'll stick to that paradigm as much as possible.

static Transport* transport;

static int thisPlayer = 0;

static int numPlayers;
static int numOtherPlayers;

static int MAX_MESSAGE_SIZE = 256;
static char sendBuffer[256];
static char receiveBuffer[256];

static PlayerMoveAction** playersLastMove;

static int frameNum = 0;

void Sync_Setup(int inNumPlayers, int inThisPlayer, Transport* inTransport) {
    numPlayers = inNumPlayers;
    numOtherPlayers = numPlayers - 1;
    thisPlayer = inThisPlayer;
    transport = inTransport;
    
    playersLastMove = new PlayerMoveAction*[numPlayers];
    for(int ctr=0; ctr<numOtherPlayers; ++ctr) {
        playersLastMove[ctr] = 0x0;
    }
}

void Sync_StartFrame() {
	++frameNum;
}

void Sync_BroadcastAction(RemoteAction* action) {
    action->serialize(sendBuffer, MAX_MESSAGE_SIZE);
    transport->sendPacket(sendBuffer);
}

void Sync_PullLatestMessages() {
    int numChars = transport->getPacket(receiveBuffer, MAX_MESSAGE_SIZE);
    while(numChars > 0) {
        // TODO: We know it's a Player Move action - that's the only one so far
        PlayerMoveAction* nextAction = new PlayerMoveAction();
        nextAction->deserialize(receiveBuffer);
        int messageSender = nextAction->sender;
        if (playersLastMove[messageSender-1] != 0x0) {
            delete playersLastMove[messageSender-1];
        }
        playersLastMove[messageSender-1] = nextAction;
        
        numChars = transport->getPacket(receiveBuffer, MAX_MESSAGE_SIZE);
    }
}

PlayerMoveAction* Sync_GetLatestBallSync(int player) {
    PlayerMoveAction* rtn = playersLastMove[player-1];
    playersLastMove[player-1] = 0x0;
    return rtn;
}

////////////////////////////////////////////////////////////////////////////////////////////
//
// RemoteAction
//

////////////////////////////////////////////////////////////////////////////////////////////
//
// PlayerMoveAction
//

PlayerMoveAction::PlayerMoveAction() {}

PlayerMoveAction::PlayerMoveAction(int inSender, int inRoom, int inPosx, int inPosy, int inVelx, int inVely) {
    sender = inSender;
    room = inRoom;
    posx = inPosx;
    posy = inPosy;
    velx = inVelx;
    vely = inVely;
}

int PlayerMoveAction::serialize(char* buffer, int bufferLength) {
    // TODO - Right now we are ignoring bufferLength
    //int numChars = sprintf(buffer, "BALL-sd%d-rm%d-px%d-py%d-vx%d-vy%d", sender, room, posx, posy, velx, vely);
    int numChars = sprintf(buffer, "BALL %d %d %d %d %d %d", sender, room, posx, posy, velx, vely);
    printf("Serialized ball state to %d chars: %s", numChars, buffer);
    return numChars;
}

void PlayerMoveAction::deserialize(const char *message) {
    char type[8];
    sscanf(message, "%s %d %d %d %d %d %d", type, &sender, &room, &posx, &posy, &velx, &vely);
}