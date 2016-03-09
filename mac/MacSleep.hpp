//
//  MacSleep.hpp
//  MacAdventure
//

#ifndef MacSleep_hpp
#define MacSleep_hpp

#include <stdio.h>

#include "Sleep.hpp"

class MacSleep: public Sleep {
public:
    MacSleep();
    
    ~MacSleep();
    
    void sleep(int seconds);
};


#endif /* MacSleep_hpp */
