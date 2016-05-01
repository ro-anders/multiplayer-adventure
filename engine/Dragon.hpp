
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

    
    Dragon(const char* label, int number, int inState, int inColor, int inRoom, int inX, int inY);
    
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
    RemoteAction* move(const int* matrix, int speed, int* displayedRoomIndex);
    
	void roar(int atX, int atY);

	int dragonNumber;

	bool hasEatenCurrentPlayer();

    
private:
    
    static int dragonResetTime;
    
    /** How many seconds left waiting to bite. */
    int timer;

    /**
     * Reset's the dragon's bite timer.
     */
    void resetTimer();
    
	void decrementTimer();

	int timerExpired();

	BALL* closestBall(int room, int x, int y);

};

#endif /* Dragon_hpp */
