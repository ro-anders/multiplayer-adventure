
#include "Board.hpp"

#include "Ball.hpp"
#include "GameObject.hpp"

Board::Board() {
    numObjects = OBJECT_MAGNET+2;
    objects = (OBJECT**)malloc(numObjects*sizeof(OBJECT*));
    objects[numObjects-1] = new OBJECT("", (const byte*)0, 0, 0, 0, -1, 0, 0);  // #12 Null
    
    int MAX_PLAYERS = 3;
    players = (BALL**)malloc(MAX_PLAYERS * sizeof(BALL*));
    numPlayers = 0;
}

Board::~Board() {
    for(int ctr=0; ctr<numObjects; ++ctr) {
        delete objects[ctr];
    }
    delete objects;
    
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        delete players[ctr];
    }
    delete players;
    
}

void Board::addObject(int pkey, OBJECT* object) {
    objects[pkey] = object;
    object->setBoard(this, pkey);
}

OBJECT* Board::getObject(int pkey) {
    return objects[pkey];
}

int Board::getNumPlayers() {
    return numPlayers;
}

void Board::addPlayer(BALL *newPlayer) {
    players[numPlayers] = newPlayer;
    ++numPlayers;
}

BALL* Board::getPlayer(int playerNum) {
    return players[playerNum];
}

bool Board::HitTestRects(int ax, int ay, int awidth, int aheight,
                  int bx, int by, int bwidth, int bheight)
{
    bool intersects = true;
    
    if ( ((ay-aheight) >= by) || (ay <= (by-bheight)) || ((ax+awidth) <= bx) || (ax >= (bx+bwidth)) )
    {
        // Does not intersect
        intersects = false;
    }
    // else must intersect
    
    return intersects;
}



