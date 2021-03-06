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
   
	// TODO: This should be private but bug in Windows requires us to 
	// reseed in the middle of the game.
	static bool randomized;

private:

    
    static long startOfProgramTime;

    // Want the number of milliseconds since some time, but in Mac that is since 1970 whereas
    // in Windows it is since this machine was rebooted.
    static long timeSinceX();

    
};

#endif /* Sys_hpp */
