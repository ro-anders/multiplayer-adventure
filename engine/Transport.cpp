//
//  Transport.cpp

#include "Transport.hpp"

#include <stdlib.h>
#include <string.h>


const int Transport::DEFAULT_PORT = 5678;

char* Transport::UNSPECIFIED = "unspecified";

const char* Transport::PACKET_DELIMETER = "\0";

Transport::Transport() :
port(DEFAULT_PORT),
ip(UNSPECIFIED)
{
    setup();
}

Transport::Transport(int inPort) :
port(inPort == 0 ? DEFAULT_PORT : inPort),
ip(NULL)
{
    setup();
}

Transport::Transport(char* inIp, int inPort) :
port(inPort == 0 ? DEFAULT_PORT : inPort),
ip(inIp)
{
    setup();
}



Transport::~Transport()
{
    delete [] streamBuffer;
}

void Transport::setup() {
    streamBufferSize = 1024;
    streamBuffer = new char[streamBufferSize]; // TODO: Make this more dynamic.
    charsInStreamBuffer = 0;
	connectNumber = -1;
}

void Transport::connect() {
    if (ip == UNSPECIFIED) {
        // Try to bind to a port.  If it's busy, assume the other program has bound and try to connect to it.
        int busy = openServerSocket();
        if (busy) {
            openClientSocket();
        }
        connectNumber = (busy ? 1 : 0);
    } else if (ip == NULL) {
        openServerSocket();
        connectNumber = 0;
    } else {
        openClientSocket();
        connectNumber = 1;
    }
}

int Transport::sendPacket(const char* packetData) {
	int n = writeData(packetData, strlen(packetData));
	if (n < 0) {
		logError("ERROR writing to socket");
	}
	else {
		int n2 = writeData(PACKET_DELIMETER, 1);
		if (n2 < 0) {
			logError("ERROR writing to socket");
		}
		else {
			char errorMessage[1000];
			sprintf(errorMessage, "Sent \"%s\"\n", packetData);
			logError(errorMessage);
		}
	}
	return n;
}


int Transport::getPacket(char* buffer, int bufferLength) {
    int hitError = 0;
    int ranOutOfData = 0;
    int delimeterIndex = -1;
    int startOfNewData = 0;
    while ((delimeterIndex < 0) && !hitError && !ranOutOfData) {
        
        // Search through the new data for a delimeter.
        for(int ctr=startOfNewData; (delimeterIndex < 0) && (ctr<charsInStreamBuffer); ++ctr) {
            delimeterIndex = (streamBuffer[ctr] == PACKET_DELIMETER[0] ? ctr : -1);
        }
        
        // Detect if we've run out of buffer.
        if ((delimeterIndex < 0) && (charsInStreamBuffer >= streamBufferSize)) {
            logError("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
            streamBuffer[streamBufferSize-1] = '\0';
            delimeterIndex = streamBufferSize-1;
        }
        
        if (delimeterIndex < 0) {
            // If we don't have delimeter, pull more data off the socket
            startOfNewData = charsInStreamBuffer;
            int charsToRead = streamBufferSize-charsInStreamBuffer;
            int n = readData(streamBuffer+charsInStreamBuffer, charsToRead);
            if (n == -1) {
                // Socket has no more data.  Return no data even if we have some sitting in the stream buffer.
                ranOutOfData = 1;
            }
            else if (n < 0) {
                hitError = n;
            } else {
                charsInStreamBuffer += n;
            }
        }
    }
    
    int charsInPacket = 0;
    if (hitError) {
        logError("ERROR reading from socket");
    } else if (ranOutOfData) {
        charsInPacket = 0;
        buffer[0] = '\0';
    } else {
        // Copy the data into the passed in buffer.
        charsInPacket = delimeterIndex; // We don't copy the delimeter
        if (delimeterIndex >= bufferLength) {
            logError("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
            charsInPacket = bufferLength-1;
        }
        memcpy(buffer, streamBuffer, charsInPacket * sizeof(char));
        buffer[charsInPacket] = '\0';
        
        // Remove the characters from the stream buffer
        memmove(streamBuffer, streamBuffer+delimeterIndex+1, (charsInStreamBuffer-delimeterIndex-1)*sizeof(char));
        charsInStreamBuffer = charsInStreamBuffer-delimeterIndex-1;
		char logMessage[1000];
        sprintf(logMessage, "Received message: \"%s\"\n",buffer);
		logError(logMessage);
    }
    
    return (hitError ? hitError : charsInPacket);
}



/**
 * Parse an socket address of the form 127.0.0.1:5678.
 * Port may be omitted, in which case the outPort is not modified.
 * TODO: This does weird things with the socket address.  It modifies it and requires is not
 * be deleted.
 */
void Transport::parseUrl(char* socketAddr, char** outIp, int* outPort) {
    *outIp = socketAddr;
    // TODO: Isn't there a find() defined somewhere?
    int colonIndex = -1;
    for(int ctr=0; (colonIndex == -1) && (socketAddr[ctr] != '\0'); ++ctr) {
        if (socketAddr[ctr] == ':') {
            colonIndex = ctr;
        }
    }
    if (colonIndex > 0) {
        *outPort = atoi(socketAddr+colonIndex+1);
        socketAddr[colonIndex] = '\0';
    }
}

