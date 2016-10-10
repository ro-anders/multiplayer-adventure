#pragma once

#ifndef Sys_hpp
#define Sys_hpp

/* Encapsulates simple system calls that have different variants on the differents OSes. */
class Sys {
public:

    /**
     * Return a random number between 0 inclusive and 1 exclusive
     */
	static float random();

    /**
     * Wait for the specified number of milliseconds 
     */
	static void sleep(int milliseconds);

    /**
     * Log the given message.
     */
	static void log(const char* message);

    /**
     * Log the given message.
     */
    static void consoleLog(const char* message);
    
    /**
     * Return number of milliseconds since 1970.
     */
	static long systemTime();
    
private:

	static bool randomized;

	static bool seedRandom();
};

#endif /* Sys_hpp */
