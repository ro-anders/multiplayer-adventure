

#ifdef WIN32
#include "stdafx.h"
#endif

#include "Sync.h"
#include "Transport.hpp"


// The existing adventure port doesn't do a lot of dynamic memory allocation, but instead 
// allocates everything up front.  We'll stick to that paradigm as much as possible.

static Transport* transport;

// The state of other players is kept in a an array, where each player gets two BALL_SYNC structures, the one the player
// last requested and the one the player will receive on the next request.


static BALL_SYNC ballSyncStates[2][2] = {
	{
		{ 1 /*player*/, 0x11 /*room*/, 0x50 * 2 /*posx*/, 0x20 * 2 /*posy*/, 0 /*velx*/, 0 /*vely*/, 0 /*relativeFrame*/ },
		{ 1 /*player*/, 0x11 /*room*/, 0x55 * 2 /*posx*/, 0x20 * 2 /*posy*/, 0 /*velx*/, 0 /*vely*/, 0 /*relativeFrame*/ }
	}, {
		{ 2 /*player*/, 0x11 /*room*/, 0x50 * 2 /*posx*/, 0x20 * 2 /*posy*/, 0 /*velx*/, 0 /*vely*/, 0 /*relativeFrame*/ },
		{ 2 /*player*/, 0x11 /*room*/, 0x55 * 2 /*posx*/, 0x20 * 2 /*posy*/, 0 /*velx*/, 0 /*vely*/, 0 /*relativeFrame*/ }
	}
};

static int current[] = { 0, 0 };
static bool received[] = { false, false };

static int frameNum = 0;

void Sync_Setup(Transport* inTransport) {
    transport = inTransport;
}

void Sync_StartFrame() {
	++frameNum;
}

void Sync_SetBall(int room, int posx, int posy, int velx, int vely) {
	// TODO: Send this to the other games.
    transport->sendPacket("updating ball state");

	// Mock a player2 by having it do exactly what player1 does only 20 pixels to the right.
	int slot = 1 - current[0];
	ballSyncStates[0][slot].room = room;
	ballSyncStates[0][slot].posx = posx + 20;
	ballSyncStates[0][slot].posy = posy - 5;
	ballSyncStates[0][slot].velx = velx;
	ballSyncStates[0][slot].vely = vely;
	ballSyncStates[0][slot].relativeFrame = 0;
	if ((ballSyncStates[0][slot].velx != ballSyncStates[0][1 - slot].velx) ||
		(ballSyncStates[0][slot].vely != ballSyncStates[0][1 - slot].vely)) {
		received[0] = true;
	}
}

BALL_SYNC* Sync_GetLatestBallSync(int player) {
    char buffer[256];
    transport->getPacket(buffer, 256);
    
	if (!received[player]) {
		return 0x0;
	}
	else {
		received[player] = false;
		current[player] = 1 - current[player];
		return &ballSyncStates[player][current[player]];
	}
}

