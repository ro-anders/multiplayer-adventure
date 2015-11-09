
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

/**
 * Call this before the start of each frame.
 * Allows the syncer to correlate how many frames ago an action was performed.
 */
void Sync_StartFrame();

/**
 *  Get the latest changes to another player.  Returns the last known state of a
 * player including their position and velocity.  Caller does not need to worry about
 * memory management, but the returned pointer is only valid until then next call of
 * this method.  If no changes have been received since the last call, this will return
 * null.
 */
BALL_SYNC* Sync_GetLatestBallSync(int player);

/**
 * Notify other players of changes to your state.
 */
void Sync_SetBall(int room, int posx, int posy, int velx, int vely);
