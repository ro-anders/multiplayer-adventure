

#ifndef Logger_hpp
#define Logger_hpp

#include <stdio.h>

/**
 * This does nothing more than printf to console any messages sent to it, but it expects subclasses to provide
 * alternate and sometimes platform dependent implementations.
 */
class Logger {
public:
    Logger();
    
    virtual ~Logger();
    
    virtual void info(const char* msg);

    virtual void error(const char* msg);
    
};

#endif /* Logger_hpp */
