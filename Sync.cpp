

#include "stdafx.h"
#include "Sync.h"

// The existing adventure port doesn't do a lot of dynamic memory allocation, but instead 
// allocates everything up front.  We'll stick to that paradigm as much as possible.

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

// Used only for mocking another player
static int lastX = 0;
static int lastY = 0;

void Sync_StartFrame() {
	++frameNum;
}

void Sync_SetBall(int room, int posx, int posy, int velx, int vely) {
	// TODO: Send this to the other games.

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

	if (!received[player]) {
		return NULL;
	}
	else {
		received[player] = false;
		current[player] = 1 - current[player];
		return &ballSyncStates[player][current[player]];
	}
}

void mockOtherPlayer() {
	// TODO - Not finished yet.
	int newX = 0;
	int newY = 0;
	if (GetAsyncKeyState(0x41/*A*/)) {
		newX = -1;
	}
	else if (GetAsyncKeyState(0x44/*D*/)) {
		newX = 1;
	}
	else if (GetAsyncKeyState(0x57/*W*/)) {
		newY = 1;
	}
	else if (GetAsyncKeyState(0x58/*X*/)) {
		newY = -1;
	}
	else {
		newX = lastX;
		newY = lastY;
	}

}

