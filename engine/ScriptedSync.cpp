
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
    
    addCommand(213, "DM 0 7 80 46 0 0 2 77"); // Place rhindle in maze
    addCommand(213, "DM 0 0 80 32 2 2 0 77"); // Hide the other two dragons and bat
    addCommand(213, "DM 0 0 80 32 2 2 1 77");
    addCommand(213, "BM 0 9 14 87 0 1 1");
    addCommand(213, "PP 0 10 -2 -4 -1 0 0 0"); // Get rid of the lance
    addCommand(219, "PP 0 -1 0 0 10 7 104 26");
    addCommand(213, "PP 0 15 -8 -5 -1 0 0 0"); // Start off with white key
    addCommand(250, "PM 0 8 51 48 0 0");
    addCommand(253, "PM 0 8 53 56 -6 0"); // Get chased by dragon
    addCommand(291, "DM 0 7 98 28 3 0 2 50");
    addCommand(313, "PM 0 7 279 56 0 0");
    addCommand(316, "PM 0 7 285 56 6 0");
    addCommand(351, "DM 0 8 3 31 3 -3 2 14");
    addCommand(354, "DM 0 8 6 28 3 0 2 14");
    addCommand(358, "PM 0 8 53 50 6 -6");
    addCommand(363, "DM 0 8 15 28 3 -3 2 14");
    addCommand(369, "DM 0 8 21 22 3 0 2 14");
    addCommand(379, "PM 0 8 95 44 6 0");
    addCommand(403, "PM 0 8 143 38 6 -6");
    addCommand(405, "DM 0 8 57 22 3 -3 2 14");
    addCommand(413, "PP 0 -1 0 0 15 1 72 96");
    addCommand(460, "PM 0 1 251 124 6 0");
    addCommand(462, "DM 0 1 114 60 3 3 2 11");
    addCommand(465, "DM 0 1 117 63 3 -3 2 11");
    addCommand(468, "DM 0 1 120 60 3 3 2 11");
    addCommand(471, "DM 0 1 123 63 3 -3 2 11");
    addCommand(474, "DM 0 1 126 60 3 3 2 11");
    addCommand(477, "DM 0 1 129 63 3 -3 2 11");
    addCommand(480, "DM 0 1 132 60 3 3 2 11");
    addCommand(483, "DM 0 1 135 63 3 -3 2 11");
    addCommand(486, "DM 0 1 138 60 3 3 2 11");
    addCommand(489, "DM 0 1 141 63 3 -3 2 11");
    addCommand(492, "DM 0 1 144 60 3 3 2 11");
    addCommand(535, "PM 0 2 83 124 0 0");
    addCommand(550, "PM 1 5 75 48 0 0"); // P2 walks into rhindle
    addCommand(742, "PM 1 5 75 42 0 -6");
    addCommand(829, "PM 1 8 69 52 -6 -6");
    addCommand(832, "PM 1 8 63 52 -6 0");
    addCommand(871, "PM 1 7 303 52 0 0");
    addCommand(958, "PM 1 7 309 52 6 0");
    addCommand(1021, "PM 1 8 119 46 6 -6");
    addCommand(1044, "DM 1 1 72 96 3 3 2 8");
    addCommand(1047, "DS 1 2 3 1 81 101");
    addCommand(1060, "PM 1 1 167 202 6 0");
    addCommand(1063, "PM 1 1 161 202 0 0");
    addCommand(1065, "DS 1 2 2 1 81 101");
    addCommand(1174, "PM 1 1 162 182 -6 0");
    addCommand(1198, "PM 1 1 162 176 -6 -6");
    addCommand(1204, "PM 1 1 168 176 0 -6");
    addCommand(1207, "PM 1 1 174 176 6 -6");
    addCommand(1216, "PM 1 1 174 182 6 0");
    addCommand(1228, "PM 1 1 174 188 6 6");
    addCommand(1240, "PM 1 1 174 182 6 0");
    addCommand(1243, "PM 1 1 168 182 0 0");

}

ScriptedSync::~ScriptedSync() {
    delete[] frameList;
    for(int ctr=currentCommand; ctr<currentCommand + numCommands; ++ctr) {
        delete[] commandList[ctr];
    }
    delete[] commandList;
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
    
    // We make sure the list is sorted by frame num, but start searching for the proper
    // point at the back of the list
    int candidateSlot = currentCommand + numCommands-1; // The slot that we will put this AFTER
    while ((candidateSlot >= currentCommand) && (frameList[candidateSlot] > frame)) {
        --candidateSlot;
    }
    // If not going in the last slot, shift all the lists up one.
    if (candidateSlot != currentCommand + numCommands-1) {
        int numToMove = currentCommand + numCommands - 1 - candidateSlot;
        memcpy(frameList+candidateSlot+2, frameList+candidateSlot+1, numToMove * sizeof(int));
        memcpy(commandList+candidateSlot+2, commandList+candidateSlot+1, numToMove * sizeof(const char*));
    }
    frameList[candidateSlot+1] = frame;
    int cmdLength = strlen(commandStr);
    char* copiedCmd = new char[cmdLength+1];
    strcpy(copiedCmd, commandStr);
    commandList[candidateSlot+1] = copiedCmd;
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