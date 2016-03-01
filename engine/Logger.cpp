

#include "Logger.hpp"

Logger::Logger() {}

Logger::~Logger() {}

void Logger::info(const char* msg) {
    printf("%s\n", msg);
}

void Logger::error(const char* msg) {
    info(msg);
}