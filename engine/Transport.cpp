//
//  Transport.cpp

#include "Transport.hpp"

#include <stdlib.h>
#include <string.h>
#include "Sys.hpp"

const int Transport::DEFAULT_PORT = 5678;

const int Transport::TPT_ERROR = -1;
const int Transport::TPT_OK = 0;
const int Transport::TPT_BUSY = 1;

const int Transport::NOT_A_TEST = -1;
const int Transport::NOT_YET_DETERMINED = -2;

const char* Transport::LOCALHOST_IP = "127.0.0.1";


Transport::Transport(bool inATest)
{
    streamBufferSize = 1024;
    streamBuffer = new char[streamBufferSize]; // TODO: Make this more dynamic.
    charsInStreamBuffer = 0;
    testSetupNumber = (inATest ? NOT_YET_DETERMINED : NOT_A_TEST);
}

Transport::~Transport()
{
    delete [] streamBuffer;
}

int Transport::getTestSetupNumber() {
    return testSetupNumber;
}
void Transport::setTestSetupNumber(int newNum) {
    testSetupNumber = newNum;
}


int Transport::sendPacket(const char* packetData) {
	int n = writeData(packetData, strlen(packetData)+1); // +1 to include the \0
	if (n < 0) {
		Sys::log("ERROR writing to socket");
	}
	else {
        char message[1000];
        sprintf(message, "Sent \"%s\"", packetData);
		Sys::log(message);
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
            delimeterIndex = (streamBuffer[ctr] == '\0' ? ctr : -1);
        }
        
        // Detect if we've run out of buffer.
        if ((delimeterIndex < 0) && (charsInStreamBuffer >= streamBufferSize)) {
			Sys::log("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
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
		Sys::log("ERROR reading from socket");
    } else if (ranOutOfData) {
        charsInPacket = 0;
        buffer[0] = '\0';
    } else {
        // Copy the data into the passed in buffer.
        charsInPacket = delimeterIndex; // We don't copy the delimeter
        if (delimeterIndex >= bufferLength) {
			Sys::log("ERROR reading from socket.  Packet too big for buffer.  Truncating.");
            charsInPacket = bufferLength-1;
        }
        memcpy(buffer, streamBuffer, charsInPacket * sizeof(char));
        buffer[charsInPacket] = '\0';
        
        // Remove the characters from the stream buffer
        memmove(streamBuffer, streamBuffer+delimeterIndex+1, (charsInStreamBuffer-delimeterIndex-1)*sizeof(char));
        charsInStreamBuffer = charsInStreamBuffer-delimeterIndex-1;
		char logMessage[1000];
        sprintf(logMessage, "Received message: \"%s\"",buffer);
		Sys::log(logMessage);
    }
    
    return (hitError ? hitError : charsInPacket);
}

void Transport::testTransport(Transport& t) {
    int NUM_MESSAGES = 10;
    // If this is a test, make sure test roles are negotiated.
    t.testSetupNumber = NOT_YET_DETERMINED;
    t.connect();
    if (t.getTestSetupNumber() == 1) {
        int numSent = 0;
        for(int ctr=0; ctr<NUM_MESSAGES; ++ctr) {
            char message[256];
            sprintf(message, "Message %d\n\0", (ctr+1));
            int charsSent = t.sendPacket(message);
            if (charsSent <= 0) {
                perror("Error sending packet");
            } else {
                ++numSent;
            }
            if (ctr == (NUM_MESSAGES/2)) {
                printf("Pausing\n");
                Sys::sleep(5000);
            }
        }
        printf("Sent %d messages.  %s.\n", numSent, (numSent == 10 ? "PASS" : "FAIL"));
    } else {
        int numReceived = 0;
        // We wait a second for the sender to send some stuff
        Sys::sleep(2000);
        for(int ctr=0;ctr<NUM_MESSAGES;++ctr) {
            char buffer[256];
            int charsReceived = t.getPacket(buffer, 256);
            if (charsReceived < 0) {
                perror("Error receiving packet");
            } else if (charsReceived == 0) {
                printf("Received no data.\n");
            } else {
                ++numReceived;
            }
        }
        printf("Received %d messages.  %s.\n", numReceived, (numReceived == 6 ? "PASS" : "FAIL"));
    }
    // Pause
    printf("Hit Return to exit");
    char tmpBuffer[256];
    fgets(tmpBuffer,255,stdin);
    printf("Exiting");
    exit(0);

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

