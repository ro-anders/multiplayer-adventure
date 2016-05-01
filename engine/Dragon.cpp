
#include "Dragon.hpp"

#include "Adventure.h"
#include "Ball.hpp"
#include "Board.hpp"
#include "RemoteAction.hpp"

const int Dragon::STALKING = 0;
const int Dragon::DEAD = 1;
const int Dragon::EATEN = 2;
const int Dragon::ROAR = 3;

bool Dragon::runFromSword = false;

// Dragon states
static const byte dragonStates [] =
{
    0,2,0,1
};

static const byte objectGfxDrag [] =
{
    // Object #6 : State #00 : Graphic
    20,
    0x06,                  //      XX
    0x0F,                  //     XXXX
    0xF3,                  // XXXX  XX
    0xFE,                  // XXXXXXX
    0x0E,                  //     XXX
    0x04,                  //      X
    0x04,                  //      X
    0x1E,                  //    XXXX
    0x3F,                  //   XXXXXX
    0x7F,                  //  XXXXXXX
    0xE3,                  // XXX   XX
    0xC3,                  // XX    XX
    0xC3,                  // XX    XX
    0xC7,                  // XX   XXX
    0xFF,                  // XXXXXXXX
    0x3C,                  //   XXXX
    0x08,                  //     X
    0x8F,                  // X   XXXX
    0xE1,                  // XXX    X
    0x3F,                  //   XXXXXX
    // Object 6 : State 01 : Graphic
    22,
    0x80,                  // X
    0x40,                  //  X
    0x26,                  //   X  XX
    0x1F,                  //    XXXXX
    0x0B,                  //     X XX
    0x0E,                  //     XXX
    0x1E,                  //    XXXX
    0x24,                  //   X  X
    0x44,                  //  X   X
    0x8E,                  // X   XXX
    0x1E,                  //    XXXX
    0x3F,                  //   XXXXXX
    0x7F,                  //  XXXXXXX
    0x7F,                  //  XXXXXXX
    0x7F,                  //  XXXXXXX
    0x7F,                  //  XXXXXXX
    0x3E,                  //   XXXXX
    0x1C,                  //    XXX
    0x08,                  //     X
    0xF8,                  // XXXXX
    0x80,                  // X
    0xE0,                   // XXX
    // Object 6 : State 02 : Graphic
    17,
    0x0C,                  //     XX
    0x0C,                  //     XX
    0x0C,                  //     XX
    0x0E,                  //     XXX
    0x1B,                  //    XX X
    0x7F,                  //  XXXXXXX
    0xCE,                  // XX  XXX
    0x80,                  // X
    0xFC,                  // XXXXXX
    0xFE,                  // XXXXXXX
    0xFE,                  // XXXXXXX
    0x7E,                  //  XXXXXX
    0x78,                  //  XXXX
    0x20,                  //   X
    0x6E,                  //  XX XXX
    0x42,                  //  X    X
    0x7E                   //  XXXXXX
};

int Dragon::dragonResetTime = Dragon::TRIVIAL;

Dragon::Dragon(const char* label, int inNumber, int inState, int inColor, int inRoom, int inX, int inY):
    OBJECT(label, objectGfxDrag, dragonStates, inState, inColor, inRoom, inX, inY),
    dragonNumber(inNumber),
    timer(0),
    eaten(NULL),
    eatenX(0),
    eatenY(0)
{}

Dragon::~Dragon() {
    
}

void Dragon::setRunFromSword(bool willRunFromSword) {
	runFromSword = willRunFromSword;
}

void Dragon::resetTimer() {
    timer = 0xFC - dragonResetTime;
}

void Dragon::decrementTimer() {
    --timer;
}

int Dragon::timerExpired() {
    return (timer <= 0);
}

void Dragon::roar(int atX, int atY) {
    state = ROAR;
    
    resetTimer();
    
    // Set the dragon's position to the same as the ball
    x = atX+1; // Added one to get over disparity between C++ port and original atari game - not the best solution
    y = atY;
    
    movementX = 0;
    movementY = 0;
    
}

void Dragon::setDifficulty(Dragon::Difficulty newDifficulty) {
    dragonResetTime = newDifficulty;
}

bool Dragon::hasEatenCurrentPlayer() {
	return (state == Dragon::EATEN) && (eaten == board->getCurrentPlayer());
}

