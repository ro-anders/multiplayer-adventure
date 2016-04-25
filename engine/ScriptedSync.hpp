
#ifndef ScriptedTransport_hpp
#define ScriptedTransport_hpp

#include "Sync.hpp"

#include <stdio.h>

/**
 * This mocks another player by playing a script.  Actually, it can even mock the current player and create an 
 * entire scripted scene.
 */
class ScriptedSync: public Sync {
public:
    ScriptedSync(int numPlayers, int thisPlayer);
    
    ~ScriptedSync();
    
protected:
    /**
     * If a scripted action is scheduled now, this returns the scripted action.
     */
    int pullNextPacket(char* buffer, int bufferSize);

private:
    
    static const int STARTING_LIST_SIZE;
    
    int numCommands;
    
    int currentCommand;
    
    int sizeAllocated;
    
    int* frameList;
    
    const char** commandList;
    
    void addCommand(int frame, const char* commandStr);
    
    void makeMoreSpace();

};
#endif /* ScriptedSync_hpp */
