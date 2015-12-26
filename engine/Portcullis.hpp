

#ifndef Portcullis_hpp
#define Portcullis_hpp

#include <stdio.h>

#include "GameObject.hpp"

class Portcullis: public OBJECT {
public:
    int insideRoom;
    OBJECT* key;
    
    static const int OPEN_STATE;
    
    static const int CLOSED_STATE;
    
    Portcullis(int outsideRoom, int insideRoom, OBJECT* key);
    
    virtual ~Portcullis();
        
};



#endif /* Portcullis_hpp */
