
#ifndef Dragon_hpp
#define Dragon_hpp

#include <stdio.h>
#include "GameObject.hpp"

class Dragon: public OBJECT {
public:
    
    static const int STALKING;
    static const int DEAD;
    static const int EATEN;
    static const int ROAR;
    
    Dragon(int number, int inState, int inColor, int inRoom, int inX, int inY);
    
    ~Dragon();
    
    /**
     * Reset's the dragon's bite timer.
     * gameLevel - which game is being played (1-3)
     * dragonDifficulty - Whether the dragons are in amateur mode (0) or pro mode (1)
     */
    void resetTimer(int gameLevel, int dragonDifficulty);
    
    void decrementTimer();
    
    int timerExpired();
    
    void roar(int atX, int atY, int gameLevel, int dragonDifficulty);
    
    int dragonNumber;

    
private:
    
    /** How many seconds left waiting to bite. */
    int timer;

};

#endif /* Dragon_hpp */
