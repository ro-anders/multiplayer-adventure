//
//  Sleep.hpp
//  MacAdventure
//

#ifndef Sleep_hpp
#define Sleep_hpp

#include <stdio.h>

class Sleep {
public:
    Sleep();
    
    virtual ~Sleep();
    
    virtual void sleep(int seconds) = 0;
};

#endif /* Sleep_hpp */
