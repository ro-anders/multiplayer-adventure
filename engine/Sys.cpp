
#include "sys.h"
#include "Sys.hpp"

#ifdef WIN32
#include <windows.h>
#else
#include <unistd.h>
#include <sys/types.h>
#include <sys/time.h>
#endif

#include <time.h>
#include <stdio.h>
#include <stdlib.h>

bool Sys::randomized = false;

long Sys::startOfProgramTime = timeSinceX();

static long dummyVal = Sys::runTime();

float Sys::random() {
    if (!randomized) {
        srand((unsigned)Sys::timeSinceX());
        randomized = true;
    }
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
	int a = lstrlenA(message);
	BSTR unicodestr = SysAllocStringLen(NULL, a);
	::MultiByteToWideChar(CP_ACP, 0, message, a, unicodestr, a);

	OutputDebugString(unicodestr);

	::SysFreeString(unicodestr);
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

const char* Sys::datetime() {
    static char string[32];
    
    time_t epochSeconds = time(0);
    struct tm* now = localtime(&epochSeconds);
    sprintf(string, "%d/%d/%d-%d:%02d:%02d", now->tm_year+1900, now->tm_mon+1,
            now->tm_mday, now->tm_hour, now->tm_min, now->tm_sec);
    return string;
}

// Want the number of milliseconds since some time, but in Mac that is since 1970 whereas
// in Windows it is since this machine was rebooted.
long Sys::timeSinceX() {
    long currentTime;
#ifdef WIN32
    currentTime = (long)GetTickCount64();
#else
    static timeval timeval;
    gettimeofday(&timeval, NULL);
    currentTime = timeval.tv_sec*1000 + timeval.tv_usec/1000;
#endif
    
    return currentTime;
}


/**
 * Number of milliseconds since this game was started.  Note, this
 * is really only useful looking at the time between two calls of this.
 */
long Sys::runTime() {
    return timeSinceX() - startOfProgramTime;
}
