

#ifndef Portcullis_hpp
#define Portcullis_hpp

#include <stdio.h>

#include "GameObject.hpp"

class Portcullis: public OBJECT {
public:
    bool isActive;
    int insideRoom;
    OBJECT* key;
    
    static const int OPEN_STATE;
    
    static const int CLOSED_STATE;
    
    Portcullis(const char* label, int outsideRoom, int insideRoom, OBJECT* key);
    
    virtual ~Portcullis();
    
    void setState(int newState, bool isActive);
    
    void updateState();
    
    void keyTouch();
    
    void openFromInside();
    
    /**
     * Called when a player enters a gate that is not completely open.
     */
    void forceOpen();
};



#endif /* Portcullis_hpp */
