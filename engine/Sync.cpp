         

#ifdef WIN32
//#include "stdafx.h"
#endif

#include "Sync.hpp"

#include "ActionQueue.hpp"
#include "RemoteAction.hpp"
#include "Transport.hpp"


// The existing adventure port doesn't do a lot of dynamic memory allocation, but instead 
// allocates everything up front.  We'll stick to that paradigm as much as possible.


Sync::Sync(int inNumPlayers, int inThisPlayer, Transport* inTransport) :
    numPlayers(inNumPlayers),
    thisPlayer(inThisPlayer),
    gameWon(NULL),
    transport(inTransport)
{
    playersLastMove = new PlayerMoveAction*[numPlayers];
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        playersLastMove[ctr] = NULL;
    }
    
}

void Sync::StartFrame() {
	++frameNum;
}

void Sync::BroadcastAction(RemoteAction* action) {
    action->serialize(sendBuffer, MAX_MESSAGE_SIZE);
    transport->sendPacket(sendBuffer);
}

void Sync::RejectMessage(const char* message, const char* errorMsg) {
    printf("Cannot process message - %s: %s", errorMsg, message);
}

void Sync::handleBatMoveMessage(const char* message) {
    BatMoveAction* nextAction = new BatMoveAction();
    nextAction->deserialize(receiveBuffer);
    batMoves.enQ(nextAction);
}

void Sync::handleBatPickupMessage(const char* message) {
    BatPickupAction* nextAction = new BatPickupAction();
    nextAction->deserialize(receiveBuffer);
    batMoves.enQ(nextAction);
}



void Sync::handlePlayerMoveMessage(const char* message) {
    PlayerMoveAction* nextAction = new PlayerMoveAction();
    nextAction->deserialize(receiveBuffer);
    int messageSender = nextAction->sender;
    if (playersLastMove[messageSender] != NULL) {
        delete playersLastMove[messageSender];
    }
    playersLastMove[messageSender] = nextAction;
}

void Sync::handleDragonMoveMessage(const char* message) {
    DragonMoveAction* nextAction = new DragonMoveAction();
    nextAction->deserialize(receiveBuffer);
    dragonMoves.enQ(nextAction);
}

void Sync::handleDragonStateMessage(const char* message) {
    DragonStateAction* nextAction = new DragonStateAction();
    nextAction->deserialize(receiveBuffer);
    dragonMoves.enQ(nextAction);
}

void Sync::handlePlayerPickupMessage(const char* message) {
    PlayerPickupAction* nextAction = new PlayerPickupAction();
    nextAction->deserialize(receiveBuffer);
    playerPickups.enQ(nextAction);
}

void Sync::handlePlayerResetMessage(const char* message) {
    PlayerResetAction* nextAction = new PlayerResetAction();
    nextAction->deserialize(receiveBuffer);
    playerResets.enQ(nextAction);
}

void Sync::handlePortcullisStateMessage(const char* message) {
    PortcullisStateAction* nextAction = new PortcullisStateAction();
    nextAction->deserialize(receiveBuffer);
    gateStateChanges.enQ(nextAction);
}

void Sync::handlePlayerWinMessage(const char* message) {
    // Don't know how we'd get this, but we ignore any win message after we receive the first one.
    if (gameWon == NULL) {
        PlayerWinAction* nextAction = new PlayerWinAction();
        nextAction->deserialize(receiveBuffer);
        gameWon = nextAction;
    }
}

void Sync::PullLatestMessages() {
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
                    case 'P': {
                        handlePlayerPickupMessage(receiveBuffer);
                        break;
                    }
                    case 'R': {
                        handlePlayerResetMessage(receiveBuffer);
                        break;
                    }
                    case 'W': {
                        handlePlayerWinMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type P%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            case 'B':
                switch (receiveBuffer[1]) {
                    case 'M': {
                        handleBatMoveMessage(receiveBuffer);
                        break;
                    }
                    case 'P': {
                        handleBatPickupMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type B%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            case 'D':
                switch (receiveBuffer[1]) {
                    case 'M': {
                        handleDragonMoveMessage(receiveBuffer);
                        break;
                    }
                    case 'S': {
                        handleDragonStateMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type D%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            case 'G':
                switch (receiveBuffer[1]) {
                    case 'S': {
                        handlePortcullisStateMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type C%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            default:
                printf("Message with unknown message type %c*: %s\n", receiveBuffer[0], receiveBuffer);
        }
        
        numChars = transport->getPacket(receiveBuffer, MAX_MESSAGE_SIZE);
    }
}

PlayerMoveAction* Sync::GetLatestBallSync(int player) {
    PlayerMoveAction* rtn = playersLastMove[player];
    playersLastMove[player] = NULL;
    return rtn;
}

PlayerResetAction* Sync::GetNextResetAction() {
    RemoteAction* next = NULL;
    if (!playerResets.isEmpty()) {
        next = playerResets.deQ();
    }
    return (PlayerResetAction*)next;
}

RemoteAction* Sync::GetNextDragonAction() {
    RemoteAction* next = NULL;
    if (!dragonMoves.isEmpty()) {
        next = dragonMoves.deQ();
    }
    return next;
}

PortcullisStateAction* Sync::GetNextPortcullisAction() {
    RemoteAction* next = NULL;
    if (!gateStateChanges.isEmpty()) {
        next = gateStateChanges.deQ();
    }
    return (PortcullisStateAction*)next;
}

RemoteAction* Sync::GetNextBatAction() {
    RemoteAction* next = NULL;
    if (!batMoves.isEmpty()) {
        next = batMoves.deQ();
    }
    return next;
}

PlayerPickupAction* Sync::GetNextPickupAction() {
    PlayerPickupAction* next = NULL;
    if (!playerPickups.isEmpty()) {
        next = (PlayerPickupAction*)playerPickups.deQ();
    }
    return next;

}

PlayerWinAction* Sync::GetGameWon() {
    PlayerWinAction* next = gameWon;
    gameWon = NULL;
    return next;
}
