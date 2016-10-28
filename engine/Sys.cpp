
#include "Sys.hpp"

#include <time.h>
#include <stdio.h>
#include <stdlib.h>
#ifdef WIN32
#include <windows.h>
#else
#include <unistd.h>
#include <sys/types.h>
#include <sys/time.h>
#endif
// Use static initialization to make sure the random number generator is randomized.
bool Sys::randomized = Sys::seedRandom();

long Sys::startOfProgramTime = Sys::runTime();

float Sys::random() {
	return (float)rand() / RAND_MAX;
}

void Sys::sleep(int milliseconds) {
#ifdef WIN32
	Sleep(milliseconds);
#else
    ::usleep(milliseconds*1000);
#endif
}

void Sys::consoleLog(const char* message) {
#ifdef WIN32
    OutputDebugString(message);
#else
    printf("%s", message);
#endif
}

/**
 * Return today's date in the form of 20161031.
 */
long Sys::today() {
    time_t epochSeconds = time(0);
    struct tm* now = localtime(&epochSeconds);
    long datenum = (now->tm_year+1900) * 10000 + (now->tm_mon+1) * 100 + now->tm_mday;
    return datenum;
}

/**
 * Number of milliseconds since this game was started.  Note, this
 * is really only useful looking at the time between two calls of this.
 */
long Sys::runTime() {
    static bool firstTime = true;
    
    // In some cases it's easier to get the number of milliseconds since some external
    // event (e.g. the epoch) than the start of this program.  So we record that time at
    // the start of the program and return the difference with every subsequent time.
    if (firstTime) {
        firstTime = false;
        startOfProgramTime = 0;
        startOfProgramTime =  runTime();
    }
    
    long currentTime;
#ifdef WIN32
    currentTime = time(NULL);
#else
    static timeval timeval;
    gettimeofday(&timeval, NULL);
    currentTime = timeval.tv_sec*1000 + timeval.tv_usec/1000;
#endif
    
    return currentTime - startOfProgramTime;
}



long Sys::systemTime() {
#ifdef WIN32
	return time(NULL);
#else 
    static timeval timeval;
    gettimeofday(&timeval, NULL);
    long milliseconds = timeval.tv_sec*1000 + timeval.tv_usec/1000;
    return milliseconds;
#endif
}

bool Sys::seedRandom() {
#ifdef WIN32
	srand((unsigned)time(NULL));
#else
	srand(systemTime());
#endif
	return true;
}
