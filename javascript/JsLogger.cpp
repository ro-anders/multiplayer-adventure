//
//  JsLogger.cpp
//  MacAdventure
//

#include <stdio.h>
#include "JsLogger.hpp"

JsLogger::JsLogger() {}
    
JsLogger::~JsLogger() {}
    
void JsLogger::info(const char* msg) {
	    printf("INFO: %s\n", msg);
}
    
void JsLogger::error(const char* msg) {
	    printf("ERROR: %s\n", msg);
}


