
#ifndef MacLogger_hpp
#define MacLogger_hpp

#include <stdio.h>

#include "Logger.hpp"

class MacLogger: public Logger {
public:
    MacLogger();
    
    ~MacLogger();
    
    void info(const char* msg);
    
    void error(const char* msg);
};



#endif /* MacLogger_hpp */
