
#ifndef Dragon_hpp
#define Dragon_hpp

#include <stdio.h>
#include "GameObject.hpp"

class BALL;
class DragonMoveAction;
class DragonStateAction;
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
    
    static const byte gfxData[];
    
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
     * Incorporate a state change from another machine into this dragon's state.
     * action - the state change message
     * volume - given how far this dragon is from this player, how loud would any
     *          sound be
     */
    void syncAction(DragonStateAction* action, int volume);

    /**
     * Incorporate a move action from another machine into this dragon's state.
     * action - the move message
     */
    void syncAction(DragonMoveAction* action);
    
	/**
	* Move the dragon this turn.
	* matrix - The dragon list of things he runs from, goes after, or guards
	* speed - the dragon's speed
	*/
    RemoteAction* move();
    
	void roar(int atRoom, int atX, int atY);
    
	int dragonNumber;

	bool hasEatenCurrentPlayer();

    /**
     * Sets up the dragon in the room it will start off in.  Overrides the OBJECT::init to also handle dragon internal state.
     */
    void init(int room, int x, int y, int state, int movementX, int movementY);

    
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
    
	BALL* closestBall(int room, int x, int y);

};

#endif /* Dragon_hpp */
