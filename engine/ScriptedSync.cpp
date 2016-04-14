
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
    
    addCommand(213, "DM 0 0 80 32 2 2 0 77"); // Hide the dragons
    addCommand(213, "DM 0 0 80 32 2 2 1 77");
    addCommand(213, "DM 0 0 80 32 2 2 2 77");
    addCommand(213, "PP 1 10 -6 -4 -1 0 0 0"); // Pickup & Place the lance
    addCommand(216, "PP 1 -1 0 0 10 4 119 92");
    addCommand(219, "PP 1 16 -6 -4 -1 0 0 0"); // P2 holds black key
    addCommand(225, "PM 0 7 155 42 0 6"); // P1 & P2 moving up in blue maze
    addCommand(225, "PM 1 7 155 162 0 6");
    addCommand(333, "PM 0 4 161 72 0 0");
    addCommand(365, "PM 1 16 155 42 -6 0"); // P2 hides the key
    addCommand(380, "PM 1 16 125 42 0 0");
    addCommand(387, "PP 1 -1 0 0 16 16 50 17");
    addCommand(390, "PM 1 16 125 42 6 0"); // P2 heads back down
    addCommand(390, "PM 0 4 161 78 0 6");
    addCommand(405, "PM 1 16 155 42 0 -6");
    addCommand(411, "PM 0 4 167 120 6 6");
    addCommand(417, "PM 0 4 179 126 6 0");
    addCommand(450, "PM 0 4 233 132 0 6");
    addCommand(456, "PM 0 4 233 138 0 0");
    addCommand(525, "PM 0 4 233 132 0 -6");
    addCommand(534, "PM 0 4 227 120 -6 0");
    addCommand(570, "PM 0 4 155 126 -6 6");
    addCommand(576, "PM 0 4 149 138 0 6");
    addCommand(630, "PM 0 16 149 54 0 0");
    addCommand(693, "PM 0 16 149 48 0 -6");
    addCommand(747, "PM 0 4 155 124 6 -6");
    addCommand(756, "PM 0 4 173 112 6 0");
    addCommand(774, "PM 0 4 209 118 6 6");
    addCommand(805, "PP 0 10 0 3 -1 0 0 0");
    addCommand(813, "PM 0 4 233 184 0 0");
    addCommand(828, "PM 0 4 233 178 0 -6");
    addCommand(858, "PM 0 4 227 118 -6 -6");
    addCommand(864, "PM 0 4 215 112 -6 0");
    addCommand(894, "PM 0 4 155 118 -6 6");
    addCommand(897, "PM 0 4 155 124 0 6");
    addCommand(936, "PM 0 4 161 202 6 6");
    addCommand(942, "PM 0 4 167 214 0 6");
    addCommand(951, "PM 0 16 167 36 0 0");
    addCommand(993, "PM 0 16 167 42 0 6");
    addCommand(999, "PM 0 16 167 48 0 0");
    addCommand(1000, "PP 0 -1 0 0 10 16 89 30");
    addCommand(1006, "PM 0 16 160 48 -6 0");
    addCommand(1009, "PM 0 16 154 54 -6 6");
    addCommand(1051, "PM 0 16 76 78 0 0");
    addCommand(1054, "PM 0 16 70 78 -6 0");
    addCommand(1057, "PM 0 16 70 78 0 0");
    addCommand(1069, "PM 0 16 64 72 -6 -6");
    addCommand(1099, "PM 0 16 28 36 6 -6");
    addCommand(1139, "PP 0 16 -3 -2 -1 0 0 0");
    addCommand(1147, "PM 0 16 106 42 0 0");
    addCommand(1162, "PM 0 16 112 42 6 0");
    addCommand(1180, "PM 0 16 142 42 0 0");
    addCommand(1186, "PM 0 16 142 48 0 6");
    addCommand(1207, "PM 0 16 148 72 6 6");
    addCommand(1216, "PM 0 16 154 84 0 6");
    addCommand(1217, "GS 0 2 13 1");
    addCommand(1219, "GS 0 2 0 1");
    addCommand(1219, "PM 0 27 154 30 0 6");
    addCommand(1246, "PM 0 27 154 78 0 0");
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