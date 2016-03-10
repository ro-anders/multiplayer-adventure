#pragma once

#ifndef Sys_hpp
#define Sys_hpp

/* Encapsulates simple system calls that have different variants on the differents OSes. */
class Sys {
public:

	static float random();

	static void sleep(int milliseconds);

	static void log(const char* message);

	static long systemTime();

private:

	static bool randomized;

	static bool seedRandom();
};

#endif /* Sys_hpp */