
#ifndef Bat_hpp
#define Bat_hpp

#include <stdio.h>
#include "GameObject.hpp"

class Bat: public OBJECT {
public:
    Bat(int color, int inRoom, int inX, int inY);
    
    virtual ~Bat();
    
};

#endif /* Bat_hpp */
