
#include "Sys.hpp"

#include <time.h>
#include <stdlib.h>
#include <windows.h>

// Use static initialization to make sure the random number generator is randomized.
bool Sys::randomized = Sys::seedRandom();

float Sys::random() {
	return (float)rand() / RAND_MAX;
}

void Sys::sleep(int milliseconds) {
	Sleep(milliseconds);
}

void Sys::log(const char* message) {
	OutputDebugString(message);
	OutputDebugString("\n");
}

long Sys::systemTime() {
	return time(NULL);
}

bool Sys::seedRandom() {
	srand(systemTime());
	return true;
}
