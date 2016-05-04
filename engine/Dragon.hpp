
#ifndef Dragon_hpp
#define Dragon_hpp

#include <stdio.h>
#include "GameObject.hpp"

class BALL;
class RemoteAction;

class Dragon: public OBJECT {
public:
    
    enum Difficulty {
        TRIVIAL = 0xD0,
        EASY = 0xE8,
        MODERATE = 0xF0,
        HARD = 0xF6
    };
    
    static const int STALKING;
    static const int DEAD;
    static const int EATEN;
    static const int ROAR;

	static bool runFromSword;
    
    BALL* eaten;
    int eatenX;
    int eatenY;

    
    /**
     * Create a dragon
     * label - used purely for debugging and logging
     * number - the dragon's number in the game (used to identify it in remote messages)
     * color - the color of the dragon
     * speed - pixels/turn that the dragon can move
     * chaseMatrix - the list of items that the dragon either runs from, attacks, or guards
     *               NOTE: Assumes chaseMatrix will not be deleted.
     */
    Dragon(const char* label, int number, int inColor, int speed, const int* chaseMatrix);
    
    ~Dragon();

	static void setRunFromSword(bool willRunFromSword);
    
    static void setDifficulty(Difficulty newDifficulty);
    
	/**
	* Move the dragon this turn.
	* matrix - The dragon list of things he runs from, goes after, or guards
	* speed - the dragon's speed
	* displayedRoomIndex - if the dragon eats the current player, the dragon controls what room is displayed
	* and needs to update the displayedRoomIndex
	*/
    RemoteAction* move(int* displayedRoomIndex);
    
	void roar(int atX, int atY);

	int dragonNumber;

	bool hasEatenCurrentPlayer();

    
private:
    
    static int dragonResetTime;
    
    /** How many seconds left waiting to bite. */
    int timer;
    
    /** How fast the dragon moves in Pixels/frame. */
    int speed;
    
    /** The matrix of things the dragon runs from, attacks, and guards. */
    const int* matrix;

    /**
     * Reset's the dragon's bite timer.
     */
    void resetTimer();
    
	void decrementTimer();

	int timerExpired();
    
    /**
     * When a dragon stops (i.e. to roar) it needs to remember it's previous velocity.
     */
    int prevMovementX;
    int prevMovementY;

	BALL* closestBall(int room, int x, int y);

};

#endif /* Dragon_hpp */
