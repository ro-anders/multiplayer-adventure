         

#ifdef WIN32
#include "stdafx.h"
#endif

#include "Sync.h"

#include "ActionQueue.hpp"
#include "RemoteAction.hpp"
#include "Transport.hpp"


// The existing adventure port doesn't do a lot of dynamic memory allocation, but instead 
// allocates everything up front.  We'll stick to that paradigm as much as possible.

static Transport* transport;

static int thisPlayer = -1;

static int numPlayers;
static int numOtherPlayers;

static int MAX_MESSAGE_SIZE = 256;
static char sendBuffer[256];
static char receiveBuffer[256];

static ActionQueue dragonMoves;

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

void Sync_RejectMessage(const char* message, const char* errorMsg) {
    printf("Cannot process message - %s: %s", errorMsg, message);
}

void handlePlayerMoveMessage(const char* message) {
    PlayerMoveAction* nextAction = new PlayerMoveAction();
    nextAction->deserialize(receiveBuffer);
    int messageSender = nextAction->sender;
    if (playersLastMove[messageSender-1] != 0x0) {
        delete playersLastMove[messageSender-1];
    }
    playersLastMove[messageSender-1] = nextAction;
}

void handleDragonMoveMessage(const char* message) {
    DragonMoveAction* nextAction = new DragonMoveAction();
    nextAction->deserialize(receiveBuffer);
    dragonMoves.enQ(nextAction);
}

void Sync_PullLatestMessages() {
    int numChars = transport->getPacket(receiveBuffer, MAX_MESSAGE_SIZE);
    while(numChars >= 4) {
        char type[5] = "XXXX";
        // First four characters are the type of message
        for(int ctr=0; ctr<4; ++ctr) {
            type[ctr] = receiveBuffer[ctr];
        }
        switch (receiveBuffer[0]) {
            case 'P':
                switch (receiveBuffer[1]) {
                    case 'M': {
                        handlePlayerMoveMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type P%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            case 'D':
                switch (receiveBuffer[1]) {
                    case 'M': {
                        handleDragonMoveMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type D%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            default:
                printf("Message with unknown message type %c*: %s\n", receiveBuffer[0], receiveBuffer);
        }
        
        numChars = transport->getPacket(receiveBuffer, MAX_MESSAGE_SIZE);
    }
}

PlayerMoveAction* Sync_GetLatestBallSync(int player) {
    PlayerMoveAction* rtn = playersLastMove[player-1];
    playersLastMove[player-1] = 0x0;
    return rtn;
}

DragonMoveAction* Sync_GetNextDragonAction() {
    DragonMoveAction* next = NULL;
    if (!dragonMoves.isEmpty()) {
        next = (DragonMoveAction*)dragonMoves.deQ();
    }
    return next;
}

