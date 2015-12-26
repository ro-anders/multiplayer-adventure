
#ifndef Bat_hpp
#define Bat_hpp

#include <stdio.h>
#include "GameObject.hpp"

class Bat: public OBJECT {
public:
    int linkedObject;           // index of linked (carried) object
    int linkedObjectX;
    int linkedObjectY;

    Bat(int color, int inRoom, int inX, int inY);
    
    virtual ~Bat();
    
};

#endif /* Bat_hpp */
