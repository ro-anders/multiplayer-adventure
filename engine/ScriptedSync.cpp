
#include "ScriptedSync.hpp"

#include <string.h>

const int ScriptedSync::STARTING_LIST_SIZE = 100;

ScriptedSync::ScriptedSync(int numPlayers, int thisPlayer):
  Sync(numPlayers, thisPlayer, NULL),
  numCommands(0),
  currentCommand(0),
  sizeAllocated(STARTING_LIST_SIZE),
  frameList(new int[STARTING_LIST_SIZE]),
  commandList(new const char*[STARTING_LIST_SIZE])
{
    // To start with, hard code some scripted commands.
    //Sent "PM 0 17 160 58 0 -6" on frame #214
    //Sent "PM 0 2 154 124 -6 -6" on frame #274
    
    //Sent "PM 0 2 148 124 -6 0" on frame #277
    
    //Sent "PM 0 2 40 130 -6 6" on frame #331
    
    //Sent "PM 0 2 34 142 0 6" on frame #337
    
    //Sent "PM 0 2 34 190 0 0" on frame #370
    addCommand(214, "PM 0 17 160 58 0 -6");
    addCommand(214, "PM 1 33 160 58 0 -6");
}

ScriptedSync::~ScriptedSync() {
    delete[] frameList;
    for(int ctr=currentCommand; ctr<currentCommand + numCommands; ++ctr) {
        delete[] commandList[ctr];
    }
    delete[] commandList;
}

void ScriptedSync::BroadcastAction(RemoteAction* action) {
    // This does nothing.
}

    
int ScriptedSync::pullNextPacket(char* buffer, int bufferLength) {
    int frameNum = getFrameNumber();
    int charsInPacket = 0;
    if ((numCommands > 0) && (frameList[currentCommand] <= frameNum)) {
        // Pull off the next command
        int commandLength = strlen(commandList[currentCommand]);
        charsInPacket = (commandLength < bufferLength ? commandLength : bufferLength-1);
        memcpy(buffer, commandList[currentCommand], charsInPacket * sizeof(char));
        buffer[charsInPacket] = '\0';
        delete commandList[currentCommand];
        commandList[currentCommand] = NULL;
        ++currentCommand;
        --numCommands;
    }
    return charsInPacket;
}

void ScriptedSync::addCommand(int frame, const char *commandStr) {
    if (currentCommand + numCommands == sizeAllocated) {
        makeMoreSpace();
    }
    int newSlot = currentCommand + numCommands;
    frameList[newSlot] = frame;
    int cmdLength = strlen(commandStr);
    char* copiedCmd = new char[cmdLength+1];
    strcpy(copiedCmd, commandStr);
    commandList[newSlot] = copiedCmd;
    ++numCommands;
}

void ScriptedSync::makeMoreSpace() {
    // If the we're using less than half the list's space, just shove everything down to the
    // front of the list.  If we're more than half full, the double the allocated size (and shove everything
    // down to the front of the list)
    if (numCommands < sizeAllocated/2) {
        memcpy(frameList, frameList+currentCommand, numCommands * sizeof(int));
        memcpy(commandList, commandList+currentCommand, numCommands * sizeof(const char*));
        currentCommand = 0;

    } else {
        int newSize = 2 * sizeAllocated;
        int* newFrameList = new int[newSize];
        memcpy(newFrameList, frameList+currentCommand, numCommands * sizeof(int));
        delete[] frameList;
        frameList = newFrameList;
        
        const char** newCommandList = new const char*[newSize];
        memcpy(newCommandList, commandList+currentCommand, numCommands * sizeof(const char*));
        delete[] commandList;
        commandList = newCommandList;
        
        sizeAllocated = newSize;
        currentCommand = 0;
    }
}