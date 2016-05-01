
#include "Board.hpp"

#include "Ball.hpp"
#include "GameObject.hpp"

Board::Board(int inScreenWidth, int inScreenHeight):
 screenWidth(inScreenWidth),
 screenHeight(inScreenHeight) {
     
    numObjects = OBJECT_MAGNET+2;
    objects = new OBJECT*[numObjects];
    objects[numObjects-1] = new OBJECT("", (const byte*)0, 0, 0, 0, -1, 0, 0);  // #12 Null
    
    int MAX_PLAYERS = 3;
    players = new BALL*[MAX_PLAYERS];
    numPlayers = 0;
}

Board::~Board() {
    for(int ctr=0; ctr<numObjects; ++ctr) {
        delete objects[ctr];
    }
    delete[] objects;
    
    for(int ctr=0; ctr<numPlayers; ++ctr) {
        delete players[ctr];
    }
    delete[] players;
    
}

int Board::getNumObjects() {
    // Don't include the "null" object.
    return numObjects - 1;
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

void Board::addPlayer(BALL *newPlayer, bool isCurrent) {
    players[numPlayers] = newPlayer;
    if (isCurrent) {
        currentPlayer = numPlayers;
    }
    ++numPlayers;
}

BALL* Board::getPlayer(int playerNum) {
    return players[playerNum];
}

BALL* Board::getCurrentPlayer() {
	return players[currentPlayer];
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

// Collision check two objects
// On the 2600 this is done in hardware by the Player/Missile collision registers
bool Board::CollisionCheckObjectObject(const OBJECT* object1, const OBJECT* object2)
{
    // Before we do pixel by pixel collision checking, do some trivial rejection
    // and return early if the object extents do not even overlap or are not in the same room
    
    if (object1->room != object2->room)
        return false;
    
    int cx1, cy1, cw1, ch1;
    int cx2, cy2, cw2, ch2;
    object1->CalcSpriteExtents(&cx1, &cy1, &cw1, &ch1);
    object2->CalcSpriteExtents(&cx2, &cy2, &cw2, &ch2);
    if (!Board::HitTestRects(cx1, cy1, cw1, ch1, cx2, cy2, cw2, ch2))
        return false;
    
    // Object extents overlap go pixel by pixel
    
    int objectX1 = object1->x;
    int objectY1 = object1->y;
    int objectSize1 = (object1->size/2) + 1;
    
    int objectX2 = object2->x;
    int objectY2 = object2->y;
    int objectSize2 = (object2->size/2) + 1;
    
    // Look up the index to the current state for the objects
    int stateIndex1 = object1->states ? object1->states[object1->state] : 0;
    int stateIndex2 = object2->states ? object2->states[object2->state] : 0;
    
    // Get the height, then the data
    // (the first byte of the data is the height)
    
    const byte* dataP1 = object1->gfxData;
    int objHeight1 = *dataP1;
    ++dataP1;
    
    const byte* dataP2 = object2->gfxData;
    int objHeight2 = *dataP2;
    ++dataP2;
    
    // Index into the proper states
    for (int i=0; i < stateIndex1; i++)
    {
        dataP1 += objHeight1; // skip over the data
        objHeight1 = *dataP1;
        ++dataP1;
    }
    for (int i=0; i < stateIndex2; i++)
    {
        dataP2 += objHeight2; // skip over the data
        objHeight2 = *dataP2;
        ++dataP2;
    }
    
    // Adjust for proper position
    objectX1 -= CLOCKS_HSYNC;
    objectX2 -= CLOCKS_HSYNC;
    
    // Scan the the object1 data
    const byte* rowByte1 = dataP1;
    for (int i=0; i < objHeight1; i++)
    {
        // Parse the object1 row - each bit is a 2 x 2 block
        for (int bit1=0; bit1 < 8; bit1++)
        {
            if (*rowByte1 & (1 << (7-bit1)))
            {
                // test this pixel of object1 for intersection against the pixels of object2
                
                // Scan the the object2 data
                objectY2 = object2->y;
                const byte* rowByte2 = dataP2;
                for (int j=0; j < objHeight2; j++)
                {
                    // Parse the object2 row - each bit is a 2 x 2 block
                    for (int bit2=0; bit2 < 8; bit2++)
                    {
                        if (*rowByte2 & (1 << (7-bit2)))
                        {
                            int wrappedX1 = objectX1+(bit1*2*objectSize1);
                            if (wrappedX1 >= screenWidth)
                                wrappedX1-=screenWidth;
                            
                            int wrappedX2 = objectX2+(bit2*2*objectSize2);
                            if (wrappedX2 >= screenWidth)
                                wrappedX2 -= screenWidth;
                            
                            if (Board::HitTestRects(wrappedX1, objectY1, 2*objectSize1, 2, wrappedX2, objectY2, 2*objectSize2, 2))
                                // The objects are touching
                                return true;
                        }
                    }
                    
                    // Object 2 - next byte and next row
                    ++rowByte2;
                    objectY2-=2;
                }
            }
        }
        
        // Object 1 - next byte and next row
        ++rowByte1;
        objectY1-=2;
    }
    
    return false;
    
}

// Checks an object for collision against the specified rectangle
// On the 2600 this is done in hardware by the Player/Missile collision registers
bool Board::CollisionCheckObject(const OBJECT* object, int x, int y, int width, int height)
{
    int objectX = object->x * 2;
    int objectY = object->y * 2;
    int objectSize = (object->size/2) + 1;
    
    // Look up the index to the current state for this object
    int stateIndex = object->states ? object->states[object->state] : 0;
    
    // Get the height, then the data
    // (the first byte of the data is the height)
    const byte* dataP = object->gfxData;
    int objHeight = *dataP;
    ++dataP;
    
    // Index into the proper state
    for (int i=0; i < stateIndex; i++)
    {
        dataP += objHeight; // skip over the data
        objHeight = *dataP;
        ++dataP;
    }
    
    // Adjust for proper position
    objectX -= CLOCKS_HSYNC;
    
    // scan the data
    const byte* rowByte = dataP;
    for (int i=0; i < objHeight; i++)
    {
        // Parse the row - each bit is a 2 x 2 block
        for (int bit=0; bit < 8; bit++)
        {
            if (*rowByte & (1 << (7-bit)))
            {
                // test this pixel for intersection
                
                int wrappedX = objectX+(bit*2*objectSize);
                if (wrappedX >= screenWidth)
                    wrappedX -= screenWidth;
                
                if (Board::HitTestRects(x, y, width, height, wrappedX, objectY, 2*objectSize, 2))
                    return true;
            }
        }
        
        // next byte - next row
        ++rowByte;
        objectY-=2;
    }
    
    return false;
}