RemoteAction* Dragon::move(const int* matrix, int speed, int* displayedRoomIndex)
{
	RemoteAction* actionTaken = NULL;
    Dragon* dragon = this;
    BALL* objectBall = board->getCurrentPlayer();
    if (dragon->state == Dragon::STALKING)
    {
        // Has the Ball hit the Dragon?
        if ((objectBall->room == dragon->room) &&
            board->CollisionCheckObject(dragon, (objectBall->x-4), (objectBall->y-4), 8, 8))
        {
            dragon->roar(objectBall->x/2, objectBall->y/2);
            
            // Notify others
            actionTaken = new DragonStateAction(dragon->dragonNumber, Dragon::ROAR, dragon->room, dragon->x, dragon->y);
                        
            // Play the sound
            Platform_MakeSound(SOUND_ROAR, MAX_VOLUME);
        }
        
        // Has the Sword hit the Dragon?
        if (board->CollisionCheckObjectObject(dragon, board->getObject(OBJECT_SWORD)))
        {
            // Set the State to 01 (Dead)
            dragon->state = Dragon::DEAD;
            dragon->movementX = 0;
            dragon->movementY = 0;
            
            // Notify others
            actionTaken = new DragonStateAction(dragon->dragonNumber, Dragon::DEAD, dragon->room, dragon->x, dragon->y);
            
            // Play the sound
            Platform_MakeSound(SOUND_DRAGONDIE, MAX_VOLUME);
        }
        
        if (dragon->state == Dragon::STALKING)
        {
            // Go through the dragon's object matrix
            // Difficulty switch determines flee or don't flee from sword
            const int* matrixP = (runFromSword ? matrix : matrix+2);
            do
            {
                int seekDir = 0; // 1 is seeking, -1 is fleeing
                int seekX=0, seekY=0;
                
                int fleeObject = *(matrixP+0);
                int seekObject = *(matrixP+1);
                
                // Dragon fleeing an object
                const OBJECT* fleeObjectPtr = board->getObject(fleeObject);
                if ((fleeObject > OBJECT_NONE) && (fleeObjectPtr != dragon))
                {
                    // get the object it is fleeing
                    if (fleeObjectPtr->room == dragon->room)
                    {
                        seekDir = -1;
                        seekX = fleeObjectPtr->x;
                        seekY = fleeObjectPtr->y;
                    }
                }
                else
                {
                    // Dragon seeking the ball
                    if (seekDir == 0)
                    {
                        if (*(matrixP+1) == OBJECT_BALL)
                        {
                            BALL* closest = closestBall(dragon->room, dragon->x, dragon->y);
                            if (closest != 0x0) {
                                seekDir = 1;
                                seekX = closest->x/2;
                                seekY = closest->y/2;
                            }
                        }
                    }
                    
                    // Dragon seeking an object
                    if ((seekDir == 0) && (seekObject > OBJECT_NONE))
                    {
                        // Get the object it is seeking
						const OBJECT* object = board->getObject(seekObject);
                        if (object->room == dragon->room)
                        {
                            seekDir = 1;
                            seekX = object->x;
                            seekY = object->y;
                        }
                    }
                }
                
                // Move the dragon
                if ((seekDir > 0) || (seekDir < 0))
                {
                    int newMovementX = 0;
                    int newMovementY = 0;
                    
                    // horizontal axis
                    if (dragon->x < seekX)
                    {
                        newMovementX = seekDir*speed;
                    }
                    else if (dragon->x > seekX)
                    {
                        newMovementX = -(seekDir*speed);
                    }
                    
                    // vertical axis
                    if (dragon->y < seekY)
                    {
                        newMovementY = seekDir*speed;
                    }
                    else if (dragon->y > seekY)
                    {
                        newMovementY = -(seekDir*speed);
                    }
                    
                    // Notify others if we've changed our direction
                    if ((dragon->room == objectBall->room) && ((newMovementX != dragon->movementX) || (newMovementY != dragon->movementY))) {
                        int distanceToMe = board->getCurrentPlayer()->distanceTo(dragon->x, dragon->y);
                        actionTaken = new DragonMoveAction(dragon->room, dragon->x, dragon->y, newMovementX, newMovementY, dragon->dragonNumber, distanceToMe);
                    }
                    dragon->movementX = newMovementX;
                    dragon->movementY = newMovementY;
                    
                    // Found something - we're done
                    return actionTaken;
                }
            }
            while (*(matrixP+=2));
            
        }
    }
    else if (dragon->state == Dragon::EATEN)
    {
        // Eaten
        dragon->eaten->room = dragon->room;
        dragon->eaten->x = (dragon->x + 3) * 2;
        dragon->eaten->y = (dragon->y - 10) * 2;
        dragon->movementX = 0;
        dragon->movementY = 0;
        if (objectBall == dragon->eaten) {
            *displayedRoomIndex = dragon->room;
        }
    }
    else if (dragon->state == Dragon::ROAR)
    {
        dragon->decrementTimer();
        if (dragon->timerExpired())
        {
            // Has the Ball hit the Dragon?
            if ((objectBall->room == dragon->room) && board->CollisionCheckObject(dragon, (objectBall->x-4), (objectBall->y-1), 8, 8))
            {
                // Set the State to 01 (eaten)
                dragon->eaten = objectBall;
                dragon->state = Dragon::EATEN;
                
                // Notify others
                actionTaken = new DragonStateAction(dragon->dragonNumber, Dragon::EATEN, dragon->room, dragon->x, dragon->y);                
                
                // Play the sound
                Platform_MakeSound(SOUND_EATEN, MAX_VOLUME);
            }
            else
            {
                // Go back to stalking
                dragon->state = Dragon::STALKING;
            }
        }
    }
    // else dead!

	return actionTaken;
}

/**
* Returns the ball closest to the point in the adventure.
*/
BALL* Dragon::closestBall(int room, int x, int y) {
	int shortestDistance = 10000; // Some big number greater than the diagnol of the board
	BALL* found = 0x0;
	int numPlayers = board->getNumPlayers();
	for (int ctr = 0; ctr<numPlayers; ++ctr) {
		BALL* nextBall = board->getPlayer(ctr);
		if (nextBall->room == room)
		{
			int dist = nextBall->distanceTo(x, y);
			if (dist < shortestDistance) {
				shortestDistance = dist;
				found = nextBall;
			}
		}
	}
	return found;
}
