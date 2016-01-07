
#include "MacLogger.hpp"

MacLogger::MacLogger() {}

MacLogger::~MacLogger() {}

void MacLogger::info(const char* msg) {
    printf("%s\n", msg);
}

void MacLogger::error(const char* msg) {
    perror(msg);
}
