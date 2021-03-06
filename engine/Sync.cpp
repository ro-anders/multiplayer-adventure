#include "sys.h"
#include "Sync.hpp"

#include "ActionQueue.hpp"
#include "Logger.hpp"
#include "RemoteAction.hpp"
#include "Transport.hpp"
#include "Sys.hpp"


// The existing adventure port doesn't do a lot of dynamic memory allocation, but instead 
// allocates everything up front.  We'll stick to that paradigm as much as possible.

const int Sync::MAX_MESSAGE_SIZE = 256;

Sync::Sync(int inNumPlayers, int inThisPlayer, Transport* inTransport) :
    numPlayers(inNumPlayers),
    thisPlayer(inThisPlayer),
    gameWon(NULL),
    transport(inTransport),
    frameNum(0)
{
    playersLastMove = new PlayerMoveAction*[numPlayers];
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        playersLastMove[ctr] = NULL;
    }
    msgsRcvdFromPlayer = new int[numPlayers];
    resetMessagesReceived();
}

Sync::~Sync() {
    delete[] playersLastMove;
}

void Sync::StartFrame() {
	++frameNum;
}

int Sync::getFrameNumber() {
    return frameNum;
}

int Sync::getMessagesReceived(int player) {
    return msgsRcvdFromPlayer[player];
}

void Sync::resetMessagesReceived() {
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        msgsRcvdFromPlayer[ctr] = 0;
    }
}

void Sync::handled(RemoteAction* action) {
    // Record we got a message from the sender
    if (action != NULL) {
        int sender = action->sender;
        if ((sender >= 0) && (sender < numPlayers)) {
            ++msgsRcvdFromPlayer[sender];
        }
    }
}

void Sync::BroadcastAction(RemoteAction* action) {
    if (action != NULL) {
        action->setSender(thisPlayer);
        action->serialize(sendBuffer, MAX_MESSAGE_SIZE);
        if (transport != NULL) {
            transport->sendPacket(sendBuffer);
        }
        
        Logger::log() << "Sent \"" << sendBuffer << "\" on frame #" << frameNum << Logger::EOM;
        
        delete action;
    }
}

void Sync::RejectMessage(const char* message, const char* errorMsg) {
    Logger::logError() << "Cannot process message - " << errorMsg << ": " << message << Logger::EOM;
}

void Sync::handleBatMoveMessage(const char* message) {
    BatMoveAction* nextAction = new BatMoveAction();
    nextAction->deserialize(receiveBuffer);
    batMoves.enQ(nextAction);
    handled(nextAction);
}

void Sync::handleBatPickupMessage(const char* message) {
    BatPickupAction* nextAction = new BatPickupAction();
    nextAction->deserialize(receiveBuffer);
    batMoves.enQ(nextAction);
    handled(nextAction);
}

void Sync::handlePlayerMoveMessage(const char* message) {
    PlayerMoveAction* nextAction = new PlayerMoveAction();
    nextAction->deserialize(receiveBuffer);
    int messageSender = nextAction->sender;
    if (playersLastMove[messageSender] != NULL) {
        delete playersLastMove[messageSender];
    }
    playersLastMove[messageSender] = nextAction;
    handled(nextAction);
}

void Sync::handleDragonMoveMessage(const char* message) {
    DragonMoveAction* nextAction = new DragonMoveAction();
    nextAction->deserialize(receiveBuffer);
    dragonMoves.enQ(nextAction);
    handled(nextAction);
}

void Sync::handleDragonStateMessage(const char* message) {
    DragonStateAction* nextAction = new DragonStateAction();
    nextAction->deserialize(receiveBuffer);
    dragonMoves.enQ(nextAction);
    handled(nextAction);
}

void Sync::handlePlayerPickupMessage(const char* message) {
    PlayerPickupAction* nextAction = new PlayerPickupAction();
    nextAction->deserialize(receiveBuffer);
    playerPickups.enQ(nextAction);
    handled(nextAction);
}

void Sync::handlePlayerResetMessage(const char* message) {
    PlayerResetAction* nextAction = new PlayerResetAction();
    nextAction->deserialize(receiveBuffer);
    playerResets.enQ(nextAction);
    handled(nextAction);
}

void Sync::handlePortcullisStateMessage(const char* message) {
    PortcullisStateAction* nextAction = new PortcullisStateAction();
    nextAction->deserialize(receiveBuffer);
    gateStateChanges.enQ(nextAction);
    handled(nextAction);
}

void Sync::handlePlayerWinMessage(const char* message) {
    // Don't know how we'd get this, but we ignore any win message after we receive the first one.
    if (gameWon == NULL) {
        PlayerWinAction* nextAction = new PlayerWinAction();
        nextAction->deserialize(receiveBuffer);
        gameWon = nextAction;
        handled(nextAction);
    }
}

void Sync::handlePingMessage(const char* message) {
    PingAction* nextAction = new PingAction();
    nextAction->deserialize(receiveBuffer);
    // Don't need to do anything with the ping except mark that it was handled.
    handled(nextAction);
}

void Sync::handleMazeSetupObjectMessage(const char* message) {
    ObjectMoveAction* nextAction = new ObjectMoveAction();
    nextAction->deserialize(receiveBuffer);
    mazeSetupActions.enQ(nextAction);
    handled(nextAction);
}

int Sync::pullNextPacket(char* buffer, int bufferSize) {
    int numChars = (transport == NULL ? 0 : transport->getPacket(buffer, bufferSize));
    if (numChars > 0) {
        Logger::log() << "Received \"" << buffer << "\" on frame #" << frameNum << Logger::EOM;
    }
    return numChars;
}


void Sync::PullLatestMessages() {
    const int TYPE_LENGTH = 2;
    int numChars = pullNextPacket(receiveBuffer, MAX_MESSAGE_SIZE);
    while(numChars >= TYPE_LENGTH+1) {
        char type[TYPE_LENGTH+1];
        // First two characters are the type of message
        for(int ctr=0; ctr<TYPE_LENGTH; ++ctr) {
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
                        printf("Message with unknown message type G%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            case 'M':
                switch (receiveBuffer[1]) {
                    case 'O': {
                        handleMazeSetupObjectMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type M%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            case 'X':
                switch (receiveBuffer[1]) {
                    case 'X': {
                        handlePingMessage(receiveBuffer);
                        break;
                    }
                    default:
                        printf("Message with unknown message type X%c: %s\n", receiveBuffer[1], receiveBuffer);
                }
                break;
            default:
                printf("Message with unknown message type %c*: %s\n", receiveBuffer[0], receiveBuffer);
        }
        
        numChars = pullNextPacket(receiveBuffer, MAX_MESSAGE_SIZE);
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

ObjectMoveAction* Sync::GetNextSetupAction() {
    ObjectMoveAction* next = NULL;
    if (!mazeSetupActions.isEmpty()) {
        next = (ObjectMoveAction*)mazeSetupActions.deQ();
    }
    return next;
    
}


