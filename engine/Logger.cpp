

#include "Logger.hpp"

#include "strings.h"

Logger::Logger() {}

Logger::~Logger() {}

void Logger::info(const char* msg) {
    printf("%s\n", msg);
}

void Logger::error(const char* msg) {
    info(msg);
}

void Logger::info(const char* printfMsg, int num) {
    int msgLen = strlen(printfMsg) + 28;
    char finalMsg[msgLen];
    
    sprintf(finalMsg, printfMsg, num);
}
