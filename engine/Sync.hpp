
#ifndef Sync_hpp
#define Sync_hpp

#include "ActionQueue.hpp"
#include "RemoteAction.hpp"

class Transport;

class Sync {
public:
    Sync(int numPlayers, int thisPlayer, Transport* transport);
    
    ~Sync();
    
    /**
     * Call this before the start of each frame.
     * Allows the syncer to correlate how many frames ago an action was performed.
     */
    void StartFrame();
    
    /**
     * This pulls messages off the socket until there are none waiting.
     * This does not process them, but demuxes them and puts them where they can be grabbed
     * when it is time to process that type of message.
     */
    void PullLatestMessages();
    
    /**
     *  Get the latest changes to another player.  Returns the last known state of a
     * player including their position and velocity.  Caller must delete this object.
     * If no changes have been received since the last call, this will return
     * null.
     */
    PlayerMoveAction* GetLatestBallSync(int player);
    
    /**
     * Get the next dragon action.  Caller must delete this object.
     * If no actions have been received, this will return null.
     */
    RemoteAction* GetNextDragonAction();
    
    /**
     * Get the next portcullis action.  Caller must delete this object.
     * If no actions have been received, this will return null.
     */
    PortcullisStateAction* GetNextPortcullisAction();
    
    /**
     * Get the next player pickup or player drop action.
     * If no actions have been received, this will return null.
     */
    PlayerPickupAction* GetNextPickupAction();
    
    /**
     * Broadcast an event to the other players
     * @param action an action to broadcast.  The Sync now owns this action and is responsible
     * for deleting it.
     */
    void BroadcastAction(RemoteAction* action);
    
private:
    Transport* transport;
    
    int thisPlayer = -1;
    
    int numPlayers;
    
    int MAX_MESSAGE_SIZE = 256;
    char sendBuffer[256];
    char receiveBuffer[256];
    
    ActionQueue dragonMoves;
    ActionQueue playerPickups;
    ActionQueue gateStateChanges;
    
    PlayerMoveAction** playersLastMove;
    
    int frameNum = 0;
    
    void RejectMessage(const char* message, const char* errorMsg);
    
    void handlePlayerMoveMessage(const char* message);

    void handlePlayerPickupMessage(const char* message);
    
    void handleDragonMoveMessage(const char* message);

    void handleDragonStateMessage(const char* message);

    void handlePortcullisStateMessage(const char* message);



};

#endif
