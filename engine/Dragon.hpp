
#ifndef Dragon_hpp
#define Dragon_hpp

#include <stdio.h>
#include "GameObject.hpp"

class BALL;

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
    
    BALL* eaten;
    int eatenX;
    int eatenY;

    
    Dragon(const char* label, int number, int inState, int inColor, int inRoom, int inX, int inY);
    
    ~Dragon();
    
    void decrementTimer();
    
    int timerExpired();
    
    void roar(int atX, int atY);
    
    static void setDifficulty(Difficulty newDifficulty);
    
    void move(const int* matrix, int speed);
    
    int dragonNumber;

    
private:
    
    static int dragonResetTime;
    
    /** How many seconds left waiting to bite. */
    int timer;

    /**
     * Reset's the dragon's bite timer.
     */
    void resetTimer();
    


};

#endif /* Dragon_hpp */
