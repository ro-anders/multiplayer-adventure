
#include "RemoteAction.hpp"

typedef struct BALL_SYNC
{
	int player;				// The player's number - 1-3
	int room;				// The room the player was in
	int posx;				// The x-coordinate of the player in the room
	int posy;				// The y-coordinate of the player in the room
	int velx;				// -1 for moving left, 1 for right, and 0 for still or just up/down
	int vely;				// -1 for down, 1 for up, and 0 for still or just left/right
	int relativeFrame;		// A guess to how many frames ago the player made this change in movement
}BALL_SYNC;

class Transport;


/**
 * Call to setup the sync tool.  Like a constructor.
 */
void Sync_Setup(int numPlayers, int thisPlayer, Transport* transport);

/**
 * Call this before the start of each frame.
 * Allows the syncer to correlate how many frames ago an action was performed.
 */
void Sync_StartFrame();

/**
 * This pulls messages off the socket until there are none waiting.
 * This does not process them, but demuxes them and puts them where they can be grabbed 
 * when it is time to process that type of message.
 */
void Sync_PullLatestMessages();

/**
 *  Get the latest changes to another player.  Returns the last known state of a
 * player including their position and velocity.  Caller must delete this object.
 * If no changes have been received since the last call, this will return
 * null.
 */
PlayerMoveAction* Sync_GetLatestBallSync(int player);

/**
 * Get the next dragon action.  Caller must delete this object.
 * If no actions have been received, this will return null.
 */
DragonMoveAction* Sync_GetNextDragonAction();


/**
 * Broadcast an event to the other players
 * @param action an action to broadcast.  The Sync now owns this action and is responsible
 * for deleting it.
 */
void Sync_BroadcastAction(RemoteAction* action);
