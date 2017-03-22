
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

const byte Dragon::gfxData [] =
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

Dragon::Dragon(const char* label, int inNumber, int inColor, int inSpeed, const int* chaseMatrix):
OBJECT(label, Dragon::gfxData, dragonStates, 0, inColor),
    dragonNumber(inNumber),
    speed(inSpeed),
    matrix(chaseMatrix),
    timer(0),
    eaten(NULL),
    eatenX(0),
    eatenY(0)
{}

Dragon::~Dragon() {
    
}

/**
 * Override the OBJECT::init to also set the dragon's previous velocities.
 */
void Dragon::init(int inRoom, int inX, int inY, int inState, int inMoveX, int inMoveY) {
    OBJECT::init(inRoom, inX, inY, inState, inMoveX, inMoveY);
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

void Dragon::roar(int atRoom, int atX, int atY) {
    state = ROAR;
    
    resetTimer();
    
    // Set the dragon's position to the same as the ball
    room = atRoom;
    x = atX+1; // Added one to get over disparity between C++ port and original atari game - not the best solution
    y = atY;
}

void Dragon::setDifficulty(Dragon::Difficulty newDifficulty) {
    dragonResetTime = newDifficulty;
}

bool Dragon::hasEatenCurrentPlayer() {
	return (state == Dragon::EATEN) && (eaten == board->getCurrentPlayer());
}

void Dragon::syncAction(DragonStateAction* action, int volume) {
    if (action->newState == Dragon::EATEN) {
        
        BALL* playerEaten = board->getPlayer(action->sender);
        // Ignore duplicates
        if (eaten != playerEaten) {
            // Set the State to 02 (eaten)
            eaten = playerEaten;
            state = Dragon::EATEN;
            room = action->room;
            x = action->posx;
            y = action->posy;
            movementX = action->velx;
            movementY = action->vely;
            // Play the sound
            Platform_MakeSound(SOUND_EATEN, volume);
        }
    } else if (action->newState == Dragon::DEAD) {
        // We ignore die actions if the dragon has already eaten somebody or if it's a duplicate.
        if ((state != Dragon::EATEN) && (state != Dragon::DEAD)) {
            state = DEAD;
            room = action->room;
            x = action->posx;
            y = action->posy;
            movementX = action->velx;
            movementY = action->vely;
            // Play the sound
            Platform_MakeSound(SOUND_DRAGONDIE, volume);
        }
    }
    else if (action->newState == Dragon::ROAR) {
        // We ignore roar actions if we are already in an eaten state or dead state
        if ((state != Dragon::EATEN) && (state != Dragon::DEAD)) {
            roar(action->room, action->posx, action->posy);
            movementX = action->velx;
            movementY = action->vely;
            // Play the sound
            Platform_MakeSound(SOUND_ROAR, volume);
        }
    }
}

void Dragon::syncAction(DragonMoveAction* action) {
    room = action->room;
    x = action->posx;
    y = action->posy;
    movementX = action->velx;
    movementY = action->vely;
}

RemoteAction* Dragon::move()
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
            dragon->roar(objectBall->room, objectBall->x/2, objectBall->y/2);
            
            // Notify others
            actionTaken = new DragonStateAction(dragon->dragonNumber, Dragon::ROAR, dragon->room, dragon->x, dragon->y,
                                                dragon->movementX, dragon->movementY);
                        
            // Play the sound
            Platform_MakeSound(SOUND_ROAR, MAX_VOLUME);
        }
        
        // Has the Sword hit the Dragon?
        if (board->CollisionCheckObjectObject(dragon, board->getObject(OBJECT_SWORD)))
        {
            // Set the State to 01 (Dead)
            dragon->state = Dragon::DEAD;
            
            // Notify others
            actionTaken = new DragonStateAction(dragon->dragonNumber, Dragon::DEAD, dragon->room, dragon->x, dragon->y,
                                                dragon->movementX, dragon->movementY);
            
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
                    if ((fleeObjectPtr->room == dragon->room) && (fleeObjectPtr->exists()))
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
        dragon->eaten->previousRoom = dragon->room;
        dragon->eaten->x = (dragon->x + 3) * 2;
        dragon->eaten->previousX = dragon->eaten->x;
        dragon->eaten->y = (dragon->y - 10) * 2;
        dragon->eaten->previousY = dragon->eaten->y;
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
                // Move the dragon up so that eating never causes the ball to shift screens
                if (objectBall->y < 48) {
                    dragon->y = 24;
                }
                
                // Notify others
                actionTaken = new DragonStateAction(dragon->dragonNumber, Dragon::EATEN, dragon->room,
                                                    dragon->x, dragon->y, dragon->movementX, dragon->movementY);
                
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
