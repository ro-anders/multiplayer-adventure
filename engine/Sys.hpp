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
    static void consoleLog(const char* message);
    
    /** 
     * Return today's date in the form of 20161031.
     */
    static long today();
    
    /**
     * Return timestamp of form 2017/3/14-01:59:26.
     * Caller does not need to delete the string when done, but the
     * string contents may change with each call.
     */
    static const char* datetime();
    
    /**
     * Number of milliseconds since this game was started.  Note, this
     * is really only useful looking at the time between two calls of this.
     */
    static long runTime();
    
    
private:

	static bool randomized;
    
    static long startOfProgramTime;

	static long dummyNumber;

	static bool seedRandom();
    
};

#endif /* Sys_hpp */
