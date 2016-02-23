//
//  JsLogger.hpp
//  MacAdventure
//

#ifndef JsLogger_hpp
#define JsLogger_hpp

#include <stdio.h>
#include "../engine/Logger.hpp"

class JsLogger: public Logger {
    
public:
    
    JsLogger();
    
    virtual ~JsLogger();
    
    virtual void info(const char* msg);
    
    virtual void error(const char* msg);
    
    
};

#endif /* JsLogger_hpp */
