
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

void Sys::log(const char* message) {
#ifdef WIN32
	OutputDebugString(message);
	OutputDebugString("\n");
#else
    printf("%s\n", message);
#endif
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
