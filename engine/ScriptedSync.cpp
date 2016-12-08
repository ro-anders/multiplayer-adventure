
#include "ScriptedSync.hpp"

#include <iostream>
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
    
    // Read commands from stdin until we come to a line starting with '.'.
    bool done = false;
    char buffer[1000];
    std::cin >> std::noskipws;
    for (int line=1; !done; ++line) {
        int frame = -1;
        done = parseCommand(&frame, buffer, line);
        if (!done && (frame >= 0)) {
            addCommand(frame, buffer);
        }
    }
    

    
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

/**
 * Read off stdin.  Expecting a line of the form:
 * Send "DM 12 12 12 46 23" on frame #256\n
 * Any line that begins with a '$' is considered a comment and is ignored.
 */
bool ScriptedSync::parseCommand(int* frame, char* buffer, int line) {
    const char COMMENT_CHAR = '$';
    const int LOOKING_FOR_QUOTE = 0;
    const int READING_COMMAND = 1;
    const int LOOKING_FOR_NUMBER = 2;
    const int LOOKING_FOR_EOLN = 3;
    const int DONE = 4;
    int state = LOOKING_FOR_QUOTE;
    bool endOfInput = false;
    char c;
    int bufferCtr = 0;
    while (state != DONE) {
        std::cin >> c;
        if (c == '\n') {
            state = DONE;
        } else if (state == LOOKING_FOR_QUOTE) {
            if (c == COMMENT_CHAR) {
                // Comment.  Throw the rest of the line out.
                state = LOOKING_FOR_EOLN;
            } else if (c == '.') {
                state = DONE;
                endOfInput = true;
            } else if (c == '"') {
                state = READING_COMMAND;
            }
        } else if (state == READING_COMMAND) {
            if (c == '"') {
                state = LOOKING_FOR_NUMBER;
            } else {
                buffer[bufferCtr] = c;
                ++bufferCtr;
            }
        } else if (state == LOOKING_FOR_NUMBER) {
            if (c == '#') {
                std::cin >> *frame;
                state = LOOKING_FOR_EOLN;
            }
        }
    }
    
    if (bufferCtr > 0) {
        if (*frame > 0) {
            // Success.  Make sure the buffer is null terminated.
            buffer[bufferCtr] = '\0';
            //std::cout << "Parsed command \"" << buffer << "\" at frame#" << *frame << std::endl;
            
        } else {
            // Something went wrong.  Don't pass back any data.
            *frame  = -1;
            buffer[0] = '\0';
            std::cout << "Failed to parse command at line " << line << "." << std::endl;
            char segment[40];
            strncpy(segment, buffer, 39);
            segment[39] = '\0';
            std::cout << '"' << segment << '"' << std::endl;
        }
    }
    return endOfInput;
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
